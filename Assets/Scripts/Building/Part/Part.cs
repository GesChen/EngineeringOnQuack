using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour
{
    public bool Selected;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Selected)
        {
            gameObject.layer = LayerMask.NameToLayer("Selected");
        }
        else
        {
			gameObject.layer = LayerMask.NameToLayer("Part");
		}
	}
}
