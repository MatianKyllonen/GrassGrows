using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SmoothCamera2D : MonoBehaviour
{
    public Transform target;
    public float dampTime = 0.12f;

    private Vector3 exactPosition;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        exactPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 destination = new Vector3(target.position.x, target.position.y, exactPosition.z);
        exactPosition = Vector3.SmoothDamp(exactPosition, destination, ref velocity, dampTime);

        transform.position = exactPosition;
    }
}