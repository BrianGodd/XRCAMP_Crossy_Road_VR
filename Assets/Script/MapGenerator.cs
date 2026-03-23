using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public Transform player;

    public GameObject[] tiles;

    public float OffsetZ = 10f;
    public int initNumber = 20;
    public int generateDistance = 10;

    private float lastZ = 2;

    private List<GameObject> spawnedTiles = new List<GameObject>();

    public bool isStart = true;

    // when true, the next SpawnTile call will avoid spawning a river (used to prevent back-to-back river runs)
    bool skipNextRiver = false;

    void Start()
    {
        StartSpawn();
    }

    void Update()
    {
        if(!isStart) return;
        Debug.Log("Player Z: " + player.position.z + ", Last Z: " + lastZ);
        if(player.position.z > lastZ - generateDistance)
        {
            SpawnTile();
        }
    }

    public void StartSpawn()
    {
        isStart = true;
        for(int i = 0; i < initNumber; i++)
        {
            SpawnTile();
        }
    }

    void SpawnTile()
    {
        // pick a prefab
        GameObject prefab = null;

        // If we need to skip rivers on this spawn, try to pick a non-river prefab
        if (skipNextRiver)
        {
            // build a small list of non-river candidates
            List<GameObject> nonRiver = new List<GameObject>();
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                if (t == null) continue;
                if (!t.name.ToLower().Contains("river"))
                    nonRiver.Add(t);
            }

            if (nonRiver.Count > 0)
            {
                prefab = nonRiver[Random.Range(0, nonRiver.Count)];
            }
            else
            {
                // no non-river prefabs available; fall back to any prefab
                prefab = tiles[Random.Range(0, tiles.Length)];
            }
        }
        else
        {
            prefab = tiles[Random.Range(0, tiles.Length)];
        }

        // if the chosen prefab's name contains "river" (case-insensitive), spawn 2..4 consecutive pieces
        int spawnCount = 1;
        bool prefabIsRiver = (prefab != null && prefab.name.ToLower().Contains("river"));
        if (prefabIsRiver)
        {
            spawnCount = Random.Range(2, 5); // 2,3,4
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject tile = Instantiate(
                prefab,
                new Vector3(0, 0, lastZ),
                Quaternion.identity
            );

            spawnedTiles.Add(tile);

            lastZ += OffsetZ;
        }

        // if we just spawned a river run, ensure the next spawn avoids starting another river
        if (prefabIsRiver && spawnCount > 0)
            skipNextRiver = true;
        else
            skipNextRiver = false;
    }
}