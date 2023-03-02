using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Kalman
{
    public class KalmanStatisticsUtils
    {
        private static readonly List<Vector2> _measurements = new();
        
        public static void WriteFileFromList(string fileName, IEnumerable<KalmanState> kalmanStates)
        {
            string fullFileName = "Assets/Output/" + fileName + ".csv";
            TextWriter writer = new StreamWriter(fullFileName, false);
            writer.WriteLine("Time" + ";" +
                             "Frame" + ";" +
                             "GroundTruth x" + ";" +
                             "GroundTruth y" + ";" +
                             "GroundTruth v_x" + ";" +
                             "GroundTruth v_y" + ";" +
                             "Measurement x" + ";" +
                             "Measurement y" + ";" +
                             "Kalman x" + ";" +
                             "Kalman y" + ";" +
                             "Kalman v_x" + ";" +
                             "Kalman v_y" + ";" +
                             "KalmanP x" + ";" +
                             "KalmanP y");
            foreach (string output in kalmanStates.Select(kalmanState => 
                         kalmanState.Time + ";" +
                         kalmanState.Frame + ";" +
                         Vector2ToCsv(kalmanState.GroundTruthPosition) + ";" +
                         Vector2ToCsv(kalmanState.GroundTruthVelocity) + ";" +
                         Vector2ToCsv(kalmanState.Measurement) + ";" +
                         Vector2ToCsv(kalmanState.KalmanPosition) + ";" +
                         Vector2ToCsv(kalmanState.KalmanVelocity) + ";" +
                         Vector2ToCsv(kalmanState.KalmanP)))
            {
                writer.WriteLine(output);
            }
            writer.Close();
        }

        public static void WriteNEESToLog(float sigmaSquared, float Rx, float Ry)
        {
            WriteFile("kalman",
                "NEES: " + KalmanManager.Instance.GetNEESValue() + "; o^2: " + sigmaSquared + "; R: " + Rx + " " + Ry);
        }
        
        private static void WriteFile(string fileName, string output)
        {
            string fullFileName = "Assets/Output/" + fileName + ".csv";
            TextWriter writer = new StreamWriter(fullFileName, true);
            writer.WriteLine(output);
            writer.Close();
        }
        
        private static string Vector2ToCsv(Vector2 input)
        {
            return input.x + ";" + input.y;
        }
        

        public static void MeasureNoise(Vector2 point)
        {
            if (GameManager.Instance.SimulatedObjects.Count == 0)
                return;
            Vector2 realPosition = GameManager.Instance.SimulatedObjects.First().PositionVector2;
            Vector2 delta = new (point.x - realPosition.x, point.y - realPosition.y);
            _measurements.Add(delta);
        }
        

        public static string CalculateNoise()
        {
            Vector2 measurementsSum = new ();
            measurementsSum = _measurements.Aggregate(measurementsSum, (current, measurement) => current + measurement);

            Vector2 mean = measurementsSum/_measurements.Count;
            float meanXY = (mean.x + mean.y)/2;
            float xSum = 0;
            float ySum = 0;
            float xySum = 0;
            foreach (Vector2 measurement in _measurements)
            {
                float x = measurement.x - mean.x;
                float y = measurement.y - mean.y;
                float xy = measurement.x + measurement.y - meanXY;
                x *= x;
                y *= y;
                xy *= xy;
                xSum += x;
                ySum += y;
                xySum += xy;
            }

            float xStandardDeviation = xSum / _measurements.Count;
            float yStandardDeviation = ySum / _measurements.Count;
            float xyStandardDeviation = xySum / (2 * _measurements.Count);
        
            return ("x: " + xStandardDeviation + " y: " + yStandardDeviation + " xy: " + xyStandardDeviation);
        }
    }
}
