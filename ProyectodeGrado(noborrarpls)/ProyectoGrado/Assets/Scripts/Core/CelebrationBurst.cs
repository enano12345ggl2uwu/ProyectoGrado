using UnityEngine;

/// <summary>
/// Celebracion visual al acertar: particulas + screen shake. Singleton.
/// Setup: crea GameObject "CelebrationBurst" en cada escena de juego.
///        Crea un ParticleSystem prefab (Unity Component > Particle System) y arrastralo a burstPrefab.
///        Si no hay prefab, solo hace screen shake.
/// </summary>
public class CelebrationBurst : MonoBehaviour
{
    public static CelebrationBurst Instance { get; private set; }

    [Header("Particles (opcional — ParticleSystem prefab)")]
    public ParticleSystem burstPrefab;

    [Header("Screen shake")]
    public float shakeDuration  = 0.25f;
    public float shakeMagnitude = 0.15f;

    [Header("Flash (opcional — Canvas Image full-screen con alpha 0)")]
    public UnityEngine.UI.Image flashImage;
    public Color flashColor = new Color(1f, 1f, 1f, 0.35f);
    public float flashDuration = 0.15f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (flashImage) flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    public void Trigger(Vector3 worldPosition)
    {
        if (burstPrefab != null)
        {
            var ps = Instantiate(burstPrefab, worldPosition, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2.5f);
        }
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(shakeDuration, shakeMagnitude);

        if (flashImage != null)
            StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        flashImage.color = flashColor;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(flashColor.a, 0f, t / flashDuration);
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
            yield return null;
        }
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }
}
