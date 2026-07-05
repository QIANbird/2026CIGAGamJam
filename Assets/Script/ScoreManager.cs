using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    [Header("\u5f15\u7528")]
    [SerializeField, InspectorName("\u5173\u5361\u7ba1\u7406\u5668")]
    private LevelManager levelManager;

    [SerializeField, InspectorName("\u76f2\u9053\u89e6\u53d1\u5668")]
    private BlindPathTrigger blindPathTrigger;

    [SerializeField, InspectorName("\u72d7\u72d7\u79fb\u52a8")]
    private DogMovement dogMovement;

    [SerializeField, InspectorName("\u4e3b\u4eba\u8ddf\u968f")]
    private OwnerFollower ownerFollower;

    [Header("\u76f2\u9053\u8ba1\u5206\u914d\u7f6e")]
    [SerializeField, Min(0f), InspectorName("\u76f2\u9053\u52a0\u5206\u901f\u5ea6\uff08\u6bcf\u79d2\uff09")]
    private float blindPathScorePerSecond = 10f;

    [SerializeField, Min(0f), InspectorName("\u4e3b\u4eba\u79fb\u52a8\u901f\u5ea6\u9608\u503c")]
    private float ownerMovingThreshold = 0.05f;

    [Header("\u6263\u5206\u914d\u7f6e")]
    [SerializeField, Min(0f), InspectorName("\u5438\u5f15\u7269\u5355\u6b21\u6263\u5206")]
    private float attractorPenaltyValue = 20f;

    [SerializeField, Min(0f), InspectorName("\u969c\u788d\u7269\u5355\u6b21\u6263\u5206")]
    private float obstaclePenaltyValue = 10f;

    [SerializeField, Min(0f), InspectorName("\u538b\u529b\u6ee1\u503c\u6263\u5206")]
    private float leashPressurePenaltyValue = 20f;

    [Header("\u969c\u788d\u7269\u68c0\u6d4b")]
    [SerializeField, InspectorName("\u969c\u788d\u7269 Tag")]
    private string obstacleTag = "obstacle";

    [Header("\u7255\u5f15\u538b\u529b\u914d\u7f6e")]
    [SerializeField, Min(0f), InspectorName("\u538b\u529b\u503c\u4e0a\u9650")]
    private float leashPressureMax = 100f;

    [SerializeField, Min(0f), InspectorName("\u7d27\u7ef7\u65f6\u538b\u529b\u589e\u957f\u901f\u5ea6\uff08\u6bcf\u79d2\uff09")]
    private float leashPressureIncreasePerSecond = 35f;

    [SerializeField, Min(0f), InspectorName("\u975e\u7d27\u7ef7\u65f6\u538b\u529b\u4e0b\u964d\u901f\u5ea6\uff08\u6bcf\u79d2\uff09")]
    private float leashPressureDecreasePerSecond = 25f;

    [Header("\u8fd0\u884c\u65f6\u7d2f\u8ba1\uff08\u53ef\u624b\u52a8\u8c03\u8bd5\uff09")]
    [SerializeField, InspectorName("\u76f2\u9053\u7d2f\u8ba1\u5f97\u5206")]
    private float blindPathMoveScore;

    [SerializeField, InspectorName("\u5438\u5f15\u7269\u7d2f\u8ba1\u6263\u5206")]
    private float attractorPenaltyTotal;

    [SerializeField, InspectorName("\u969c\u788d\u7269\u7d2f\u8ba1\u6263\u5206")]
    private float obstaclePenaltyTotal;

    [SerializeField, InspectorName("\u538b\u529b\u7d2f\u8ba1\u6263\u5206")]
    private float leashPenaltyTotal;

    [SerializeField, InspectorName("\u7d2f\u8ba1\u65f6\u95f4")]
    private float elapsedTime;

    [SerializeField, InspectorName("\u5f53\u524d\u538b\u529b\u503c")]
    private float leashPressure;

    [SerializeField, InspectorName("\u538b\u529b\u6ee1\u503c\u6b21\u6570")]
    private int leashPressureFullCount;

    [SerializeField, InspectorName("\u4e3b\u4eba\u5f53\u524d\u901f\u5ea6\uff08\u8c03\u8bd5\uff09")]
    private float ownerPlanarSpeed;

    private readonly HashSet<DogAttractor> subscribedAttractors = new HashSet<DogAttractor>();

    private DogScoreCollisionReporter dogCollisionReporter;
    private Vector3 lastOwnerPosition;
    private bool ownerPositionInitialized;

    public event Action ScoresChanged;
    public event Action<Transform> ObstacleHit;

    public float BlindPathMoveScore => blindPathMoveScore;
    public float AttractorPenaltyTotal => attractorPenaltyTotal;
    public float ObstaclePenaltyTotal => obstaclePenaltyTotal;
    public float LeashPenaltyTotal => leashPenaltyTotal;
    public float PenaltyTotal => attractorPenaltyTotal + obstaclePenaltyTotal + leashPenaltyTotal;
    public float ElapsedTime => elapsedTime;
    public float TotalScore => blindPathMoveScore - PenaltyTotal;
    public float CurrentLeashPressure => leashPressure;
    public float MaxLeashPressure => leashPressureMax;
    public int LeashPressureFullCount => leashPressureFullCount;
    public float LeashPressureNormalized =>
        leashPressureMax <= Mathf.Epsilon ? 0f : leashPressure / leashPressureMax;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        RegisterAttractors();
        RegisterCollisionReporter();
        InitializeOwnerPositionTracking();
        NotifyScoresChanged();
    }

    private void OnValidate()
    {
        blindPathScorePerSecond = Mathf.Max(0f, blindPathScorePerSecond);
        ownerMovingThreshold = Mathf.Max(0f, ownerMovingThreshold);
        attractorPenaltyValue = Mathf.Max(0f, attractorPenaltyValue);
        obstaclePenaltyValue = Mathf.Max(0f, obstaclePenaltyValue);
        leashPressurePenaltyValue = Mathf.Max(0f, leashPressurePenaltyValue);
        leashPressureMax = Mathf.Max(0f, leashPressureMax);
        leashPressureIncreasePerSecond = Mathf.Max(0f, leashPressureIncreasePerSecond);
        leashPressureDecreasePerSecond = Mathf.Max(0f, leashPressureDecreasePerSecond);
        leashPressure = Mathf.Clamp(leashPressure, 0f, leashPressureMax);
        leashPressureFullCount = Mathf.Max(0, leashPressureFullCount);

        if (Application.isPlaying)
        {
            NotifyScoresChanged();
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateOwnerPlanarSpeed(deltaTime);

        if (!CanScore() || deltaTime <= 0f)
        {
            return;
        }

        bool hasChanged = false;

        elapsedTime += deltaTime;
        hasChanged = true;

        if (ShouldAwardBlindPathScore())
        {
            blindPathMoveScore += blindPathScorePerSecond * deltaTime;
        }

        if (UpdateLeashPressure(deltaTime))
        {
            hasChanged = true;
        }

        if (hasChanged)
        {
            NotifyScoresChanged();
        }
    }

    private void OnDestroy()
    {
        foreach (DogAttractor attractor in subscribedAttractors)
        {
            if (attractor != null)
            {
                attractor.DogContacted -= HandleDogContactedAttractor;
            }
        }

        subscribedAttractors.Clear();

        if (dogCollisionReporter != null)
        {
            dogCollisionReporter.ObstacleEntered -= HandleObstacleEntered;
        }
    }

    private void ResolveReferences()
    {
        if (levelManager == null)
        {
            levelManager = GetComponent<LevelManager>();
        }

        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }

        if (dogMovement == null)
        {
            dogMovement = FindObjectOfType<DogMovement>();
        }

        if (ownerFollower == null)
        {
            ownerFollower = FindObjectOfType<OwnerFollower>();
        }

        if (blindPathTrigger == null)
        {
            blindPathTrigger = FindObjectOfType<BlindPathTrigger>();
        }
    }

    private void RegisterAttractors()
    {
        DogAttractor[] attractors = FindObjectsOfType<DogAttractor>();

        foreach (DogAttractor attractor in attractors)
        {
            if (attractor == null || subscribedAttractors.Contains(attractor))
            {
                continue;
            }

            attractor.DogContacted += HandleDogContactedAttractor;
            subscribedAttractors.Add(attractor);
        }
    }

    private void RegisterCollisionReporter()
    {
        if (dogMovement == null)
        {
            return;
        }

        dogCollisionReporter = dogMovement.GetComponent<DogScoreCollisionReporter>();

        if (dogCollisionReporter == null)
        {
            dogCollisionReporter = dogMovement.gameObject.AddComponent<DogScoreCollisionReporter>();
        }

        dogCollisionReporter.ObstacleTag = obstacleTag;
        dogCollisionReporter.ObstacleEntered -= HandleObstacleEntered;
        dogCollisionReporter.ObstacleEntered += HandleObstacleEntered;
    }

    private void InitializeOwnerPositionTracking()
    {
        if (ownerFollower == null)
        {
            ownerPositionInitialized = false;
            return;
        }

        lastOwnerPosition = ownerFollower.transform.position;
        ownerPositionInitialized = true;
        ownerPlanarSpeed = 0f;
    }

    private void UpdateOwnerPlanarSpeed(float deltaTime)
    {
        if (ownerFollower == null || deltaTime <= 0f)
        {
            ownerPlanarSpeed = 0f;
            return;
        }

        Vector3 currentOwnerPosition = ownerFollower.transform.position;

        if (!ownerPositionInitialized)
        {
            lastOwnerPosition = currentOwnerPosition;
            ownerPositionInitialized = true;
            ownerPlanarSpeed = 0f;
            return;
        }

        Vector3 delta = currentOwnerPosition - lastOwnerPosition;
        delta.y = 0f;
        ownerPlanarSpeed = delta.magnitude / deltaTime;
        lastOwnerPosition = currentOwnerPosition;
    }

    private bool ShouldAwardBlindPathScore()
    {
        if (blindPathTrigger == null || dogMovement == null)
        {
            return false;
        }

        return blindPathTrigger.IsOwnerInside &&
               ownerPlanarSpeed > ownerMovingThreshold &&
               dogMovement.CurrentLeashState != DogMovement.LeashState.Slack;
    }

    private bool UpdateLeashPressure(float deltaTime)
    {
        if (dogMovement == null)
        {
            return false;
        }

        float nextPressure = leashPressure;

        if (dogMovement.CurrentLeashState == DogMovement.LeashState.Taut)
        {
            nextPressure += leashPressureIncreasePerSecond * deltaTime;
        }
        else
        {
            nextPressure -= leashPressureDecreasePerSecond * deltaTime;
        }

        nextPressure = Mathf.Clamp(nextPressure, 0f, leashPressureMax);

        if (nextPressure >= leashPressureMax)
        {
            leashPenaltyTotal += leashPressurePenaltyValue;
            leashPressureFullCount++;
            leashPressure = 0f;
            return true;
        }

        if (Mathf.Approximately(leashPressure, nextPressure))
        {
            return false;
        }

        leashPressure = nextPressure;
        return true;
    }

    private void HandleDogContactedAttractor(DogAttractor attractor, DogMovement dog)
    {
        if (!CanScore() || dogMovement == null || dog != dogMovement)
        {
            return;
        }

        attractorPenaltyTotal += attractorPenaltyValue;
        NotifyScoresChanged();
    }

    private void HandleObstacleEntered(Transform obstacleTransform)
    {
        if (!CanScore())
        {
            return;
        }

        obstaclePenaltyTotal += obstaclePenaltyValue;
        NotifyScoresChanged();
        ObstacleHit?.Invoke(obstacleTransform);
    }

    private bool CanScore()
    {
        return levelManager != null &&
               !levelManager.IsPaused &&
               !levelManager.IsLevelComplete;
    }

    private void NotifyScoresChanged()
    {
        ScoresChanged?.Invoke();
    }
}
