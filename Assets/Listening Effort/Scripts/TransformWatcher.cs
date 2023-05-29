using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformWatcher : MonoBehaviour
{
    public event EventHandler<Transform> TransformChanged;

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            TransformChanged?.Invoke(this, transform);
            transform.hasChanged = false;
        }
    }
}
