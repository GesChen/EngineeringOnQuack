using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LineScale : MonoBehaviour
{
	public RectTransform depend;
	public void Start()
	{		
		Canvas.ForceUpdateCanvases();

		GetComponent<RectTransform>().sizeDelta = new(depend.rect.width, depend.rect.height);
	}
}
