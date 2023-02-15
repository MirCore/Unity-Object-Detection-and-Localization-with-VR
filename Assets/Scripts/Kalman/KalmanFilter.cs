using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class KalmanFilter : MonoBehaviour
{
    public Matrix<double> F;
    public Matrix<double> FT;
    public Matrix<double> H;
    public Matrix<double> HT;
    public Matrix<double> Gd;
    public Matrix<double> I;
    public Matrix<double> R;
    public Matrix<double> GdQGdT;
    public Matrix<double> Q;
    public Matrix<double> P { get; set; }
    public Vector<double> x { get; set; }

    public float Ts;

    private GameObject KalmanPosition;
    public Renderer Hologram;

    [SerializeField] private bool SelfDestruct = true;       
    [SerializeField] private int SelfDestructDistance = 500;
    
    private List<Vector3> KalmanPositions = new ();

    private Vector2 PositionMeanWithoutKalman;
    private List<Vector2> LastMeasurementPositions = new ();


    private Vector2? _measurement = null;
    private Color _color;

    private void Awake()
    {
        PopulateMatrices(GameManager.Instance.GetOmegaSquared());

        KalmanManager.Instance.SetNewKalmanFilter(this);
    }

    private void PopulateMatrices(double omegaSquared)
    {
        Ts = Time.fixedDeltaTime;

        // State-Transition:
        F = DenseMatrix.OfArray(new double[,]
        {
            { 1, 0, Ts, 0 },
            { 0, 1, 0, Ts },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        });

        FT = F.Transpose();

        // 
        H = DenseMatrix.OfArray(new double[,]
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 }
        });

        HT = H.Transpose();

        Gd = DenseMatrix.OfArray(new double[,]
        {
            { Ts * Ts / 2, 0 },
            { 0, Ts * Ts / 2 },
            { Ts, 0 },
            { 0, Ts }
        });

        GdQGdT = Gd * omegaSquared * Gd.Transpose();

        I = DenseMatrix.CreateIdentity(4);

        R = DenseMatrix.OfDiagonalArray(new double[] { GameManager.Instance.GetRx(), GameManager.Instance.GetRy() });

        P = DenseMatrix.OfDiagonalArray(new double[] { 100, 100, 10, 10 });

        // Initial
        x = DenseVector.OfArray(new double[] { 0, 0, 0, 0 });
    }

    private void Start()
    {
        KalmanPosition = gameObject;
        _color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        Hologram.material.color =  _color;
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
            LastMeasurementPositions.Add((Vector2) _measurement);
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
       PositionMeanWithoutKalman = new Vector2( LastMeasurementPositions.Average(pos=>pos.x),LastMeasurementPositions.Average(pos=>pos.y));
       while (LastMeasurementPositions.Count > 4)
       {
           LastMeasurementPositions.RemoveAt(0);
       }
    }

    private void UpdateKalmanGameObject()
    {
        Vector3 newPosition = GetVector3Position();
        KalmanPosition.transform.position = newPosition;
        KalmanPosition.transform.localScale = new Vector3((float)P[0, 0], 0.1f, (float)P[1, 1]);
        Debug.DrawLine(newPosition, newPosition + GetVector3Velocity(), Color.red);
        KalmanPositions.Add(newPosition);
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
        for (int i = 0; i < KalmanPositions.Count - 1; i++)
        {
            Gizmos.color = _color;
            Gizmos.DrawLine(KalmanPositions[i], KalmanPositions[i+1]);
        }
        
        if (x == null)
            return;
        
        GizmosUtils.DrawText(GUI.skin, "x", new Vector3((float)x[0], 0, (float)x[1]), color: Color.black, fontSize: 10);
    }

    private Vector3 GetVector3Position()
    {
        return new Vector3((float)x[0], 0, (float)x[1]);
    }

    private Vector3 GetVector3Velocity()
    {
        return new Vector3((float)x[2], 0, (float)x[3]);
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
