using System;
using System.Collections.Generic;
using System.Linq;
using Kalman;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using YoloV4Tiny;

public class ObjectTracker : MonoBehaviour
{
    [Header("Object-detection settings")]
    [SerializeField] private bool StereoImage = false;
    
    [Header("Raycast setting")]
    private int MaxRayDistance = 30;
    
    private CameraToWorld _cameraToWorld;
    
    private readonly List<List<Point>> _gizmoList = new ();
    private Camera Camera1;

    private readonly List<Vector2> _measurements = new();

    private void Start()
    {
        Camera1 = Camera.main;
    }

    public void SetNewDetectionData(IEnumerable<Detection> detections)
    {
        List<Point> points = new();
        _cameraToWorld ??= new CameraToWorld(Camera1);
        
        foreach (Detection detection in detections)
        {
            if (detection.classIndex is not (14 or 8 or 6)) // Filter for detected objects by Label
                continue;
            
            _cameraToWorld.ProcessDetection(detection, MaxRayDistance, StereoImage, out Point p); // Raycast from Camera to Floor

            if (p != null)
            {
                points.Add(p);
            }
                
        }

        if (points.Count == 0)
            return;

        if (points.Count == 1)
            MeasureNoise(points[0].Position);

        ProcessKalman(points);
        _gizmoList.Add(new List<Point>(points));
    }

    public void SetNewSimulationData(List<Point> points)
    {
        if (points.Count == 0)
            return;

        ProcessKalman(points);
    }

    private void MeasureNoise(Vector2 point)
    {
        if (GameManager.Instance.SimulatedObjects.Count == 0)
            return;
        Vector2 realPosition = GameManager.Instance.SimulatedObjects.First().PositionVector2;
        Vector2 delta = new (point.x - realPosition.x, point.y - realPosition.y);
        _measurements.Add(delta);
    }

    private void CalculateNoise()
    {
        Vector2 measurementsSum = new ();
        measurementsSum = _measurements.Aggregate(measurementsSum, (current, measurement) => current + measurement);

        Vector2 mean = measurementsSum/_measurements.Count;
        float meanXY = (mean.x + mean.y)/2;
        float xSum = 0;
        float ySum = 0;
        float xySum = 0;
        foreach (Vector2 measurement in _measurements)
        {
            float x = measurement.x - mean.x;
            float y = measurement.y - mean.y;
            float xy = measurement.x + measurement.y - meanXY;
            x *= x;
            y *= y;
            xy *= xy;
            xSum += x;
            ySum += y;
            xySum += xy;
        }

        float xStandardDeviation = xSum / _measurements.Count;
        float yStandardDeviation = ySum / _measurements.Count;
        float xyStandardDeviation = xySum / (2 * _measurements.Count);
        
        print("x: " + xStandardDeviation + " y: " + yStandardDeviation + " xy: " + xyStandardDeviation);
    }

    private void OnDestroy()
    {
        CalculateNoise();
    }

    private static void ProcessKalman(IReadOnlyList<Point> points)
    {
        if (points.Count == 0)
            return;
        if (KalmanManager.Instance.KalmanFilters.Count == 0)
            InstantiateKalman(points[0].Position);
        
        float[][] distanceArray = CreateDistanceArray(points);

        int kalmanFilterCount = KalmanManager.Instance.KalmanFilters.Count;
        for (int i = 0; i < points.Count; i++)
        {
            // Find pair of KalmanFilter and Point with smallest distance
            Utils.FindMinIndexOfMulti(distanceArray, out int kalman, out int point);

            // Send Point to KalmanFilter or Instantiate a new KalmanFilter if the distance is higher than the threshold
            if (float.IsPositiveInfinity(distanceArray[kalman][point]) || float.IsNaN(distanceArray[kalman][point]))
            {
                InstantiateKalman(points[point].Position);
            }
            else
            {
                KalmanManager.Instance.KalmanFilters[kalman].SetNewMeasurement(points[point].Position);
            
                // Set the distance of the used KalmanFilter and Point to NaN in the distanceArray
                Utils.RemoveUsedPairsInDistanceArray(points.Count, kalmanFilterCount, distanceArray, point, kalman);
            }
        }
    }
    
    private static void InstantiateKalman(Vector2 position)
    {
        KalmanManager.Instance.InstantiateNewKalmanFilter(position);
        KalmanManager.Instance.KalmanFilters.LastOrDefault()?.SetNewMeasurement(position);
    }

    private static float[][] CreateDistanceArray(IReadOnlyList<Point> points)
    {
        float[][] distanceMultiArray = new float[KalmanManager.Instance.KalmanFilters.Count][];

        for (int k = 0; k < KalmanManager.Instance.KalmanFilters.Count; k++)
        {
            distanceMultiArray[k] = new float[points.Count];
            
            for (int p = 0; p < points.Count; p++)
            {
                float distance = Vector2.Distance(KalmanManager.Instance.KalmanFilters[k].GetVector2Position(), points[p].Position);
                //distanceMultiArray[k][p] = distance < GameManager.Instance.MaxKalmanMeasurementDistance ? distance : float.PositiveInfinity;
                float maxOfP = KalmanManager.Instance.KalmanFilters[k].GetP().magnitude;
                float currentSpeed = KalmanManager.Instance.KalmanFilters[k].GetVector2Velocity().magnitude;
                distanceMultiArray[k][p] = distance < (currentSpeed + 2 * maxOfP) ? distance : float.PositiveInfinity;
            }
        }

        return distanceMultiArray;
    }
    
    private void OnDrawGizmos()
    {
        while (_gizmoList.Count > 500)
        {
            _gizmoList.RemoveAt(0);
        }
        
        if (!GameManager.Instance)
            return;
        if (!GameManager.Instance.Messpunkte)
            return;
        
        int count = _gizmoList.Count - 1;
        for (int i = count; i >= 0 ; i--)
        {
            Color color = Color.HSVToRGB(0, 1f / count * i, 1);
            foreach (Point point in _gizmoList[i])
            {
                GizmosUtils.DrawText(GUI.skin, "+", new Vector3(point.X, 0, point.Z), color: color, fontSize: 10);
            }
        }
    }
}
