using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoSpawner : MonoBehaviour
{
    public DemoBoard board;
    public GameObject[] pawnPrefabs;

    public int initialCount = 3;
    public int maxAlive = 6;
    public float firstDelay = 0.5f;
    public float interval = 1.8f;
    public float jitter = 0.7f;

    private void Start()
    {
        if (!board) board = DemoBoard.Instance;
        StartCoroutine(Loop());
    }

    private IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstDelay);

        for (int i = 0; i < initialCount; i++)
        {
            Spawn();
            yield return new WaitForSeconds(0.25f);
        }

        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, interval + Random.Range(-jitter, jitter)));
            if (board.pawns.Count < maxAlive) Spawn();
        }
    }

    private void Spawn()
    {
        if (pawnPrefabs == null || pawnPrefabs.Length == 0) return;

        // Ring one square outside the board, all four sides
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
            Vector2Int c = ring[i];
            ring.RemoveAt(i);
            if (board.PawnAt(c)) continue;

            var prefab = pawnPrefabs[Random.Range(0, pawnPrefabs.Length)];
            var go = Instantiate(prefab, board.CellToWorld(c, prefab.transform.position.z), Quaternion.identity);
            go.GetComponent<DemoPawn>().Init(board);
            return;
        }
    }
}