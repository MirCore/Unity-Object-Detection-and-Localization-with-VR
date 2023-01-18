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
    private List<Point> Points = new List<Point>();
    
    private CameraToWorld _cameraToWorld;
    [SerializeField] private GameObject _kalmanFilter;
    [SerializeField] private GameObject _kalmanFilterCAM;
    [SerializeField] private List<KalmanFilter> _kalmanFilters = new List<KalmanFilter>();
    private List<KalmanFilterCAM> _kalmanFiltersCAM = new List<KalmanFilterCAM>();

    private void Start()
    {
        _cameraToWorld = new CameraToWorld(Camera.main);
    }

    public void SetNewDetectionData(IEnumerable<Detection> detections)
    {
        List<Point> points = new List<Point>();

        foreach (KalmanFilter kalmanFilter in _kalmanFilters.Where(kalmanFilter => kalmanFilter == null))
        {
            _kalmanFilters.Remove(kalmanFilter);
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

    private void ProcessKalman(List<Point> points)
    {
        if (points.Count == 0)
            return;
        if (_kalmanFilters.Count == 0)
            InstantiateNewKalmanFilter();


        float[][] distanceArray = CreateDistanceArray(points);

        int kalmanFilterCount = _kalmanFilters.Count;
        for (int i = 0; i < points.Count; i++)
        {
            // Find pair of KalmanFilter and Point with smallest distance
            FindMinIndexOfMulti(distanceArray, out int kalman, out int point);

            // Send Point to KalmanFilter or Instantiate a new KalmanFilter if the distance is higher than the threshold
            KalmanFilter kalmanFilter = float.IsPositiveInfinity(distanceArray[kalman][point]) ? InstantiateNewKalmanFilter() : _kalmanFilters[kalman];
            kalmanFilter.SetNewMeasurement(points[point].Position);
           
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

    private KalmanFilter InstantiateNewKalmanFilter()
    {
        KalmanFilter kalmanFilter = Instantiate(_kalmanFilter, new Vector3(), new Quaternion()).GetComponent<KalmanFilter>();
        _kalmanFilters.Add(kalmanFilter);
        return kalmanFilter;
    }

    private float[][] CreateDistanceArray(IReadOnlyList<Point> points)
    {
        float[][] distanceMultiArray = new float[_kalmanFilters.Count][];

        for (int k = 0; k < _kalmanFilters.Count; k++)
        {
            distanceMultiArray[k] = new float[points.Count];
            
            for (int p = 0; p < points.Count; p++)
            {
                distanceMultiArray[k][p] = Vector2.Distance(_kalmanFilters[k].GetVector2Position(), points[p].Position);
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

    private void OnDrawGizmos()
    {
        //foreach (Point point in Points)
        if (Points.Count > 0)
        {
            GizmosUtils.DrawText(GUI.skin, "O", new Vector3(Points.LastOrDefault().X, 0, Points.LastOrDefault().Z), color: Color.black, fontSize: 10);
        }
    }
}
