using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CameraMovementManager : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private bool RecordPositions = false;
    [SerializeField] private bool PlayRecordedPositions = false;
    [SerializeField] private bool PlayRecordedRotations = false;
    [SerializeField] private bool RotateAroundCenter = false;
    [SerializeField] private int TimeScale = 1;
    private TextWriter _textWriter;
    private Transform _cameraTransform;
    [SerializeField] private TextAsset RecordedPositionsFile ;
    private string[] _recordedPositionsFileRows;
    
    private readonly List<Vector3> CameraPositions = new ();

    private void Awake()
    {
        _cameraTransform = _camera.transform;
        if (RecordPositions)
            _textWriter = new StreamWriter("Assets/Output/camera_" + DateTime.Now.ToString("HH_mm_ss") + ".csv", false);
        
        if (RecordedPositionsFile != null && !RecordPositions && (PlayRecordedPositions || PlayRecordedRotations))
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

        if (RotateAroundCenter)
            RotateCameraAroundCenter();
    }

    private void RotateCameraAroundCenter()
    {
        _cameraTransform.LookAt(Vector3.zero);
        _cameraTransform.Translate(Vector3.right * Time.fixedTime/50);
    }

    private void MoveToRecordedOrientation()
    {
        int timeDisplacement = 3;
        int frameNumber = (GameManager.Instance.FixedFrameNumber - timeDisplacement) / TimeScale;
        if (frameNumber >= _recordedPositionsFileRows.Length - 1 - timeDisplacement)
        {
            GameManager.Instance.ResetFrameNumber();
            return;
        }
        if (frameNumber < 0)
            return;
        string[] lineData = _recordedPositionsFileRows[frameNumber].Trim().Trim('(', ')').Split(";"[0]);
        Vector3 recordedPosition = new(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]));
        Quaternion recordedRotation = new(float.Parse(lineData[4]), float.Parse(lineData[5]), float.Parse(lineData[6]), float.Parse(lineData[3]));
        if (PlayRecordedRotations)
            _cameraTransform.rotation = recordedRotation;
        if (PlayRecordedPositions)
            _cameraTransform.position = recordedPosition;
        
        CameraPositions.Add(recordedPosition);
    }     
    
    private void OnDrawGizmos()
    {
        GizmosUtils.DrawLine(CameraPositions, Color.black);
    }
}
