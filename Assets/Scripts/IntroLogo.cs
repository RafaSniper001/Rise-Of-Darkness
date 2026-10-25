using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// cena Intro (a primeira da build): depois da tela de abertura do Unity, mostra a logo da FANS
// sozinha, com fade, e abre o menu principal. Qualquer tecla ou clique pula
public class IntroLogo : MonoBehaviour
{
    public CanvasGroup logo;
    public string nextScene = "MainMenu";

    public float fadeInTime = 0.8f;
    public float holdTime = 2f;
    public float fadeOutTime = 0.8f;

    bool leaving;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        StartCoroutine(Play());
    }

    void Update()
    {
        if (!leaving && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            StopAllCoroutines();
            StartCoroutine(Leave());
        }
    }

    IEnumerator Play()
    {
        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Leave();
    }

    IEnumerator Leave()
    {
        leaving = true;
        yield return Fade(logo != null ? logo.alpha : 1f, 0f, fadeOutTime);
        Cursor.visible = true;
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        if (logo == null)
            yield break;

        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            logo.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }

        logo.alpha = to;
    }
}
