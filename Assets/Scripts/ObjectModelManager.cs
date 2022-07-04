using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectModelManager : MonoBehaviour
{
    [SerializeField] public List<ModelObject> Objects;
    [SerializeField] private PlacedObject DetectedObjectPrefab;

    public static ObjectModelManager Instance;

    private void Start()
    {
        Instance = this;
    }

    public GameObject GetObject(string label)
    {
        if (Objects.Exists(s => s.Label == label))
            return Objects.Find(o => o.Label == label).Model;
        return null;
    }

    public PlacedObject GetPrefabDefinition()
    {
        return DetectedObjectPrefab;
    }
}

[Serializable]
public class ModelObject
{
    public string Label;
    public GameObject Model;
}
