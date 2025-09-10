using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using PiggyRace.Gameplay.Pig;
using System.Collections.Generic;

namespace PiggyRace.Networking
{
    // Networked pig controller using server-authoritative simulation by default,
    // with optional client-authoritative mode for ultra-smooth local feel.
    [DisallowMultipleComponent]
    public class NetworkPig : NetworkBehaviour
    {
        public enum AuthorityMode : byte { ServerAuthoritative = 0, ClientAuthoritative = 1 }
        [Header("Debug")]
        public bool VerboseLogs = false;
        [Header("Authority")]
        public AuthorityMode Authority = AuthorityMode.ServerAuthoritative;
        [Header("Tuning (mirrors PigMotor)")]
        public float MaxSpeed = 16f;
        public float ReverseSpeed = 8f;
        public float Accel = 24f;
        public float BrakeDecel = 35f;
        public float LinearDrag = 1.2f;
        public float TurnRateDeg = 140f;
        public float DriftTurnMultiplier = 1.35f;
        public float BoostSpeedAdd = 6f;
        public float BoostDuration = 0.6f;
        public float BoostCooldown = 1.4f;
        [Header("Visual")]
        public float RotationSpeedDeg = 720f;
        [Header("Smoothing")]
        public float RemoteLerpRate = 12f; // higher = faster catch-up
        public float OwnerReconcileThreshold = 1.0f; // meters
        public float OwnerReconcileRate = 10f;
        public float OwnerLerpRate = 8f; // always blend owner to server state (client-only)
        [Header("Net Tuning")]
        public float SnapshotRateHz = 30f;
        public float MaxClientSpeed = 35f; // m/s validation for client-authoritative state
        public float MaxClientYawRateDeg = 540f; // deg/s validation for client-authoritative state

        private PigMotor _motor;
        private Rigidbody _rb;
        [SerializeField] private PigVisualController _visual; // assign in Inspector; auto-fallback if null
        private Vector3 _lastPos;

        // Last input seen by server
        private float _inThrottle;
        private float _inSteer;
        private bool _inBrake;
        private bool _inDrift;
        private bool _inBoost;

        // Locally sampled inputs (owner)
        private float _locThrottle, _locSteer; private bool _locBrake, _locDrift, _locBoost;
        private float _lastInputLogTime;

        private NetworkVariable<Vector3> NetPos = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> NetYaw = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> IsSpectator = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Prediction & reconciliation (owner-client)
        private struct InputCmd { public int tick; public float th, st; public bool br, dr, bo; }
        private struct PredState { public int tick; public Vector3 pos; public float yaw; public PigMotor.Snapshot motor; }
        private readonly List<InputCmd> _inputBuf = new List<InputCmd>(256);
        private readonly List<PredState> _stateBuf = new List<PredState>(256);
        private int _localTick;
        private int _serverTick;
        private int _lastClientTickReceivedByServer;
        private float _svSnapshotAccum;
        private Vector3 _svLastPos;
        private float _svLastYaw;
        private float _svLastTime;
        private bool _svHasLast;

