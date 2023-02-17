using System;
using System.Collections;
using System.Collections.Generic;
using Kalman;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Simulator;
using TMPro;
using UnityEngine;

public class KalmanManager : MonoBehaviour
{
    public static KalmanManager Instance { get; private set; }
    public List<KalmanFilter> KalmanFilters = new();
    [SerializeField] private GameObject _kalmanFilterPrefab;

    [SerializeField] private TMP_Text text;

    private float NEES;
    private float MSEKalman;
    private float MSE;
    private int NEEScount;
    private KalmanState _kalmanState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;
    }

    private void FixedUpdate()
    {
        List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs = GetKalmanSimulationPairs();
        CalculateNormalizedEstimatedErrorSquared(kalmanSimulationPairs);
    }

    private void CalculateNormalizedEstimatedErrorSquared(List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs)
    {
        if (kalmanSimulationPairs == null)
            return;
        
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
            MSE += (simulatedObject.PositionVector2 - kalman.GetPositionMeanWithoutKalman()).sqrMagnitude;
            MSEKalman += (simulatedObject.PositionVector2 - kalman.GetVector2Position()).sqrMagnitude;
            NEEScount++;
            text.SetText("NEES: " + (NEES / NEEScount).ToString("F4") +
                         "\n MSE: " + (MSE / NEEScount).ToString("F4") +
                         "\n MSEKalman: " + (MSEKalman / NEEScount).ToString("F4"));

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

    private List<Tuple<KalmanFilter, SimulationController>> GetKalmanSimulationPairs()
    {
        if (GameManager.Instance.SimulatedObjects.Count == 0 || KalmanFilters.Count == 0)
            return null;
        
        float[][] distanceArray = CreateDistanceArray();
        List<Tuple<KalmanFilter, SimulationController>> kalmanSimulationPairs = new();
        for (int i = 0; i < GameManager.Instance.SimulatedObjects.Count; i++)
        {
            // Find pair of KalmanFilter and Point with smallest distance
            HelperFunctions.FindMinIndexOfMulti(distanceArray, out int kalman, out int simulatedObject);

            if (float.IsPositiveInfinity(distanceArray[kalman][simulatedObject]) || float.IsNaN(distanceArray[kalman][simulatedObject]))
                continue;
            kalmanSimulationPairs.Add(new Tuple<KalmanFilter, SimulationController>(KalmanFilters[kalman], GameManager.Instance.SimulatedObjects[simulatedObject]));

            // Set the distance of the used KalmanFilter and Point to Infinity in the distanceArray
            for (int k = 0; k < KalmanFilters.Count; k++)
            {
                for (int s = 0; s < GameManager.Instance.SimulatedObjects.Count; s++)
                {
                    if (k == kalman || s == simulatedObject)
                        distanceArray[k][s] = float.NaN;
                }

            }
        }

        return kalmanSimulationPairs;
    }

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


    public void InstantiateNewKalmanFilter()
    {
        Instantiate(_kalmanFilterPrefab, new Vector3(), new Quaternion());
    }
    public void InstantiateNewKalmanFilter(Vector2 position)
    {
        Instantiate(_kalmanFilterPrefab, new Vector3(position.x, 0, position.y), new Quaternion());
    }

    public void SetNewKalmanFilter(KalmanFilter kalmanFilter)
    {
        KalmanFilters.Add(kalmanFilter);
    }

    public void RemoveKalmanFilter(KalmanFilter kalmanFilter)
    {
        KalmanFilters.Remove(kalmanFilter);
    }

    public float GetNEESValue()
    {
        return NEES / NEEScount;
    }

    public string GetMSEText()
    {
        return ("NEES: " + (NEES / NEEScount).ToString("F4") +
                "\n MSEKalman: " + (MSEKalman / NEEScount).ToString("F4"));
    }

    public KalmanState GetKalmanState()
    {
        return _kalmanState;
    }
}
