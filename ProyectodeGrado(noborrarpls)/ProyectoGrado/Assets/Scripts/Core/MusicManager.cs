using UnityEngine;

/// <summary>
/// Musica de fondo persistente entre escenas. Asigna un AudioClip y arranca.
/// Setup: crea GameObject "MusicManager" en MainMenu con este script. DontDestroyOnLoad.
/// Arrastra tu clip en musicClip y ajusta volume.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Clip")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.35f;
    public bool playOnStart = true;

    private AudioSource _source;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source           = gameObject.AddComponent<AudioSource>();
        _source.loop      = true;
        _source.volume    = volume;
        _source.playOnAwake = false;

        if (playOnStart && musicClip != null)
        {
            _source.clip = musicClip;
            _source.Play();
        }
    }

    public void SetClip(AudioClip clip)
    {
        if (clip == null) return;
        _source.clip = clip;
        _source.Play();
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        _source.volume = volume;
    }

    public void Mute(bool mute) { _source.mute = mute; }
    public void Stop()          { _source.Stop(); }
}
