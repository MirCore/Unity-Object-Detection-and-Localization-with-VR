using System;
using System.Collections;
using System.Collections.Generic;
using Simulator;
using UnityEditor;
using UnityEngine;
using YoloV4Tiny;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    public enum LocalisationMethodEnum { Cluster, Kalman }
    public LocalisationMethodEnum LocalisationMethod;
    
    [field:Header("Object Detection Settings")]
    [field:SerializeField] public List<string> LabelsToFind { get; private set; }
    [field:SerializeField] public bool StereoImage { get; private set; }

    private DateTime _startTime;
    private Visualizer _visualizer;
    public int FixedFrameNumber { get; private set; }
    
    [SerializeField] private int AutoPauseAfterSeconds = 0;

    #region ClusteringValues

    [SerializeField] private ObjectTrackerClustering ObjectTrackerClustering;

    #endregion

    
    #region KalmanValues

    private ObjectTrackerKalman ObjectTrackerKalman;

    private DetectionSimulator _detectionSimulator;
    [SerializeField] private bool EnableYOLO = true;
    [SerializeField] private bool EnableMeasureSimulation = false;

    [field:Header("Kalman Values")]
    [field:SerializeField] public float SigmaSquared { get; private set; } = 30;
    [field:SerializeField] public float Rx { get; private set; } = 10;
    [field:SerializeField] public float Ry { get; private set; } = 10;

    [Header("Gizmos")]
    [SerializeField] public bool ShowMeasurementGizmos = true;

    [Header("Debug")]
    public List<SimulationController> SimulatedObjects = new();
    
    #endregion
    
    private void Awake()
    {
        // Singleton Instantiation
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;

        switch (LocalisationMethod)
        {
            case LocalisationMethodEnum.Cluster:
                break;
            case LocalisationMethodEnum.Kalman:
                ObjectTrackerKalman = gameObject.AddComponent<ObjectTrackerKalman>();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Start()
    {
        _visualizer = GetComponent<Visualizer>();
        _visualizer.enabled = EnableYOLO;
        _detectionSimulator = GetComponent<DetectionSimulator>();
        if (_detectionSimulator != null)
            _detectionSimulator.enabled = EnableMeasureSimulation;
        
        if (AutoPauseAfterSeconds != 0)
            StartCoroutine(PauseGame(AutoPauseAfterSeconds));
    }

    private void FixedUpdate()
    {
        FixedFrameNumber++;
    }
    
    private void OnDestroy()
    {
        //KalmanStatisticsUtils.WriteNEESToLog(SigmaSquared, Rx, Ry);
        //KalmanStatisticsUtils.WriteFileFromList("log", _kalmanStates);
        //Debug.Log(KalmanManager.Instance.GetMSEText());
    }

    /// <summary>
    /// Pauses Game after seconds. 
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private static IEnumerator PauseGame(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        EditorApplication.isPaused = true;
    }

    public void ResetFrameNumber()
    {
        FixedFrameNumber = 0;
    }

    /// <summary>
    /// Forwards new Object Detection data to Kalman or Clustering class
    /// </summary>
    /// <param name="detectorDetections"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetNewDetectionData(IEnumerable<Detection> detectorDetections)
    {
        switch (LocalisationMethod)
        {
            case LocalisationMethodEnum.Cluster:
                ObjectTrackerClustering.SetNewDetectionData(detectorDetections);
                break;
            case LocalisationMethodEnum.Kalman:
                ObjectTrackerKalman.SetNewDetectionData(detectorDetections);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

