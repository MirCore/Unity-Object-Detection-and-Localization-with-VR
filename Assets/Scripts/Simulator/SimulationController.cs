using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Serialization;

namespace Simulator
{
    public class SimulationController : MonoBehaviour
    {
        private Vector3 _lastPos;
        public Vector3 PositionVector3 { get; private set; }
        public Vector2 PositionVector2 { get; private set; }
        public Vector3 VelocityVector3 { get; private set; }
        public Vector2 VelocityVector2 { get; private set; }
        
        [SerializeField] private bool RecordPositions = false;
        private TextWriter _textWriter;
        
        [SerializeField] private TextAsset RecordedPositionsFile ;
        private string[] _recordedPositionsFileRows;
        private readonly List<Vector3> Positions = new ();
        
        private void Awake()
        {
            GameManager.Instance.SimulatedObjects.Add(this);
            
            if (RecordPositions)
                _textWriter = new StreamWriter("Assets/Output/positions_" + DateTime.Now.ToString("HH_mm_ss") + ".csv", false);

            if (RecordedPositionsFile != null && !RecordPositions)
                _recordedPositionsFileRows =  RecordedPositionsFile.text.Split("\n"[0]);
        }

        private void FixedUpdate()
        {
            _lastPos = PositionVector3;
            PositionVector3 = transform.position;
            PositionVector2 = new Vector2(PositionVector3.x, PositionVector3.z);
            VelocityVector3 = (PositionVector3 - _lastPos) / Time.deltaTime;
            VelocityVector2 = new Vector2(VelocityVector3.x, VelocityVector3.z);
            
            Debug.DrawLine(PositionVector3, PositionVector3 + VelocityVector3);
            
            if (RecordPositions)
            {
                Vector3 position = transform.position;
                _textWriter.WriteLine("(" + position.x + "; " + position.y + "; " + position.z + ")");
            }
            
            if (_recordedPositionsFileRows != null && !RecordPositions)
                MoveToRecordedLocation();

            Positions.Add(transform.position);
        } 
        
        private void OnDestroy()
        {
            _textWriter?.Close();
        }

        private void MoveToRecordedLocation()
        {
            if (GameManager.Instance.FrameNumber >= _recordedPositionsFileRows.Length - 1)
                return;
            string[] lineData = _recordedPositionsFileRows[GameManager.Instance.FrameNumber].Trim().Trim('(', ')').Split(";"[0]);
            Vector3 recordedPosition = new(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]));
            LookAt(recordedPosition);
            SetNewPosition(recordedPosition);
        }

        private void SetNewPosition(Vector3 position)
        {
            transform.position = position;
        }

        private void LookAt(Vector3 position)
        {
            transform.LookAt(position);
        }
        
        private void OnDrawGizmos()
        {
            GizmosUtils.DrawLine(Positions, Color.black);
        }
    }
}
