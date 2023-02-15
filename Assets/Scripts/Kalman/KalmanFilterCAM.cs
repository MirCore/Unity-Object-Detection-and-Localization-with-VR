using MathNet.Numerics.LinearAlgebra.Double;
using Unity.Mathematics;
using UnityEngine;

namespace Kalman
{
    [ExecuteInEditMode]
    public class KalmanFilterCAM : KalmanFilter
    {
        private void PopulateMatrices(double omegaSquared)
        {
            Ts = Time.fixedDeltaTime;

            F = DenseMatrix.OfArray(new double[,]
            {
                { 1, Ts, math.sqrt(Ts) / 2, 0, 0, 0 },
                { 0, 1, Ts, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 1, Ts, math.sqrt(Ts) / 2 },
                { 0, 0, 0, 0, 1, Ts },
                { 0, 0, 0, 0, 0, 1 }
            });

            FT = F.Transpose();

            H = DenseMatrix.OfArray(new double[,]
            {
                { 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0 }
            });

            HT = H.Transpose();

            Gd = DenseMatrix.OfArray(new double[,]
            {
                { Ts * Ts / 2, 0 },
                { Ts, 0 },
                { 1, 0 },
                { 0, Ts * Ts / 2 },
                { 0, Ts },
                { 0, 1 }
            });

            GdQGdT = Gd * omegaSquared * Gd.Transpose();

            I = DenseMatrix.CreateIdentity(6);

            R = DenseMatrix.OfDiagonalArray(new double[] { GameManager.Instance.GetRx(), GameManager.Instance.GetRy() });

            P = DenseMatrix.OfDiagonalArray(new double[] { 1000, 1000, 100, 100, 10, 10 });

            // Initial
            x = DenseVector.OfArray(new double[] { 0, 0, 0, 0, 0, 0 });
        }

    }
}