        public override void OnNetworkSpawn()
        {
            _rb = GetComponent<Rigidbody>();
            // Disable NetworkTransform if present; we handle sync manually for prediction/smoothing
            var nt = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt != null) nt.enabled = false;
            if (_rb != null)
            {
                if (Authority == AuthorityMode.ServerAuthoritative)
                {
                    // Server simulates physics; clients are kinematic
                    _rb.isKinematic = !IsServer;
                }
                else
                {
                    // Client-authoritative: only the owner simulates; all others are kinematic (including server replica)
                    _rb.isKinematic = !(IsOwner);
                }
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Initialize motor with current yaw
            float yaw = transform.eulerAngles.y;
            _motor = new PigMotor(yaw)
            {
                MaxSpeed = MaxSpeed,
                ReverseSpeed = ReverseSpeed,
                Accel = Accel,
                BrakeDecel = BrakeDecel,
                LinearDrag = LinearDrag,
                TurnRateDeg = TurnRateDeg,
                DriftTurnMultiplier = DriftTurnMultiplier,
                BoostSpeedAdd = BoostSpeedAdd,
                BoostDuration = BoostDuration,
                BoostCooldown = BoostCooldown
            };

            if (IsServer)
            {
                NetPos.Value = transform.position;
                NetYaw.Value = yaw;
                var gm = Object.FindObjectOfType<NetworkGameManager>();
                if (gm != null)
                {
                    // Only block input during Countdown. If not in countdown, allow immediate driving.
                    if (gm.Phase.Value == RacePhase.Countdown)
                    {
                        IsSpectator.Value = true;
                        gm.Phase.OnValueChanged += OnPhaseChangedServer;
                    }
                    else
                    {
                        IsSpectator.Value = false;
                    }
                }
            }

            if (IsOwner)
            {
                TryBindFollowCamera();
                if (VerboseLogs)
                {
                    Debug.Log($"[NetworkPig] OnSpawn. Owner={OwnerClientId} Local={NetworkManager.LocalClientId} IsServer={IsServer} IsOwner={IsOwner}");
                }
            }

            // visuals
            if (_visual == null) _visual = GetComponentInChildren<PigVisualController>();
            _lastPos = transform.position;

            if (IsClient)
            {
                NetPos.OnValueChanged += OnNetPosChanged;
                NetYaw.OnValueChanged += OnNetYawChanged;
            }
        }

        private void OnDestroy()
        {
            if (IsClient)
            {
                NetPos.OnValueChanged -= OnNetPosChanged;
                NetYaw.OnValueChanged -= OnNetYawChanged;
            }
            if (IsServer)
            {
                var gm = Object.FindObjectOfType<NetworkGameManager>();
                if (gm != null)
                {
                    gm.Phase.OnValueChanged -= OnPhaseChangedServer;
                }
            }
        }

        // Server-side: when countdown finishes, drop spectators into active state so they can drive
        private void OnPhaseChangedServer(RacePhase previous, RacePhase next)
        {
            if (!IsServer) return;
            if (next == RacePhase.Race)
            {
                IsSpectator.Value = false;
                var gm = Object.FindObjectOfType<NetworkGameManager>();
                if (gm != null)
                {
                    gm.Phase.OnValueChanged -= OnPhaseChangedServer;
                }
            }
        }

        private void OnNetPosChanged(Vector3 oldValue, Vector3 newValue)
        {
            if (VerboseLogs)
            {
                Debug.Log($"[NetworkPig][Client {NetworkManager.LocalClientId}] NetPos changed {oldValue} -> {newValue} (owner={OwnerClientId})");
            }
        }

        private void OnNetYawChanged(float oldValue, float newValue)
        {
            if (VerboseLogs)
            {
                Debug.Log($"[NetworkPig][Client {NetworkManager.LocalClientId}] NetYaw changed {oldValue:F1} -> {newValue:F1} (owner={OwnerClientId})");
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (IsOwner && IsClient)
            {
                ReadOwnerInput(out _locThrottle, out _locSteer, out _locBrake, out _locDrift, out _locBoost);
                if (VerboseLogs && Time.time - _lastInputLogTime > 0.5f)
                {
                    _lastInputLogTime = Time.time;
                    Debug.Log($"[NetworkPig][Client {NetworkManager.LocalClientId}] input th={_locThrottle:F2} st={_locSteer:F2} br={_locBrake} dr={_locDrift} bo={_locBoost}");
                }
            }

            // server sim moved to FixedUpdate for consistency

            if (IsClient && !IsOwner)
            {
                // Smooth remote to latest net state
                var targetPos = NetPos.Value;
                var targetYaw = NetYaw.Value;
                float t = Mathf.Clamp01(RemoteLerpRate * dt);
                Vector3 newPos = Vector3.Lerp(transform.position, targetPos, t);
                float newYaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, RotationSpeedDeg * dt);
                SetTransform(newPos, newYaw);
            }

            // Owner reconciliation handled via targeted snapshots; avoid constant blending to NetVars

            // Visuals: compute normalized speed and update animator/particles on all peers
            float speed = 0f;
            if (_rb != null && !_rb.isKinematic)
            {
                var v = _rb.linearVelocity; // Unity 6 Rigidbody linearVelocity
                speed = new Vector3(v.x, 0f, v.z).magnitude;
            }
            else
            {
                Vector3 delta = transform.position - _lastPos;
                if (dt > 0f) speed = new Vector3(delta.x, 0f, delta.z).magnitude / dt;
            }
            float norm = MaxSpeed > 0f ? Mathf.Clamp01(speed / MaxSpeed) : 0f;
            _visual?.SpeedUpdate(norm);
            _lastPos = transform.position;
        }

