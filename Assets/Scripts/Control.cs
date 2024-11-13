using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using TMPro;

using Photon.Realtime;
using Photon.Pun;

public class Control : MonoBehaviour
{
    private int curCarIndex = 0;
    private List<Camera> cameras = new List<Camera>();
    private List<Quaternion> cameraStartRotation = new List<Quaternion>();
    
    // private List<GameObject> carObjects = new List<GameObject>();
    private List<WheelController> cars = new List<WheelController>();
    private List<PhotonView> carViews = new List<PhotonView>();
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
        if (WaitForMp()) return;

        var car = cars[curCarIndex];
        if (car == null) {
            Debug.Log($"no car {curCarIndex} found, num={cars.Count}");
            return;
        }

        /*
        var carPhotonView = carViews[curCarIndex];
        if (!carPhotonView.IsMine) {
            Debug.Log($"selected car {curCarIndex} not owned, cannot update");
            return;
        }
        */
        
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
            Debug.Log($"reset car: {car.transform.position} -> {carStartPositions[curCarIndex]}");
            car.transform.position = carStartPositions[curCarIndex];
            car.transform.rotation = Quaternion.identity;
        }

        //TODO: implement car switch for MP mode (exchange indices, show player name on car)
        // if(!PhotonNetwork.IsConnected)
        {
            if (Input.GetKeyDown("t") || _touchControls.Released(TouchControls.ButtonType.NextCar)) {
                var prevCarIndex = curCarIndex;

                var nextCamIndex = -1;
                if (PhotonNetwork.IsConnected) {
                    var takenCars = MpGetTakenCars();
                    if (takenCars.Count < cars.Count) {
                        var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
                        nextCamIndex = curCarIndex;
                        var nextCar = car;
                        do {
                            nextCamIndex = (nextCamIndex + 1) % cameras.Count;
                            nextCar = cars[nextCamIndex];
                        } while (takenCars.Contains(nextCamIndex));

                        Debug.Log($"switch car: {prevCarIndex} -> {nextCamIndex}, ownId={ownId}");

                        MpSelectCar(nextCamIndex);

                        carViews[nextCamIndex].TransferOwnership(ownId);
                    }
                } else {
                    nextCamIndex = (curCarIndex + 1) % cameras.Count;
                    Debug.Log($"switch car: {prevCarIndex} -> {nextCamIndex}");                
                }

                // disable current camera
                cameras[prevCarIndex].enabled = false;
                
                // enable next camera
                cameras[nextCamIndex].enabled = true;
                // cameras[nextCamIndex].transform.rotation = cameraStartRotation[curCarIndex];
                
                curCarIndex = nextCamIndex;
            }
        }
    }

    private void StartCars()
    {
        Debug.Log("control start");

        // find cameras
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

        // find cars
        i = 1;
        while (true) {
            var carObject = GameObject.Find("toyCar"+i);
            if (carObject == null) {
                break;
            }

            carStartPositions.Add(carObject.transform.position);
            
            var carView = carObject.GetComponent<PhotonView>();
            carViews.Add(carView);

            var car = carObject.GetComponent<WheelController>();
            cars.Add(car);
            car.SetCarIndex(i-1);

            var carName = $"mpPlayerName{i}";
            // Debug.Log($"car {i} name obj: {carName}");
            var mpPlayerNameObject = GameObject.Find(carName);
            if (mpPlayerNameObject != null) {
                car._mpPlayerNameText = mpPlayerNameObject.GetComponent<TMP_Text>();
                Debug.Log($"found name text for car {i}: {car._mpPlayerNameText}");
                car._mpPlayerNameText.enabled = false;
            } else {
                Debug.Log($"no name object found for car {i}");
            }

            i++;
        }

        Debug.Log("num cars: " + cars.Count);

        curCarIndex = 0;
        cameras[curCarIndex].enabled = true;
    }

    public void ScheduleStartMp() {
        Debug.Log("ScheduleStartMp");

        TriggerCarResync();

        //TODO: quicker sync for PhotonNetwork.PlayerListOthers?
        if (PhotonNetwork.CountOfPlayers == 1) {
            // no players to sync car player id from yet
            StartMp();
        } else{
            _mpStartScheduled = true;
        }
    }

    public bool WaitForMp() {
        if (_mpStartScheduled) {
            foreach (var remotePlayer in PhotonNetwork.PlayerListOthers) {
                var playerId = remotePlayer.CustomProperties["playerId"];
                if (playerId == null) {
                    return true;
                }
            }
            _mpStartScheduled = false;
            StartMp();
        }

        return false;
    }

    public void StartMp() {
        var ownId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("StartMp, ownId: " + ownId);

        // select free car
        var takenCars = MpGetTakenCars();
        curCarIndex = -1;
        for(var j=0; j<cars.Count; j++) {
            var curCar = cars[j];

            if (!takenCars.Contains(j)) {
                curCarIndex = j;
                Debug.Log($"selected car {curCarIndex}");

                carViews[curCarIndex].TransferOwnership(ownId);
                cameras[curCarIndex].enabled = true;

                MpSelectCar(curCarIndex);

                break;
            }
        }
        if (curCarIndex == -1) {
            //TODO: disconnect, red MP button
            Debug.Log("no free car found");
        }
    }

    private List<int> MpGetTakenCars() {
        var takenCars = PhotonNetwork.PlayerListOthers.Select(p => (int)p.CustomProperties["playerId"]).ToList();
        Debug.Log("MpGetTakenCars: " + string.Join(", ", takenCars));
        return takenCars;
    }

    private void MpSelectCar(int carIndex) {
        Debug.Log("MpSelectCar: " + carIndex);
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "playerId", carIndex } });
    }

    private void StartTouchControls()
    {
        _touchControls = GameObject.Find("UI").GetComponent<TouchControls>();
    }

    public void TriggerCarResync()
    {
        Debug.Log("TriggerCarResync");
    }

    public void HandleMpPlayerLeft(int playerId) {
        Debug.Log("HandleMpPlayerLeft: " + playerId);
    }

    public void HandleMpPlayerPropertiesUpdate(Player player, ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log($"HandleMpPlayerPropertiesUpdate: player {player.ActorNumber}, properties {propertiesThatChanged}");
        // Debug.Log($"HandleMpPlayerPropertiesUpdate: player {player.ActorNumber}");

        var oldCarIndex = -1;
        if (player != null && player.CustomProperties != null && player.CustomProperties["playerId"] != null) {
            // remove previous car's player name
            // oldCarIndex = (int)player.CustomProperties["playerId"];
            oldCarIndex = (int)player.CustomProperties["playerId"] - 1;
            if (oldCarIndex < 0) oldCarIndex = cars.Count -1;
            WheelController oldCar = cars[oldCarIndex];
            // oldCar._mpPlayerNameText.text = "";
            oldCar._mpPlayerNameText.enabled = false;
        } else {
            Debug.Log("player CustomProperties null");
        }

        if (propertiesThatChanged["playerId"] != null) {
            var newCarIndex = (int)propertiesThatChanged["playerId"];
            Debug.Log($"player {player.ActorNumber}/'{player.NickName}' car switch: {oldCarIndex} -> {newCarIndex}");

            //TODO: truncate
            // add new car's player name
            WheelController newCar = cars[newCarIndex];
            newCar._mpPlayerNameText.text = player.NickName;
            newCar._mpPlayerNameText.enabled = true;
        }
    }
}
