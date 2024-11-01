using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class TouchControls : MonoBehaviour
{
    private bool _touchEnabled = false;

    public enum ButtonType
    {
        Accelerate = 0,
        Brake,
        NextCar,
        ResetCar,
        SteerLeft,
        SteerRight,
        ToggleTouch,
        NumButtonTypes
    }
    private GameObject[] _buttonObjects = new GameObject[(int)ButtonType.NumButtonTypes];
    private Button[] _buttons = new Button[(int)ButtonType.NumButtonTypes];
    private MyButton[] _myButtons = new MyButton[(int)ButtonType.NumButtonTypes];
    private bool[] _buttonWasPressed = new bool[(int)ButtonType.NumButtonTypes];
    private bool[] _buttonPressed = new bool[(int)ButtonType.NumButtonTypes];
    private string[] _buttonNames = new string[(int)ButtonType.NumButtonTypes] {
        "Accelerate",
        "Brake",
        "NextCar",
        "ResetCar",
        "SteerLeft",
        "SteerRight",
        "ToggleTouch"
    };
    // TMP_InputButton _steerLeftButton;
    Button _steerLeftButton;

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {
        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        // PhotonNetwork.AutomaticallySyncScene = true;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        for(var i=0; i<(int)ButtonType.NumButtonTypes; i++) {
            _buttonObjects[i] = GameObject.Find(_buttonNames[i]);
            _buttons[i] = _buttonObjects[i].GetComponent<Button>();

            _myButtons[i] = _buttonObjects[i].GetComponent<MyButton>();
            if (_myButtons[i] != null) {
                Debug.Log($"button {i}: set type");
                _myButtons[i].SetButtonType(i);
            }

            //  _buttons[i].onClick.AddListener(() => ButtonClicked(i));
            if (i != (int)ButtonType.ToggleTouch) {
                // Debug.Log($"button {i} disabled");
                _buttonObjects[i].SetActive(false);
            }
        }
    }

    // Update is called once per frame
    public void Update()
    {
        for(var i=0; i<(int)ButtonType.NumButtonTypes; i++) {
            _buttonWasPressed[i] = _buttonPressed[i];
            _buttonPressed[i] = _myButtons[i]._pressed;
        }
    }

    public void ToggleTouch()
    {
        Debug.Log("ToggleTouch");
        _touchEnabled = !_touchEnabled;
        for(var i=0; i<(int)ButtonType.NumButtonTypes; i++) {
            if (i != (int)ButtonType.ToggleTouch) {
                _buttonObjects[i].SetActive(_touchEnabled);
            }
        }
    }

    public bool Pressed(ButtonType buttonType)
    {
        return !_buttonWasPressed[(int)buttonType] && _buttonPressed[(int)buttonType];
    }

    public bool Held(ButtonType buttonType)
    {
        return _buttonPressed[(int)buttonType];
    }

    public bool Released(ButtonType buttonType)
    {
        return _buttonWasPressed[(int)buttonType] && !_buttonPressed[(int)buttonType];
    }
}
