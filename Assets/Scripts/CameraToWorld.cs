using System;
using UnityEngine;
using UnityEngine.Profiling;
using YoloV4Tiny;

public class CameraToWorld
{
    private readonly Camera _camera;

    public CameraToWorld(Camera camera)
    {
        _camera = camera;
    }

    private bool RaycastDetection(Detection detection, float maxRayDistance, bool stereoImage, out Vector3 point, out float width)
    {
        width = 0;
        point = new Vector3();

        float x = detection.x;
        float w = detection.w;

        if (stereoImage) // * 2 because of Valve Index double lens image
        {
            x *= 2;
            w *= 2;
        }

        int pixelWidth = _camera.pixelWidth;
        float x1CameraSpace = (x + w) * pixelWidth;
        float x2CameraSpace = (x - w)  * pixelWidth;
        float yCameraSpace = (1 - (detection.y + detection.h / 2)) * _camera.pixelHeight;
        
        Ray ray1 = _camera.ScreenPointToRay(new Vector2(x1CameraSpace, yCameraSpace));
        Ray ray2 = _camera.ScreenPointToRay(new Vector2(x2CameraSpace, yCameraSpace));

        if (!RayPlaneIntersection(ray1, out float t1))
            return false;
        if (!RayPlaneIntersection(ray2, out float t2)) 
            return false;
        if (t1 > maxRayDistance || t2 > maxRayDistance)
            return false;
        point = (ray1.GetPoint(t1) + ray2.GetPoint(t2)) / 2;
        width = Vector3.Distance(ray1.GetPoint(t1), ray2.GetPoint(t2)) / 2;
        
        /*if (!Physics.Raycast(ray1, out RaycastHit hit1, maxRayDistance, 1 << LayerMask.NameToLayer("Floor")))
            return false;
        if (!Physics.Raycast(ray2, out RaycastHit hit2, maxRayDistance, 1 << LayerMask.NameToLayer("Floor")))
            return false;
        point = (hit1.point + hit2.point) / 2;
        width = Vector3.Distance(hit1.point, hit2.point);*/

        return true;
    }

    private static bool RayPlaneIntersection(Ray ray, out float t)
    {
        t = 0;
        float denominator = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denominator) < Mathf.Epsilon)
            return false;
        t = Vector3.Dot(-ray.origin, Vector3.up) / denominator;
        return t > 0;
    }

    public void ProcessDetection(Detection detection, int maxRayDistance, bool stereoImage, out Point p)
    {
        Profiler.BeginSample("RaycastDetection");
        
        p = null;
        
        if (stereoImage)
            if (detection.x >= 0.5) // Filter out detected objects in right camera image
                return;
        if (!RaycastDetection(detection, maxRayDistance, stereoImage, out Vector3 point, out float width))
            return;

        Vector3 hitDirection = point - _camera.transform.position;
        hitDirection.y = 0;
        hitDirection.Normalize();
        
        point += hitDirection * (width / 4); // move the point backwards by half the width
        float height = (detection.h * width) / (2 * detection.w);
        
        p =  new Point
        {
            X = point.x,
            Z = point.z,
            W = width,
            H = height,
            Timestamp = DateTime.Now
        };

        Profiler.EndSample();
    }
}