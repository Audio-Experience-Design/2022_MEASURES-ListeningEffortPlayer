using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PointerClicker : MonoBehaviour
{
    public bool isRightHandController = false;
    //private Transform pointer;
    // from previous frame, to prevent double triggerings
    bool wasTriggered = false;

    const KeyCode LeftTriggerCode = KeyCode.JoystickButton14;
    const KeyCode RightTriggerCode = KeyCode.JoystickButton15;

    bool isLeftPicoControllerTriggered => Input.GetKey(LeftTriggerCode);
    bool isRightPicoControllerTriggered => Input.GetKey(RightTriggerCode);


    // ray when trigger was pulled
    Vector3 rayOrigin;
    Vector3 rayDirection;

    public OVRRaycaster ovrRaycaster;

    // Start is called before the first frame update
    void Start()
    {
        //pointer = this.transform;
    }

    bool _temp = false;
    bool _temp2 = false;
    void Update()
    {
        bool rightclicked = Input.GetKey(RightTriggerCode);
        if (isRightPicoControllerTriggered != _temp)
        {
            Debug.Log($"rightClicked == {rightclicked}");
            _temp = rightclicked;
        }
        bool leftClicked = Input.GetKey(LeftTriggerCode);
        if (isLeftPicoControllerTriggered != _temp2)
        {
            Debug.Log($"leftClicked == {leftClicked}");
            _temp2 = leftClicked;
        }
    }

    void LateUpdate()
    {
        //#if !UNITY_EDITOR

        bool isTriggerPulled = (isRightHandController && isRightPicoControllerTriggered) || (!isRightHandController && isLeftPicoControllerTriggered);

        if (!wasTriggered && isTriggerPulled)
        {
            //rayOrigin = transform.position;
            //rayDirection = transform.forward;
            //Physics.Raycast(transform.position, transform.forward, out RaycastHit hit);

            //Debug.Log($"* Ray cast from origin {rayOrigin} in direction {rayDirection} from {(isRightHandController ? "right" : "left")} hand.");
            //if (hit.collider && hit.transform.gameObject)
            //{
            //    Debug.Log($"Hit {hit.transform.gameObject.name}.");
            //    hit.transform.gameObject.GetComponent<Button>()?.onClick.Invoke();
            //}

            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            ovrRaycaster.Raycast(null, results, new Ray(transform.position, transform.forward), false);
            Debug.Log($"OVR raycast results count: {results.Count}");
            var buttons = results.Where(result => result.gameObject.GetComponent<Button>() != null).ToList();
            Debug.Log($"OVR raycast buttons count: {buttons.Count}");
            buttons.ForEach(result => result.gameObject.GetComponent<Button>().onClick.Invoke());


        }
        //#endif

        if (isTriggerPulled)
        {
            Debug.DrawRay(rayOrigin, rayDirection * 200_000, Color.yellow);
        }

        if (isTriggerPulled && !wasTriggered)
        {
            Debug.Log($"{(isRightHandController ? "Right" : "Left")} trigger pulled", this);
        }
        else if (!isTriggerPulled && wasTriggered)
        {
            Debug.Log($"{(isRightHandController ? "Right" : "Left")} trigger released", this);
        }

        wasTriggered = isTriggerPulled;
    }
}


