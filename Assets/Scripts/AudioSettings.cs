using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// controla os volumes do jogo
public class AudioSettings : MonoBehaviour
{
    // referência para o mixer
    public AudioMixer mixer;

    // slider do volume geral
    public Slider masterSlider;

    // slider da música
    public Slider musicSlider;

    // slider dos efeitos
    public Slider effectsSlider;

    // executa ao iniciar
    void Start()
    {
        // carrega valores salvos
        float masterVolume =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                1f
            );

        float musicVolume =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                1f
            );

        float effectsVolume =
            PlayerPrefs.GetFloat(
                "EffectsVolume",
                1f
            );

        // configura sliders
        masterSlider.value = masterVolume;
        musicSlider.value = musicVolume;
        effectsSlider.value = effectsVolume;

        // aplica volumes ao mixer
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetEffectsVolume(effectsVolume);

        // registra eventos
        masterSlider.onValueChanged.AddListener(
            SetMasterVolume
        );

        musicSlider.onValueChanged.AddListener(
            SetMusicVolume
        );

        effectsSlider.onValueChanged.AddListener(
            SetEffectsVolume
        );
    }

    // altera volume geral
    public void SetMasterVolume(float value)
    {
        // evita log10(0)
        value = Mathf.Max(value, 0.001f);

        // aplica ao mixer
        mixer.SetFloat(
            "MasterVolume",
            Mathf.Log10(value) * 20f
        );

        // salva
        PlayerPrefs.SetFloat(
            "MasterVolume",
            value
        );
    }

    // altera volume da música
    public void SetMusicVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        mixer.SetFloat(
            "MusicVolume",
            Mathf.Log10(value) * 20f
        );

        PlayerPrefs.SetFloat(
            "MusicVolume",
            value
        );
    }

    // altera volume dos efeitos
    public void SetEffectsVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        mixer.SetFloat(
            "EffectsVolume",
            Mathf.Log10(value) * 20f
        );

        PlayerPrefs.SetFloat(
            "EffectsVolume",
            value
        );
    }
}