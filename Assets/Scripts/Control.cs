using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    private int curCameraIndex = 0;
    private List<Camera> cameras = new List<Camera>();
    private List<Quaternion> cameraStartRotation = new List<Quaternion>();
    private List<WheelController> cars = new List<WheelController>();
    
    private bool rotatingCamera = false;
    private float camRotDeltaSqr = 20f;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("control start");

        // detect cameras
        int i = 1;
        while (true) {
            Debug.Log("checking camera " + i);
            
            var camObject = GameObject.Find("camera"+i);
            if (camObject == null) {
                break;
            }

            var cam = camObject.GetComponent<Camera>();
            cam.enabled = false;
            cameras.Add(cam);
            cameraStartRotation.Add(cam.transform.rotation);
            
            i++;
        }

        Debug.Log("num cameras: " + cameras.Count);

        cameras[0].enabled = true;
        
        // detect cars
        i = 1;
        while (true) {
            var carObject = GameObject.Find("toyCar"+i);
            if (carObject == null) {
                break;
            }

            cars.Add(carObject.GetComponent<WheelController>());
            
            i++;
        }

        Debug.Log("num cars: " + cars.Count);
    }

    // Update is called once per frame
    void Update()
    {
        var car = cars[curCameraIndex];
        
        //TODO: clean/move touch handling
        // handle touches
        var touchTop = false;
        var touchBottom = false;
        var touchLeft = false;
        var touchRight = false;
        var touchNext = false;
        if (Input.touches.Length == 0) {
            rotatingCamera = false;
        } else {
            var firstTouch = Input.touches[0];
            if (rotatingCamera) {
                if (firstTouch.phase == TouchPhase.Ended) {
                    rotatingCamera = false;
                } else {
                    // Debug.Log($"touch moved: {firstTouch.deltaPosition.x}, {firstTouch.deltaPosition.y}");
                    cameras[curCameraIndex].transform.Rotate(firstTouch.deltaPosition.y/50, -firstTouch.deltaPosition.x/50, 0);
                }
            } else if (Input.touches.Length == 1 && firstTouch.phase != TouchPhase.Began && firstTouch.phase != TouchPhase.Ended && firstTouch.deltaPosition.sqrMagnitude > camRotDeltaSqr) {
                rotatingCamera = true;
            } else {
                foreach (var touch in Input.touches) {
                    var xPosNorm = touch.position.x / Screen.width;
                    var yPosNorm = touch.position.y / Screen.height;
                    
                    if (touch.phase == TouchPhase.Began) {
                        touchNext |= xPosNorm < 0.5f && yPosNorm > 0.6f;
                    } else if (touch.phase != TouchPhase.Ended) {
                        touchTop |= xPosNorm > 0.6f && yPosNorm > 0.6f;
                        touchBottom |= xPosNorm > 0.6f && yPosNorm < 0.4f;
                        
                        touchLeft |= xPosNorm < 0.25f && yPosNorm < 0.4f;
                        touchRight |= !touchLeft && xPosNorm < 0.5f && yPosNorm < 0.4f;
                    }
                }
            }
        }
        
        var accelFactor = 0f;
        var brakeFactor = 0f;
        var steerFactor = 0f;
        
        // accelFactor = acceleration * Input.GetAxis("Vertical");
        if (Input.GetKey("up") || touchTop) {
            if (car.rollingBack) {
                brakeFactor = 1f;
            } else {
                accelFactor += 1f;
            }
        }

        // brake on space
        // if (Input.GetKey(KeyCode.Space)) {
        if (Input.GetKey("down") || touchBottom) {
            if (car.rollingForward) {
                brakeFactor = 1f;
            } else {
                accelFactor -= 1f;
            }
        }

        if (Input.GetKey("left") || touchLeft) {
            steerFactor -= 1f;
        }
        if (Input.GetKey("right") || touchRight) {
            steerFactor += 1f;
        }

        // curTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
        
        car.SetControls(accelFactor, brakeFactor, steerFactor);
        
        if (Input.GetKeyDown("t") || touchNext) {
            Debug.Log("switch car");
            
            // disable current camera
            cameras[curCameraIndex].enabled = false;
            
            curCameraIndex = (curCameraIndex + 1) % cameras.Count;
            
            // enable next camera
            cameras[curCameraIndex].enabled = true;
            cameras[curCameraIndex].transform.rotation = cameraStartRotation[curCameraIndex];
        }
    }
}
