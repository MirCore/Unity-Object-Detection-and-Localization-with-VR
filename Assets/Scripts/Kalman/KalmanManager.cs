using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Simulator;
using TMPro;
using UnityEngine;

namespace Kalman
{
    public class KalmanManager : MonoBehaviour
    {
        public static KalmanManager Instance { get; private set; }
        public List<KalmanFilter> KalmanFilters = new();
        [SerializeField] private GameObject _kalmanFilterPrefab;

        [SerializeField] private TMP_Text text;

        private float NEES;
        private float MSE;
        private int NEEScount;
        private KalmanState _kalmanState;

        private void Awake()
        {
            // Singleton check
            if (Instance != null && Instance != this)
                Destroy(this);
            Instance = this;
        }

        private void FixedUpdate()
        {
            EvaluateKalmanPerformance();
        }

        /// <summary>
        /// Creates a List of KalmanFilter and SimulatedObject pairs and calls NEES and KalmanState functions
        /// </summary>
        private void EvaluateKalmanPerformance()
        {
            List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs = GetKalmanSimulationPairs();

            if (kalmanSimulationPairs == null)
                return;

            CalculateNormalizedEstimatedErrorSquared(kalmanSimulationPairs);
            CreateKalmanState(kalmanSimulationPairs);
        }

        /// <summary>
        /// Creates a KalmanState for each pair of Kalman Filter and SimulatedObject
        /// Currently only one KalmanState is used, as each new one overwrites the last
        /// </summary>
        /// <param name="kalmanSimulationPairs"></param>
        private void CreateKalmanState(List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs)
        {
            foreach ((KalmanFilter kalman, SimulationController simulatedObject) in kalmanSimulationPairs)
            {
                _kalmanState = new KalmanState
                {
                    GroundTruthPosition = simulatedObject.PositionVector2,
                    GroundTruthVelocity = simulatedObject.VelocityVector2,
                    KalmanPosition = kalman.GetVector2Position(),
                    KalmanVelocity = kalman.GetVector2Velocity(),
                    KalmanP = kalman.GetP(),
                    Measurement = kalman.GetMeasurement(),
                    Time = Time.realtimeSinceStartup,
                    Frame = Time.frameCount
                };
            }
        }

        /// <summary>
        /// Calculates NEES and MSE of KalmanFilter and SimulatedObject Pair
        /// </summary>
        /// <param name="kalmanSimulationPairs"></param>
        private void CalculateNormalizedEstimatedErrorSquared(List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs)
        {
            foreach ((KalmanFilter kalman, SimulationController simulatedObject) in kalmanSimulationPairs)
            {
                Matrix<double> groundTruth = DenseMatrix.OfArray(new double[,] { 
                    { simulatedObject.PositionVector3.x }, 
                    { simulatedObject.PositionVector3.z }, 
                    { simulatedObject.VelocityVector3.x },
                    { simulatedObject.VelocityVector3.z }});
                Matrix<double> x = groundTruth - DenseMatrix.OfColumnVectors(kalman.x);

                Matrix<double> normalizedEstimatedErrorSquared = x.Transpose() * kalman.P.Inverse() * x;
                
                NEES += (float) normalizedEstimatedErrorSquared[0, 0];
                MSE += (simulatedObject.PositionVector2 - kalman.GetVector2Position()).sqrMagnitude;
                NEEScount++;
                
                text.SetText("NEES: " + (NEES / NEEScount).ToString("F4") +
                             "\n MSE: " + (MSE / NEEScount).ToString("F4"));
            }
        }

        /// <summary>
        /// Creates an List of KalmanFilter and SimulatedObject pairs
        /// </summary>
        /// <returns></returns>
        private List<Tuple<KalmanFilter, SimulationController>> GetKalmanSimulationPairs()
        {
            if (GameManager.Instance.SimulatedObjects.Count == 0 || KalmanFilters.Count == 0)
                return null;
        
            float[][] distanceArray = CreateDistanceArray();
            List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs = new();
            int simulatedObjectsCount = GameManager.Instance.SimulatedObjects.Count;
            for (int i = 0; i < simulatedObjectsCount; i++)
            {
                // Find pair of KalmanFilter and Point with smallest distance
                HelperFunctions.FindMinIndexOfMulti(distanceArray, out int kalman, out int simulatedObject);

                if (float.IsPositiveInfinity(distanceArray[kalman][simulatedObject]) || float.IsNaN(distanceArray[kalman][simulatedObject]))
                    continue;
                kalmanSimulationPairs.Add(new Tuple<KalmanFilter, SimulationController>(KalmanFilters[kalman], GameManager.Instance.SimulatedObjects[simulatedObject]));

                // Set the distance of the used KalmanFilter and Point to Infinity in the distanceArray
                HelperFunctions.RemoveUsedPairsInDistanceArray(simulatedObjectsCount, KalmanFilters.Count,
                    distanceArray, simulatedObject, kalman);
            }

            return kalmanSimulationPairs;
        }

        /// <summary>
        /// Creates an Array of KalmanFilter.Count x SimulatedObjects.Count dimensions
        /// The Array is filled with distances between all KalmanFilters and SimulatedObjects
        /// </summary>
        /// <returns>returns an Array float[][] with distances</returns>
        private float[][] CreateDistanceArray()
        {
            float[][] distanceMultiArray = new float[KalmanFilters.Count][];

            for (int k = 0; k < KalmanFilters.Count; k++)
            {
                distanceMultiArray[k] = new float[GameManager.Instance.SimulatedObjects.Count];
            
                for (int p = 0; p < GameManager.Instance.SimulatedObjects.Count; p++)
                {
                    distanceMultiArray[k][p] = Vector3.Distance(KalmanFilters[k].transform.position, GameManager.Instance.SimulatedObjects[p].transform.position);
                }
            }

            return distanceMultiArray;
        }
    
        /// <summary>
        /// Instantiates a new KalmanFilter at position
        /// </summary>
        /// <param name="position"></param>
        public void InstantiateNewKalmanFilter(Vector2 position)
        {
            Instantiate(_kalmanFilterPrefab, new Vector3(position.x, 0, position.y), new Quaternion());
        }

        /// <summary>
        /// Adds a KalmanFilter to the KalmanFilter List
        /// </summary>
        /// <param name="kalmanFilter"></param>
        public void SetNewKalmanFilter(KalmanFilter kalmanFilter)
        {
            KalmanFilters.Add(kalmanFilter);
        }

        /// <summary>
        /// Removes a KalmanFilter from the KalmanFilter list
        /// </summary>
        /// <param name="kalmanFilter"></param>
        public void RemoveKalmanFilter(KalmanFilter kalmanFilter)
        {
            KalmanFilters.Remove(kalmanFilter);
        }

        /// <summary>
        /// Returns NEES
        /// </summary>
        /// <returns></returns>
        public float GetNEESValue()
        {
            return NEES / NEEScount;
        }

        /// <summary>
        /// Returns NEES and MSE string
        /// </summary>
        /// <returns></returns>
        public string GetMSEText()
        {
            return ("NEES: " + (NEES / NEEScount).ToString("F4") +
                    "\n MSEKalman: " + (MSE / NEEScount).ToString("F4"));
        }

        /// <summary>
        /// Returns KalmanState
        /// </summary>
        /// <returns></returns>
        public KalmanState GetKalmanState()
        {
            return _kalmanState;
        }
    }
}
