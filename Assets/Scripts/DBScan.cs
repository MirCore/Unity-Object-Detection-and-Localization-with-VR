using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public static class DBScan
{
    public static List<Cluster> Main(List<Point> points, double eps, int minPts)
    {
        if (points == null)
            return null;
        
        eps *= eps; // square eps

        return GetClusters(points, eps, minPts);
    }

    private static List<Cluster> GetClusters(List<Point> points, double eps, int minPts)
    {
        //List<List<Point>> clusters = new();
        List<Cluster> clusters = new();
        
        int clusterId = 1;
        foreach (Point p in points)
        {
            if (p.ClusterId != Point.UNCLASSIFIED)
                continue;

            if (ExpandCluster(points, p, clusterId, eps, minPts))
            {
                clusters.Add(new Cluster{Id = clusterId});
                clusterId++;
            }
        }

        return clusters;
    }

    private static List<Point> GetRegion(IEnumerable<Point> points, Point p, double eps)
    {
        return points.Where(otherPoint => DistanceSquared(p, otherPoint) <= eps).ToList();
    }

    private static float DistanceSquared(Point p1, Point p2)
    {
        float diffX = p2.X - p1.X;
        float diffY = p2.Z - p1.Z;
        return diffX * diffX + diffY * diffY;
    }

    private static bool ExpandCluster(IReadOnlyCollection<Point> points, Point p, int clusterId, double eps, int minPts)
    {
        Profiler.BeginSample("GetRegion1");

        List<Point> seeds = GetRegion(points, p, eps);

        Profiler.EndSample();

        if (seeds.Count < minPts) // no core point
        {
            p.ClusterId = Point.NOISE;
            return false;
        }
        
        // all points in seeds are density reachable from point 'p'

        foreach (Point point in seeds)
            point.ClusterId = clusterId;
        seeds.Remove(p);

        while (seeds.Count > 0)
        {
            Point currentP = seeds.First();
            Profiler.BeginSample("GetRegion2");
            
            List<Point> result = GetRegion(points, currentP, eps);
            Profiler.EndSample();
            if (result.Count >= minPts)
            {
                foreach (Point resultP in result.Where(resultP => resultP.ClusterId is Point.UNCLASSIFIED or Point.NOISE))
                {
                    if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                    resultP.ClusterId = clusterId;
                }
            }
            seeds.Remove(currentP);
        }
        
        return true;
    }
    
}

public class DBScanMono : MonoBehaviour
{
    public void Main(DetectedObject detectedObjects, double eps, int minPts, int index, List<int> coroutineList)
    {
        if (detectedObjects.DetectedPoints == null)
            return;
        
        eps *= eps; // square eps
        StartCoroutine(GetClusters(detectedObjects, eps, minPts, index, coroutineList));
    }

    private static IEnumerator GetClusters(DetectedObject detectedObjects, double eps, int minPts, int index, ICollection<int> coroutineList)
    {
        coroutineList.Add(index);

        List<Point> points = detectedObjects.DetectedPoints;

        List<Cluster> clusters = detectedObjects.Clusters;
        
        int clusterId = 1;
        foreach (Point p in points)
        {
            if (p.ClusterId != Point.UNCLASSIFIED)
                continue;

            float startTime = Time.realtimeSinceStartup;
            Profiler.BeginSample("GetRegion1");
            List<Point> seeds = GetRegion(points, p, eps);
            Profiler.EndSample();
            if( Time.realtimeSinceStartup - startTime > 0.005f )
                yield return null;
            
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = Point.NOISE;
                continue;
            }
        
            // all points in seeds are density reachable from point 'p'

            foreach (Point point in seeds)
                point.ClusterId = clusterId;
            seeds.Remove(p);
            
            startTime = Time.realtimeSinceStartup;
            while (seeds.Count > 0)
            {
                Point currentP = seeds.First();
                
                Profiler.BeginSample("GetRegion2");
                List<Point> result = GetRegion(points, currentP, eps);
                Profiler.EndSample();
                
                if( Time.realtimeSinceStartup - startTime > 0.005f )
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }

                if (result.Count >= minPts)
                {
                    foreach (Point resultP in result.Where(resultP => resultP.ClusterId is Point.UNCLASSIFIED or Point.NOISE))
                    {
                        if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                        resultP.ClusterId = clusterId;
                    }
                }
                seeds.Remove(currentP);
            }

            clusters.Add(new Cluster{Id = clusterId});
            clusterId++;
        }
        
        coroutineList.Remove(index);
    }

    private static List<Point> GetRegion(IEnumerable<Point> points, Point p, double eps)
    {
        return points.Where(otherPoint => DistanceSquared(p, otherPoint) <= eps).ToList();
    }

    private static float DistanceSquared(Point p1, Point p2)
    {
        float diffX = p2.X - p1.X;
        float diffY = p2.Z - p1.Z;
        return diffX * diffX + diffY * diffY;
    }

}