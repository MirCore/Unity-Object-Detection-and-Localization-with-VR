using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using Random = UnityEngine.Random;

namespace Simulator
{
   // [ExecuteInEditMode]
    public class DetectionSimulator : MonoBehaviour
    {
        private ObjectTrackerKalman _objectTracker;
        [SerializeField] private GameObject SimulationObject;
        [SerializeField] private int NumberOfSimulationSpawnsOnStartup;
        [SerializeField] private float Noise = 1f;
        [SerializeField] private float MeasureDelta = 1f;

        [SerializeField] private bool MeasurePosition = true;
        [SerializeField] private bool SendMeasuredPositions = true;

        private List<Point> _points = new ();
        private List<List<Point>> _gizmoList = new ();
        
        [Header("Debug Button(s)")]
        [InspectorButton("OnSpawnSimulationButtonClicked")]
        public bool SpawnSimulationButton;
                
        /// <summary>
        /// Spawn Simulation
        /// </summary>
        private void OnSpawnSimulationButtonClicked()
        {
            SpawnSimulation();
            Debug.Log("SpawnSimulation button clicked");
        }


        private void Start()
        {
            Random.InitState(42);
            _objectTracker = gameObject.AddComponent<ObjectTrackerKalman>();
            
            for (int i = 0; i < NumberOfSimulationSpawnsOnStartup; i++)
            {
                SpawnSimulation();
            }
            GarbageCollector.incrementalTimeSliceNanoseconds = 3000000;
            StartCoroutine(Measure());
        }

        private void SpawnSimulation()
        {
            Instantiate(SimulationObject, new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5)),
                Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
        }

        private IEnumerator Measure()
        {
            while (true)
            {
                if (!MeasurePosition || GameManager.Instance.SimulatedObjects.Count < 0)
                {
                    yield return new WaitForSeconds(MeasureDelta);
                    continue;
                }
                    
                _points.Clear();
                foreach (Vector3 position in GameManager.Instance.SimulatedObjects.Select(simulatedObject => simulatedObject.transform.position))
                {
                    _points.Add(new Point
                    {
                        X = position.x + Utils.GenerateGaussianRandom(0f, Noise),
                        Z = position.z + Utils.GenerateGaussianRandom(0f, Noise)
                    });
                }

                _gizmoList.Add(new List<Point>(_points));
                if (SendMeasuredPositions)
                    _objectTracker.SetNewSimulationData(_points);

                yield return new WaitForSeconds(MeasureDelta);
            }
        }

        private void OnDrawGizmos()
        {
            int count = _gizmoList.Count - 1;
            for (int i = count; i >= 0 ; i--)
            {
                Color color = Color.HSVToRGB(0, 1f / count * i, 1);
                foreach (Point point in _gizmoList[i])
                {
                    GizmosUtils.DrawText(GUI.skin, "O", new Vector3(point.X, 0, point.Z), color: color, fontSize: 10);
                }
            }

            if (_gizmoList.Count > 100)
            {
                _gizmoList.RemoveAt(0);
            }
        }
    }
}
