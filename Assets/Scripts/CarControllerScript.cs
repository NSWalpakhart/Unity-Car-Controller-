using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;

//public class CarControllerScript : MonoBehaviour
    public class CarControllerScript : NetworkBehaviour
{

    public enum Drivetrain
    {
        FWD,
        RWD,
        AWD
    };

    private float horizontalInput, verticalInput;
    private float steeringAngle;
    private bool isDriveingForward;
    private float localVelocityZ;
    private float localVelocityX;
    private float carSpeed;
    Rigidbody carRigidbody;
    public Text carSpeedText;

    WheelFrictionCurve defCurve;
    WheelFrictionCurve frictionCurve;
    WheelFrictionCurve frictionCurveRear;

    public Vector3 bodyMassCenter;

    public bool useEffects = false;
    public bool isDrifting;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    [Header("Car Settings")]
    public Drivetrain drivetrain = Drivetrain.FWD;
    public float maxSteeringAngle = 30;
    public float torque = 60;
    public float brakeForce = 80;

    [Header("Wheel Colliders")]
    public WheelCollider frontRightWheel;
    public WheelCollider frontLeftWheel;
    public WheelCollider rearRightWheel;
    public WheelCollider rearLeftWheel;

    [Header("Wheels")]
    public Transform frontRightTransform;
    public Transform frontLeftTransform;
    public Transform rearRightTransfrom;
    public Transform rearLeftTransform;

    private void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");


