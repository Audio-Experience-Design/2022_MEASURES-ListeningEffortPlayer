using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ColorCalibrationSphere : MonoBehaviour
{
	public float brightness
	{
		get => GetComponent<Renderer>().material.color.r;
		set => SetBrightness(value);
	}

    // Start is called before the first frame update
    void Awake()
    {
		// Invert normals
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		mesh.triangles = mesh.triangles.Reverse().ToArray();
    }


	public void SetBrightness(float brightness)
	{
		GetComponent<Renderer>().material.color = new Color(brightness, brightness, brightness);
	}

	
	// Update is called once per frame
	void Update()
    {
        
    }
}
