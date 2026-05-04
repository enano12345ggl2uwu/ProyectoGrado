using UnityEngine;

/// <summary>
/// Asigna la musica de fondo de la escena. Crea MusicManager si no existe
/// y le pasa el clip. Si ya esta sonando el mismo clip, no lo reinicia.
///
/// Setup: GameObject vacio "SceneMusic" en la escena, arrastra el AudioClip
/// correspondiente (ej. bosque.flac en ColorJump, hielo.mp3 en BalloonPop).
/// </summary>
public class SceneMusicController : MonoBehaviour
{
    [Header("Clip de esta escena")]
    public AudioClip sceneClip;

    [Range(0f, 1f)] public float volume = 0.35f;

    void Start()
    {
        if (sceneClip == null) return;

        if (MusicManager.Instance == null)
        {
            var go = new GameObject("MusicManager");
            var mm = go.AddComponent<MusicManager>();
            mm.musicClip = sceneClip;
            mm.volume    = volume;
            // Awake del MusicManager hara DontDestroyOnLoad y arrancara el clip.
            return;
        }

        MusicManager.Instance.SetVolume(volume);

        var src = MusicManager.Instance.GetComponent<AudioSource>();
        if (src != null && src.clip == sceneClip && src.isPlaying) return;

        MusicManager.Instance.SetClip(sceneClip);
    }
}
