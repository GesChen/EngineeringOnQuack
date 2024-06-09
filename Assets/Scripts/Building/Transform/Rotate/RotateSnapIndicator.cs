using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSnapIndicator : MonoBehaviour
{
    public TransformTools main; 
    public GameObject indicatorSphere;
    public List<GameObject> spheres;
    [Space]
    public bool inUse;
    public Transform parent;
    public float startAngle;
    public Material material;

    bool lastInUse;

    void Update()
    {
        if (inUse != lastInUse && inUse)
            ShowIndicators();
        else if (inUse != lastInUse && !inUse)
            HideIndicators();

        lastInUse = inUse;
    }

    public void ShowIndicators()
    {
        int numSpheres = 360 / (int)main.rotateSnappingIncrement;
        for (int i = 0; i < numSpheres; i++)
        {
            float radians = (startAngle + i * main.rotateSnappingIncrement) * Mathf.Deg2Rad;
            Vector3 circlePosition = new (Mathf.Sin(radians), Mathf.Cos(radians));
            GameObject newObj = Instantiate(indicatorSphere, parent);
            newObj.transform.localPosition = circlePosition;
            newObj.SetActive(true);
            Renderer renderer = newObj.GetComponent<Renderer>();
            renderer.material = material;
            //renderer.material.SetInt

            spheres.Add(newObj);
        }
    }
    public void HideIndicators()
    {
        foreach (GameObject g in spheres)
        {
            Destroy(g);
        }
        spheres.Clear();
    }
}
