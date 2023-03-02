using UnityEngine;
using YoloV4Tiny;

public class ObjectDetectionManager : Visualizer
{
    [SerializeField] private Texture VirtualCameraSource;
    private bool _isCameraSourceSpecified;

    void Start()
    {
        _isCameraSourceSpecified = VirtualCameraSource != null;
        _detector = new ObjectDetector(_resources);
        for (var i = 0; i < _markers.Length; i++)
            _markers[i] = Instantiate(_markerPrefab, _preview.transform);
    }

    void OnDisable()
        => _detector?.Dispose();

    void OnDestroy()
    {
        for (var i = 0; i < _markers.Length; i++) Destroy(_markers[i]);
    }
    
    void Update()
    {
        _detector.ProcessImage(_isCameraSourceSpecified ? VirtualCameraSource : _source.Texture, _threshold);

        var i = 0;
        foreach (var d in _detector.Detections)
        {
            if (i == _markers.Length) break;
            _markers[i++].SetAttributes(d);
        }
        
        GameManager.Instance.SetNewDetectionData(_detector.Detections);

        for (; i < _markers.Length; i++) _markers[i].Hide();

        _preview.texture = _isCameraSourceSpecified ? VirtualCameraSource : _source.Texture;
    }

}
