
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerController player;
    public BoardGrid board;

    public Animator fader;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Midnight")]
    public float midnightTime = 720f;         
    public float midnightFadeDuration = 6f;

    public ParticleSystem leaves;
    public ParticleSystem rain;

    public Light2D lightA;
    public Color lightAMidnightColor = new Color(0.25f, 0.3f, 0.7f);

    public Light2D lightB;
    public Color lightBMidnightColor = new Color(0.4f, 0.2f, 0.6f);

    public EnemySpawner spawner;

    public bool IsMidnight { get; private set; }

    [Header("Game Over")]
    public CanvasGroup gameOverCanvasGroup;
    public TextMeshProUGUI gameOverText;

    [Header("Game Over Timing")]
    public float initialDelay = 1f;
    public float canvasFadeDuration = 1f;
    public float textFadeDelay = 1f;
    public float textFadeDuration = 1f;

    private float elapsedTime;
    private bool timerRunning = false;
    private bool isGameOver = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        StartTimer();

        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (fader != null)
            fader.SetTrigger("FadeEnd");
    }

    private void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;

            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);

            if (!IsMidnight && elapsedTime >= midnightTime)
                StartMidnight();

            if (timerText != null)
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartMidnight();
        }
    }

    private void StartMidnight()
    {
        if (IsMidnight)
            return;

        IsMidnight = true;


        elapsedTime = midnightTime;
        spawner.elapsed = midnightTime;

        PauseTimer();

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMidnightMusic(6.5f, 0.0f);

        StartCoroutine(FadeLights());

        if (spawner)
            spawner.OnMidnight();
    }

    private IEnumerator FadeLights()
    {
        Color startA = lightA ? lightA.color : Color.white;
        Color startB = lightB ? lightB.color : Color.white;

        for (float t = 0f; t < midnightFadeDuration; t += Time.deltaTime)
        {
            float p = t / midnightFadeDuration;
            if (lightA) lightA.color = Color.Lerp(startA, lightAMidnightColor, p);
            if (lightB) lightB.color = Color.Lerp(startB, lightBMidnightColor, p);
            yield return null;
        }

        rain.Play();

        var main = leaves.main;

        main.startSpeed = 7f;

        var emission = leaves.emission;
        emission.rateOverTime = 8f; 


        if (lightA) lightA.color = lightAMidnightColor;
        if (lightB) lightB.color = lightBMidnightColor;

        timerRunning = true;
    }


    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        StopTimer();

        // Transition to game over music.
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayGameOverMusic(1, 0);

        StartCoroutine(GameOverSequence());
    }




    private IEnumerator GameOverSequence()
    {
        // Start with everything invisible.
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 0f;

        if (gameOverText != null)
        {
            Color color = gameOverText.color;
            color.a = 0f;
            gameOverText.color = color;
        }

        // Slowly fade in the canvas during the initial delay.
        Coroutine canvasFader = null;

        yield return new WaitForSeconds(1f);

        if (gameOverCanvasGroup != null)
        {
            canvasFader = StartCoroutine(FadeCanvasGroup(gameOverCanvasGroup, 0f,1f,initialDelay * 1.4f));
        }

        yield return new WaitForSeconds(initialDelay);

        // Stop the canvas fade.
        if (canvasFader != null)
            StopCoroutine(canvasFader);

        // Force full opacity.
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 1f;
        // Instantly force everything to full opacity.
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 1f;

        

        if (gameOverText != null)
        {
            Color color = gameOverText.color;
            color.a = 1f;
            gameOverText.color = color;
        }

        // Wait before fading out the text.
        yield return new WaitForSeconds(textFadeDelay);

        // Fade the text away.
        if (gameOverText != null)
        {
            yield return StartCoroutine(
                FadeText(gameOverText, 1f, 0f, textFadeDuration)
            );
        }

        SceneManager.LoadScene(0);
    }



    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        float timer = 0f;
        group.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = duration > 0f
                ? Mathf.Clamp01(timer / duration)
                : 1f;

            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private IEnumerator FadeText(
        TextMeshProUGUI text,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        float timer = 0f;
        Color color = text.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = duration > 0f
                ? Mathf.Clamp01(timer / duration)
                : 1f;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            text.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        text.color = color;
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void PauseTimer()
    {
        timerRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;

        if (timerText != null)
            timerText.text = "00:00";
    }
}
