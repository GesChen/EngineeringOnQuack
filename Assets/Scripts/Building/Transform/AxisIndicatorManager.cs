using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisIndicatorManager : MonoBehaviour
{
	public GameObject axisIndicatorObject;
	public List<AxisIndicator> indicators;
	
	public AxisIndicator NewIndicator()
	{
		GameObject indicator = Instantiate(axisIndicatorObject, transform);
		AxisIndicator component = indicator.GetComponent<AxisIndicator>();
		indicators.Add(component);
		return component;
	}

	public void DestroyIndicator(AxisIndicator indicator)
	{
		if (indicators.Contains(indicator))
		{
			indicators.Remove(indicator);
			Destroy(indicator.gameObject);
		}
	}
}