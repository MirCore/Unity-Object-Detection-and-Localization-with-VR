using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kalman
{
    public class KalmanFilter : MonoBehaviour
    {
        protected Matrix<double> F;
        protected Matrix<double> FT;
        protected Matrix<double> H;
        protected Matrix<double> HT;
        protected Matrix<double> Gd;
        protected Matrix<double> I;
        protected Matrix<double> R;
        protected Matrix<double> GdQGdT;
        private Matrix<double> Q;
        public Matrix<double> P { get; protected set; }
        public Vector<double> x { get; protected set; }

        public float Ts;

        private Transform _kalmanTransform;
        public Renderer Hologram;

        [SerializeField] private bool SelfDestruct = true;       
        [SerializeField] private int SelfDestructDistance = 500;
    
        private readonly List<Vector3> _kalmanPositions = new ();

        private float NIS;
        private int NISCount;

        private Vector2? _measurement;
        [SerializeField] private Color Color;

        private void Awake()
        {
            // Create Matrices
            PopulateMatrices(GameManager.Instance.SigmaSquared);

            // Report this Kalman filter to the KalmanManager
            KalmanManager.Instance.SetNewKalmanFilter(this);
        }

        /// <summary>
        /// Creates Matrices used for Kalman filtering
        /// </summary>
        /// <param name="sigmaSquared"></param>
        private void PopulateMatrices(double sigmaSquared)
        {
            Ts = Time.fixedDeltaTime;

            // State-Transition Model
            F = DenseMatrix.OfArray(new double[,]
            {
                { 1, 0, Ts, 0 },
                { 0, 1, 0, Ts },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            });

            FT = F.Transpose();

            // Observation Model
            H = DenseMatrix.OfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 }
            });

            HT = H.Transpose();

            // System Noise Gain
            Gd = DenseMatrix.OfArray(new double[,]
            {
                { Ts * Ts / 2, 0 },
                { 0, Ts * Ts / 2 },
                { Ts, 0 },
                { 0, Ts }
            });

            GdQGdT = Gd * sigmaSquared * Gd.Transpose();

            I = DenseMatrix.CreateIdentity(4);

            R = DenseMatrix.OfDiagonalArray(new double[] { GameManager.Instance.Rx, GameManager.Instance.Ry });

            // Initial
            P = DenseMatrix.OfDiagonalArray(new double[] { 100, 100, 10, 10 });
            Vector3 position = transform.position;
            x = DenseVector.OfArray(new double[] { position.x, position.z, 0, 0 });
        }

        private void Start()
        {
            _kalmanTransform = transform;
            
            // generate a Random color for the Gizmos
            Color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            Hologram.material.color =  Color;
        }

        private void FixedUpdate()
        {
            // Predict
            Predict();
            // Update
            if (_measurement != null)
            {
                UpdatePrediction((Vector2) _measurement);
                _measurement = null;
            }
        
            // Update GameObject
            UpdateKalmanGameObject();

            // Check if Kalman filter has lost the position for to long. If true Destroy the Kalman filter
            if (SelfDestruct && P[0, 0] > SelfDestructDistance)
                SelfDestroy();
        }

        private void Predict()
        {
            x = F * x;
            P = F * P * FT + GdQGdT;
        }

        private void UpdatePrediction(Vector2 measurement)
        {
            DenseVector z = DenseVector.OfArray(new double[] {measurement.x, measurement.y});

            
            Vector<double> y = z - H * x;
            Matrix<double> PHT = P * HT;
            Matrix<double> S = H * PHT + R;
            Matrix<double> K = PHT * S.Inverse();
            x += K * y;
            // P = (I - K * H) * Ptilde;
            // more numerically stable version:
            // P = (I - KH) * P * (I-KH).T + KRK.T
            Matrix<double> I_KH = I - K * H;
            P = I_KH * P * I_KH.Transpose() + K * R * K.Transpose();

            
            //Matrix<double> v = DenseMatrix.OfColumnVectors(y); // residual (for NIS)
            //NIS += (float) (v.Transpose() * S.Inverse() * v)[0,0];
            //NISCount++;
        }

        /// <summary>
        /// Removes the Kalman filter from the KalmanManager List and destroys itself
        /// </summary>
        private void SelfDestroy()
        {
            KalmanManager.Instance.RemoveKalmanFilter(this);
            Destroy(gameObject);
        }

        // Updates the Transform to the new predictions
        private void UpdateKalmanGameObject()
        {
            Vector3 newPosition = GetVector3Position();
            _kalmanTransform.position = newPosition;
            UpdateGizmos(newPosition);
        }
    
        /// <summary>
        /// Saves a new measurement for the net update cycle
        /// </summary>
        /// <param name="measurement"></param>
        public void SetNewMeasurement(Vector2 measurement)
        {
            _measurement = measurement;
        }

        /// <summary>
        /// Updates size of the P Gizmo and draws a line for the velocity vector
        /// </summary>
        /// <param name="newPosition"></param>
        private void UpdateGizmos(Vector3 newPosition)
        {
            _kalmanTransform.localScale = new Vector3((float)P[0, 0], 0.1f, (float)P[1, 1]);
            Debug.DrawLine(newPosition, newPosition + GetVector3Velocity(), Color.red);
            _kalmanPositions.Add(newPosition);
        }
    
        private void OnDrawGizmos()
        {
            GizmosUtils.DrawLine(_kalmanPositions, Color);
        
            if (x == null)
                return;
        
            GizmosUtils.DrawText(GUI.skin, "x", new Vector3((float)x[0], 0, (float)x[1]), color: Color.black, fontSize: 10);
        }

        private Vector3 GetVector3Position()
        {
            return new Vector3((float)x[0], 0, (float)x[1]);
        }

        private Vector3 GetVector3Velocity()
        {
            return new Vector3((float)x[2], 0, (float)x[3]);
        }

        public Vector2 GetVector2Position()
        {
            return new Vector2((float)x[0], (float)x[1]);
        }

        public Vector2 GetVector2Velocity()
        {
            return new Vector2((float)x[2], (float)x[3]);
        }

        public Vector2 GetP()
        {
            return new Vector2((float)P[0, 0], (float)P[1, 1]);
        }

        public Vector2 GetMeasurement()
        {
            if (_measurement != null)
                return (Vector2)_measurement;
            return Vector2.zero;
        }

        public float GetNIS()
        {
            return NIS / NISCount;
        }
    }
}
