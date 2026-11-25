using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public Animator fader;
    public Animator menuPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fader.SetTrigger("FadeEnd");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartGame()
    {
        StartCoroutine(StartGameDelay());
    }

    public IEnumerator StartGameDelay()
    {
        fader.SetTrigger("FadeStart");
        yield return new WaitForSeconds(0.6f);
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitGameDelay());
    }

    public IEnumerator QuitGameDelay()
    {
        fader.SetTrigger("FadeStart");
        yield return new WaitForSeconds(0.6f);
        Application.Quit();
    }
}
