using UnityEngine;

public class ScaleMain : MonoBehaviour
{
    public TransformTools main;
    public Scale X;
    public Scale Y;
    public Scale Z;
    public Scale Full;

    void Update()
    {
        X.enabled = main.scaling;
        Y.enabled = main.scaling;
        Z.enabled = main.scaling;
        Full.enabled = main.scaling;
        Full.gameObject.SetActive(!main.translating); // center conflicts 

        transform.localScale = main.scaling ? Vector3.one : Vector3.zero;
    }
}
