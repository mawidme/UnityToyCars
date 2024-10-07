using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelController : MonoBehaviour
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
    public float breakForce = 300f;
    public float standBreakForce = 10f;
    // public float maxTurnAngle = 15f;
    public float maxTurnAngle = 25f;

    private float curAcceleration = 0f;
    private float curBreakForce = 0f;
    private float curTurnAngle = 0f;

    public string forwardKey = "w";
    public string backKey = "s";
    public string rightKey = "d";
    public string leftKey = "a";
    
    private void FixedUpdate() {
        curAcceleration = 0f;
        curBreakForce = 0f;

        var totalRpm = frontRight.rpm + frontLeft.rpm + backRight.rpm + backLeft.rpm;
        var rollingForward = totalRpm > 10f;
        var rollingBack = totalRpm < -10f;

        var noKeyPressed = true;

        // curAcceleration = acceleration * Input.GetAxis("Vertical");
        if (Input.GetKey(forwardKey)) {
            noKeyPressed = false;
            if (rollingBack) {
                curBreakForce = breakForce;
            } else {
                curAcceleration += acceleration;
            }
        }

        // brake on space
        // if (Input.GetKey(KeyCode.Space)) {
        if (Input.GetKey(backKey)) {
            noKeyPressed = false;
            if (rollingForward) {
                curBreakForce = breakForce;
            } else {
                curAcceleration -= acceleration;
            }
        }

        if (noKeyPressed) {
            curBreakForce = standBreakForce;
        }

        frontRight.motorTorque = curAcceleration;
        frontLeft.motorTorque = curAcceleration;

        frontRight.brakeTorque = curBreakForce;
        frontLeft.brakeTorque = curBreakForce;
        backRight.brakeTorque = curBreakForce;
        backLeft.brakeTorque = curBreakForce;

        // curTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
        var steer = 0f;
        if (Input.GetKey(leftKey)) {
            steer -= 1f;
        }
        if (Input.GetKey(rightKey)) {
            steer += 1f;
        }
        curTurnAngle = maxTurnAngle * steer;
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
}
