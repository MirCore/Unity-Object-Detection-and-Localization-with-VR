using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Unity.Mathematics;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class KalmanFilter : MonoBehaviour
{
    private static Matrix<double> F;
    private static Matrix<double> FT;
    private static Matrix<double> H;
    private static Matrix<double> HT;
    private static Matrix<double> Gd;
    private static Matrix<double> I;
    private static Matrix<double> R;
    private static Matrix<double> GdQGdT;
    private Matrix<double> P;
    private Vector<double> x;

    //private Vector<double> xtilde;
    //private Matrix<double> Ptilde;
    
    private static float Ts = 1 / 60;
    [SerializeField] private float Q = 1;
    [SerializeField] private float RValue = 1;

    [SerializeField] private GameObject KalmanPosition;
    [SerializeField] private Renderer Hologram;

    [SerializeField] private bool SelfDestruct = true;

    private Vector3? _measurement = null;

    // Start is called before the first frame update
    private void Awake()
    {
        Ts = Time.fixedDeltaTime;
        
        F = DenseMatrix.OfArray(new double[,]
        {
            {1,0,Ts,0},
            {0,1,0,Ts},
            {0,0,1,0},
            {0,0,0,1}
        });

        FT = F.Transpose();
        
        H = DenseMatrix.OfArray(new double[,]
        {
            {1,0,0,0},
            {0,1,0,0}
        });

        HT = H.Transpose();
        
        Gd = DenseMatrix.OfArray(new double[,]
        {
            {math.sqrt(Ts)/2, 0},
            {0, math.sqrt(Ts)/2},
            {Ts, 0},
            {0, Ts}
        });

        GdQGdT = Gd * Q * Gd.Transpose();
        
        I = DenseMatrix.CreateIdentity(4);
        
        R = DenseMatrix.OfDiagonalArray(new double[] {RValue, RValue});
        
        P = DenseMatrix.OfDiagonalArray(new double[] {1000, 1000, 1000, 1000});
         
        // Initial
        x = DenseVector.OfArray(new double[] {0, 0, 0, 0});
        //xtilde = x;
        //Ptilde = P;
    }

    private void Start()
    {
        Hologram.material.color =  Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
    }

    private void UpdateMatrix(float time)
    {
        F = DenseMatrix.OfArray(new double[,]
        {
            {1,0,time,0},
            {0,1,0,time},
            {0,0,1,0},
            {0,0,0,1}
        });

        Gd = DenseMatrix.OfArray(new double[,]
        {
            {math.sqrt(time)/2, 0},
            {0, math.sqrt(time)/2},
            {time/2, 0},
            {0, time/2}
        }); 
        
        GdQGdT = Gd * Q * Gd.Transpose();
    }

    private void OnValidate()
    {
        if (GdQGdT != null)
        {
            GdQGdT = Gd * Q * Gd.Transpose();
        }
    }

    private void FixedUpdate()
    {
        // Predict
        Predict();
        // Update
        if (_measurement != null)
        {
            UpdatePrediction((Vector3)_measurement);
            _measurement = null;
        }
        else
        {
            //x = xtilde;
            //P = Ptilde;
        }
        
        KalmanPosition.transform.position = new Vector3((float) x[0],0, (float) x[1]);
        KalmanPosition.transform.localScale = new Vector3((float)P[0,0], 0.1f, (float)P[1,1]);

        if (!SelfDestruct) return;
        if (P[0,0] > 100)
        {
            Destroy(gameObject);
        }
    }

    private void Predict()
    {
        x = F * x;
        P = F * P * FT + GdQGdT;
    }

    private void UpdatePrediction(Vector3 measurement)
    {
        DenseVector z = DenseVector.OfArray(new double[] {measurement.x, measurement.z}); 
        Vector<double> y = z - H * x;
        Matrix<double> PHT = P * HT;
        Matrix<double> S = H * PHT + R;
        Matrix<double> K = PHT * S.Inverse();
        x += K * y;
        // P = (I - K * H) * Ptilde;
        // more numerically stable version:
        // P = (I - KH) * P * (I-KH).T + KRK.T
        Matrix<double> I_KH = I - K * H;
        P = I_KH * P * I_KH.Transpose() + K * R * K.Transpose();
    }
    
    public void SetNewMeasurement(Vector2 measurement)
    {
        _measurement = new Vector3(measurement.x, 0, measurement.y);
    }
    
    private void OnDrawGizmos()
    {
        if (x != null)
            GizmosUtils.DrawText(GUI.skin, "x", new Vector3((float)x[0], 0, (float)x[1]), color: Color.black, fontSize: 10);
    }

    public Vector2 GetVector2Position()
    {
        Vector3 position = transform.position;
        return new Vector2(position.x, position.z);
    }
}
