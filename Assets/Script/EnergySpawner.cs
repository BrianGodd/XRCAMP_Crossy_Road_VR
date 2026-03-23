using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EnergySpawner : MonoBehaviour
{
    [Header("Area (XZ plane)")]
    public Vector2 areaSize = new Vector2(2f, 2f);
    public Vector3 areaLocalOffset = Vector3.zero;

    [Header("Spawn settings")]
    [Tooltip("Prefab to spawn when probability check passes.")]
    public GameObject energyPrefab;
    [Range(0f,1f)]
    [Tooltip("Chance (0..1) to spawn one energy when TrySpawn() is called.")]
    public float spawnProbability = 0.2f;
    [Tooltip("Parent for spawned energy. If null, uses this GameObject.")]
    public Transform spawnParent;
    [Tooltip("If true, attempt to spawn once on Start/Enter Play Mode.")]
    public bool spawnOnStart = true;

    [Header("Gizmos")]
    public Color gizmoColor = new Color(0f, 0.5f, 1f, 0.15f);
    public Color gizmoWireColor = Color.cyan;

    Transform spawnedInstance;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (spawnOnStart)
            TrySpawn();
    }

    /// <summary>
    /// Attempt to spawn one energy based on spawnProbability.
    /// Returns true if spawned.
    /// </summary>
    public bool TrySpawn()
    {
        if (energyPrefab == null) return false;

        if (Random.value <= spawnProbability)
        {
            SpawnAtRandomPosition();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Force spawn one energy at a random position inside the rectangle.
    /// </summary>
    public void SpawnAtRandomPosition()
    {
        ClearSpawned();

        if (energyPrefab == null) return;
        if (spawnParent == null) spawnParent = this.transform;

        Vector3 localPos = new Vector3(
            Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
            0f,
            Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f)
        );

        Vector3 worldPos = transform.TransformPoint(areaLocalOffset + localPos);

        var go = PrefabUtility_Compatible.Instantiate(energyPrefab, spawnParent);
        go.transform.position = worldPos;
        spawnedInstance = go.transform;
    }

    /// <summary>
    /// Remove spawned energy if present.
    /// </summary>
    public void ClearSpawned()
    {
        if (spawnedInstance != null)
        {
            if (Application.isPlaying)
                Destroy(spawnedInstance.gameObject);
            else
                DestroyImmediate(spawnedInstance.gameObject);

            spawnedInstance = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Vector3 center = transform.TransformPoint(areaLocalOffset);
        Vector3 size = new Vector3(areaSize.x, 0.01f, areaSize.y);
        Gizmos.DrawCube(center, size);

        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(center, size);

        if (spawnedInstance != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawnedInstance.position, 0.12f);
        }
    }

    void OnValidate()
    {
        if (areaSize.x < 0f) areaSize.x = 0f;
        if (areaSize.y < 0f) areaSize.y = 0f;
        if (spawnProbability < 0f) spawnProbability = 0f;
        if (spawnProbability > 1f) spawnProbability = 1f;
    }

    // small helper similar to ObstacleSpawner to instantiate safely in edit/play mode
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
                go = (GameObject)UnityEngine.Object.Instantiate(prefab, parent);
                go.name = prefab.name;
            }
            return go;
        }
    }
}
