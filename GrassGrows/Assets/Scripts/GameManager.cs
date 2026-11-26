
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerController player;
    public BoardGrid board;

    public Animator fader;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

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

            if (timerText != null)
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        StopTimer();

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // Wait before showing the game over screen.
        yield return new WaitForSeconds(initialDelay);

        // Fade in the canvas group.
        if (gameOverCanvasGroup != null)
        {
            yield return StartCoroutine(
                FadeCanvasGroup(gameOverCanvasGroup, 0f, 1f, canvasFadeDuration)
            );
        }

        // Wait before fading the text.
        yield return new WaitForSeconds(textFadeDelay);

        // Fade the text alpha to zero.
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

    public void ResetTimer()
    {
        elapsedTime = 0f;

        if (timerText != null)
            timerText.text = "00:00";
    }
}
