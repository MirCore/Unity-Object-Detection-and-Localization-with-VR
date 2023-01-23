using UnityEngine;
using UnityEngine.UI;
using YoloV4Tiny;

sealed class VisualizerSimulation: MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Texture _CameraSource = null;
    [SerializeField, Range(0, 1)] float _threshold = 0.5f;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] RawImage _preview = null;
    [SerializeField] Marker _markerPrefab = null;
    
    [SerializeField] private ObjectTracker Tracker;

    #endregion

    #region Internal objects

    ObjectDetector _detector;
    Marker[] _markers = new Marker[50];

    #endregion
    
    #region MonoBehaviour implementation

    void Start()
    {
        _detector = new ObjectDetector(_resources);
        for (var i = 0; i < _markers.Length; i++)
            _markers[i] = Instantiate(_markerPrefab, _preview.transform);
    }

    void OnDisable()
      => _detector.Dispose();

    void OnDestroy()
    {
        for (var i = 0; i < _markers.Length; i++) Destroy(_markers[i]);
    }

    void Update()
    {
        _detector.ProcessImage(_CameraSource, _threshold);

        var i = 0;
        foreach (var d in _detector.Detections)
        {
            if (i == _markers.Length) break;
            _markers[i++].SetAttributes(d);
        }
        
        Tracker.SetNewDetectionData(_detector.Detections);

        for (; i < _markers.Length; i++) _markers[i].Hide();

        _preview.texture = _CameraSource;
    }

    #endregion
}
