using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Simulator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private DateTime StartTime;
    
    [SerializeField] private bool EnableYOLO = true;
    [SerializeField] private bool EnableMeasureSimulation = false;
    
    private VisualizerSimulation _visualizerSimulation;
    private DetectionSimulator _detectionSimulator;

    private List<KalmanState> _kalmanStates = new();

    [SerializeField] private int DurationSeconds = 10;
    
    [Header("Kalman Values")]
    [SerializeField] private float OmegaSquared = 10;
    [SerializeField] private float Rx = 10;
    [SerializeField] private float Ry = 10;
    
    [Header("File IO")]
    private TextWriter _textWriter;
    private string _fileData ;
    [SerializeField] private string FilePath ;
    private string[] _lines;
    [SerializeField] private bool RecordPositions = false;
    private int _frameNumber;
    

    [Header("")]
    public List<SimulationController> SimulatedObjects = new();
    [SerializeField] private SimulationController NPC;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        _visualizerSimulation = GetComponent<VisualizerSimulation>();
        _visualizerSimulation.enabled = EnableYOLO;
        _detectionSimulator = GetComponent<DetectionSimulator>();
        _detectionSimulator.enabled = EnableMeasureSimulation;
        
        if (DurationSeconds != 0)
            StartCoroutine(StopGame(DurationSeconds));
        if (RecordPositions)
        {
            string path = "Assets/Output";
            string fileName = "positions_" + DateTime.Now.ToString("HH_mm_ss");
            string fullFileName = path + "/" + fileName + ".csv";
            Debug.Log(fullFileName);
            _textWriter = new StreamWriter(fullFileName, false);
        }

        if (FilePath == "")
            return;
        _fileData = File.ReadAllText(FilePath);
        _lines = _fileData.Split("\n"[0]);
    }

    private void FixedUpdate()
    {
        if (RecordPositions)
        {
            Vector3 position = SimulatedObjects.First().transform.position;
            string text = "(" + position.x + "; " + position.y + "; " + position.z + ")";
            _textWriter.WriteLine(text);
        }

        KalmanState kalmanState = KalmanManager.Instance.GetKalmanState();
        if (kalmanState != null)
            _kalmanStates.Add(kalmanState);
        
        if (_lines == null)
            return;
        if (_frameNumber >= _lines.Length - 1)
            return;
        //_controller.transform.position += new Vector3(1 * Time.deltaTime, 0, 0);
        string[] lineData = _lines[_frameNumber].Trim().Trim('(',')').Split(";"[0]);
        Vector3 recordedPosition = new Vector3(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]));
        NPC.LookAt(recordedPosition);
        NPC.SetNewPosition(recordedPosition);
        _frameNumber++;
    }
    
    private void OnDestroy()
    {
        WriteFile("kalman", "NEES: " + KalmanManager.Instance.GetNEESValue() + "; o^2: " + OmegaSquared + "; R: " + Rx + " " + Ry);
        WriteFileFromList("log", _kalmanStates);

        _textWriter?.Close();
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

    private IEnumerator PauseGame(int seconds)
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
