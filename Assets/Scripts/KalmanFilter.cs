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
    private static Matrix<double> G;
    private static Matrix<double> Gd;
    private static Matrix<double> I;
    private static Matrix<double> R;
    private static Matrix<double> GdQGdT;
    private static Matrix<double> Q;
    public Matrix<double> P { get; private set; }
    public Vector<double> x { get; private set; }

    //private Vector<double> xtilde;
    //private Matrix<double> Ptilde;
    
    private static float Ts;

    [SerializeField] private GameObject KalmanPosition;
    [SerializeField] private Renderer Hologram;

    [SerializeField] private bool SelfDestruct = true;       
    [SerializeField] private int SelfDestructDistance = 500;

    private Vector2 PositionMeanWithoutKalman;
    private List<Vector2> LastPositions = new ();


    private Vector2? _measurement = null;

    // Start is called before the first frame update
    private void Awake()
    {
        double omegaSquared = GameManager.Instance.GetOmegaSquared();
       
        Ts = Time.fixedDeltaTime;
        
        // State-Transition:
        F = DenseMatrix.OfArray(new double[,]
        {
            {1,0,Ts,0},
            {0,1,0,Ts},
            {0,0,1,0},
            {0,0,0,1}
        });

        FT = F.Transpose();
        
        // 
        H = DenseMatrix.OfArray(new double[,]
        {
            {1,0,0,0},
            {0,1,0,0}
        });

        HT = H.Transpose();

        Gd = DenseMatrix.OfArray(new double[,]
        {
            {Ts*Ts/2, 0},
            {0, Ts*Ts/2},
            {Ts, 0},
            {0, Ts}
        });

        GdQGdT = Gd * omegaSquared * Gd.Transpose();

        I = DenseMatrix.CreateIdentity(4);
        
        R = DenseMatrix.OfDiagonalArray(new double[] {GameManager.Instance.GetRx(), GameManager.Instance.GetRy()});
        
        P = DenseMatrix.OfDiagonalArray(new double[] {100, 100, 10, 10});
         
        // Initial
        x = DenseVector.OfArray(new double[] {0, 0, 0, 0});

        KalmanManager.Instance.SetNewKalmanFilter(this);
    }

    private void Start()
    {
        Random.InitState(42);
        Hologram.material.color =  Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
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
            LastPositions.Add((Vector2) _measurement);
            CalculateMeanPositionWithoutKalman();
            UpdatePrediction((Vector2) _measurement);
            _measurement = null;
        }
        
        UpdateKalmanGameObject();

        if (!SelfDestruct) return;
        if (P[0,0] > SelfDestructDistance)
        {
            KalmanManager.Instance.RemoveKalmanFilter(this);
            Destroy(gameObject);
        }
    }

    private void CalculateMeanPositionWithoutKalman()
    {
       PositionMeanWithoutKalman = new Vector2( LastPositions.Average(pos=>pos.x),LastPositions.Average(pos=>pos.y));
       while (LastPositions.Count > 4)
       {
           LastPositions.RemoveAt(0);
       }
    }

    private void UpdateKalmanGameObject()
    {
        KalmanPosition.transform.position = new Vector3((float)x[0], 0, (float)x[1]);
        KalmanPosition.transform.localScale = new Vector3((float)P[0, 0], 0.1f, (float)P[1, 1]);
        Debug.DrawLine(transform.position, transform.position + new Vector3((float)x[2], 0, (float)x[3] ), Color.red);
    }

    private void Predict()
    {
        x = F * x;
        P = F * P * FT + GdQGdT;
    }

    private void UpdatePrediction(Vector2 measurement)
    {
        DenseVector z = DenseVector.OfArray(new double[] {measurement.x, measurement.y}); 
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
        _measurement = measurement;
    }
    
    private void OnDrawGizmos()
    {
        if (x == null)
            return;
        
        GizmosUtils.DrawText(GUI.skin, "x", new Vector3((float)x[0], 0, (float)x[1]), color: Color.black, fontSize: 10);
    }

    public Vector2 GetVector2Position()
    {
        Vector3 position = transform.position;
        return new Vector2(position.x, position.z);
    }

    public Vector2 GetVector2Velocity()
    {
        return new Vector2((float)x[2], (float)x[3]);
    }

    public Vector2 GetP()
    {
        return new Vector2((float)P[0, 0], (float)P[1, 1]);
    }

    public Vector2 GetMeasurement()
    {
        if (_measurement != null)
            return (Vector2)_measurement;
        return Vector2.zero;
    }

    public Vector2 GetPositionMeanWithoutKalman()
    {
        return PositionMeanWithoutKalman;
    }
}
