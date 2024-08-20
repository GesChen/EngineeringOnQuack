using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentDescription : MonoBehaviour
{
	public List<bool> any; // if any is true, it will display, otherwise it will not
	public GameObject mainObject;
	void Update()
	{
		mainObject.SetActive(any.Contains(true));
		transform.SetAsLastSibling(); // always on top
	}
}