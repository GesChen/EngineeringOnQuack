using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisIndicator : MonoBehaviour
{
    public float alpha;
    private TransformTools main;
    private Material mat;
    void Start()
    {
        main = GetComponentInParent<TransformTools>();
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if (!main.dragging)
        {
            alpha = Mathf.Lerp(alpha, 0f, main.alphaSmoothness);
            
        }
		mat.SetFloat("_Alpha", alpha);
	}
}
