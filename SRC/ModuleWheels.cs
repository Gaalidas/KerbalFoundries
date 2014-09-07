﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("ModuleWheels")]
    public class ModuleWheels : PartModule
    {
        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();
        [KSPField]
        public FloatCurve steeringCurve = new FloatCurve();
        [KSPField]
        public FloatCurve brakeSteeringCurve = new FloatCurve();
        [KSPField]
        public float brakingTorque;
        [KSPField]
        public float rollingResistanceMultiplier;
        [KSPField]
        public float rollingResistance;
        [KSPField]
        public float raycastError;
        [KSPField(isPersistant = false, guiActive = true, guiName = "RPM", guiFormat = "F1")]
        public float averageTrackRPM;
        [KSPField]
        public float maxRPM = 350;
        [KSPField(isPersistant = true)]
        public bool brakesApplied;

        ModuleWheelMaster master;
        public float brakeTorque;

        public float brakeSteering;
        public float degreesPerTick;
        public float motorTorque;
        public float numberOfWheels = 1; //if it's 0 at the start it send things into and NaN fit.
        public float trackRPM = 0;

        public float wheelCount;
        public float calculatedRollingResistance;

        //tweakables
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque ratio"), UI_FloatRange(minValue = 0, maxValue = 2f, stepIncrement = .25f)]
        public float torque = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float springRate;        //this is what's tweaked by the line above
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 1.0f, stepIncrement = 0.025f)]
        public float damperRate;        //this is what's tweaked by the line above
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
        public bool steeringDisabled;

        public override void OnStart(PartModule.StartState start)  //when started
        {
            print("ModuleWheels called");
            base.OnStart(start);
            if (HighLogic.LoadedSceneIsEditor)
            {
                //Do nothing in editor
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                master = this.part.GetComponentInChildren<ModuleWheelMaster>();
                wheelCount = 0;
                this.part.force_activate(); //force the part active or OnFixedUpate is not called
                foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>()) //set colliders to values chosen in editor and activate
                {
                    wheelCount++;
                    JointSpring userSpring = wc.suspensionSpring;
                    userSpring.spring = springRate;
                    userSpring.damper = damperRate;
                    wc.suspensionSpring = userSpring;
                    wc.enabled = true;
                }

                if (brakesApplied)
                {
                    brakeTorque = brakingTorque; //were the brakes left applied
                }

            }//end scene is flight
        }//end OnStart

        public override void OnFixedUpdate()
        {
            //User input
            float electricCharge;
            float chargeRequest;
            float forwardTorque = torqueCurve.Evaluate((float)this.vessel.srfSpeed) * torque; //this is used a lot, so may as well calculate once


            Vector3 travelVector = this.vessel.GetSrfVelocity();

            //float travelDirection = Vector3.Dot(this.part.transform.forward, travelVector); //compare travel velocity with the direction the part is pointed.
            //print(travelDirection);




            motorTorque = (forwardTorque * master.directionCorrector * this.vessel.ctrlState.wheelThrottle); //forward and low speed steering torque. Direction controlled by precalulated directioncorrector
            chargeRequest = Math.Abs(motorTorque * 0.0005f); //calculate the requested charge

            electricCharge = part.RequestResource("ElectricCharge", chargeRequest); //ask the vessel for requested charge

            float freeWheelRPM = 0;
            foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>())
            {
                if (electricCharge == 0 || Math.Abs(averageTrackRPM) >= maxRPM)
                {
                    motorTorque = 0;
                }
                wc.motorTorque = motorTorque;
                wc.brakeTorque = brakeTorque + rollingResistance;

                if (wc.isGrounded) //only count wheels in contact with the floor. Others will be freewheeling and will wreck the calculation. 
                {
                    numberOfWheels++;
                    trackRPM += wc.rpm;
                }
                else if (wc.suspensionDistance != 0) //the sprocket colliders could be doing anything. Don't count them.
                {
                    freeWheelRPM += wc.rpm;
                }
            }
            if (numberOfWheels > 1)
            {
                averageTrackRPM = trackRPM / numberOfWheels;
            }
            else
            {
                averageTrackRPM = freeWheelRPM / wheelCount;
            }
            trackRPM = 0;

        }//end OnFixedUpdate

        public override void OnUpdate()
        {
            base.OnUpdate();
            degreesPerTick = (averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel
            numberOfWheels = 1; //reset number of wheels. Setting to zero gives NaN!
        } //end OnUpdate

        //Action groups
        [KSPAction("Brakes", KSPActionGroup.Brakes)]
        public void brakes(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate)
            {
                brakeTorque = brakingTorque * ((torque / 2) + .5f);
                brakesApplied = true;
            }
            else
            {
                brakeTorque = 0;
                brakesApplied = false;
            }
        }
        [KSPAction("Increase Torque")]
        public void increase(KSPActionParam param)
        {
            if (torque < 2)
            {
                torque += 0.25f; 
            }

        }//End increase

        [KSPAction("Decrease Toqrque")]
        public void decrease(KSPActionParam param)
        {
            if (torque > 0)
            {
                torque -= 0.25f;
            }
        }//end decrease
        [KSPAction("Toggle Steering")]
        public void toggleSteering(KSPActionParam param)
        {
            steeringDisabled = !steeringDisabled;
        }//end toggle steering
        //end action groups
    }//end class
}//end namespace