using System;
using System.Collections;
using System.IO;
using System.Linq;
using Simulator;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private DateTime _localDate = DateTime.Now;
    
    [SerializeField] private int DurationSeconds = 10;
    [SerializeField] private DetectionSimulator _detectionSimulator;
    
    [Header("Kalman Values")]
    [SerializeField] private float Q = 10;
    [SerializeField] private float RValue = 10;
    private TextWriter _textWriter;
    private string _fileData ;
    [SerializeField] private string FilePath ;
    private string[] _lines;
    [SerializeField] private bool RecordPositions = false;

    private int _frameNumber;
    [SerializeField] private CharacterController _controller;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(StopGame(DurationSeconds));
        if (RecordPositions)
        {
            string path = "Assets/Output";
            string fileName = "positions_" + _localDate.ToString("HH_mm_ss");
            string fullFileName = path + "/" + fileName + ".csv";
            Debug.Log(fullFileName);
            _textWriter = new StreamWriter(fullFileName, false);
        }

        if (FilePath == "")
            return;
        _fileData = File.ReadAllText(FilePath);
        _lines = _fileData.Split("\n"[0]);
        print(_lines);
    }

    private void FixedUpdate()
    {
        if (RecordPositions)
        {
            Vector3 position = _detectionSimulator.SimulatedObjects.First().transform.position;
            string text = "(" + position.x + "; " + position.y + "; " + position.z + ")";
            _textWriter.WriteLine(text);
        }
        
        if (_lines == null)
            return;
        
        string[] lineData = _lines[_frameNumber].Trim().Trim('(',')').Split(";"[0]);
        Vector3 recordedPosition = new Vector3(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]));
        _controller.transform.LookAt(recordedPosition);
        _controller.transform.position = recordedPosition;
        _frameNumber++;
    }
    
    /// <summary>
    /// Writes a csv-file with the current time in its name.
    /// On Android files are saved under /storage/emulated/0/Android/data/<packagename>/files
    /// on Windows under %userprofile%\AppData\Local\Packages\<productname>\LocalState
    /// </summary>

    private void OnDestroy()
    {
        string path = "Assets/Output";
        string fileName = "kalman";
        string fullFileName = path + "/" + fileName + ".csv";
        TextWriter writer = new StreamWriter(fullFileName, true);
        writer.WriteLine("NEES: " + KalmanManager.Instance.GetNEESValue() + "; Q: " + Q + "; R: " + RValue);
        writer.Close();
        
        _textWriter?.Close();
    }


    public float GetQ()
    {
        return Q;
    }
    public float GetRValue()
    {
        return RValue;
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
}
