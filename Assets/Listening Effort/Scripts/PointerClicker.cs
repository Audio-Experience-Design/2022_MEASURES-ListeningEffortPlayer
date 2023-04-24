using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

    bool _wasRightTriggerPressed = false;
    bool _wasLeftTriggerPressed = false;
    void Update()
    {
        bool rightclicked = Input.GetKey(RightTriggerCode);
        if (isRightPicoControllerTriggered != _wasRightTriggerPressed && isRightHandController)
        {
            Debug.Log($"rightClicked == {rightclicked}");
            _wasRightTriggerPressed = rightclicked;
        }
        bool leftClicked = Input.GetKey(LeftTriggerCode);
        if (isLeftPicoControllerTriggered != _wasLeftTriggerPressed && !isRightHandController)
        {
            Debug.Log($"leftClicked == {leftClicked}");
            _wasLeftTriggerPressed = leftClicked;
        }
    }

    void LateUpdate()
    {
        //#if !UNITY_EDITOR

        bool isTriggerPulled = (isRightHandController && isRightPicoControllerTriggered) || (!isRightHandController && isLeftPicoControllerTriggered);

        if (!wasTriggered && isTriggerPulled)
        {
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            ovrRaycaster.Raycast(null, results, new Ray(transform.position, transform.forward), false);
            Debug.Log($"OVR raycast results count: {results.Count}. {string.Join(", ", results.Select(r => r.gameObject.name))}");

            var buttons = results.Where(result => result.gameObject.GetComponent<Button>() != null).ToList();
            Debug.Log($"OVR raycast buttons count: {buttons.Count}");
            buttons.ForEach(result => result.gameObject.GetComponent<Button>().onClick.Invoke());

            results.Where(result => result.gameObject.GetComponent<Dropdown>() != null).ToList().ForEach(result => result.gameObject.GetComponent<Dropdown>().Show());

            //PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            //pointerEventData.button = PointerEventData.InputButton.Left;
            var toggles = results
                .Where(result => result.gameObject.GetComponent<Toggle>() != null)
                .Select(result => result.gameObject.GetComponent<Toggle>())
                .ToList();
            Debug.Log($"OVR raycast toggle count: {toggles.Count}");


            //.ForEach(toggle => toggle.OnSubmit(new BaseEventData(EventSystem.current)));
            toggles.ForEach(toggle => toggle.isOn = !toggle.isOn);
            //.ForEach(toggle => toggle.OnPointerClick(pointerEventData));
            //.ForEach(toggle => toggle.);
        }
        //#endif

        //if (isTriggerPulled)
        //{
        //    Debug.DrawRay(rayOrigin, rayDirection * 200_000, Color.yellow);
        //}

        //if (isTriggerPulled && !wasTriggered)
        //{
        //    Debug.Log($"{(isRightHandController ? "Right" : "Left")} trigger pulled", this);
        //}
        //else if (!isTriggerPulled && wasTriggered)
        //{
        //    Debug.Log($"{(isRightHandController ? "Right" : "Left")} trigger released", this);
        //}

        wasTriggered = isTriggerPulled;
    }
}


