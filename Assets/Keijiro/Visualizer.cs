using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using YoloV4Tiny;

public class Visualizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] protected ImageSource _source;
    [SerializeField, Range(0, 1)] protected float _threshold = 0.5f;
    [SerializeField] protected ResourceSet _resources = null;
    [SerializeField] protected RawImage _preview = null;
    [SerializeField] protected Marker _markerPrefab = null;
    
    #endregion

    #region Internal objects

    protected ObjectDetector _detector;
    protected Marker[] _markers = new Marker[50];
    private bool IsLocatorNotNull;
    private bool IsTrackerNotNull;

    #endregion
    
    #region MonoBehaviour implementation

    void Start()
    {
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
        _detector.ProcessImage(_source.Texture, _threshold);

        var i = 0;
        foreach (var d in _detector.Detections)
        {
            if (i == _markers.Length) break;
            _markers[i++].SetAttributes(d);
        }

        GameManager.Instance.SetNewDetectionData(_detector.Detections);
        

        for (; i < _markers.Length; i++) _markers[i].Hide();

        _preview.texture = _source.Texture;
    }

    #endregion
}
