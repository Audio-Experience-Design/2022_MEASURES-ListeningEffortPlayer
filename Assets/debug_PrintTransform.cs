using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debug_PrintTransform : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log($"pos {transform.position}, rot {transform.rotation}");
    }
}
