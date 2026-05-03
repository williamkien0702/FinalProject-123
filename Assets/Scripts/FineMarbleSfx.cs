using UnityEngine;

public class FineMarbleSfx : MonoBehaviour
{
    public static FineMarbleSfx Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.6f;
    [Range(0f, 1f)] public float uiVolume = 0.45f;
    [Range(0f, 1f)] public float gameplayVolume = 0.65f;

    private AudioSource oneShot2D;
    private AudioSource oneShot2DAlt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        oneShot2D = gameObject.AddComponent<AudioSource>();
        oneShot2D.playOnAwake = false;
        oneShot2D.loop = false;
        oneShot2D.spatialBlend = 0f;

        oneShot2DAlt = gameObject.AddComponent<AudioSource>();
        oneShot2DAlt.playOnAwake = false;
        oneShot2DAlt.loop = false;
        oneShot2DAlt.spatialBlend = 0f;
    }

    private void Play2D(AudioClip clip, float volumeScale = 1f, bool alternate = false)
    {
        if (clip == null) return;

        AudioSource src = alternate ? oneShot2DAlt : oneShot2D;
        src.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
    }

    private AudioClip BuildTone(
        float frequency,
        float duration,
        float amplitude = 0.2f,
        float slideToFrequency = -1f,
        bool squareWave = false)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        float[] data = new float[sampleCount];

        if (slideToFrequency <= 0f)
            slideToFrequency = frequency;

        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float freq = Mathf.Lerp(frequency, slideToFrequency, t);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float env;
            if (t < 0.08f) env = t / 0.08f;
            else if (t > 0.75f) env = Mathf.Clamp01((1f - t) / 0.25f);
            else env = 1f;

            float sample;
            if (squareWave)
                sample = Mathf.Sign(Mathf.Sin(phase));
            else
                sample = Mathf.Sin(phase);

            data[i] = sample * amplitude * env;
        }

        AudioClip clip = AudioClip.Create("RuntimeTone", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip BuildNoiseBurst(float duration, float amplitude = 0.15f)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float env = Mathf.Clamp01(1f - t);
            data[i] = Random.Range(-1f, 1f) * amplitude * env;
        }

        AudioClip clip = AudioClip.Create("RuntimeNoise", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayUiClick()
    {
        Play2D(BuildTone(900f, 0.07f, 0.12f, 1200f), uiVolume);
    }

    public void PlayCoinPickup()
    {
        Play2D(BuildTone(950f, 0.08f, 0.14f, 1400f), gameplayVolume);
    }

    public void PlayKingCoinPickup()
    {
        Play2D(BuildTone(700f, 0.10f, 0.14f, 1200f), gameplayVolume);
        Play2D(BuildTone(1300f, 0.12f, 0.10f, 1700f), gameplayVolume * 0.9f, true);
    }

    public void PlaySpeedBoost()
    {
        Play2D(BuildTone(500f, 0.18f, 0.13f, 1000f), gameplayVolume);
    }

    public void PlayShieldPickup()
    {
        Play2D(BuildTone(420f, 0.16f, 0.13f, 760f), gameplayVolume);
    }

    public void PlayTeleport()
    {
        Play2D(BuildTone(900f, 0.15f, 0.12f, 300f), gameplayVolume);
    }

    public void PlayLaserWarning()
    {
        Play2D(BuildTone(1200f, 0.12f, 0.12f, 1200f, true), gameplayVolume);
    }

    public void PlayLaserHit()
    {
        Play2D(BuildTone(1500f, 0.10f, 0.13f, 500f), gameplayVolume);
    }

    public void PlayBombWarning()
    {
        Play2D(BuildTone(300f, 0.10f, 0.13f, 240f, true), gameplayVolume);
    }

    public void PlayBombExplosion()
    {
        Play2D(BuildNoiseBurst(0.22f, 0.22f), gameplayVolume);
        Play2D(BuildTone(140f, 0.18f, 0.10f, 70f), gameplayVolume, true);
    }

    public void PlayShieldBlock()
    {
        Play2D(BuildTone(1000f, 0.10f, 0.11f, 1500f), gameplayVolume);
    }

    public void PlayRoundEnd()
    {
        Play2D(BuildTone(500f, 0.18f, 0.12f, 700f), gameplayVolume);
        Play2D(BuildTone(760f, 0.22f, 0.10f, 980f), gameplayVolume, true);
    }
}