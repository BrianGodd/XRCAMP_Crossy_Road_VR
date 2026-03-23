using UnityEngine;
using System.Collections;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] cars;

    [Range(0f, 1f)]
    [Tooltip("Chance that this road will spawn cars at all (0 = never, 1 = always)")]
    public float spawnChance = 1f;

    [Tooltip("Force this road to spawn regardless of spawnChance (useful for testing)")]
    public bool forceSpawn = false;

    [Header("Initial delay (seconds)")]
    public float initialDelayMin = 1f;
    public float initialDelayMax = 2f;

    [Header("Cooldown between spawns (seconds)")]
    public float cooldownMin = 2f;
    public float cooldownMax = 4f;

    private bool willSpawn = false;
    private Coroutine spawnRoutine;

    void Start()
    {
        // Decide once per road whether it will spawn cars
        willSpawn = forceSpawn || (Random.value <= Mathf.Clamp01(spawnChance));

        // Ensure ranges are valid
        if (initialDelayMax < initialDelayMin) initialDelayMax = initialDelayMin;
        if (cooldownMax < cooldownMin) cooldownMax = cooldownMin;

        if (!willSpawn)
        {
            // This road will not spawn cars
            return;
        }

        // Randomize initial delay per-road so spawn times differ
        float initialDelay = Random.Range(initialDelayMin, initialDelayMax);
        spawnRoutine = StartCoroutine(SpawnLoop(initialDelay));
    }

    IEnumerator SpawnLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            SpawnCar();
            float cooldown = Random.Range(cooldownMin, cooldownMax);
            yield return new WaitForSeconds(cooldown);
        }
    }

    void SpawnCar()
    {
        if (cars == null || cars.Length == 0) return;

        GameObject prefab = cars[Random.Range(0, cars.Length)];
        if (prefab == null) return;

        Instantiate(prefab, transform.position + new Vector3(13, 0, 0), Quaternion.identity);
    }

    void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
    }
}