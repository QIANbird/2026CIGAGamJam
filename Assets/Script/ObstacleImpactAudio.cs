using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class ObstacleImpactAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ScoreManager scoreManager;

    [Tooltip("Must match the obstacle root detected by ScoreManager. Leave empty when this component is on that root.")]
    [SerializeField]
    private Transform obstacleRoot;

    [SerializeField]
    private AudioSource audioSource;

    [Header("Impact Sound")]
    [SerializeField]
    private AudioClip impactClip;

    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [SerializeField, Min(0f)]
    private float minimumInterval = 0.08f;

    [SerializeField]
    private bool randomizePitch = true;

    [SerializeField, Range(0.1f, 3f)]
    private float minimumPitch = 0.95f;

    [SerializeField, Range(0.1f, 3f)]
    private float maximumPitch = 1.05f;

    private bool isSubscribed;
    private float lastPlayTime = float.NegativeInfinity;

    private void Reset()
    {
        obstacleRoot = transform;
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    private void Awake()
    {
        if (obstacleRoot == null)
        {
            obstacleRoot = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // ScoreSystemBootstrap can create ScoreManager after this component's
        // OnEnable, but it is available before Start.
        TrySubscribe();

        if (impactClip == null)
        {
            Debug.LogWarning("ObstacleImpactAudio has no Impact Clip assigned.", this);
        }
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        minimumInterval = Mathf.Max(0f, minimumInterval);
        minimumPitch = Mathf.Clamp(minimumPitch, 0.1f, 3f);
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 3f);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        if (scoreManager == null)
        {
            return;
        }

        scoreManager.ObstacleHit += HandleObstacleHit;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || scoreManager == null)
        {
            isSubscribed = false;
            return;
        }

        scoreManager.ObstacleHit -= HandleObstacleHit;
        isSubscribed = false;
    }

    private void HandleObstacleHit(Transform hitObstacle)
    {
        if (hitObstacle == null || hitObstacle != obstacleRoot || impactClip == null)
        {
            return;
        }

        if (Time.unscaledTime - lastPlayTime < minimumInterval)
        {
            return;
        }

        lastPlayTime = Time.unscaledTime;
        audioSource.pitch = randomizePitch
            ? Random.Range(minimumPitch, maximumPitch)
            : 1f;
        audioSource.PlayOneShot(impactClip, volume);
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }
}
