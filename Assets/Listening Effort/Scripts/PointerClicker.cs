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


    public OVRRaycaster ovrRaycaster;

    // Start is called before the first frame update
    void Start()
    {
    }

    //bool _wasRightTriggerPressed = false;
    //bool _wasLeftTriggerPressed = false;
    void Update()
    {
        //bool rightclicked = Input.GetKey(RightTriggerCode);
        //if (isRightPicoControllerTriggered != _wasRightTriggerPressed && isRightHandController)
        //{
        //    Debug.Log($"rightClicked == {rightclicked}");
        //    _wasRightTriggerPressed = rightclicked;
        //}
        //bool leftClicked = Input.GetKey(LeftTriggerCode);
        //if (isLeftPicoControllerTriggered != _wasLeftTriggerPressed && !isRightHandController)
        //{
        //    Debug.Log($"leftClicked == {leftClicked}");
        //    _wasLeftTriggerPressed = leftClicked;
        //}
    }

    void LateUpdate()
    {
        bool isTriggerPulled = (isRightHandController && isRightPicoControllerTriggered) || (!isRightHandController && isLeftPicoControllerTriggered);

        if (!wasTriggered && isTriggerPulled)
        {
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            ovrRaycaster.Raycast(null, results, new Ray(transform.position, transform.forward), false);
            //Debug.Log($"OVR raycast results count: {results.Count}. {string.Join(", ", results.Select(r => r.gameObject.name))}");

            var buttons = results
                .Select(result => result.gameObject.GetComponent<Button>())
                .Where(button => button?.isActiveAndEnabled ?? false).ToList();
            //Debug.Log($"OVR raycast buttons count: {buttons.Count}");
            buttons.ForEach(go => go.GetComponent<Button>().onClick.Invoke());

            // Dropdowns don't work as I can't get the dropped down bit to interact
            //results
            //    .Select(result => result.gameObject.GetComponent<Dropdown>())
            //    .Where(d => d?.isActiveAndEnabled ?? false)
            //    .ToList()
            //    .ForEach(d => d.Show());

            var toggles = results
                .Select(result => result.gameObject.GetComponent<Toggle>())
                .Where(toggle => toggle?.isActiveAndEnabled ?? false)
                .ToList();
            //Debug.Log($"OVR raycast toggle count: {toggles.Count}");
            toggles.ForEach(toggle => toggle.isOn = !toggle.isOn);
        }


        wasTriggered = isTriggerPulled;
    }
}


