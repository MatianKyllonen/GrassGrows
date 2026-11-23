using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageIndicator : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float lifetime = 0.6f;
    public float minDist = 2f;
    public float maxDist = 3f;
    private Vector2 iniPos;
    private Vector2 targetPos;
    private float timer;

    void Start()
    {
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
        }
        else
        {
            if (timer > fadeStart)
            {
                Color currentColor = text.color;
                float fadeProgress = (timer - fadeStart) / (lifetime - fadeStart);
                currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                text.color = currentColor;
            }

            // Scale uppies and then down
            float progress = timer / lifetime;
            float scaleProgress = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = Vector3.one * scaleProgress;

            transform.position = Vector2.Lerp(iniPos, targetPos, Mathf.SmoothStep(0f, 1f, progress));
        }
    }

    public void SetDamageText(float damage, string sign)
    {
        text.text = sign + damage.ToString();
    }
}
