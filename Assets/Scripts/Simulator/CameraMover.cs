using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Simulator
{
    public class CameraMover : MonoBehaviour
    {
        [SerializeField] private float RotationAngle = 0.1f;
        
        private void FixedUpdate()
        {
            transform.Rotate(transform.up, RotationAngle);
        }
    }
}
