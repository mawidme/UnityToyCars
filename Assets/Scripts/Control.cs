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
    private List<Vector3> carStartPositions = new List<Vector3>();

    private TouchControls _touchControls;

    private bool _mpStartScheduled = false;

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
        if (_mpStartScheduled) {
            for (var i=0; i<cars.Count; i++) {
                if (!cars[i].GetComponent<WheelController>().synced) {
                    return;
                }
            }
            _mpStartScheduled = false;
            StartMp();
        }

        var carGameObject = cars[curCameraIndex];
        var car = carGameObject.GetComponent<WheelController>();
        if (car == null) {
            Debug.Log($"no car {curCameraIndex} found, num={cars.Count}");
            return;
        }

        var carPhotonView = carGameObject.GetComponent<PhotonView>();
        if (!carPhotonView.IsMine) {
            // Debug.Log($"selected car {curCameraIndex} not owned, cannot update");
            // return;
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
        
        if (Input.GetKeyDown("r") || _touchControls.Released(TouchControls.ButtonType.ResetCar)) {
            Debug.Log($"reset car: {car.transform.position} -> {carStartPositions[curCameraIndex]}");
            car.transform.position = carStartPositions[curCameraIndex];
            car.transform.rotation = Quaternion.identity;
        }

        //TODO: implement car switch for MP mode (exchange indices, show player name on car)
        // if(!PhotonNetwork.IsConnected)
        {
            if (Input.GetKeyDown("t") || _touchControls.Released(TouchControls.ButtonType.NextCar)) {
                var prevCamIndex = curCameraIndex;

                var nextCamIndex = -1;
                if (PhotonNetwork.IsConnected) {

                    var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
                    nextCamIndex = curCameraIndex;
                    // var nextCarPhotonView = carPhotonView;
                    var nextCar = car;
                    do {
                        nextCamIndex = (nextCamIndex + 1) % cameras.Count;
                        // nextCarPhotonView = cars[nextCamIndex].GetComponent<PhotonView>();
                        nextCar = cars[nextCamIndex].GetComponent<WheelController>();
                    } while (nextCar.playerId != 0 && nextCar.playerId != ownId);

                    Debug.Log($"switch car: {prevCamIndex} -> {nextCamIndex}, ownId={ownId}");

                    car.playerId = 0;
                    nextCar.playerId = ownId;

                    cars[nextCamIndex].GetComponent<PhotonView>().TransferOwnership(ownId);
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

            carObject.GetComponent<WheelController>().SetCarIndex(i-1);

            cars.Add(carObject);
            
            carStartPositions.Add(carObject.transform.position);
            
            i++;
        }

        Debug.Log("num cars: " + cars.Count);

        curCameraIndex = 0;
        cameras[curCameraIndex].enabled = true;
    }

    public void ScheduleStartMp() {
        Debug.Log("ScheduleStartMp");
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1) {
            // no players to sync car player id from yet
            StartMp();
        } else{
            _mpStartScheduled = true;
        }
    }

    public void StartMp() {
        var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("StartMp, ownId: " + ownId);

        // 1) select car based on actor id
        /*
        curCameraIndex = ownId-1;
        var carPhotonView = cars[curCameraIndex].GetComponent<PhotonView>();
        carPhotonView.TransferOwnership(ownId);
        cameras[curCameraIndex].enabled = true;
        Debug.Log($"selected car {curCameraIndex}");
        */

        // 2) select free car
        curCameraIndex = -1;
        for(var j=0; j<cars.Count; j++) {
            var curCar = cars[j].GetComponent<WheelController>();

            if (curCar.playerId == 0) {
                curCameraIndex = j;
                Debug.Log($"selected car {curCameraIndex}");

                cars[curCameraIndex].GetComponent<PhotonView>().TransferOwnership(ownId);
                cameras[curCameraIndex].enabled = true;

                curCar.playerId = ownId;
                break;
            }
        }
        if (curCameraIndex == -1) {
            //TODO: handle no car free
            Debug.Log("no free car found");
        }

        for(var j=0; j<cars.Count; j++) {
            var curPhotonView = cars[j].GetComponent<PhotonView>();
            var curCar = cars[j].GetComponent<WheelController>();
            Debug.Log($"car {j}: PlayerId={curCar.playerId}, IsMine={curPhotonView.IsMine}, OwnerActorNr={curPhotonView.OwnerActorNr}");
        }
    }

    private void StartTouchControls()
    {
        _touchControls = GameObject.Find("UI").GetComponent<TouchControls>();
    }
}
