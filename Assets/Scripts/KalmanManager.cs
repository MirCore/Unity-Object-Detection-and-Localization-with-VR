using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KalmanManager : MonoBehaviour
{
    public static KalmanManager Instance { get; private set; }
    [SerializeField] public List<KalmanFilter> KalmanFilters = new List<KalmanFilter>();
    [SerializeField] private GameObject _kalmanFilter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        Instance = this;
    }

    
    public void InstantiateNewKalmanFilter()
    {
        Instantiate(_kalmanFilter, new Vector3(), new Quaternion());
    }

    public void SetNewKalmanFilter(KalmanFilter kalmanFilter)
    {
        KalmanFilters.Add(kalmanFilter);
    }

    public void RemoveKalmanFilter(KalmanFilter kalmanFilter)
    {
        KalmanFilters.Remove(kalmanFilter);
    }
}
