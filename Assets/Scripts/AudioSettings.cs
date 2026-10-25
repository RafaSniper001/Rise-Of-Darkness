using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public AudioMixer mixer;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider effectsSlider;

    const string MasterKey = "MasterVolume";
    const string MusicKey = "MusicVolume";
    const string EffectsKey = "EffectsVolume";

    // grupo dos efeitos do mixer, para sons criados na hora (PlayEffectAt)
    public static AudioMixerGroup EffectsGroup { get; private set; }

    // som solto no mundo (como AudioSource.PlayClipAtPoint), mas passando pelo grupo de
    // efeitos do mixer: o volume de efeitos do menu de pausa vale para ele também
    public static void PlayEffectAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        GameObject go = new GameObject("Som " + clip.name);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.outputAudioMixerGroup = EffectsGroup;
        source.Play();

        Destroy(go, clip.length + 0.1f);
    }

    void Awake()
    {
        if (mixer != null)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Effects");

            if (groups.Length > 0)
                EffectsGroup = groups[0];
        }
    }

    void Start()
    {
        float masterVolume =
            PlayerPrefs.GetFloat(MasterKey, 1f);

        float musicVolume =
            PlayerPrefs.GetFloat(MusicKey, 1f);

        float effectsVolume =
            PlayerPrefs.GetFloat(EffectsKey, 1f);

        if (masterSlider != null)
        {
            masterSlider.value = masterVolume;
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (effectsSlider != null)
        {
            effectsSlider.value = effectsVolume;
            effectsSlider.onValueChanged.AddListener(SetEffectsVolume);
        }

        ApplyMasterVolume(masterVolume);
        ApplyMusicVolume(musicVolume);
        ApplyEffectsVolume(effectsVolume);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        ApplyMasterVolume(value);

        PlayerPrefs.SetFloat(MasterKey, value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        ApplyMusicVolume(value);

        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
    }

    public void SetEffectsVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        ApplyEffectsVolume(value);

        PlayerPrefs.SetFloat(EffectsKey, value);
        PlayerPrefs.Save();
    }

    void ApplyMasterVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        if (mixer != null)
        {
            mixer.SetFloat(
                "MasterVolume",
                Mathf.Log10(value) * 20f
            );
        }
    }

    void ApplyMusicVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        if (mixer != null)
        {
            mixer.SetFloat(
                "MusicVolume",
                Mathf.Log10(value) * 20f
            );
        }
    }

    void ApplyEffectsVolume(float value)
    {
        value = Mathf.Max(value, 0.001f);

        if (mixer != null)
        {
            mixer.SetFloat(
                "EffectsVolume",
                Mathf.Log10(value) * 20f
            );
        }
    }
}