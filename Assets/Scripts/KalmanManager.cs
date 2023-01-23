using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Simulator;
using TMPro;
using UnityEngine;

public class KalmanManager : MonoBehaviour
{
    public static KalmanManager Instance { get; private set; }
    [SerializeField] public List<KalmanFilter> KalmanFilters = new List<KalmanFilter>();
    [SerializeField] private GameObject _kalmanFilter;

    [SerializeField] private DetectionSimulator _detectionSimulator;
    [SerializeField] private TMP_Text text;
    
    private float NEES;
    private int NEEScount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;
    }

    private void Update()
    {
        List<Tuple<KalmanFilter, CharacterController>> kalmanSimulationPairs = GetKalmanSimulationPairs();
        CalculateNormalizedEstimatedErrorSquared(kalmanSimulationPairs);
    }

    private void CalculateNormalizedEstimatedErrorSquared(List<Tuple<KalmanFilter, CharacterController>> kalmanSimulationPairs)
    {
        if (kalmanSimulationPairs == null)
            return;
        
        foreach (Tuple<KalmanFilter, CharacterController> kalmanSimulationPair in kalmanSimulationPairs)
        {
            KalmanFilter kalman = kalmanSimulationPair.Item1;
            Vector3 simulatedObjectVelocity = kalmanSimulationPair.Item2.velocity;
            Vector3 simulatedObjectPosition = kalmanSimulationPair.Item2.transform.position;
            
            Matrix<double> groundTruth = DenseMatrix.OfArray(new double[,] { 
                { simulatedObjectPosition.x }, 
                { simulatedObjectPosition.z }, 
                { simulatedObjectVelocity.x },
                { simulatedObjectVelocity.z }});
            Matrix<double> x = groundTruth - DenseMatrix.OfColumnVectors(kalman.x);


            Matrix<double> normalizedEstimatedErrorSquared = x.Transpose() * kalmanSimulationPair.Item1.P.Inverse() * x;
            NEES += (float) normalizedEstimatedErrorSquared[0, 0];
            NEEScount++;
            text.SetText((NEES / NEEScount).ToString());
        }
    }

    private List<Tuple<KalmanFilter, CharacterController>> GetKalmanSimulationPairs()
    {
        if (_detectionSimulator.SimulatedObjects.Count == 0 || KalmanFilters.Count == 0)
            return null;
        
        float[][] distanceArray = CreateDistanceArray();
        List<Tuple<KalmanFilter, CharacterController>> kalmanSimulationPairs = new List<Tuple<KalmanFilter, CharacterController>>();
        for (int i = 0; i < _detectionSimulator.SimulatedObjects.Count; i++)
        {
            // Find pair of KalmanFilter and Point with smallest distance
            HelperFunctions.FindMinIndexOfMulti(distanceArray, out int kalman, out int simulatedObject);

            kalmanSimulationPairs.Add(new Tuple<KalmanFilter, CharacterController>(KalmanFilters[kalman], _detectionSimulator.SimulatedObjects[simulatedObject]));
        
            // Set the distance of the used KalmanFilter and Point to Infinity in the distanceArray
            for (int k = 0; k < KalmanFilters.Count; k++)
            {
                for (int s = 0; s < _detectionSimulator.SimulatedObjects.Count; s++)
                {
                    if (k == kalman || s == simulatedObject)
                        distanceArray[k][s] = float.PositiveInfinity;
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
            distanceMultiArray[k] = new float[_detectionSimulator.SimulatedObjects.Count];
            
            for (int p = 0; p < _detectionSimulator.SimulatedObjects.Count; p++)
            {
                distanceMultiArray[k][p] = Vector3.Distance(KalmanFilters[k].transform.position, _detectionSimulator.SimulatedObjects[p].transform.position);
            }
        }

        return distanceMultiArray;
    }


    public void InstantiateNewKalmanFilter()
    {
        Instantiate(_kalmanFilter, new Vector3(), new Quaternion());
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
}
