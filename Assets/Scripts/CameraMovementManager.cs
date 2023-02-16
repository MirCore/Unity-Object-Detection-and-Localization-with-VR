using System;
using System.IO;
using UnityEngine;

public class CameraMovementManager : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private bool RecordPositions = false;
    [SerializeField] private bool PlayRecordedPositions = false;
    private TextWriter _textWriter;
    private Transform _cameraTransform;
    [SerializeField] private TextAsset RecordedPositionsFile ;
    private string[] _recordedPositionsFileRows;

    private void Awake()
    {
        _cameraTransform = _camera.transform;
        if (RecordPositions)
            _textWriter = new StreamWriter("Assets/Output/camera_" + DateTime.Now.ToString("HH_mm_ss") + ".csv", false);
        
        if (RecordedPositionsFile != null && !RecordPositions && PlayRecordedPositions)
            _recordedPositionsFileRows =  RecordedPositionsFile.text.Split("\n"[0]);
    }

    private void FixedUpdate()
    {
        if (RecordPositions)
        {
            Vector3 position = _cameraTransform.position;
            Quaternion rotation = _cameraTransform.rotation;
            _textWriter.WriteLine(
                "(" + position.x + "; " + position.y + "; " + position.z + "; " + 
                rotation.w + "; " + rotation.x + "; " + rotation.y + "; " + rotation.z + ")"
            );
        }
        
        if (_recordedPositionsFileRows != null && !RecordPositions)
            MoveToRecordedOrientation();
    }

    private void MoveToRecordedOrientation()
    {
        int timeDisplacement = 3;
        int frameNumber = GameManager.Instance.FrameNumber - timeDisplacement;
        if (frameNumber >= _recordedPositionsFileRows.Length - 1 - timeDisplacement)
            return;
        if (frameNumber < 0)
            return;
        string[] lineData = _recordedPositionsFileRows[frameNumber].Trim().Trim('(', ')').Split(";"[0]);
        Vector3 recordedPosition = new(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]));
        Quaternion recordedRotation = new(float.Parse(lineData[4]), float.Parse(lineData[5]), float.Parse(lineData[6]), float.Parse(lineData[3]));
        _cameraTransform.rotation = recordedRotation;
        _cameraTransform.position = recordedPosition;
    }
}
