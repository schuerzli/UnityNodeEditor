using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ShaderArrayFiller : MonoBehaviour
{
	public int numPoints = 100;
	public List<Vector2> points = new List<Vector2>(100);
	public Graphic uiGraphic;
	private float[] floatArray;

	void Start()
	{
		for (int i = 0; i < numPoints; ++i) points.Add(Vector2.zero);
		floatArray = new float[numPoints * 2];
		uiGraphic?.material.SetFloat("numPts", numPoints);
	}

	void Update()
	{
		points.Insert(0, Input.mousePosition);
		if (points.Count > numPoints) points.RemoveRange(numPoints, points.Count - numPoints);

		for (int i = 0; i < numPoints; ++i){
			floatArray[i * 2] = points[i].x / Screen.width;
			floatArray[(i * 2)+1] = points[i].y / Screen.height;
		}

		if (uiGraphic != null){
            uiGraphic.material.SetFloatArray("points", floatArray);
			RectTransform trans = uiGraphic.transform as RectTransform;
			uiGraphic?.material.SetVector("_TexSize", new Vector4(trans.rect.width, trans.rect.height, 0, 0));
        }
    }
}
