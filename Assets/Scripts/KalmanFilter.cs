using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

public class KalmanFilter : MonoBehaviour
{
    private float[,] F;
    private float[,] H;
    private float Ts;

    // Start is called before the first frame update
    void Start()
    {
        Ts = 0.1f;
        F = new float[4,4] {{1,0,Ts,0},{0,1,0,Ts},{0,0,1,0},{0,0,0,1}};
        H = new float[2,4] {{1,0,0,0},{0,1,0,0}};
        Debug.Log(F*H);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
