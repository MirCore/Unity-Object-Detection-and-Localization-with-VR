using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using YoloV4Tiny;

public class VisualizerSimulation: Visualizer
{
    #region Editable attributes

    [SerializeField] Texture _CameraSource = null;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _detector = new ObjectDetector(_resources);
        for (var i = 0; i < _markers.Length; i++)
            _markers[i] = Instantiate(_markerPrefab, _preview.transform);
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
