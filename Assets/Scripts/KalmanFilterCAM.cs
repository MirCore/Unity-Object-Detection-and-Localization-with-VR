using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Unity.Mathematics;

[ExecuteInEditMode]
public class KalmanFilterCAM: MonoBehaviour
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

    private static float Ts;
    [SerializeField] private float Q = 1;
    [SerializeField] private float RValue = 1;

    [SerializeField] private GameObject KalmanPosition;

    private Vector3? _measurement = null;

    // Start is called before the first frame update
    private void Awake()
    {
        Ts = Time.fixedDeltaTime;
        
        F = DenseMatrix.OfArray(new double[,]
        {
            {1, Ts, math.sqrt(Ts)/2, 0, 0,  0},
            {0, 1,  Ts,              0, 0,  0},
            {0, 0,  1,               0, 0,  0},
            {0, 0,  0,               1, Ts, math.sqrt(Ts)/2},
            {0, 0,  0,               0, 1,  Ts},
            {0, 0,  0,               0, 0,  1}
        });

        FT = F.Transpose();
        
        H = DenseMatrix.OfArray(new double[,]
        {
            {1,0,0,0,0,0},
            {0,0,0,1,0,0}
        });

        HT = H.Transpose();
        
        Gd = DenseMatrix.OfArray(new double[,]
        {
            {math.sqrt(Ts)/2, 0},
            {Ts, 0},
            {1, 0},
            {0, math.sqrt(Ts)/2},
            {0, Ts},
            {0, 1}
        });

        GdQGdT = Gd * Q * Gd.Transpose();
        
        I = DenseMatrix.CreateIdentity(6);
        
        R = DenseMatrix.OfDiagonalArray(new double[] {RValue, RValue});
        
        P = DenseMatrix.OfDiagonalArray(new double[] {1000, 1000, 100, 100, 10, 10});
         
        // Initial
        x = DenseVector.OfArray(new double[] {0, 0, 0, 0, 0, 0});
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
        
        KalmanPosition.transform.position = new Vector3((float) x[0],0, (float) x[3]);
        KalmanPosition.transform.localScale = new Vector3((float)P[0,0], 0.1f, (float)P[3,3]);
    }

    private void Predict()
    {
        x = F * x;
        P = F * P * FT; //+ GdQGdT;
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
            GizmosUtils.DrawText(GUI.skin, "x", new Vector3((float)x[0], 0, (float)x[3]), color: Color.black, fontSize: 10);
    }
}
