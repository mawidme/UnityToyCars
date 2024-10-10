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
    public float brakeForce = 300f;
    public float standBrakeForce = 10f;
    // public float maxTurnAngle = 15f;
    public float maxTurnAngle = 25f;

    private float curAcceleration = 0f;
    private float curBrakeForce = 0f;
    private float curTurnAngle = 0f;

    // public string forwardKey = "w";
    // public string backKey = "s";
    // public string rightKey = "d";
    // public string leftKey = "a";
    private string forwardKey = "up";
    private string backKey = "down";
    private string rightKey = "right";
    private string leftKey = "left";
    
    public float deltaX = 0f;
    public bool touchToggle = false;
    
    public bool isActive = false;
    
    public void activate() { isActive = true; Debug.Log("car activated"); }
    public void deactivate() { isActive = false; Debug.Log("car deactivated"); }
    
    private void FixedUpdate() {
        curAcceleration = 0f;
        curBrakeForce = 0f;
        var steer = 0f;

        var totalRpm = frontRight.rpm + frontLeft.rpm + backRight.rpm + backLeft.rpm;
        var rollingForward = totalRpm > 10f;
        var rollingBack = totalRpm < -10f;

        var controlInput = false;

        var touchTop = false;
        var touchBottom = false;
        var touchLeft = false;
        var touchRight = false;
        var touchPrev = false;
        var touchNext = false;
        foreach (var touch in Input.touches) {
            var xPosNorm = touch.position.x / Screen.width;
            var yPosNorm = touch.position.y / Screen.height;
            
            touchTop |= xPosNorm > 0.6f && yPosNorm > 0.6f;
            touchBottom |= xPosNorm > 0.6f && yPosNorm < 0.4f;
            
            touchLeft |= xPosNorm < 0.25f && yPosNorm < 0.4f;
            touchRight |= !touchLeft && xPosNorm < 0.5f && yPosNorm < 0.4f;

            touchPrev |= xPosNorm < 0.25f && yPosNorm > 0.6f;
            touchNext |= !touchPrev && xPosNorm < 0.5f && yPosNorm > 0.6f;
        }
        
        if (isActive) {
            // curAcceleration = acceleration * Input.GetAxis("Vertical");
            if (Input.GetKey(forwardKey) || touchTop) {
                controlInput = true;
                if (rollingBack) {
                    curBrakeForce = brakeForce;
                } else {
                    curAcceleration += acceleration;
                }
            }

            // brake on space
            // if (Input.GetKey(KeyCode.Space)) {
            if (Input.GetKey(backKey) || touchBottom) {
                controlInput = true;
                if (rollingForward) {
                    curBrakeForce = brakeForce;
                } else {
                    curAcceleration -= acceleration;
                }
            }

            if (Input.GetKey(leftKey) || touchLeft) {
                steer -= 1f;
            }
            if (Input.GetKey(rightKey) || touchRight) {
                steer += 1f;
            }
        }

        if (!controlInput) {
            curBrakeForce = standBrakeForce;
        }

        frontRight.motorTorque = curAcceleration;
        frontLeft.motorTorque = curAcceleration;

        frontRight.brakeTorque = curBrakeForce;
        frontLeft.brakeTorque = curBrakeForce;
        backRight.brakeTorque = curBrakeForce;
        backLeft.brakeTorque = curBrakeForce;

        // curTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal");
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
        Debug.Log("WheelController start, isActive=" + isActive);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
