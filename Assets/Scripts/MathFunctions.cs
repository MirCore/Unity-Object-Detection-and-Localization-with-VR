using UnityEngine;

public class MathFunctions
{
    public static class GaussianRandom
    {
        public static float GenerateNormalRandom(float mu, float sigma)
        {
            float rand1 = Random.Range(0.0f, 1.0f);
            float rand2 = Random.Range(0.0f, 1.0f);

            float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);

            return (mu + sigma * n);
        }
    }
}
