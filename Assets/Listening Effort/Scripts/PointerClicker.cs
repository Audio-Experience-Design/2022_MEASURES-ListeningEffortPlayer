using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointerClicker : MonoBehaviour
{
    public bool isRightHandController = false;
    private Transform pointer;
    // from previous frame, to prevent double triggerings
    bool wasTriggered = false;

    bool isLeftPicoControllerTriggered => Input.GetKey(KeyCode.JoystickButton15);
    bool isRightPicoControllerTriggered => Input.GetKey(KeyCode.JoystickButton14);

    // Start is called before the first frame update
    void Start()
    {
        pointer = this.transform;
    }

    bool _temp = false;
    void Update()
    {
        bool rightclicked = Input.GetKey(KeyCode.JoystickButton14);
        if (rightclicked != _temp)
        {
            Debug.Log($"rightClicked = {rightclicked}");
            _temp = rightclicked;
        }
    }

    void LateUpdate()
    {
        //#if !UNITY_EDITOR

        GameObject objectToTrigger = null;

        Ray ray = new Ray(pointer.position, pointer.forward);
        RaycastHit hit;
        Physics.Raycast(ray.origin, ray.direction, out hit);

        bool isTriggerPulled = (isRightHandController && isRightPicoControllerTriggered) || (!isRightHandController && isRightPicoControllerTriggered);

        if (isTriggerPulled)
        {
            Debug.Log($"Ray cast from origin {ray.origin} in direction {ray.direction} from {(isRightHandController? "right" : "left")} hand.");
            if (hit.collider)
                if (hit.transform.gameObject)
                {
                    objectToTrigger = hit.transform.gameObject;
                }
        }

        if (objectToTrigger != null && !wasTriggered)
        {
            objectToTrigger.GetComponent<Button>()?.onClick.Invoke();
            wasTriggered = true;
        }
        else if (objectToTrigger == null)
        {
            wasTriggered = false;
        }
        //#endif

    }
}


