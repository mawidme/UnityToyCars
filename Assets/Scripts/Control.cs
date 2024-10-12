using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    private int curCameraIndex = 0;
    private List<Camera> cameras = new List<Camera>();
    private List<WheelController> cars = new List<WheelController>();
    
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
        
        cars[0].activate();
    }

    // Update is called once per frame
    void Update()
    {
        var touchNext = false;
        foreach (var touch in Input.touches) {
            if (touch.phase != TouchPhase.Began) {
                if (touch.phase != TouchPhase.Ended) {
                    // Debug.Log($"touch moved: {touch.deltaPosition.x}, {touch.deltaPosition.y}");
                    cameras[curCameraIndex].transform.Rotate(touch.deltaPosition.y/50, -touch.deltaPosition.x/50, 0);
                }
                continue;
            }
            
            var xPosNorm = touch.position.x / Screen.width;
            var yPosNorm = touch.position.y / Screen.height;
            
            touchNext |= xPosNorm < 0.5f && yPosNorm > 0.6f;
        }
        
        if (Input.GetKeyDown("t") || touchNext) {
            Debug.Log("switch car");
            
            // disable current car/camera
            cars[curCameraIndex].deactivate();
            cameras[curCameraIndex].enabled = false;
            
            curCameraIndex = (curCameraIndex + 1) % cameras.Count;
            
            // enable next car/camera
            cars[curCameraIndex].activate();
            cameras[curCameraIndex].enabled = true;
        }
    }
}
