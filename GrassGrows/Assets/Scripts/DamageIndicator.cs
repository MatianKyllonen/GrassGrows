using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DamageIndicator : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float lifetime = 0.6f;
    public float minDist = 2f;
    public float maxDist = 3f;

    private Vector2 iniPos;
    private Vector2 targetPos;
    private float timer;

    private Camera mainCamera;
    private PixelPerfectCamera pixelPerfectCamera;

    void Start()
    {
        mainCamera = Camera.main;
        pixelPerfectCamera = mainCamera.GetComponent<PixelPerfectCamera>();

        iniPos = transform.position;

        float angle = Random.Range(0f, 360f);
        float dist = Random.Range(minDist, maxDist);

        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        targetPos = iniPos + offset;

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float fadeStart = lifetime / 2f;

        if (timer > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (timer > fadeStart)
        {
            Color currentColor = text.color;
            float fadeProgress = (timer - fadeStart) / (lifetime - fadeStart);
            currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
            text.color = currentColor;
        }

        float progress = timer / lifetime;

        // Scale up and down
        float scaleProgress = Mathf.Sin(progress * Mathf.PI);
        transform.localScale = Vector3.one * scaleProgress;

        // Move
        Vector2 newPosition = Vector2.Lerp(
            iniPos,
            targetPos,
            Mathf.SmoothStep(0f, 1f, progress)
        );

        // Snap position to the pixel grid
        transform.position = SnapToPixelGrid(newPosition);
    }

    Vector2 SnapToPixelGrid(Vector2 position)
    {
        if (pixelPerfectCamera == null)
            return position;

        float pixelSize = 1f / pixelPerfectCamera.assetsPPU;

        position.x = Mathf.Round(position.x / pixelSize) * pixelSize;
        position.y = Mathf.Round(position.y / pixelSize) * pixelSize;

        return position;
    }

    public void SetDamageText(float damage, string sign)
    {
        text.text = sign + damage.ToString();
    }
}