using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    [Serializable]
    public class TrainConfig
    {
        [SerializeField] private Transform trainRoot;
        [SerializeField] private float offScreenLeftX = -15f;
        [SerializeField] private float onScreenX = 0f;
        [SerializeField] private float offScreenRightX = 15f;
        [SerializeField] private List<TrainCompartmentPayload> payloadData = new();

        public Transform TrainRoot => trainRoot;
        public float OffScreenLeftX => offScreenLeftX;
        public float OnScreenX => onScreenX;
        public float OffScreenRightX => offScreenRightX;
        public List<TrainCompartmentPayload> PayloadData => payloadData;

        public static TrainConfig CreateDefault(Transform root)
        {
            return new TrainConfig
            {
                trainRoot = root
            };
        }
    }

    [Serializable]
    public class TrainCompartmentPayload
    {
        [SerializeField] private TrainInspectable compartment;
        [SerializeField] private string componentName;
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [SerializeField] private bool isSuspicious;
        [SerializeField] private string suspiciousReason;

        public void Apply()
        {
            if (compartment == null)
            {
                return;
            }

            compartment.SetPayloadData(componentName, description, isSuspicious, suspiciousReason);
        }
    }

    [Serializable]
    public class TriggeredTrainSet
    {
        [Tooltip("Trigger id required to run this train set. Leave empty to run at level start.")]
        [SerializeField] private string triggerId;
        [SerializeField] private List<int> trainIndices = new();
        [SerializeField] private bool startAllSelectedTrainsAtOnce = true;
        [Min(0f)]
        [SerializeField] private float delayBeforeSetStarts;
        [SerializeField] private bool snapSelectedTrainsToOffScreenLeftBeforeEntry = true;

        public string TriggerId => triggerId;
        public IReadOnlyList<int> TrainIndices => trainIndices;
        public bool StartAllSelectedTrainsAtOnce => startAllSelectedTrainsAtOnce;
        public float DelayBeforeSetStarts => delayBeforeSetStarts;
        public bool SnapSelectedTrainsToOffScreenLeftBeforeEntry => snapSelectedTrainsToOffScreenLeftBeforeEntry;
    }

    private enum TrainDestination
    {
        OnScreen,
        OffScreenLeft,
        OffScreenRight
    }

    [Serializable]
    private class TrainSpeedProfile
    {
        [Header("Distance Breakpoints (units from target)")]
        [SerializeField] private float farRange = 10f;
        [SerializeField] private float nearRange = 3f;

        [Header("Speed At Breakpoints (units / second)")]
        [SerializeField] private float farSpeed = 9f;
        [SerializeField] private float midSpeed = 5f;
        [SerializeField] private float nearSpeed = 2f;

        public float GetSpeed(float distanceToTarget)
        {
            var clampedNearRange = Mathf.Max(0.01f, nearRange);
            var clampedFarRange = Mathf.Max(clampedNearRange, farRange);
            var clampedNearSpeed = Mathf.Max(0.01f, nearSpeed);
            var clampedMidSpeed = Mathf.Max(clampedNearSpeed, midSpeed);
            var clampedFarSpeed = Mathf.Max(clampedMidSpeed, farSpeed);

            if (distanceToTarget >= clampedFarRange)
            {
                return clampedFarSpeed;
            }

            if (distanceToTarget >= clampedNearRange)
            {
                var t = Mathf.InverseLerp(clampedNearRange, clampedFarRange, distanceToTarget);
                return Mathf.Lerp(clampedMidSpeed, clampedFarSpeed, t);
            }

            var nearT = Mathf.InverseLerp(0f, clampedNearRange, distanceToTarget);
            return Mathf.Lerp(clampedNearSpeed, clampedMidSpeed, nearT);
        }
    }

    [Header("Train Configuration")]
    [SerializeField] private List<TrainConfig> trains = new();
    [SerializeField] private TrainSpeedProfile speedProfile = new();
    [SerializeField] private float delayBetweenTrains = 0.15f;
    [SerializeField] private bool autoRunOnStart;
    [Header("Triggered Entry Sets")]
    [SerializeField] private List<TriggeredTrainSet> triggeredTrainSets = new();

    public bool IsTrainMoving { get; private set; }

    public event Action<bool> OnTrainMovementChanged;

    private int activeTrainMovements;
    private bool[] activeTrainMask = Array.Empty<bool>();
    private Coroutine triggerDrivenEntryRoutine;

    private void Start()
    {
        EnsureDefaultTrainConfig();
        ResetForLevelStart();

        if (autoRunOnStart)
        {
            HandleTrigger(string.Empty);
        }
    }

    public void ResetForLevelStart()
    {
        StopAllCoroutines();
        triggerDrivenEntryRoutine = null;
        activeTrainMovements = 0;
        SetTrainMoving(false);
        EnsureDefaultTrainConfig();
        SnapAllToOffScreenLeft();
        ApplyAllPayloadData();
        ResetActiveTrainMask(false);
    }

    public void HandleTrigger(string triggerId)
    {
        EnsureDefaultTrainConfig();
        if (triggerDrivenEntryRoutine != null)
        {
            StopCoroutine(triggerDrivenEntryRoutine);
        }

        triggerDrivenEntryRoutine = StartCoroutine(RunTriggeredTrainSets(triggerId));
    }

    public void BeginTrainEntry()
    {
        BeginTrainEntry(GetAllTrainIndices(), resetActiveMask: true, moveAllAtOnce: false);
    }

    public void BeginTrainEntry(
        IReadOnlyList<int> selectedTrainIndices,
        bool resetActiveMask = false,
        bool moveAllAtOnce = false,
        bool snapToLeftBeforeMove = true)
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        triggerDrivenEntryRoutine = null;
        activeTrainMovements = 0;
        SetTrainMoving(false);
        ApplyAllPayloadData();

        if (resetActiveMask)
        {
            ResetActiveTrainMask(false);
        }

        MarkTrainsAsActive(selectedTrainIndices);
        StartCoroutine(MoveTrainsWithDelay(
            TrainDestination.OnScreen,
            snapToLeftBeforeMove: snapToLeftBeforeMove,
            selectedTrainIndices: selectedTrainIndices,
            moveAllAtOnce: moveAllAtOnce));
    }

    public void SendAcceptedTrainsOffScreen()
    {
        SendAcceptedTrainsOffScreen(GetAllActiveTrainIndices());
    }

    public void SendAcceptedTrainsOffScreen(IReadOnlyList<int> selectedTrainIndices)
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        activeTrainMovements = 0;
        SetTrainMoving(false);
        StartCoroutine(MoveTrainsWithDelay(
            TrainDestination.OffScreenRight,
            snapToLeftBeforeMove: false,
            accelerateFromStart: true,
            selectedTrainIndices: selectedTrainIndices,
            moveAllAtOnce: false));
    }

    public void SendRejectedTrainsOffScreen()
    {
        SendRejectedTrainsOffScreen(GetAllActiveTrainIndices());
    }

    public void SendRejectedTrainsOffScreen(IReadOnlyList<int> selectedTrainIndices)
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        activeTrainMovements = 0;
        SetTrainMoving(false);
        StartCoroutine(MoveTrainsWithDelay(
            TrainDestination.OffScreenLeft,
            snapToLeftBeforeMove: false,
            accelerateFromStart: true,
            selectedTrainIndices: selectedTrainIndices,
            moveAllAtOnce: false));
    }

    public int GetTrainCount()
    {
        EnsureDefaultTrainConfig();
        return trains?.Count ?? 0;
    }

    private System.Collections.IEnumerator MoveTrainsWithDelay(
        TrainDestination destination,
        bool snapToLeftBeforeMove,
        bool accelerateFromStart = false,
        IReadOnlyList<int> selectedTrainIndices = null,
        bool moveAllAtOnce = false)
    {
        if (trains == null || trains.Count == 0)
        {
            yield break;
        }

        var selectedIndices = selectedTrainIndices?.Count > 0
            ? new HashSet<int>(selectedTrainIndices)
            : null;

        for (var i = 0; i < trains.Count; i++)
        {
            if (selectedIndices != null && !selectedIndices.Contains(i))
            {
                continue;
            }

            if (activeTrainMask.Length > i && !activeTrainMask[i])
            {
                continue;
            }

            var train = trains[i];
            if (train == null || train.TrainRoot == null)
            {
                continue;
            }

            if (snapToLeftBeforeMove)
            {
                SnapTrainToX(train, train.OffScreenLeftX);
            }

            StartCoroutine(MoveSingleTrain(i, train, destination, accelerateFromStart));

            if (!moveAllAtOnce && delayBetweenTrains > 0f)
            {
                yield return new WaitForSeconds(delayBetweenTrains);
            }
        }
    }

    private System.Collections.IEnumerator RunTriggeredTrainSets(string triggerId)
    {
        var normalizedTrigger = triggerId?.Trim() ?? string.Empty;
        var hasAnySetRun = false;
        for (var i = 0; i < triggeredTrainSets.Count; i++)
        {
            var set = triggeredTrainSets[i];
            if (set == null)
            {
                continue;
            }

            var setTriggerId = set.TriggerId?.Trim() ?? string.Empty;
            if (!string.Equals(setTriggerId, normalizedTrigger, StringComparison.Ordinal))
            {
                continue;
            }

            hasAnySetRun = true;
            if (set.DelayBeforeSetStarts > 0f)
            {
                yield return new WaitForSeconds(set.DelayBeforeSetStarts);
            }

            var selectedIndices = GetValidTrainIndices(set.TrainIndices);
            if (selectedIndices.Count == 0)
            {
                selectedIndices = GetAllTrainIndices();
            }

            MarkTrainsAsActive(selectedIndices);
            yield return MoveTrainsWithDelay(
                TrainDestination.OnScreen,
                set.SnapSelectedTrainsToOffScreenLeftBeforeEntry,
                selectedTrainIndices: selectedIndices,
                moveAllAtOnce: set.StartAllSelectedTrainsAtOnce);
            yield return WaitForTrainMovementToComplete();
        }

        if (!hasAnySetRun && string.IsNullOrEmpty(normalizedTrigger) && triggeredTrainSets.Count == 0)
        {
            BeginTrainEntry();
        }

        triggerDrivenEntryRoutine = null;
    }

    private System.Collections.IEnumerator WaitForTrainMovementToComplete()
    {
        while (IsTrainMoving)
        {
            yield return null;
        }
    }

    private System.Collections.IEnumerator MoveSingleTrain(
        int trainIndex,
        TrainConfig train,
        TrainDestination destination,
        bool accelerateFromStart)
    {
        if (train == null || train.TrainRoot == null)
        {
            yield break;
        }

        activeTrainMovements++;
        SetTrainMoving(true);

        var targetX = GetTargetX(train, destination);
        var startX = train.TrainRoot.localPosition.x;
        var totalDistance = Mathf.Abs(targetX - startX);
        while (!Mathf.Approximately(train.TrainRoot.localPosition.x, targetX))
        {
            var position = train.TrainRoot.localPosition;
            var distanceToTarget = Mathf.Abs(targetX - position.x);
            var distanceForSpeedProfile = accelerateFromStart
                ? Mathf.Max(0f, totalDistance - distanceToTarget)
                : distanceToTarget;
            var movementSpeed = (speedProfile ??= new TrainSpeedProfile()).GetSpeed(distanceForSpeedProfile);
            var nextX = Mathf.MoveTowards(position.x, targetX, movementSpeed * Time.deltaTime);
            train.TrainRoot.localPosition = new Vector3(nextX, position.y, position.z);
            yield return null;
        }

        if ((destination == TrainDestination.OffScreenLeft || destination == TrainDestination.OffScreenRight) &&
            activeTrainMask.Length > trainIndex)
        {
            activeTrainMask[trainIndex] = false;
        }

        activeTrainMovements = Mathf.Max(0, activeTrainMovements - 1);
        if (activeTrainMovements == 0)
        {
            SetTrainMoving(false);
        }
    }

    private void EnsureDefaultTrainConfig()
    {
        if (trains != null && trains.Count > 0)
        {
            return;
        }

        trains = new List<TrainConfig>
        {
            TrainConfig.CreateDefault(transform)
        };
    }

    private void SnapAllToOffScreenLeft()
    {
        if (trains == null)
        {
            return;
        }

        for (var i = 0; i < trains.Count; i++)
        {
            var train = trains[i];
            if (train == null || train.TrainRoot == null)
            {
                continue;
            }

            SnapTrainToX(train, train.OffScreenLeftX);
        }
    }

    private void SnapTrainToX(TrainConfig train, float x)
    {
        var position = train.TrainRoot.localPosition;
        train.TrainRoot.localPosition = new Vector3(x, position.y, position.z);
    }

    private float GetTargetX(TrainConfig train, TrainDestination destination)
    {
        return destination switch
        {
            TrainDestination.OnScreen => train.OnScreenX,
            TrainDestination.OffScreenLeft => train.OffScreenLeftX,
            TrainDestination.OffScreenRight => train.OffScreenRightX,
            _ => train.OnScreenX
        };
    }

    private void ApplyAllPayloadData()
    {
        if (trains == null)
        {
            return;
        }

        for (var trainIndex = 0; trainIndex < trains.Count; trainIndex++)
        {
            var train = trains[trainIndex];
            var payloadData = train?.PayloadData;
            if (payloadData == null)
            {
                continue;
            }

            for (var payloadIndex = 0; payloadIndex < payloadData.Count; payloadIndex++)
            {
                payloadData[payloadIndex]?.Apply();
            }
        }
    }

    private void ResetActiveTrainMask(bool defaultValue)
    {
        var count = trains?.Count ?? 0;
        activeTrainMask = new bool[count];
        for (var i = 0; i < count; i++)
        {
            activeTrainMask[i] = defaultValue;
        }
    }

    private void MarkTrainsAsActive(IReadOnlyList<int> indices)
    {
        if (activeTrainMask == null || activeTrainMask.Length != (trains?.Count ?? 0))
        {
            ResetActiveTrainMask(false);
        }

        var validIndices = GetValidTrainIndices(indices);
        if (validIndices.Count == 0)
        {
            for (var i = 0; i < activeTrainMask.Length; i++)
            {
                activeTrainMask[i] = true;
            }

            return;
        }

        for (var i = 0; i < validIndices.Count; i++)
        {
            activeTrainMask[validIndices[i]] = true;
        }
    }

    private List<int> GetValidTrainIndices(IReadOnlyList<int> indices)
    {
        var validIndices = new List<int>();
        var maxCount = trains?.Count ?? 0;
        if (indices == null)
        {
            return validIndices;
        }

        for (var i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            if (index < 0 || index >= maxCount || validIndices.Contains(index))
            {
                continue;
            }

            validIndices.Add(index);
        }

        return validIndices;
    }

    private List<int> GetAllTrainIndices()
    {
        var count = trains?.Count ?? 0;
        var indices = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    private List<int> GetAllActiveTrainIndices()
    {
        var activeIndices = new List<int>();
        for (var i = 0; i < activeTrainMask.Length; i++)
        {
            if (activeTrainMask[i])
            {
                activeIndices.Add(i);
            }
        }

        return activeIndices;
    }

    private void SetTrainMoving(bool isMoving)
    {
        if (IsTrainMoving == isMoving)
        {
            return;
        }

        IsTrainMoving = isMoving;
        OnTrainMovementChanged?.Invoke(IsTrainMoving);
    }
}
