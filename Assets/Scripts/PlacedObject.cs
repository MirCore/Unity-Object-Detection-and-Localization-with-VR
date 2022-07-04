using System;
using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    private Vector2 _averagePosition;
    [SerializeField] private int LabelIndex;
    [SerializeField] private string Label;
    private GameObject _gameObject;
    private Vector3 _meshSize;
    private DateTime _timestamp;


    public void SetNewPositionAndScale(Cluster cluster)
    {
        SetNewPosition(cluster);
        SetNewScale(cluster);
        _timestamp = DateTime.Now;
    }

    private void SetNewPosition(Cluster cluster)
    {
        transform.position = new Vector3(cluster.Center.x, 0, cluster.Center.y);
    }

    private void SetNewScale(Cluster cluster)
    {
        transform.localScale = new Vector3(cluster.W / _meshSize.x, cluster.H / _meshSize.y, cluster.W / _meshSize.z);;
    }
    
    public float GetDistanceToPoint(Vector2 point)
    {
        if (_averagePosition == Vector2.zero)
            return -Mathf.Infinity;
        return Vector2.Distance(point, _averagePosition);
    }

    public void SetType(int labelIndex)
    {
        LabelIndex = labelIndex;
        Label = Marker.Labels[LabelIndex];
        
        _gameObject = ObjectModelManager.Instance.GetObject(Label);
        if (_gameObject != null)
            Instantiate(_gameObject, transform);
        SetMeshSize();
    }

    private void SetMeshSize()
    {
        Mesh m = _gameObject.GetComponent<MeshFilter>().sharedMesh;
        Bounds meshBounds = m.bounds;
        _meshSize = meshBounds.size;
    }

    public bool TriggerDestroy(float seconds)
    {
        if (_timestamp > DateTime.Now.AddSeconds(-seconds))
            return false;
        
        Destroy(gameObject);
        return true;
    }
}
