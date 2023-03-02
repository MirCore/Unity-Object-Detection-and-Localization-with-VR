using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GameManager))]
    public class LocalisationMethodEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("LocalisationMethod"));

            var type = (GameManager.LocalisationMethodEnum)serializedObject.FindProperty("LocalisationMethod").enumValueIndex;
            
            EditorGUI.indentLevel++;
            
            switch (type)
            {
                case GameManager.LocalisationMethodEnum.Cluster:
                    DisplayClusterInfo();
                    break;
                case GameManager.LocalisationMethodEnum.Kalman:
                    DisplayKalmanInfo();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoPauseAfterSeconds"));
            EditorGUILayout.PropertyField(FindPropertyByAutoPropertyName(serializedObject, "LabelsToFind"));
            EditorGUILayout.PropertyField(FindPropertyByAutoPropertyName(serializedObject, "StereoImage"));
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayClusterInfo()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ObjectTrackerClustering"));
        }

        private void DisplayKalmanInfo()
        {
            SerializedProperty enableYoloProperty = serializedObject.FindProperty("EnableYOLO");
            
            EditorGUILayout.PropertyField(enableYoloProperty);

            if (!enableYoloProperty.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableMeasureSimulation"));
            }
            
            EditorGUILayout.PropertyField(FindPropertyByAutoPropertyName(serializedObject, "SigmaSquared"));
            EditorGUILayout.PropertyField(FindPropertyByAutoPropertyName(serializedObject, "Rx"));
            EditorGUILayout.PropertyField(FindPropertyByAutoPropertyName(serializedObject, "Ry"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowMeasurementGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SimulatedObjects"));
        }


        private static SerializedProperty FindPropertyByAutoPropertyName(SerializedObject obj, string propName)
        {
            return obj.FindProperty($"<{propName}>k__BackingField");
        }
    }
    
}
