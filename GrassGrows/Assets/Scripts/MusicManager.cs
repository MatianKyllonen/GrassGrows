
using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource gameplayMusic;
    public AudioSource gameOverMusic;

    [Header("Music Clips")]
    public AudioClip normalMusic;
    public AudioClip midnightMusic;
    public AudioClip gameOverTrack;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float gameplayVolume = 0.5f;

    [Range(0f, 1f)]
    public float gameOverVolume = 0.5f;

    [Header("Fade Settings")]
    public float fadeOutDuration = 2f;
    public float fadeInDuration = 3f;

    private Coroutine transitionRoutine;

    private bool midnightStarted;
    private bool gameOverStarted;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameplayMusic != null)
        {
            gameplayMusic.clip = normalMusic;
            gameplayMusic.volume = gameplayVolume;
            gameplayMusic.loop = true;
            gameplayMusic.Play();
        }

        if (gameOverMusic != null)
        {
            gameOverMusic.clip = gameOverTrack;
            gameOverMusic.volume = 0f;
            gameOverMusic.loop = true;
            gameOverMusic.Stop();
        }
    }

    public void PlayMidnightMusic(float fadeOutTime, float fadeInTime)
    {
        if (midnightStarted || gameOverStarted)
            return;

        midnightStarted = true;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(
            ChangeMusic(midnightMusic, gameplayVolume, fadeOutTime, fadeInTime)
        );
    }

    public void PlayGameOverMusic(float fadeOutTime, float fadeInTime)
    {
        if (gameOverStarted)
            return;

        gameOverStarted = true;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(
            GameOverMusicTransition(fadeOutTime, fadeInTime)
        );
    }

    private IEnumerator ChangeMusic(
     AudioClip newClip,
     float targetVolume,
     float fadeOutTime,
     float fadeInTime)
    {
        // Fade out current music.
        if (gameplayMusic != null && gameplayMusic.isPlaying)
        {
            yield return StartCoroutine(
                FadeMusic(
                    gameplayMusic,
                    gameplayMusic.volume,
                    0f,
                    fadeOutTime
                )
            );

            gameplayMusic.Stop();
        }

        // Swap the music clip.
        if (gameplayMusic != null)
        {
            gameplayMusic.clip = newClip;
            gameplayMusic.volume = 0f;
            gameplayMusic.Play();

            // Fade in new music.
            yield return StartCoroutine(
                FadeMusic(
                    gameplayMusic,
                    0f,
                    targetVolume,
                    fadeInTime
                )
            );
        }

        transitionRoutine = null;
    }

    private IEnumerator GameOverMusicTransition(
    float fadeOutTime,
    float fadeInTime)
    {
        // Fade out current gameplay or midnight music.
        if (gameplayMusic != null && gameplayMusic.isPlaying)
        {
            yield return StartCoroutine(
                FadeMusic(
                    gameplayMusic,
                    gameplayMusic.volume,
                    0f,
                    fadeOutTime
                )
            );

            gameplayMusic.Stop();
        }

        // Swap the game over clip.
        if (gameOverMusic != null)
        {
            gameOverMusic.clip = gameOverTrack;
            gameOverMusic.volume = 0f;
            gameOverMusic.Play();

            yield return StartCoroutine(
                FadeMusic(
                    gameOverMusic,
                    0f,
                    gameOverVolume,
                    fadeInTime
                )
            );
        }

        transitionRoutine = null;
    }

    private IEnumerator FadeMusic(
        AudioSource source,
        float startVolume,
        float targetVolume,
        float duration)
    {
        float timer = 0f;

        source.volume = startVolume;

        if (duration <= 0f)
        {
            source.volume = targetVolume;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / duration);

            source.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                progress
            );

            yield return null;
        }

        source.volume = targetVolume;
    }
}
