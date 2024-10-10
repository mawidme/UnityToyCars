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

            if (i > 10) {
                Debug.Log("imax");
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
        if (Input.GetKeyDown("t")) {
            Debug.Log("switch car");
            
            // disable old car
            var carName = "toyCar"+(curCameraIndex+1);
            var carObject = GameObject.Find(carName);
            carObject.GetComponent<WheelController>().deactivate();

            cameras[curCameraIndex].enabled = false;
            curCameraIndex = (curCameraIndex + 1) % cameras.Count;
            cameras[curCameraIndex].enabled = true;
            
            // enable new car
            carName = "toyCar"+(curCameraIndex+1);
            carObject = GameObject.Find(carName);
            carObject.GetComponent<WheelController>().activate();
        }
    }
}
