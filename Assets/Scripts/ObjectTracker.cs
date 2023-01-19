using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YoloV4Tiny;

public class ObjectTracker : MonoBehaviour
{
    [Header("Object-detection settings")]
    [SerializeField] private bool StereoImage = false;
    
    [Header("Raycast setting")]
    [SerializeField] private int MaxRayDistance = 30;
    
    private CameraToWorld _cameraToWorld;

    private void Start()
    {
        _cameraToWorld = new CameraToWorld(Camera.main);
    }

    public void SetNewDetectionData(IEnumerable<Detection> detections)
    {
        List<Point> points = new List<Point>();

        foreach (KalmanFilter kalmanFilter in KalmanManager.Instance.KalmanFilters.Where(kalmanFilter => kalmanFilter == null))
        {
            KalmanManager.Instance.KalmanFilters.Remove(kalmanFilter);
            break;
        }
        
        foreach (Detection detection in detections)
        {
            if (detection.classIndex != 14) // Filter for detected objects by Label
                return;
            
            _cameraToWorld.ProcessDetection(detection, MaxRayDistance, StereoImage, out Point p); // Raycast from Camera to Floor

            if (p != null)
            {
                points.Add(p);
            }
                
        }

        if (points.Count == 0)
            return;

        ProcessKalman(points);
    }

    public void SetNewSimulationData(List<Point> points)
    {
        if (points.Count == 0)
            return;

        ProcessKalman(points);
    }

    private static void ProcessKalman(IReadOnlyList<Point> points)
    {
        if (points.Count == 0)
            return;
        if (KalmanManager.Instance.KalmanFilters.Count == 0)
            KalmanManager.Instance.InstantiateNewKalmanFilter();


        float[][] distanceArray = CreateDistanceArray(points);

        int kalmanFilterCount = KalmanManager.Instance.KalmanFilters.Count;
        for (int i = 0; i < points.Count; i++)
        {
            // Find pair of KalmanFilter and Point with smallest distance
            FindMinIndexOfMulti(distanceArray, out int kalman, out int point);

            // Send Point to KalmanFilter or Instantiate a new KalmanFilter if the distance is higher than the threshold
            if (float.IsPositiveInfinity(distanceArray[kalman][point]))
            {
                KalmanManager.Instance.InstantiateNewKalmanFilter();
                KalmanManager.Instance.KalmanFilters.LastOrDefault()?.SetNewMeasurement(points[point].Position);
            }
            else
            {
                KalmanManager.Instance.KalmanFilters[kalman].SetNewMeasurement(points[point].Position);
            
                // Set the distance of the used KalmanFilter and Point to Infinity in the distanceArray
                for (int k = 0; k < kalmanFilterCount; k++)
                {
                    for (int p = 0; p < points.Count; p++)
                    {
                        if (k == kalman || p == point)
                            distanceArray[k][p] = float.PositiveInfinity;
                    }
                }
            
            }
        }
    }


    private static float[][] CreateDistanceArray(IReadOnlyList<Point> points)
    {
        float[][] distanceMultiArray = new float[KalmanManager.Instance.KalmanFilters.Count][];

        for (int k = 0; k < KalmanManager.Instance.KalmanFilters.Count; k++)
        {
            distanceMultiArray[k] = new float[points.Count];
            
            for (int p = 0; p < points.Count; p++)
            {
                distanceMultiArray[k][p] = Vector2.Distance(KalmanManager.Instance.KalmanFilters[k].GetVector2Position(), points[p].Position);
            }
        }

        return distanceMultiArray;
    }

    private static void FindMinIndexOfMulti(IReadOnlyList<float[]> arr, out int index, out int element)
    {
        index = 0;
        element = 0;
        for (int i = 0; i < arr.Count; i++)
        {
            for (int e = 0; e < arr[i].Length; e++)
            {
                if (!(arr[i][e] < arr[index][element])) continue;
                
                index = i;
                element = e;
            }
            
        }
    }
}