        if (localVelocityZ >0.5) isDriveingForward = true;
        else isDriveingForward = false;

    }
    public void CarSpeedUI()
    {
                float absoluteCarSpeed = Mathf.Abs(carSpeed);
                carSpeedText.text = Mathf.RoundToInt(absoluteCarSpeed).ToString();
        }

    private void Accelerate()
    {
        switch (drivetrain)
        {
            case Drivetrain.FWD:
                frontLeftWheel.motorTorque = torque * verticalInput;
                frontRightWheel.motorTorque = torque * verticalInput;
                break;
            case Drivetrain.RWD:
                rearLeftWheel.motorTorque = torque * verticalInput;
                rearRightWheel.motorTorque = torque * verticalInput;
                break;
            case Drivetrain.AWD:
                frontLeftWheel.motorTorque = torque * verticalInput;
                frontRightWheel.motorTorque = torque * verticalInput;
                rearLeftWheel.motorTorque = torque * verticalInput;
                rearRightWheel.motorTorque = torque * verticalInput;
                break;
        }
    }


    public void Brake()
    {

        if (verticalInput == -1 && isDriveingForward)
        {
            DriftCarPS();
            frontLeftWheel.brakeTorque = brakeForce;
            frontRightWheel.brakeTorque = brakeForce;
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
        }
        else
        {

            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
            
            if (verticalInput == 0)
            {
                MotorBrake();
            }
            else
            {
                frontLeftWheel.brakeTorque = 0;
                frontRightWheel.brakeTorque = 0;
                rearLeftWheel.brakeTorque = 0;
                rearRightWheel.brakeTorque = 0;
            }

        }
        if (Input.GetKey(KeyCode.Space))
        {
            HandBrake();
        }
        else
        {
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }

    public void MotorBrake()
    {
        frontLeftWheel.brakeTorque = brakeForce/50;
            frontRightWheel.brakeTorque = brakeForce/50;
            rearLeftWheel.brakeTorque = brakeForce / 50;
            rearRightWheel.brakeTorque = brakeForce / 50;
        }

    public void HandBrake()
    {
        if(Mathf.Abs(localVelocityX) > 0.1f)
        {
            DriftCarPS();
        }
        rearLeftWheel.brakeTorque = brakeForce;
        rearRightWheel.brakeTorque = brakeForce;
        if (isDrifting)
        {
            switch (drivetrain)
            {
                case Drivetrain.RWD:
                    {
                        frictionCurve.extremumSlip = 1.8f;
                        frictionCurve.stiffness = 1.15f;
                        frictionCurveRear.extremumSlip = 2.2f;
                        frictionCurveRear.stiffness = 0.95f;
                        frontLeftWheel.sidewaysFriction = frictionCurve;
                        frontRightWheel.sidewaysFriction = frictionCurve;
                        rearLeftWheel.sidewaysFriction = frictionCurveRear;
                        rearLeftWheel.sidewaysFriction = frictionCurveRear;
                        rearLeftWheel.motorTorque = torque * verticalInput * 10;
                        rearRightWheel.motorTorque = torque * verticalInput * 10;
                        break;
                    }
                default:
                    frontLeftWheel.sidewaysFriction = defCurve;
                    frontRightWheel.sidewaysFriction = defCurve;
                    rearLeftWheel.sidewaysFriction = defCurve;
                    rearLeftWheel.sidewaysFriction = defCurve;
                    break;
            }
        }

    }

    private void Steer()
    {
        steeringAngle = maxSteeringAngle * horizontalInput;
        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;
    }

    public void Drift()
    {
        if (isDrifting)
        {
            if (Mathf.Abs(localVelocityX) > 2.5f)
            {
                DriftCarPS();
            }
            else
            {
                StopDriftCarPS();
            }
            switch (drivetrain)
            {
                case Drivetrain.RWD:
                    {
                        if (Mathf.Abs(localVelocityX) > 2.5)
                        {
                            frontLeftWheel.sidewaysFriction = frictionCurve;
                            frontRightWheel.sidewaysFriction = frictionCurve;
                            rearLeftWheel.sidewaysFriction = frictionCurveRear;
                            rearLeftWheel.sidewaysFriction = frictionCurveRear;
                            if (Mathf.Abs(localVelocityX) > 5)
                            {
                                frictionCurve.extremumSlip = 1.8f;
                                frictionCurve.stiffness = 1.15f;
                                frictionCurveRear.extremumSlip = 2.2f;
                                frictionCurveRear.stiffness = 0.95f;
                                frontLeftWheel.sidewaysFriction = frictionCurve;
                                frontRightWheel.sidewaysFriction = frictionCurve;
                                rearLeftWheel.sidewaysFriction = frictionCurveRear;
                                rearLeftWheel.sidewaysFriction = frictionCurveRear;
                                rearLeftWheel.motorTorque = torque * verticalInput * 10;
                                rearRightWheel.motorTorque = torque * verticalInput * 10;
                            }
                        }
                        else
                        {
                            frontLeftWheel.sidewaysFriction = defCurve;
                            frontRightWheel.sidewaysFriction = defCurve;
                            rearLeftWheel.sidewaysFriction = defCurve;
                            rearLeftWheel.sidewaysFriction = defCurve;
                            rearLeftWheel.motorTorque = torque * verticalInput;
                            rearRightWheel.motorTorque = torque * verticalInput;
                        }
                    }
                    break;
                default:
                    frontLeftWheel.sidewaysFriction = defCurve;
                    frontRightWheel.sidewaysFriction = defCurve;
                    rearLeftWheel.sidewaysFriction = defCurve;
                    rearLeftWheel.sidewaysFriction = defCurve;
                    break;
            }
        }
    }
    public void StopDriftCarPS()
    {
        RLWParticleSystem.Stop();
        RRWParticleSystem.Stop();
        RLWTireSkid.emitting = false;
        RRWTireSkid.emitting = false;
    }
    public void DriftCarPS()
    {

        if (useEffects)
        {
                    RLWParticleSystem.Play();
                    RRWParticleSystem.Play();
                    RLWTireSkid.emitting = true;
                    RRWTireSkid.emitting = true;

        }
        else if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftWheel, frontLeftTransform);
        UpdateWheelPose(frontRightWheel, frontRightTransform);
        UpdateWheelPose(rearLeftWheel, rearLeftTransform);
        UpdateWheelPose(rearRightWheel, rearRightTransfrom);
    }

    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos = wheelTransform.position;
        Quaternion rot = wheelTransform.rotation;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        defCurve = frontLeftWheel.sidewaysFriction;
        carRigidbody.centerOfMass = bodyMassCenter;


        frictionCurve = frontLeftWheel.sidewaysFriction;
        frictionCurveRear = frontLeftWheel.sidewaysFriction;
        frictionCurve.extremumSlip = 1.39f;
        frictionCurveRear.extremumSlip = 1.8f;
        frictionCurve.stiffness = 1f;
    }
    private void Update()
    {
        switch (drivetrain)
        {
            case Drivetrain.RWD:
                carSpeed = (2 * Mathf.PI * frontLeftWheel.radius * frontLeftWheel.rpm * 60) / 1000;
                break;
            case Drivetrain.FWD:
                carSpeed = (2 * Mathf.PI * rearLeftWheel.radius * rearLeftWheel.rpm * 60) / 1000;
                break;
            default: break;
        }



        localVelocityZ = transform.InverseTransformDirection(carRigidbody.velocity).z;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.velocity).x;


        if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

        CarSpeedUI();
        GetInput();
        Steer();
        Drift();
        Brake();
        Accelerate();
        UpdateWheelPoses();
    }
}
