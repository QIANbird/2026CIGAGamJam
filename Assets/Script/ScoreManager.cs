using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private LevelManager levelManager;

    [SerializeField]
    private BlindPathTrigger blindPathTrigger;

    [SerializeField]
    private DogMovement dogMovement;

    [SerializeField]
    private OwnerFollower ownerFollower;

    [Header("Blind Path Score")]
    [SerializeField, Min(0f)]
    private float blindPathScorePerSecond = 10f;

    [SerializeField, Min(0f)]
    private float ownerMovingThreshold = 0.05f;

    [Header("Penalty Values")]
    [SerializeField, Min(0f)]
    private float attractorPenaltyValue = 20f;

    [SerializeField, Min(0f)]
    private float obstaclePenaltyValue = 10f;

    [SerializeField, Min(0f)]
    private float leashPressurePenaltyValue = 20f;

    [Header("Obstacle Detection")]
    [SerializeField]
    private string obstacleTag = "obstacle";

    [Header("Leash Pressure")]
    [SerializeField, Min(0f)]
    private float leashPressureMax = 100f;

    [SerializeField, Min(0f)]
    private float leashPressureIncreasePerSecond = 35f;

    [SerializeField, Min(0f)]
    private float leashPressureDecreasePerSecond = 25f;

    private readonly HashSet<DogAttractor> subscribedAttractors = new HashSet<DogAttractor>();

    private DogScoreCollisionReporter dogCollisionReporter;
    private Vector3 lastOwnerPosition;
    private bool ownerPositionInitialized;
    private float ownerPlanarSpeed;

    private float blindPathMoveScore;
    private float attractorPenaltyTotal;
    private float obstaclePenaltyTotal;
    private float leashPenaltyTotal;
    private float elapsedTime;
    private float leashPressure;

    public event Action ScoresChanged;

    public float BlindPathMoveScore => blindPathMoveScore;
    public float AttractorPenaltyTotal => attractorPenaltyTotal;
    public float ObstaclePenaltyTotal => obstaclePenaltyTotal;
    public float LeashPenaltyTotal => leashPenaltyTotal;
    public float PenaltyTotal => attractorPenaltyTotal + obstaclePenaltyTotal + leashPenaltyTotal;
    public float ElapsedTime => elapsedTime;
    public float TotalScore => blindPathMoveScore - PenaltyTotal;
    public float CurrentLeashPressure => leashPressure;
    public float MaxLeashPressure => leashPressureMax;
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
