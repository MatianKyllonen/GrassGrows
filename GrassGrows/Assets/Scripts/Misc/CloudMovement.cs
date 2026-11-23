
using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float speed = 2f;
    public bool moveRight = false;
    public float despawnDistance = 20f;

    public Vector3 spawnOrigin;

    void Update()
    {
        // Move cloud
        float direction = moveRight ? 1f : -1f;

        transform.position += Vector3.right * direction
            * speed * Time.deltaTime;

        // Check if cloud has moved out of bounds
        if (moveRight)
        {
            if (transform.position.x > spawnOrigin.x + despawnDistance)
                Destroy(gameObject);
        }
        else
        {
            if (transform.position.x < spawnOrigin.x - despawnDistance)
                Destroy(gameObject);
        }
    }
}
