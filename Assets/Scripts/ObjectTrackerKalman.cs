using System.Collections.Generic;
using System.Linq;
using Kalman;
using UnityEngine;
using YoloV4Tiny;

public class ObjectTrackerKalman : ObjectTracker
{
    private readonly List<List<Point>> _gizmoList = new ();


    private void Start()
    {
        SetupObjectTracker();
    }

    public void SetNewDetectionData(IEnumerable<Detection> detections)
    {
        List<Point> points = new();
        
        foreach (Detection detection in detections)
        {
            if (!LabelsToFindIndexes.Contains((int) detection.classIndex)) // Filter for detected objects by Label
                return;
            
            CameraToWorld.ProcessDetection(detection, MaxRayDistance, GameManager.Instance.StereoImage, out Point p); // Raycast from Camera to Floor

            if (p != null)
            {
                points.Add(p);
            }
                
        }

        if (points.Count == 0)
            return;

        //if (points.Count == 1)
        //    KalmanStatisticsUtils.MeasureNoise(points[0].Position);

        ProcessKalman(points);
        _gizmoList.Add(new List<Point>(points));
    }

    public void SetNewSimulationData(List<Point> points)
    {
        if (points.Count == 0)
            return;

        ProcessKalman(points);
    }

    private void OnDestroy()
    {
        //print(KalmanStatisticsUtils.CalculateNoise());
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
        if (!GameManager.Instance.ShowMeasurementGizmos)
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