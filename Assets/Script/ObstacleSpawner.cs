using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Area (XZ plane)")]
    [Tooltip("Size of the spawn rectangle on the X (width) and Z (depth) axes.")]
    public Vector2 areaSize = new Vector2(5f, 5f);
    [Tooltip("Local offset of rectangle center relative to this transform.")]
    public Vector3 areaLocalOffset = Vector3.zero;

    [Header("Spawn settings")]
    [Tooltip("Array of obstacle prefabs to choose from randomly.")]
    public GameObject[] obstaclePrefabs;
    [Tooltip("Maximum number of obstacles that may spawn. Actual spawn count is random between 0 and this value.")]
    public int maxCount = 5;
    [Tooltip("Minimum allowed distance between spawned obstacles.")]
    public float minDistance = 1.0f;
    [Tooltip("Maximum attempts to find a non-overlapping position per obstacle.")]
    public int maxAttemptsPerItem = 20;
    [Tooltip("Parent transform for spawned obstacles (optional). If null, spawned objects will be children of this GameObject.")]
    public Transform spawnParent;
    [Tooltip("If true, spawn once on Start/Enter Play Mode.")]
    public bool spawnOnStart = true;

    [Header("Debug / Gizmos")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.25f);
    public Color gizmoWireColor = Color.green;

    // internal
    List<Transform> spawned = new List<Transform>();

    void Start()
    {
        if (!Application.isPlaying) return;
        if (spawnOnStart)
            SpawnRandomObstacles();
    }

    /// <summary>
    /// Clear previously spawned obstacles
    /// </summary>
    public void ClearSpawned()
    {
        for (int i = spawned.Count - 1; i >= 0; --i)
        {
            if (spawned[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawned[i].gameObject);
                else
                    DestroyImmediate(spawned[i].gameObject);
            }
        }
        spawned.Clear();
    }

    /// <summary>
    /// Spawn a random number (0..maxCount) of obstacles at random positions inside the rectangle area, ensuring minDistance between them when possible.
    /// </summary>
    public void SpawnRandomObstacles()
    {
        ClearSpawned();

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0 || maxCount <= 0)
            return;

        if (spawnParent == null)
            spawnParent = this.transform;

        int count = Random.Range(0, maxCount + 1);

        List<Vector3> chosenPositions = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
            {
                Vector3 localPos = new Vector3(
                    Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                    0f,
                    0f
                );

                Vector3 worldPos = transform.TransformPoint(areaLocalOffset + localPos);

                // check spacing
                bool ok = true;
                foreach (var p in chosenPositions)
                {
                    if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(worldPos.x, 0f, worldPos.z)) < minDistance)
                    {
                        ok = false; break;
                    }
                }

                if (ok)
                {
                    // pick random prefab
                    var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                    if (prefab == null) { placed = true; break; }

                    var go = (GameObject)PrefabUtility_Compatible.Instantiate(prefab, spawnParent);
                    go.transform.position = worldPos;
                    // optional: random Y rotation
                    go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    // optional: align Y to prefab's own position (leave as is)
                    spawned.Add(go.transform);
                    chosenPositions.Add(worldPos);
                    placed = true;
                    break;
                }
            }

            // if cannot place after attempts, skip this item
            if (!placed)
            {
                Debug.LogWarning("ObstacleSpawner: could not find non-overlapping position for item " + i);
            }
        }
    }

    /// <summary>
    /// Helper instantiate that works both in editor and play mode without requiring UnityEditor references.
    /// Uses GameObject.Instantiate in playmode, and Object.Instantiate in editor. PrefabUtility is avoided to keep runtime-safe.
    /// </summary>
    static class PrefabUtility_Compatible
    {
        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            GameObject go = null;
            if (Application.isPlaying)
            {
                go = GameObject.Instantiate(prefab, parent);
            }
            else
            {
                // in edit mode, use Object.Instantiate to create a copy in the scene
                go = (GameObject)UnityEngine.Object.Instantiate(prefab, parent);
                go.name = prefab.name; // remove (Clone)
                // register undo? skipping to avoid Editor only API
            }
            return go;
        }
    }

    void OnDrawGizmosSelected()
    {
        // draw rectangle on XZ plane
        Gizmos.color = gizmoColor;
        Vector3 center = transform.TransformPoint(areaLocalOffset);
        Vector3 size = new Vector3(areaSize.x, 0.01f, areaSize.y);
        Gizmos.DrawCube(center, size);

        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(center, size);

        // draw existing spawned positions
        Gizmos.color = Color.red;
        foreach (var t in spawned)
        {
            if (t != null)
                Gizmos.DrawSphere(t.position, 0.1f);
        }
    }
}
