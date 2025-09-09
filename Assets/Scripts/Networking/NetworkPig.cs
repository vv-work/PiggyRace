using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using PiggyRace.Gameplay.Pig;

namespace PiggyRace.Networking
{
    // Networked pig controller using server-authoritative simulation and simple client prediction/smoothing.
    [DisallowMultipleComponent]
    public class NetworkPig : NetworkBehaviour
    {
        [Header("Debug")]
        public bool VerboseLogs = false;
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

        private PigMotor _motor;
        private Rigidbody _rb;

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

        public override void OnNetworkSpawn()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                // We drive motion explicitly; keep RB kinematic and interpolated for visuals.
                _rb.isKinematic = true;
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
            }

            if (IsOwner)
            {
                TryBindFollowCamera();
                if (VerboseLogs)
                {
                    Debug.Log($"[NetworkPig] OnSpawn. Owner={OwnerClientId} Local={NetworkManager.LocalClientId} IsServer={IsServer} IsOwner={IsOwner}");
                }
            }

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

            if (IsClient && IsOwner && !IsServer)
            {
                // Always blend owner towards server state to ensure visible movement even if prediction fails
                float t = Mathf.Clamp01(OwnerLerpRate * dt);
                Vector3 newPos = Vector3.Lerp(transform.position, NetPos.Value, t);
                float newYaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, NetYaw.Value, RotationSpeedDeg * dt);
                SetTransform(newPos, newYaw);
                if (VerboseLogs && Time.time - _lastInputLogTime > 0.5f)
                {
                    Debug.Log($"[NetworkPig][Client {NetworkManager.LocalClientId}] blend to net pos={NetPos.Value} local={transform.position} d={(NetPos.Value-transform.position).magnitude:F2}");
                }
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (IsOwner && IsClient)
            {
                // Prediction step on client when not server
                if (!IsServer)
                {
                    StepAndApply(dt, _locThrottle, _locSteer, _locBrake, _locDrift, _locBoost, RotationSpeedDeg);
                }
                // Send input to server each fixed step
                SubmitInputServerRpc(_locThrottle, _locSteer, _locBrake, _locDrift, _locBoost);
            }

            if (IsServer)
            {
                var (delta, targetYaw) = _motor.Step(dt, _inThrottle, _inSteer, _inBrake, _inDrift, _inBoost);
                ApplyTransform(delta, targetYaw, RotationSpeedDeg);
                NetPos.Value = transform.position;
                NetYaw.Value = transform.eulerAngles.y;
                if (VerboseLogs && Time.time - _lastInputLogTime > 0.5f)
                {
                    _lastInputLogTime = Time.time;
                    Debug.Log($"[NetworkPig][Server] step owner={OwnerClientId} pos={transform.position}");
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
        private void SubmitInputServerRpc(float throttle, float steer, bool brake, bool drift, bool boost, ServerRpcParams rpcParams = default)
        {
            if (VerboseLogs)
            {
                Debug.Log($"[NetworkPig] Server received input from {rpcParams.Receive.SenderClientId} for pig owned by {OwnerClientId} thr={throttle:F2} st={steer:F2}");
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
                _rb.MovePosition(_rb.position + new Vector3(delta.x, 0f, delta.y));
                _rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
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
                _rb.MovePosition(pos);
                _rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
            }
            else
            {
                transform.position = pos;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
