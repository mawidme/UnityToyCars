using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Photon.Pun;

public class WheelController : MonoBehaviour, IPunObservable
{
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider backRight;
    [SerializeField] WheelCollider backLeft;
    
    //TODO
    // to turn wheels: link to individual wheel meshes
    // [SerializeField] Transform frontRightTransform;
    // [SerializeField] Transform frontLeftTransform;
    // [SerializeField] Transform backRightTransform;
    // [SerializeField] Transform backLeftTransform;

    public float acceleration = 500f;
    public float brakeForce = 300f;
    public float standBrakeForce = 10f;
    // public float maxTurnAngle = 15f;
    public float maxTurnAngle = 25f;

    private float curAcceleration = 0f;
    private float curBrakeForce = 0f;
    private float curTurnAngle = 0f;
    
    public float deltaX = 0f;
    public bool touchToggle = false;
    
    public bool rollingForward = false;
    public bool rollingBack = false;

    // MP
    public bool needToSync = true; // set to true when player joins
    public int playerId = 0;
    
    private int carIndex = -1;
    public void SetCarIndex(int index) {
        carIndex = index;
    }

    public void SetControls(float accelFactor, float brakeFactor, float steerFactor) {
        curAcceleration = accelFactor * acceleration;
        curBrakeForce = brakeFactor * brakeForce;
        if (accelFactor == 0f && brakeFactor == 0f) {
            curBrakeForce = standBrakeForce;
        }
        
        curTurnAngle = steerFactor * maxTurnAngle;        
    }
    
    private void FixedUpdate() {

        var totalRpm = frontRight.rpm + frontLeft.rpm + backRight.rpm + backLeft.rpm;
        rollingForward = totalRpm > 10f;
        rollingBack = totalRpm < -10f;

        frontRight.motorTorque = curAcceleration;
        frontLeft.motorTorque = curAcceleration;

        frontRight.brakeTorque = curBrakeForce;
        frontLeft.brakeTorque = curBrakeForce;
        backRight.brakeTorque = curBrakeForce;
        backLeft.brakeTorque = curBrakeForce;

        frontLeft.steerAngle = curTurnAngle;
        frontRight.steerAngle = curTurnAngle;

        //TODO
        // UpdateWheel(frontRight, frontRightTransform);
        // UpdateWheel(frontLeft, frontLeftTransform);
        // UpdateWheel(backRight, backRightTransform);
        // UpdateWheel(backLeft, backLeftTransform);
    }

    void UpdateWheel(WheelCollider col, Transform transform) {
        Vector3 pos;
        Quaternion rot;

        col.GetWorldPose(out pos, out rot);

        // set wheel transform
        transform.position = pos;
        transform.rotation = rot;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

 #region IPunObservable implementation
    //TODO: refactor as PUN RPC to save bandwidth?
    //TODO: move to separate controller class and add as component to all cars
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(playerId);
            // Debug.Log($"sent playerId {playerId}");
        }
        else
        {
            // Network player, receive data
            var oldPlayerId = playerId;
            playerId = (int)stream.ReceiveNext();
            if (playerId != oldPlayerId) {
                Debug.Log($"car {carIndex}: playerId {oldPlayerId} -> {playerId}");
            }

            if (needToSync) {
                needToSync = false;
                // Debug.Log($"car {carIndex}: synced");
            }
        }
    }

#endregion    
}
