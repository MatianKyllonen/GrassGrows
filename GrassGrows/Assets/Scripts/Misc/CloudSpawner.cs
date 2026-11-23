
using UnityEngine;
using System.Collections;

public class CloudSpawner : MonoBehaviour
{
    [Header("Cloud Sprites")]
    public Sprite[] cloudSprites;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public Vector2 spawnArea = new Vector2(15f, 5f);

    [Header("Cloud Size")]
    public Vector2 sizeRange = new Vector2(0.5f, 2f);

    [Header("Movement")]
    public Vector2 speedRange = new Vector2(1f, 3f);
    public bool moveRight = false;

    [Header("Despawn Area")]
    public float despawnDistance = 20f;

    void Start()
    {
        StartCoroutine(SpawnClouds());
    }

    IEnumerator SpawnClouds()
    {
        while (true)
        {
            SpawnCloud();
            yield return new WaitForSeconds(spawnInterval);
        }
    }


    void SpawnCloud()
    {
        if (cloudSprites.Length == 0)
            return;

        // Pick a random cloud sprite
        Sprite selectedSprite = cloudSprites[
            Random.Range(0, cloudSprites.Length)
        ];

        // Randomize spawn position along either edge
        float randomY = Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f);

        float spawnX = moveRight
            ? -spawnArea.x / 2f
            : spawnArea.x / 2f;

        Vector3 spawnPosition = transform.position + new Vector3(
            spawnX,
            randomY,
            0f
        );

        // Create cloud
        GameObject cloud = new GameObject("Cloud");
        cloud.transform.position = spawnPosition;

        // Add sprite renderer
        SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
        renderer.sprite = selectedSprite;
        renderer.color = new Color(0, 0, 0, 0.1f);
        renderer.sortingOrder = 20;

        // Randomize size
        float size = Random.Range(sizeRange.x, sizeRange.y);
        cloud.transform.localScale = Vector3.one * size;

        // Add movement
        CloudMovement movement = cloud.AddComponent<CloudMovement>();
        movement.speed = Random.Range(speedRange.x, speedRange.y);
        movement.moveRight = moveRight;
        movement.despawnDistance = spawnArea.x;
        movement.spawnOrigin = transform.position;
    }

}
