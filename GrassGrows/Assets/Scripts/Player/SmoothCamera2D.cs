
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class SmoothCamera2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public PlayerController player;

    [Header("Smoothing")]
    public float dampTime = 0.12f;

    [Header("Camera Bounds")]
    public bool useBounds = true;

    public Vector2 boundsMin = new Vector2(-10f, -10f);
    public Vector2 boundsMax = new Vector2(10f, 10f);

    [Header("Healing Vignette")]
    public Volume volume;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.2f;
    public float hitChromaticIntensity = 0.9f;
    public float hitVignetteBoost = 0.35f;

    private ChromaticAberration chromaticAberration;
    private float normalChromaticIntensity;
    private float normalVignetteIntensity;
    private float hitFlashTimer;

    [Range(0f, 1f)]
    public float healVignetteIntensity = 0.45f;

    [Range(0f, 1f)]
    public float healVignetteSmoothness = 0.8f;

    public float vignetteReturnSpeed = 2.5f;

    private Camera cam;
    private Vignette vignette;

    private float normalVignetteSmoothness;

    private Vector3 exactPosition;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        exactPosition = transform.position;

        if (!player && target)
            player = target.GetComponent<PlayerController>();

        if (volume && volume.profile.TryGet(out vignette))
        {
            normalVignetteIntensity = vignette.intensity.value;
            normalVignetteSmoothness = vignette.smoothness.value;

            volume.profile.TryGet(out chromaticAberration);

            if (chromaticAberration != null)
                normalChromaticIntensity = chromaticAberration.intensity.value;

            if (vignette != null)
                normalVignetteIntensity = vignette.intensity.value;
        }



    }

    private void LateUpdate()
    {
        if (target)
        {
            Vector3 destination = new Vector3(
                target.position.x,
                target.position.y,
                exactPosition.z
            );

            if (useBounds)
                destination = ClampToBounds(destination);

            exactPosition = Vector3.SmoothDamp(
                exactPosition,
                destination,
                ref velocity,
                dampTime
            );

            if (useBounds)
                exactPosition = ClampToBounds(exactPosition);

            transform.position = exactPosition;
        }

        UpdateHealingEffects();
        UpdateHitFlash();
    }

    // ---------- Healing Vignette ----------

    private void UpdateHealingEffects()
    {
        bool healing = player && player.IsHealing;

        if (vignette == null)
            return;

        if (healing)
        {
            float progress = player.HealProgress;

            vignette.intensity.value = Mathf.Lerp(
                normalVignetteIntensity,
                healVignetteIntensity,
                progress
            );

            vignette.smoothness.value = Mathf.Lerp(
                normalVignetteSmoothness,
                healVignetteSmoothness,
                progress
            );
        }
        else
        {
            vignette.intensity.value = Mathf.MoveTowards(
                vignette.intensity.value,
                normalVignetteIntensity,
                vignetteReturnSpeed * Time.deltaTime
            );

            vignette.smoothness.value = Mathf.MoveTowards(
                vignette.smoothness.value,
                normalVignetteSmoothness,
                vignetteReturnSpeed * Time.deltaTime
            );
        }
    }

    // ---------- Camera Bounds ----------

    private Vector3 ClampToBounds(Vector3 position)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = boundsMin.x + halfWidth;
        float maxX = boundsMax.x - halfWidth;

        float minY = boundsMin.y + halfHeight;
        float maxY = boundsMax.y - halfHeight;

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

    // ---------- Gizmos ----------

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

    private void OnEnable()
    {
        if (player != null)
            player.onHit.AddListener(TriggerHitFlash);
    }

    private void OnDisable()
    {
        if (player != null)
            player.onHit.RemoveListener(TriggerHitFlash);
    }

    private void TriggerHitFlash()
    {
        hitFlashTimer = hitFlashDuration;
    }

    private void UpdateHitFlash()
    {
        if (hitFlashTimer > 0f)
            hitFlashTimer -= Time.deltaTime;

        float strength = hitFlashDuration > 0f
            ? Mathf.Clamp01(hitFlashTimer / hitFlashDuration)
            : 0f;

        // Chromatic aberration
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(
                normalChromaticIntensity,
                hitChromaticIntensity,
                strength
            );
        }

        // Vignette: calculate from the current healing intensity.
        if (vignette != null)
        {
            float healingIntensity = normalVignetteIntensity;

            if (player && player.IsHealing)
            {
                healingIntensity = Mathf.Lerp(
                    normalVignetteIntensity,
                    healVignetteIntensity,
                    player.HealProgress
                );
            }

            vignette.intensity.value = Mathf.Clamp01(
                healingIntensity + hitVignetteBoost * strength
            );
        }
    }
}
