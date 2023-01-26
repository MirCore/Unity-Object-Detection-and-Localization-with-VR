using System;
using UnityEngine;

namespace Simulator
{
    public class SimulationController : MonoBehaviour
    {
        private Vector3 _lastPos;
        public Vector3 PositionVector3 { get; private set; }
        public Vector2 PositionVector2 { get; private set; }
        public Vector3 VelocityVector3 { get; private set; }
        public Vector2 VelocityVector2 { get; private set; }


        private void Awake()
        {
            GameManager.Instance.SimulatedObjects.Add(this);
        }

        private void FixedUpdate()
        {
            _lastPos = PositionVector3;
            PositionVector3 = transform.position;
            PositionVector2 = new Vector2(PositionVector3.x, PositionVector3.z);
            VelocityVector3 = (PositionVector3 - _lastPos) / Time.deltaTime;
            VelocityVector2 = new Vector2(VelocityVector3.x, VelocityVector3.z);
            
            Debug.DrawLine(PositionVector3, PositionVector3 + VelocityVector3);
        }

        public void SetNewPosition(Vector3 position)
        {
            transform.position = position;
        }
        
        public void LookAt(Vector3 position)
        {
            transform.LookAt(position);
        }
    }
}
