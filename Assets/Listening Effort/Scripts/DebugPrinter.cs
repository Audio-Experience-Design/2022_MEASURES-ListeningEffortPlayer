using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPrinter : MonoBehaviour
{
    public void testButton()
    {
        Debug.Log("DebugPrinter: testButton", this);
    }
}
