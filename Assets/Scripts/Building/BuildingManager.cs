using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public Transform mainPartsContainer;
    public List<Part> Parts;
    public SelectionManager SelectionManager;
    public TransformTools transformTools;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach(Part part in Parts)
        {
            part.Selected = SelectionManager.selection.Contains(part.transform);
		}
    }
}
