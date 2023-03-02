using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YoloV4Tiny;

public class ObjectTrackerClustering : ObjectTracker
{
    private readonly List<DetectedObject> _detectedObjects = new();

    private PlacedObject _detectedObjectPrefab;

    [SerializeField] private bool UseObjectModels = true;
    
    [Header("Clustering settings")]
    [SerializeField] private double Eps = 1;
    [SerializeField] private int MinPts = 3;
    
    [Header("Lifetimes (seconds)")]
    [SerializeField] private int GeneralPointLifetime = 30;
    [SerializeField] private int NoiseLifetime = 5;
    [SerializeField] private int ObjectLifetime = 30;
    
    private DBScanMono _dbScanMono;

    private readonly List<int> _listOfRunningClusteringCoroutines = new();


    private void Start()
    {
        SetupObjectTracker();
        
        _dbScanMono = gameObject.AddComponent<DBScanMono>();
        _detectedObjectPrefab = ObjectModelManager.Instance.GetPrefabDefinition();
        
        // Initialize Lists
        for (int i = 0; i < Marker.Labels.Length; i++)
        {
            _detectedObjects.Add(new DetectedObject());
        }

    }

    private void Update()
    {
        if (_listOfRunningClusteringCoroutines.Count == 0)
        {
            ProcessObjectType();
            CleanOldPoints();
            ClusterPoints();
        }
    }

    private void ClusterPoints()
    {
        foreach (int index in LabelsToFindIndexes)
        {
            _detectedObjects[index].DetectedPoints.AddRange(_detectedObjects[index].NewDetectedPoints);
            _detectedObjects[index].NewDetectedPoints.Clear();
            _detectedObjects[index].Clusters.Clear();
            _dbScanMono.Main(_detectedObjects[index], Eps, MinPts, index, _listOfRunningClusteringCoroutines); // Cluster points
        }
    }
    
    private void ProcessObjectType()
    {
        CalculateCenterAndDimensions(); // Calculate Center and Dimensions of each Cluster
        foreach (int labelIndex in LabelsToFindIndexes)
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
                    closestObject.ToggleModel(UseObjectModels);
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
                cluster.W = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id)
                    .Average(point => point.W);
                cluster.H = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id)
                    .Average(point => point.H);

                float x = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id)
                    .Average(point => point.X);
                float y = detectedObject.DetectedPoints.Where(p => p.ClusterId == cluster.Id)
                    .Average(point => point.Z);

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
            if (!LabelsToFindIndexes.Contains((int) detection.classIndex)) // Filter for detected objects by Label
                return;
            
            CameraToWorld.ProcessDetection(detection, MaxRayDistance, GameManager.Instance.StereoImage, out Point p); // Raycast from Camera to Floor
            
            if (p != null)
                _detectedObjects[(int) detection.classIndex].NewDetectedPoints.Add(p); // Write hit points to _detectedObjects List
        }
    }

    private void OnDrawGizmos()
    {
        foreach (DetectedObject detectedObject in _detectedObjects)
        {
            int clusterCount = detectedObject.Clusters.Count + 1;
            foreach (Point point in detectedObject.DetectedPoints)
            {
                Color color = Color.HSVToRGB((float) point.ClusterId / clusterCount, 1f, 1f);
                GizmosUtils.DrawText(GUI.skin, point.ClusterId.ToString(), new Vector3(point.X, 0, point.Z), color, 10);
            }
            
        }
    }
}
