using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MyButton : Button
{
    public int buttonType = 0;
    public bool _pressed = false;

    public void SetButtonType(int type)
    {
        buttonType = type;
    }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        Debug.Log($"Down {buttonType}");
        _pressed = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        Debug.Log($"Up {buttonType}");
        _pressed = false;
    }
}