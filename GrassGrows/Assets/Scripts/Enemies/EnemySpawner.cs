using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        public float weight = 1f;
        [Tooltip("Seconds after start before this enemy can appear")]
        public float unlockTime = 0f;
    }

    public SpawnEntry[] enemies;
    public LayerMask enemyMask;

    [Header("Pacing")]
    public float firstSpawnDelay = 2f;
    public float startInterval = 2.5f;
    public float minInterval = 0.7f;
    [Tooltip("Seconds it takes to ramp from startInterval to minInterval")]
    public float rampTime = 120f;
    public int maxAlive = 12;

    [Header("Position")]
    [Tooltip("Spawn one column past the right edge so enemies walk in instead of popping in")]

    private BoardGrid board;
    private PlayerController player;
    private readonly List<GameObject> alive = new List<GameObject>();
    private float elapsed;

    private void Start()
    {
        board = GameManager.instance.board;
        player = GameManager.instance.player;
        StartCoroutine(SpawnLoop());
    }

    private void Update() => elapsed += Time.deltaTime;

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (!player.IsDead)
        {
            alive.RemoveAll(g => g == null);
            if (alive.Count < maxAlive) TrySpawn();

            float interval = Mathf.Lerp(startInterval, minInterval, elapsed / rampTime);
            yield return new WaitForSeconds(interval);
        }
    }

    private void TrySpawn()
    {
        GameObject prefab = PickPrefab();
        if (!prefab) return;

        // Every cell in the ring one square outside the board, all four sides
        var ring = new List<Vector2Int>();
        for (int x = 0; x < board.size.x; x++)
        {
            ring.Add(new Vector2Int(x, -1));
            ring.Add(new Vector2Int(x, board.size.y));
        }
        for (int y = 0; y < board.size.y; y++)
        {
            ring.Add(new Vector2Int(-1, y));
            ring.Add(new Vector2Int(board.size.x, y));
        }

        while (ring.Count > 0)
        {
            int i = Random.Range(0, ring.Count);
            var c = ring[i];
            ring.RemoveAt(i);

            if (!IsFree(c)) continue;

            var go = Instantiate(prefab, board.CellToWorld(c, prefab.transform.position.z), Quaternion.identity);
            alive.Add(go);
            return;
        }
    }

    private bool IsFree(Vector2Int c) =>
        Physics2D.OverlapPoint(board.CellToWorld(c), enemyMask) == null;

    private GameObject PickPrefab()
    {
        float total = 0f;
        foreach (var e in enemies)
            if (e.prefab && elapsed >= e.unlockTime) total += e.weight;
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (var e in enemies)
        {
            if (!e.prefab || elapsed < e.unlockTime) continue;
            roll -= e.weight;
            if (roll <= 0f) return e.prefab;
        }
        return null;
    }
}