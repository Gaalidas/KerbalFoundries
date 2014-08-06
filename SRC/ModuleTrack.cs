﻿/*
 * KSP [0.23.5] Anti-Grav Repulsor plugin by Lo-Fi
 * Much inspiration taken from BahamutoD, snjo and the guys from RBI fgor the original track code.
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("ModuleTrack")]
    public class ModuleTrack : PartModule
    {
//variable setup 
        public int directionCorrector;
        public float motorTorque;
        public float numberOfWheels = 1; //if it's 0 at the start it send things into and NaN fit.
        public float trackRPM = 0;
        public float averageTrackRPM;
        [KSPField]
        public float trackLength = 100;
        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();
        [KSPField]
        public FloatCurve steeringCurve = new FloatCurve();
        [KSPField]
        public FloatCurve brakeSteeringCurve = new FloatCurve();
        [KSPField]
        public float brakingTorque;
        public float brakeTorque;
        public float brakeSteering;


        public GameObject trackSurface;

        //public Transform bounds;
        public bool boundsDestroyed;
        [KSPField(isPersistant = true)]
        public bool brakesApplied;

        [KSPField]
        public float raycastError;

        public float degreesPerTick;
//tweakables
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque ratio"), UI_FloatRange(minValue = 0, maxValue = 2f, stepIncrement = .1f)]
        public float torque = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring strength"), UI_FloatRange(minValue = 0, maxValue = 3.00f, stepIncrement = 0.2f)]
        public float springRate;        //this is what's tweaked by the line above
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 0.2f, stepIncrement = 0.025f)]
        public float damperRate;        //this is what's tweaked by the line above
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
        public bool steeringDisabled;
//end twekables
        
//end variable setup

        public override void OnStart(PartModule.StartState start)  //when started
        {
            print("ModuleTrack called");
            base.OnStart(start);
            if (HighLogic.LoadedSceneIsEditor)
            {
                
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //torque /= 100;
                this.part.force_activate(); //force the part active or OnFixedUpate is not called
                foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>())
                {
                    JointSpring userSpring = wc.suspensionSpring;
                    userSpring.spring = springRate;
                    userSpring.damper = damperRate;
                    wc.suspensionSpring = userSpring;
                    wc.enabled = true;
                }

                float dot = Vector3.Dot(this.part.transform.forward, vessel.ReferenceTransform.up); // up is forward
                if (dot < 0) // below 0 means the engine is on the left side of the craft
                {
                    directionCorrector = -1;
                    print("left");
                }
                else
                {
                    directionCorrector = 1;
                    print("right");
                }

/*
                if (this.part.orgPos.x < 0)
                {
                    directionCorrector = 1;
                }
                else
                {
                    directionCorrector = -1;
                }
*/
                foreach (SkinnedMeshRenderer potentialTrackOrWheel in this.part.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    trackSurface = potentialTrackOrWheel.gameObject;
                }

                if (brakesApplied)
                {
                    brakeTorque = brakingTorque;
                }
                if (boundsDestroyed == false)
                {
                    Transform bounds = transform.Search("Bounds");
                    GameObject.Destroy(bounds.gameObject);
                    boundsDestroyed = true;
                    print("destroying Bounds");
                }
            }
       }//end OnStart



        public override void OnUpdate()
        {
//User input
            float electricCharge;
            float chargeRequest;
            float forwardTorque = torqueCurve.Evaluate((float)this.vessel.srfSpeed) * torque;
            float steeringTorque;
            float brakeSteeringTorque;

            Vector3 roverForward = this.vessel.ReferenceTransform.forward;
            Vector3 roverUp = this.vessel.ReferenceTransform.up;
            Vector3 travelVector = this.vessel.GetSrfVelocity();
            float travelDirection = Vector3.Dot((roverForward + roverUp), travelVector);

            if (!steeringDisabled)
            {
                steeringTorque = steeringCurve.Evaluate((float)this.vessel.srfSpeed) * torque;
                brakeSteering = brakeSteeringCurve.Evaluate(travelDirection);
            }
            else
            {
                steeringTorque = 0;
                brakeSteering = 0;
            }


            motorTorque = (forwardTorque * directionCorrector * this.vessel.ctrlState.wheelThrottle) - (steeringTorque * this.vessel.ctrlState.wheelSteer);
            brakeSteeringTorque = Mathf.Clamp(brakeSteering * directionCorrector * this.vessel.ctrlState.wheelSteer, 0, 150); //if the calculated value is negative, disregards
            chargeRequest = Math.Abs(motorTorque * 0.002f);

            electricCharge = part.RequestResource("ElectricCharge", chargeRequest);

            float freeWheelRPM = 0;
            foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>())
            {
                if (electricCharge == 0)
                {
                    motorTorque = 0;
                }
                wc.motorTorque = motorTorque;
                wc.brakeTorque = brakeTorque + brakeSteeringTorque;

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
                averageTrackRPM = freeWheelRPM / 4;
            }
            degreesPerTick = (averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel
            trackRPM = 0;
            float distanceTravelled = (float)((averageTrackRPM * 2 * Math.PI) / 60) * Time.deltaTime; //calculate how far the track will need to move
            Material trackMaterial = trackSurface.renderer.material;    //set things up for changing the texture offset on the track
            Vector2 textureOffset = trackMaterial.mainTextureOffset;
            textureOffset = textureOffset + new Vector2(-distanceTravelled/trackLength, 0); //tracklength is used to fine tune the speed of movement.
            trackMaterial.SetTextureOffset("_MainTex", textureOffset);
            trackMaterial.SetTextureOffset("_BumpMap", textureOffset);
            print("frame");

            //print(roverForward);
            print((float) brakeSteeringCurve.Evaluate(travelDirection));
            print(travelDirection);
            //print(this.vessel.srf_velocity.z);
            //print(this.vessel.GetFwdVector());
//            print(this.vessel.angularVelocity.z);
            //print(motorTorque);
            //print(brakeTorque);
            //print(brakeSteeringTorque);
            motorTorque = 0; //reset motortorque
            numberOfWheels = 1; //reset number of wheels. Setting to zero gives NaN!
        } //end OnUpdate
//Action groups
        [KSPAction("Brakes", KSPActionGroup.Brakes)]
        public void brakes(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate)
            {
                brakeTorque = brakingTorque * ((torque/2)+.5f);
                brakesApplied = true;
            }
            else
            {
                brakeTorque = 0;
                brakesApplied = false;
            }
        }
//end action groups
    }//end class
}//end namespace