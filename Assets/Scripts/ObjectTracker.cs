using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTracker : MonoBehaviour
{
    protected readonly List<int> LabelsToFindIndexes = new();
    
    protected CameraToWorld CameraToWorld;
    private Camera _mainCamera;

    [Header("Raycast setting")]
    protected const int MaxRayDistance = 30;

    protected void SetupObjectTracker()
    {
        FindLabels();
        _mainCamera = Camera.main;
    
        CameraToWorld = new CameraToWorld(_mainCamera);
    }

    private void FindLabels()
    {
        foreach (string label in GameManager.Instance.LabelsToFind)
        {
            LabelsToFindIndexes.Add(Array.IndexOf(Marker.Labels, label));
            //Debug.Log("labelIndex: " + Array.IndexOf(Marker.Labels, label) + " name: " + Marker.Labels[Array.IndexOf(Marker.Labels, label)]);
        }
    }
}