        private void OnValidate()
        {
            if (_visual == null) _visual = GetComponentInChildren<PigVisualController>();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var gm = Object.FindObjectOfType<NetworkGameManager>();
            bool raceActive = gm == null || gm.Phase.Value == RacePhase.Race;
            bool canDrive = raceActive && !IsSpectator.Value;
            if (IsOwner && IsClient)
            {
                // Prediction step on client when not server
                if (!IsServer && Authority == AuthorityMode.ServerAuthoritative)
                {
                    float th = canDrive ? _locThrottle : 0f;
                    float st = canDrive ? _locSteer : 0f;
                    bool br = canDrive && _locBrake;
                    bool dr = canDrive && _locDrift;
                    bool bo = canDrive && _locBoost;
                    var cmd = new InputCmd { tick = _localTick, th = th, st = st, br = br, dr = dr, bo = bo };
                    _inputBuf.Add(cmd);
                    StepAndApply(dt, cmd.th, cmd.st, cmd.br, cmd.dr, cmd.bo, RotationSpeedDeg);
                    _stateBuf.Add(new PredState { tick = _localTick, pos = transform.position, yaw = transform.eulerAngles.y, motor = _motor.Capture() });
                    if (_inputBuf.Count > 512) _inputBuf.RemoveRange(0, _inputBuf.Count - 512);
                    if (_stateBuf.Count > 512) _stateBuf.RemoveRange(0, _stateBuf.Count - 512);
                }
                // Client-authoritative: owner always simulates locally
                if (Authority == AuthorityMode.ClientAuthoritative)
                {
                    float th = canDrive ? _locThrottle : 0f;
                    float st = canDrive ? _locSteer : 0f;
                    bool br = canDrive && _locBrake;
                    bool dr = canDrive && _locDrift;
                    bool bo = canDrive && _locBoost;
                    StepAndApply(dt, th, st, br, dr, bo, RotationSpeedDeg);
                    if (!IsServer)
                    {
                        SubmitOwnerStateServerRpc(transform.position, transform.eulerAngles.y);
                    }
                }
                // Send input to server each fixed step
                if (Authority == AuthorityMode.ServerAuthoritative)
                {
                    SubmitInputServerRpc(
                    canDrive ? _locThrottle : 0f,
                    canDrive ? _locSteer : 0f,
                    canDrive && _locBrake,
                    canDrive && _locDrift,
                    canDrive && _locBoost,
                    _localTick);
                    _localTick++;
                }
            }

            if (IsServer)
            {
                if (Authority == AuthorityMode.ServerAuthoritative)
                {
                    // Authoritative physics on server
                    float th = canDrive ? _inThrottle : 0f;
                    float st = canDrive ? _inSteer : 0f;
                    bool br = canDrive && _inBrake;
                    bool dr = canDrive && _inDrift;
                    bool bo = canDrive && _inBoost;
                    var (delta, targetYaw) = _motor.Step(dt, th, st, br, dr, bo);
                    ApplyTransform(delta, targetYaw, RotationSpeedDeg);
                    NetPos.Value = transform.position;
                    NetYaw.Value = transform.eulerAngles.y;
                    // Send owner authoritative snapshot for reconciliation (only if owner is a remote client), throttled
                    _svSnapshotAccum += dt;
                    float interval = SnapshotRateHz > 0f ? (1f / SnapshotRateHz) : 0f;
                    if (OwnerClientId != NetworkManager.ServerClientId && _svSnapshotAccum >= interval)
                    {
                        _svSnapshotAccum = 0f;
                        var sendTo = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };
                        OwnerStateClientRpc(transform.position, transform.eulerAngles.y, _serverTick, _lastClientTickReceivedByServer, sendTo);
                    }
                    _serverTick++;
                    if (VerboseLogs && Time.time - _lastInputLogTime > 0.5f)
                    {
                        _lastInputLogTime = Time.time;
                        Debug.Log($"[NetworkPig][Server] step owner={OwnerClientId} pos={transform.position}");
                    }
                }
                else
                {
                    // Client-authoritative: server just echoes state for remotes; host-owner uses its local sim
                    NetPos.Value = transform.position;
                    NetYaw.Value = transform.eulerAngles.y;
                }
            }
        }

