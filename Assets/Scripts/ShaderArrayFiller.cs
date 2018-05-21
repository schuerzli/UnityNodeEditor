using System.Collections.Generic;
using UnityEngine;

public class ShaderArrayFiller : MonoBehaviour
{
	public int numPoints = 100;
	public List<Vector2> points = new List<Vector2>(100);
	public Renderer lineRenderer;
	private float[] floatArray;

	private void Start()
	{
		for (int i = 0; i < numPoints; ++i) points.Add(Vector2.zero);
		floatArray = new float[numPoints * 2];
		lineRenderer.material.SetFloat("numPts", numPoints);
	}

	void Update() 
	{
		points.Insert(0, Input.mousePosition);
		if (points.Count > numPoints) points.RemoveRange(numPoints, points.Count - numPoints);

		for (int i = 0; i < numPoints; ++i){
			floatArray[i * 2] = points[i].x / Screen.width;
			floatArray[(i * 2)+1] = points[i].y / Screen.height;
		}
		if (lineRenderer != null)
		{
			lineRenderer.material.SetFloatArray("points", floatArray);
		}
    }
}
