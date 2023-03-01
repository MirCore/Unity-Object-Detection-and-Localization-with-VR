using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Utils
{
    public static void FindMinIndexOfMulti(IReadOnlyList<float[]> arr, out int index, out int element)
    {
        index = 0;
        element = 0;
        for (int i = 0; i < arr.Count; i++)
        {
            for (int e = 0; e < arr[i].Length; e++)
            {
                if (float.IsNaN(arr[i][e])) continue;
                if (!(arr[i][e] < arr[index][element]) && !float.IsNaN(arr[index][element])) continue;

                index = i;
                element = e;
            }
        }
    }
    
    public static void RemoveUsedPairsInDistanceArray(int aCount, int bCount, IReadOnlyList<float[]> distanceArray, int a,
        int b)
    {
        for (int x = 0; x < bCount; x++)
        {
            distanceArray[x][a] = float.NaN;
        }

        for (int y = 0; y < aCount; y++)
        {
            distanceArray[b][y] = float.NaN;
        }
    }

    public static float GenerateGaussianRandom(float mu, float sigma)
    {
        float rand1 = Random.Range(0.0f, 1.0f);
        float rand2 = Random.Range(0.0f, 1.0f);

        float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);

        return (mu + sigma * n);
    }
}

public class Point
{
    private float _x;
    private float _z;
    public float W, H;

    public float X
    {
        get => _x;
        set
        {
            _x = value;
            _position.x = value;
        }
    }
    public float Z
    {
        get => _z;
        set
        {
            _z = value;
            _position.y = value;
        }
    }
    
    private Vector2 _position;

    public Vector2 Position
    {
        get => _position;
        set {
            _position = value;
            X = value.x;
            Z = value.y;
        }
    }
    public DateTime Timestamp;
    
    public const int NOISE = -1;
    public const int UNCLASSIFIED = 0;
    public int ClusterId;
}

public class KalmanState
{
    public Vector2 GroundTruthPosition;
    public Vector2 GroundTruthVelocity;
    public Vector2 KalmanPosition;
    public Vector2 KalmanVelocity;
    public Vector2 KalmanP;
    public Vector2 Measurement;
    public float Time;
    public int Frame;
}

public class DetectedObject
{
    public List<Cluster> Clusters = new(); // Clusters (containing sorted Points)
    public readonly List<Point> DetectedPoints = new(); // unsorted Points
    public readonly List<Point> NewDetectedPoints = new(); // unsorted Points
    public readonly List<PlacedObject> PlacedObjects = new(); // GameObjects representing the detection
}

public class Cluster
{
    public int Id;
    public Vector2 Center;
    public float W, H;
}