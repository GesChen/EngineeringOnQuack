using UnityEngine;

public class TranslateMain : MonoBehaviour
{
    public TransformTools main;
    public Translate[] interactiveElements;
    void Update()
    {
        foreach (Translate translate in interactiveElements)
            translate.enabled = main.translating;

        transform.localScale = main.translating ? Vector3.one : Vector3.zero;
    }
}
