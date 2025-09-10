using UnityEditor;
using UnityEngine;
using PiggyRace.Gameplay.Race;

namespace PiggyRace.Editor
{
    public class TrackToolsWindow : EditorWindow
    {
        private int checkpointCount = 8;
        private float radiusX = 25f;
        private float radiusZ = 15f;
        private float startAngle = 0f;
        private Vector3 checkpointColliderSize = new Vector3(3f, 2f, 6f);
        private int spawnCount = 8;
        private float spawnRowSpacing = 2.5f;
        private float spawnColSpacing = 2.0f;
        private int spawnCols = 2;

        [MenuItem("Tools/PiggyRace/Track Tools")] 
        public static void Open() => GetWindow<TrackToolsWindow>("Track Tools");

        private void OnGUI()
        {
            GUILayout.Label("Loop Layout", EditorStyles.boldLabel);
            checkpointCount = EditorGUILayout.IntSlider("Checkpoints", checkpointCount, 3, 64);
            radiusX = EditorGUILayout.FloatField("Radius X", radiusX);
            radiusZ = EditorGUILayout.FloatField("Radius Z", radiusZ);
            startAngle = EditorGUILayout.FloatField("Start Angle", startAngle);
            checkpointColliderSize = EditorGUILayout.Vector3Field("Checkpoint Size", checkpointColliderSize);
            if (GUILayout.Button("Create TrackManager + Loop"))
            {
                CreateTrackWithLoop();
            }

            GUILayout.Space(10);
            GUILayout.Label("Spawn Grid", EditorStyles.boldLabel);
            spawnCount = EditorGUILayout.IntField("Spawn Count", spawnCount);
            spawnCols = EditorGUILayout.IntField("Columns", Mathf.Max(1, spawnCols));
            spawnRowSpacing = EditorGUILayout.FloatField("Row Spacing", spawnRowSpacing);
            spawnColSpacing = EditorGUILayout.FloatField("Column Spacing", spawnColSpacing);
            if (GUILayout.Button("Add/Replace Spawn Points At Start"))
            {
                AddSpawnPointsAtStart();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Auto-Index Checkpoints On Selected TrackManager"))
            {
                AutoIndexSelectedTrack();
            }
        }

        [MenuItem("Tools/PiggyRace/Create Track Loop (Quick)")]
        private static void CreateQuick()
        {
            var w = CreateInstance<TrackToolsWindow>();
            w.CreateTrackWithLoop();
            DestroyImmediate(w);
        }

        private void CreateTrackWithLoop()
        {
            LoopLayout.GenerateEllipse(checkpointCount, radiusX, radiusZ, startAngle, out var pos, out var rot);

            var trackGo = new GameObject("TrackManager");
            Undo.RegisterCreatedObjectUndo(trackGo, "Create TrackManager");
            var track = trackGo.AddComponent<TrackManager>();

            track.Checkpoints.Clear();
            for (int i = 0; i < checkpointCount; i++)
            {
                var cpGo = new GameObject($"Checkpoint {i}");
                Undo.RegisterCreatedObjectUndo(cpGo, "Create Checkpoint");
                cpGo.transform.SetParent(trackGo.transform, false);
                cpGo.transform.SetPositionAndRotation(pos[i], rot[i]);
                var cp = cpGo.AddComponent<Checkpoint>();
                cp.Index = i;
                cp.Track = track;
                var col = cpGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = checkpointColliderSize;
                track.Checkpoints.Add(cp);
            }
            Selection.activeObject = trackGo;
        }

        private void AddSpawnPointsAtStart()
        {
            var track = FindObjectOfType<TrackManager>();
            if (track == null || track.Checkpoints == null || track.Checkpoints.Count == 0)
            {
                EditorUtility.DisplayDialog("PiggyRace", "No TrackManager with checkpoints found in the scene.", "OK");
                return;
            }

            // Start is at checkpoint 0, place a grid behind it along -forward
            var start = track.Checkpoints[0].transform;

            // Clear existing spawn points (children named Spawn *)
            for (int i = track.SpawnPoints.Count - 1; i >= 0; i--) track.SpawnPoints.RemoveAt(i);
            foreach (Transform child in track.transform)
            {
                if (child.name.StartsWith("Spawn ")) Undo.DestroyObjectImmediate(child.gameObject);
            }

            int rows = Mathf.CeilToInt(spawnCount / (float)spawnCols);
            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < spawnCols && idx < spawnCount; c++, idx++)
                {
                    Vector3 offset = -start.forward * (1f + r * spawnRowSpacing) + start.right * ((c - (spawnCols - 1) * 0.5f) * spawnColSpacing);
                    var spGo = new GameObject($"Spawn {idx}");
                    Undo.RegisterCreatedObjectUndo(spGo, "Create Spawn");
                    spGo.transform.SetParent(track.transform, false);
                    spGo.transform.position = start.position + offset;
                    spGo.transform.rotation = start.rotation;
                    track.SpawnPoints.Add(spGo.transform);
                }
            }
            Selection.activeObject = track.gameObject;
        }

        private void AutoIndexSelectedTrack()
        {
            var track = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<TrackManager>() : null;
            if (track == null)
            {
                EditorUtility.DisplayDialog("PiggyRace", "Select a TrackManager in the Hierarchy first.", "OK");
                return;
            }
            track.Checkpoints.Clear();
            var cps = track.GetComponentsInChildren<Checkpoint>(true);
            for (int i = 0; i < cps.Length; i++)
            {
                cps[i].Index = i;
                cps[i].Track = track;
                track.Checkpoints.Add(cps[i]);
                cps[i].name = $"Checkpoint {i}";
            }
            EditorUtility.SetDirty(track);
        }
    }
}

