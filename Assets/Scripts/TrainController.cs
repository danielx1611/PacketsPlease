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

    private enum TrainDestination
    {
        OnScreen,
        OffScreenLeft,
        OffScreenRight
    }

    [Header("Train Configuration")]
    [SerializeField] private List<TrainConfig> trains = new();
    [SerializeField] private float entrySpeed = 4f;
    [SerializeField] private float delayBetweenTrains = 0.15f;
    [SerializeField] private bool autoRunOnStart;

    public bool IsTrainMoving { get; private set; }

    public event Action<bool> OnTrainMovementChanged;

    private int activeTrainMovements;

    private void Start()
    {
        EnsureDefaultTrainConfig();
        SnapAllToOffScreenLeft();
        ApplyAllPayloadData();

        if (autoRunOnStart)
        {
            BeginTrainEntry();
        }
    }

    public void BeginTrainEntry()
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        activeTrainMovements = 0;
        SetTrainMoving(false);
        ApplyAllPayloadData();
        StartCoroutine(MoveTrainsWithDelay(TrainDestination.OnScreen, snapToLeftBeforeMove: true));
    }

    public void SendAcceptedTrainsOffScreen()
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        activeTrainMovements = 0;
        SetTrainMoving(false);
        StartCoroutine(MoveTrainsWithDelay(TrainDestination.OffScreenRight, snapToLeftBeforeMove: false));
    }

    public void SendRejectedTrainsOffScreen()
    {
        EnsureDefaultTrainConfig();
        StopAllCoroutines();
        activeTrainMovements = 0;
        SetTrainMoving(false);
        StartCoroutine(MoveTrainsWithDelay(TrainDestination.OffScreenLeft, snapToLeftBeforeMove: false));
    }

    private System.Collections.IEnumerator MoveTrainsWithDelay(TrainDestination destination, bool snapToLeftBeforeMove)
    {
        if (trains == null || trains.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < trains.Count; i++)
        {
            var train = trains[i];
            if (train == null || train.TrainRoot == null)
            {
                continue;
            }

            if (snapToLeftBeforeMove)
            {
                SnapTrainToX(train, train.OffScreenLeftX);
            }

            StartCoroutine(MoveSingleTrain(train, destination));

            if (delayBetweenTrains > 0f)
            {
                yield return new WaitForSeconds(delayBetweenTrains);
            }
        }
    }

    private System.Collections.IEnumerator MoveSingleTrain(TrainConfig train, TrainDestination destination)
    {
        if (train == null || train.TrainRoot == null)
        {
            yield break;
        }

        activeTrainMovements++;
        SetTrainMoving(true);

        var targetX = GetTargetX(train, destination);
        while (!Mathf.Approximately(train.TrainRoot.position.x, targetX))
        {
            var position = train.TrainRoot.position;
            var nextX = Mathf.MoveTowards(position.x, targetX, entrySpeed * Time.deltaTime);
            train.TrainRoot.position = new Vector3(nextX, position.y, position.z);
            yield return null;
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
        var position = train.TrainRoot.position;
        train.TrainRoot.position = new Vector3(x, position.y, position.z);
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
