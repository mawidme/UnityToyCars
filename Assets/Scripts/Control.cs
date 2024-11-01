using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Photon.Realtime;
using Photon.Pun;

public class Control : MonoBehaviour
{
    private int curCameraIndex = 0;
    private List<Camera> cameras = new List<Camera>();
    private List<Quaternion> cameraStartRotation = new List<Quaternion>();
    // private List<WheelController> cars = new List<WheelController>();
    private List<GameObject> cars = new List<GameObject>();
    
    private TouchControls _touchControls;

    // camera rotation disabled
    // private bool rotatingCamera = false;
    // private float camRotDeltaSqr = 20f;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCars();
        
        StartTouchControls();
    }

    // Update is called once per frame
    void Update()
    {
        // var car = cars[curCameraIndex];
        var carGameObject = cars[curCameraIndex];
        var car = carGameObject.GetComponent<WheelController>();
        if (car == null) {
            Debug.Log($"no car {curCameraIndex} found, num={cars.Count}");
            return;
        }

        var carPhotonView = carGameObject.GetComponent<PhotonView>();
        if (!carPhotonView.IsMine) {
            Debug.Log($"selected car {curCameraIndex} not owned, cannot update");
            return;
        }
        
        var accelFactor = 0f;
        var brakeFactor = 0f;
        var steerFactor = 0f;
        
        // accelFactor = acceleration * Input.GetAxis("Vertical");
        if (Input.GetKey("up") || _touchControls.Held(TouchControls.ButtonType.Accelerate)) {
            if (car.rollingBack) {
                brakeFactor = 1f;
            } else {
                accelFactor += 1f;
            }
        }

        // brake on space
        // if (Input.GetKey(KeyCode.Space)) {
        if (Input.GetKey("down") || _touchControls.Held(TouchControls.ButtonType.Brake)) {
            if (car.rollingForward) {
                brakeFactor = 1f;
            } else {
                accelFactor -= 1f;
            }
        }

        if (Input.GetKey("left") || _touchControls.Held(TouchControls.ButtonType.SteerLeft)) {
            steerFactor -= 1f;
        }
        if (Input.GetKey("right") || _touchControls.Held(TouchControls.ButtonType.SteerRight)) {
            steerFactor += 1f;
        }

        // curTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
        
        car.SetControls(accelFactor, brakeFactor, steerFactor);
        
        if (Input.GetKeyDown("r") || _touchControls.Released(TouchControls.ButtonType.NextCar)) {
        }
        
        //TODO: implement car switch for MP mode
        if(!PhotonNetwork.IsConnected) {
            if (Input.GetKeyDown("t") || _touchControls.Released(TouchControls.ButtonType.NextCar)) {
                var prevCamIndex = curCameraIndex;

                var nextCamIndex = -1;
                if (PhotonNetwork.IsConnected) {

                    var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
                    nextCamIndex = curCameraIndex;
                    var nextCarPhotonView = carPhotonView;
                    do {
                        nextCamIndex = (nextCamIndex + 1) % cameras.Count;
                        nextCarPhotonView = cars[nextCamIndex].GetComponent<PhotonView>();
                    } while (nextCarPhotonView.OwnerActorNr != 0 && nextCarPhotonView.OwnerActorNr != ownId);

                    Debug.Log($"switch car: {prevCamIndex} -> {nextCamIndex}, ownId={ownId}");

                    carPhotonView.TransferOwnership(0);

                    nextCarPhotonView.TransferOwnership(ownId);
                } else {
                    nextCamIndex = (curCameraIndex + 1) % cameras.Count;
                    Debug.Log($"switch car: {prevCamIndex} -> {nextCamIndex}");                
                }

                // disable current camera
                cameras[prevCamIndex].enabled = false;
                
                // enable next camera
                cameras[nextCamIndex].enabled = true;
                // cameras[nextCamIndex].transform.rotation = cameraStartRotation[curCameraIndex];
                
                curCameraIndex = nextCamIndex;
            }
        }
    }

    private void StartCars()
    {
        Debug.Log("control start");

        // detect cameras
        int i = 1;
        while (true) {
            // Debug.Log("checking camera " + i);
            
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

        // detect cars
        i = 1;
        while (true) {
            var carObject = GameObject.Find("toyCar"+i);
            if (carObject == null) {
                break;
            }

            // cars.Add(carObject.GetComponent<WheelController>());
            cars.Add(carObject);
            
            i++;
        }

        Debug.Log("num cars: " + cars.Count);

        if(PhotonNetwork.IsConnected) {
            var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("ownId: " + ownId);

            // 1) select car based on actor id
            curCameraIndex = ownId-1;
            var carPhotonView = cars[curCameraIndex].GetComponent<PhotonView>();
            carPhotonView.TransferOwnership(ownId);
            cameras[curCameraIndex].enabled = true;
            Debug.Log($"selected car {curCameraIndex}");

            // 2) select free car
            /*
            curCameraIndex = -1;
            for(var j=0; j<cars.Count; j++) {
                var carPhotonView = cars[j].GetComponent<PhotonView>();

                if (curCameraIndex >= 0) {
                    if (carPhotonView.OwnerActorNr == ownId) {
                        Debug.Log($"de-selected car {j}");
                        carPhotonView.TransferOwnership(0);
                    }
                } else if (carPhotonView.IsMine || carPhotonView.OwnerActorNr == 0) {
                    curCameraIndex = j;
                    Debug.Log($"selected car {curCameraIndex}");
                    carPhotonView.TransferOwnership(ownId);
                    cameras[curCameraIndex].enabled = true;
                }
            }
            if (curCameraIndex == -1) {
                //TODO: handle no car free
                Debug.Log("no free car found");
            }
            */

            for(var j=0; j<cars.Count; j++) {
                var curPhotonView = cars[j].GetComponent<PhotonView>();
                Debug.Log($"car {j}: IsMine={curPhotonView.IsMine}, OwnerActorNr={curPhotonView.OwnerActorNr}");
            }
        } else {
            // single player (PUN not connected)
            Debug.Log("single player -> selected first car");
            
            curCameraIndex = 0;
            cameras[curCameraIndex].enabled = true;
        }
    }

    private void StartTouchControls()
    {
        _touchControls = GameObject.Find("TouchControls").GetComponent<TouchControls>();
    }
}
