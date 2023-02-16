using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Simulator;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private DateTime StartTime;
    
    [SerializeField] private bool EnableYOLO = true;
    [SerializeField] private bool EnableMeasureSimulation = false;
    
    private Visualizer _visualizer;
    private DetectionSimulator _detectionSimulator;

    private List<KalmanState> _kalmanStates = new();

    [SerializeField] private int DurationSeconds = 10;
    
    [Header("Kalman Values")]
    [SerializeField] private float OmegaSquared = 10;
    [SerializeField] private float Rx = 10;
    [SerializeField] private float Ry = 10;

    [Header("Gizmos")] [SerializeField] public bool Messpunkte = true;
    
    public int FrameNumber { get; private set; }

    [Header("")]
    public List<SimulationController> SimulatedObjects = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;

        //Time.timeScale = 0.5f;
    }

    private void Start()
    {
        _visualizer = GetComponent<Visualizer>();
        _visualizer.enabled = EnableYOLO;
        _detectionSimulator = GetComponent<DetectionSimulator>();
        if (_detectionSimulator != null)
            _detectionSimulator.enabled = EnableMeasureSimulation;
        
        if (DurationSeconds != 0)
            StartCoroutine(PauseGame(DurationSeconds));
    }

    private void FixedUpdate()
    {

        KalmanState kalmanState = KalmanManager.Instance.GetKalmanState();
        if (kalmanState != null)
            _kalmanStates.Add(kalmanState);

        FrameNumber++;
    }
    
    private void OnDestroy()
    {
        WriteFile("kalman", "NEES: " + KalmanManager.Instance.GetNEESValue() + "; o^2: " + OmegaSquared + "; R: " + Rx + " " + Ry);
        WriteFileFromList("log", _kalmanStates);

        Debug.Log(KalmanManager.Instance.GetNEESValue());
    }

    private static void WriteFileFromList(string fileName, List<KalmanState> kalmanStates)
    {
        string fullFileName = "Assets/Output/" + fileName + ".csv";
        TextWriter writer = new StreamWriter(fullFileName, false);
        writer.WriteLine("Time" + ";" +
                         "Frame" + ";" +
                         "GroundTruth x" + ";" +
                         "GroundTruth y" + ";" +
                         "GroundTruth v_x" + ";" +
                         "GroundTruth v_y" + ";" +
                         "Measurement x" + ";" +
                         "Measurement y" + ";" +
                         "Kalman x" + ";" +
                         "Kalman y" + ";" +
                         "Kalman v_x" + ";" +
                         "Kalman v_y" + ";" +
                         "KalmanP x" + ";" +
                         "KalmanP y");
        foreach (string output in kalmanStates.Select(kalmanState => 
                     kalmanState.Time + ";" +
                     kalmanState.Frame + ";" +
                     Vector2ToCsv(kalmanState.GroundTruthPosition) + ";" +
                     Vector2ToCsv(kalmanState.GroundTruthVelocity) + ";" +
                     Vector2ToCsv(kalmanState.Measurement) + ";" +
                     Vector2ToCsv(kalmanState.KalmanPosition) + ";" +
                     Vector2ToCsv(kalmanState.KalmanVelocity) + ";" +
                     Vector2ToCsv(kalmanState.KalmanP)))
        {
            writer.WriteLine(output);
        }
        writer.Close();
    }

    private static string Vector2ToCsv(Vector2 input)
    {
        return input.x + ";" + input.y;
    }

    private static void WriteFile(string fileName, string output)
    {
        string fullFileName = "Assets/Output/" + fileName + ".csv";
        TextWriter writer = new StreamWriter(fullFileName, true);
        writer.WriteLine(output);
        writer.Close();
    }

    public float GetRx()
    {
        return Rx;
    }
    public float GetRy()
    {
        return Ry;
    }

    private static IEnumerator PauseGame(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        EditorApplication.isPaused = true;
    }

    private IEnumerator StopGame(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        EditorApplication.isPlaying = false;
    }

    public double GetOmegaSquared()
    {
        return OmegaSquared;
    }

    public void ResetFrameNumber()
    {
        FrameNumber = 0;
    }
}

public class KalmanState
{
    public Vector2 GroundTruthPosition;
    public Vector2 GroundTruthVelocity;
    public Vector2 KalmanPosition;
    public Vector2 KalmanVelocity;
    public Vector2 KalmanP;
    public Vector2 Measurement;
    public float Time;
    public int Frame;
}