        private void TryBindFollowCamera()
        {
            // Prefer Cinemachine if present
            var vcam = Object.FindObjectOfType<Unity.Cinemachine.CinemachineVirtualCameraBase>();
            if (vcam != null)
            {
                vcam.Follow = transform;
                if (vcam.LookAt == null) vcam.LookAt = transform;
                return;
            }

            // Fallback to simple follow camera
            var simple = Object.FindObjectOfType<PiggyRace.Camera.SimpleFollowCamera>();
            if (simple != null) simple.SetTarget(transform);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitInputServerRpc(float throttle, float steer, bool brake, bool drift, bool boost, int clientTick, ServerRpcParams rpcParams = default)
        {
            if (VerboseLogs)
            {
                Debug.Log($"[NetworkPig] Server received input from {rpcParams.Receive.SenderClientId} for pig owned by {OwnerClientId} thr={throttle:F2} st={steer:F2} tick={clientTick}");
            }
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
            {
                // Ignore inputs not from the owner of this pig
                return;
            }
            _inThrottle = Mathf.Clamp(throttle, -1f, 1f);
            _inSteer = Mathf.Clamp(steer, -1f, 1f);
            _inBrake = brake;
            _inDrift = drift;
            _inBoost = boost;
            _lastClientTickReceivedByServer = clientTick;

            // Acknowledge back to the sender (owner) for debugging the pipeline
            var sendTo = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { rpcParams.Receive.SenderClientId } }
            };
            AckInputClientRpc(_inThrottle, _inSteer, _inBrake, _inDrift, _inBoost, sendTo);
        }

        [ClientRpc]
        private void AckInputClientRpc(float throttle, float steer, bool brake, bool drift, bool boost, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            if (VerboseLogs)
            {
                Debug.Log($"[NetworkPig][Client {NetworkManager.LocalClientId}] server ack thr={throttle:F2} st={steer:F2} br={brake} dr={drift} bo={boost}");
            }
        }

        private void ReadOwnerInput(out float throttle, out float steer, out bool brake, out bool drift, out bool boost)
        {
            var k = Keyboard.current;
            if (k == null) { throttle = steer = 0f; brake = drift = boost = false; return; }
            float fwd = k.wKey.isPressed ? 1f : 0f;
            float back = k.sKey.isPressed ? 1f : 0f;
            throttle = Mathf.Clamp(fwd - back, -1f, 1f);
            float right = k.dKey.isPressed ? 1f : 0f;
            float left = k.aKey.isPressed ? 1f : 0f;
            steer = Mathf.Clamp(right - left, -1f, 1f);
            brake = k.spaceKey.isPressed;
            drift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            boost = k.leftCtrlKey.wasPressedThisFrame || k.rightCtrlKey.wasPressedThisFrame;
        }

        private void StepAndApply(float dt, float throttle, float steer, bool brake, bool drift, bool boost, float rotSpeed)
        {
            var (delta, targetYaw) = _motor.Step(dt, throttle, steer, brake, drift, boost);
            float currentYaw = transform.eulerAngles.y;
            float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotSpeed * dt);
            ApplyTransform(delta, newYaw, rotSpeed, absoluteYaw: true);
        }

        private void ApplyTransform(Vector2 delta, float targetYaw, float rotSpeed, bool absoluteYaw = false)
        {
            float newYaw = absoluteYaw ? targetYaw : Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, rotSpeed * Time.deltaTime);
            if (_rb != null)
            {
                if (_rb.isKinematic)
                {
                    // Clients: visual smoothing/prediction via kinematic moves
                    _rb.MovePosition(_rb.position + new Vector3(delta.x, 0f, delta.y));
                    _rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
                }
                else
                {
                    // Server: dynamic rigidbody — convert delta to velocity this fixed step
                    float invDt = (Time.fixedDeltaTime > 0f) ? 1f / Time.fixedDeltaTime : 0f;
                    Vector3 velXZ = new Vector3(delta.x, 0f, delta.y) * invDt;
                    _rb.linearVelocity = new Vector3(velXZ.x, _rb.linearVelocity.y, velXZ.z); // keep Y (gravity)
                    _rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
                }
            }
            else
            {
                transform.position += new Vector3(delta.x, 0f, delta.y);
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
        }

        private void SetTransform(Vector3 pos, float yaw)
        {
            if (_rb != null)
            {
                if (_rb.isKinematic)
                {
                    _rb.MovePosition(pos);
                    _rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
                }
                else
                {
                    // Only expected on server during hard snaps (rare); set directly to avoid fights with velocity
                    _rb.position = pos;
                    _rb.rotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
            else
            {
                transform.position = pos;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        // Client-authoritative: client pushes its current state to server for distribution
        [ServerRpc(RequireOwnership = true)]
        private void SubmitOwnerStateServerRpc(Vector3 position, float yaw, ServerRpcParams rpcParams = default)
        {
            if (Authority != AuthorityMode.ClientAuthoritative) return;
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            // Server-side validation/clamping
            float now = NetworkManager.ServerTime.TimeAsFloat;
            if (_svHasLast)
            {
                MovementValidator.ClampState(_svLastPos, _svLastYaw, _svLastTime,
                    position, yaw, now,
                    MaxClientSpeed, MaxClientYawRateDeg,
                    out var clampedPos, out var clampedYaw);
                position = clampedPos;
                yaw = clampedYaw;
            }
            _svLastPos = position;
            _svLastYaw = yaw;
            _svLastTime = now;
            _svHasLast = true;

            SetTransform(position, yaw);
            NetPos.Value = position;
            NetYaw.Value = yaw;
        }

        [ClientRpc]
        private void OwnerStateClientRpc(Vector3 pos, float yaw, int serverTick, int echoedClientTick, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner || IsServer) return;
            int idx = _stateBuf.FindIndex(s => s.tick == echoedClientTick);
            if (idx < 0) return;
            var predictedAtEcho = _stateBuf[idx];
            float posErr = (predictedAtEcho.pos - pos).magnitude;
            float yawErr = Mathf.Abs(Mathf.DeltaAngle(predictedAtEcho.yaw, yaw));
            if (posErr < OwnerReconcileThreshold && yawErr < 5f) return;

            // Rewind to echoed tick and replay
            _motor.Restore(predictedAtEcho.motor);
            Vector3 rewindPos = pos;
            float rewindYaw = yaw;
            SetTransform(rewindPos, rewindYaw);

            for (int i = idx + 1; i < _stateBuf.Count; i++)
            {
                int t = _stateBuf[i].tick;
                // find matching input
                for (int j = 0; j < _inputBuf.Count; j++)
                {
                    if (_inputBuf[j].tick == t)
                    {
                        var c = _inputBuf[j];
                        var step = _motor.Step(Time.fixedDeltaTime, c.th, c.st, c.br, c.dr, c.bo);
                        rewindPos += new Vector3(step.deltaXZ.x, 0f, step.deltaXZ.y);
                        rewindYaw = step.yawDeg;
                        break;
                    }
                }
            }
            SetTransform(rewindPos, rewindYaw);
        }
    }
}
