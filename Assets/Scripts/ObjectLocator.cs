using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YoloV4Tiny;

public class ObjectLocator : MonoBehaviour
{
    private readonly List<DetectedObject> _detectedObjects = new();

    private PlacedObject _detectedObjectPrefab;

    [Header("Object-detection settings")]
    [SerializeField] private List<string> LabelsToFind;
    private readonly List<int> _labelsToFindIndexes = new();
    
    [Header("Raycast setting")]
    [SerializeField] private int MaxRayDistance = 3;
    
    [Header("Clustering settings")]
    [SerializeField] private double Eps = 1;
    [SerializeField] private int MinPts = 3;
    
    [Header("Lifetimes (seconds)")]
    [SerializeField] private int GeneralPointLifetime = 30;
    [SerializeField] private int NoiseLifetime = 5;
    [SerializeField] private int ObjectLifetime = 30;
    
    private CameraToWorld _cameraToWorld;
    private DBScanMono _dbScanMono;

    private void Start()
    {
        _cameraToWorld = new CameraToWorld(Camera.main);
        _dbScanMono = gameObject.AddComponent<DBScanMono>();
        _detectedObjectPrefab = ObjectModelManager.Instance.GetPrefabDefinition();
        
        // Initialize Lists
        for (int i = 0; i < Marker.Labels.Length; i++)
        {
            _detectedObjects.Add(new DetectedObject());
        }

        foreach (string label in LabelsToFind)
        {
            _labelsToFindIndexes.Add(Array.IndexOf(Marker.Labels, label));
            Debug.Log("labelIndex: " + Array.IndexOf(Marker.Labels, label) + " name: " + Marker.Labels[Array.IndexOf(Marker.Labels, label)]);
        }

        InvokeRepeating(nameof(ClusterPoints), 0, 1);
        InvokeRepeating(nameof(ProcessObjectType), 0.5f, 1);
    }
    
    private void ClusterPoints()
    {
        CleanOldPoints();
        foreach (int index in _labelsToFindIndexes)
        {
            _detectedObjects[index].DetectedPoints.AddRange(_detectedObjects[index].NewDetectedPoints);
            _detectedObjects[index].NewDetectedPoints.Clear();
            _detectedObjects[index].Clusters = _dbScanMono.Main(_detectedObjects[index].DetectedPoints, Eps, MinPts); // Cluster points
        }
    }
    
    private void ProcessObjectType()
    {
        CalculateCenterAndDimensions(); // Calculate Center and Dimensions of each Cluster
        foreach (int labelIndex in _labelsToFindIndexes)
        {

            // For each cluster, get closest placedObject (if any) and update position and size
            // If no close placedObject was found spawn a new one
            List<PlacedObject> placedObjects = new(_detectedObjects[labelIndex].PlacedObjects);
            foreach (Cluster cluster in _detectedObjects[labelIndex].Clusters)
            {
                GetClosestObject(cluster.Center, placedObjects, out PlacedObject closestObject, out float distance);

                if (distance > 1f || closestObject == null)
                {
                    PlacedObject newPlacedObject = Instantiate(_detectedObjectPrefab);
                    _detectedObjects[labelIndex].PlacedObjects.Add(newPlacedObject);
                    newPlacedObject.SetType(labelIndex);
                    newPlacedObject.SetNewPositionAndScale(cluster);
                }
                else
                {
                    closestObject.SetNewPositionAndScale(cluster);
                    placedObjects.Remove(closestObject);
                }
            }

            // Destroy PlacedObjects that are still in the list, therefore are not detected anymore
            foreach (PlacedObject placedObject in placedObjects)
            {
                if (placedObject.TriggerDestroy(ObjectLifetime))
                    _detectedObjects[labelIndex].PlacedObjects.Remove(placedObject);
            }
        }
    }

    private static void GetClosestObject(Vector2 point, List<PlacedObject> tempDetectedObject, out PlacedObject closestObject, out float distance)
    {
        distance = Mathf.Infinity;
        closestObject = null;

        foreach (PlacedObject o in tempDetectedObject)
        {
            if (distance <= o.GetDistanceToPoint(point))
                continue;
            distance = o.GetDistanceToPoint(point);
            closestObject = o;
        }
    }

    /// <summary>
    /// Calculates average position and dimensions for all points of each cluster
    /// </summary>
    private void CalculateCenterAndDimensions()
    {
        foreach (DetectedObject detectedObject in _detectedObjects)
        {
            foreach (Cluster cluster in detectedObject.Clusters)
            {
                cluster.W = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id).Average(point => point.W);
                cluster.H = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id).Average(point => point.H);

                float x = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id).Average(point => point.X);
                float y = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id).Average(point => point.Z);

                cluster.Center = new Vector2(x, y);
            }
        }
    }

    private void CleanOldPoints()
    {
        DateTime timestamp = DateTime.Now.AddSeconds(-GeneralPointLifetime);
        DateTime noiseGate = DateTime.Now.AddSeconds(-NoiseLifetime);
        foreach (DetectedObject detectedObject in _detectedObjects)
        {
            detectedObject.DetectedPoints.RemoveAll(p => p.ClusterId == Point.NOISE && p.Timestamp < noiseGate);
            detectedObject.DetectedPoints.RemoveAll(p => p.Timestamp < timestamp);
            foreach (Point point in detectedObject.DetectedPoints)
            {
                point.ClusterId = Point.UNCLASSIFIED;
            }
        }
    }

    public void SetNewDetectionData(IEnumerable<Detection> detections)
    {
        foreach (Detection detection in detections)
        {
            if (!_labelsToFindIndexes.Contains((int) detection.classIndex)) // Filter for detected objects by Label
                return;
            
            _cameraToWorld.ProcessDetection(detection, MaxRayDistance, out Point p); // Raycast from Camera to Floor
            
            if (p != null)
                _detectedObjects[(int) detection.classIndex].NewDetectedPoints.Add(p); // Write hit points to _detectedObjects List
        }
    }

    public List<DetectedObject> GetDetectedObjects()
    {
        return _detectedObjects;
    }
}

public class DetectedObject
{
    public List<Cluster> Clusters = new(); // Clusters (containing sorted Points)
    public readonly List<Point> DetectedPoints = new(); // unsorted Points
    public readonly List<Point> NewDetectedPoints = new(); // unsorted Points
    public readonly List<PlacedObject> PlacedObjects = new(); // GameObjects representing the detection
}

// ReSharper disable InconsistentNaming
public class Point
{
    public float X, Z, W, H;
    public DateTime Timestamp;
    
    public const int NOISE = -1;
    public const int UNCLASSIFIED = 0;
    public int ClusterId;
}

public class Cluster
{
    public int Id;
    public Vector2 Center;
    public float W, H;
}