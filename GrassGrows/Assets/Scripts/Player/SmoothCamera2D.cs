
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SmoothCamera2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Smoothing")]
    public float dampTime = 0.12f;

    [Header("Camera Bounds")]
    public bool useBounds = true;

    public Vector2 boundsMin = new Vector2(-10f, -10f);
    public Vector2 boundsMax = new Vector2(10f, 10f);

    private Camera cam;

    private Vector3 exactPosition;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        exactPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 destination = new Vector3(
            target.position.x,
            target.position.y,
            exactPosition.z
        );

        // Clamp destination to camera bounds
        if (useBounds)
            destination = ClampToBounds(destination);

        // Smooth camera movement
        exactPosition = Vector3.SmoothDamp(
            exactPosition,
            destination,
            ref velocity,
            dampTime
        );

        // Clamp again to prevent smoothing from moving outside bounds
        if (useBounds)
        {
            exactPosition = ClampToBounds(exactPosition);
        }

        transform.position = exactPosition;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = boundsMin.x + halfWidth;
        float maxX = boundsMax.x - halfWidth;

        float minY = boundsMin.y + halfHeight;
        float maxY = boundsMax.y - halfHeight;

        // Handle bounds smaller than the camera view
        if (minX > maxX)
            position.x = (boundsMin.x + boundsMax.x) / 2f;
        else
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY > maxY)
            position.y = (boundsMin.y + boundsMax.y) / 2f;
        else
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.green;

        Vector3 center = new Vector3(
            (boundsMin.x + boundsMax.x) / 2f,
            (boundsMin.y + boundsMax.y) / 2f,
            transform.position.z
        );

        Vector3 size = new Vector3(
            boundsMax.x - boundsMin.x,
            boundsMax.y - boundsMin.y,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
}
