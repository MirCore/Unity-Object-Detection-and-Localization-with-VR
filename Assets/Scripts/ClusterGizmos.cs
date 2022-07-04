using UnityEngine;

[ExecuteInEditMode]
public class ClusterGizmos : MonoBehaviour
{
    private ObjectLocator _objectLocator;

    private void OnEnable()
    {
        _objectLocator = GetComponent<ObjectLocator>();
    }

    private void OnDrawGizmos()
    {
        foreach (DetectedObject detectedObject in _objectLocator.GetDetectedObjects())
        {
            int clusterCount = detectedObject.Clusters.Count + 1;
            foreach (Point point in detectedObject.DetectedPoints)
            {
                Color color = Color.HSVToRGB((float) point.ClusterId / clusterCount, 1f, 1f);
                GizmosUtils.DrawText(GUI.skin, point.ClusterId.ToString(), new Vector3(point.X, 0, point.Z), color, 20);
            }
            
        }
    }
}