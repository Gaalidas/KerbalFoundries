﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("TrackWheel")]
    public class TrackWheel : PartModule
    {
        //start variables
        [KSPField]
        public string wheelName;
        [KSPField]
        public string colliderName;
        [KSPField]
        public string sustravName;
        [KSPField]
        public string steeringName;
        [KSPField]
        public bool useDirectionCorrector = false; //make sure it's set to false if not specified in the config
        [KSPField]
        public bool isSprocket;
        [KSPField]
        public bool isIdler;
        [KSPField]
        public bool hasSteering;
        [KSPField]
        public float smoothSpeed = 40;

        WheelCollider wheelCollider;
        Transform susTrav;
        Transform wheel;
        Transform trackSteering;
        ModuleTrack track;
        ModuleWheelMaster master;
        Vector3 initialTraverse;
        Vector3 initialSteeringAngles;

        Vector3 currentAngles;

        float smoothSteeringAngle;
        
        float lastTempTraverse;
        [KSPField]
        public float rotationCorrection = 1;

        public int directionCorrector = 1;
        //end variables

        [KSPField]
        public float wheelRotationX = 1;
        [KSPField]
        public float wheelRotationY = 0;
        [KSPField]
        public float wheelRotationZ = 0;

        [KSPField]
        public string susTravAxis = "Y";
        [KSPField]
        public string steeringAxis = "Y";

        int susTravIndex = 1;
        int steeringIndex = 1;

        Vector3 wheelRotation;

        //OnStart
        public override void OnStart(PartModule.StartState state)
        {
            print("TrackWheel Called");
            if (HighLogic.LoadedSceneIsEditor)
            {

            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                //find names onjects in part
                this.part.force_activate();

                

                foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>())
                {
                    if (wc.name.Equals(colliderName, StringComparison.Ordinal))
                    {
                        wheelCollider = wc;
                    }
                }
                foreach (Transform tr in this.part.GetComponentsInChildren<Transform>())
                {
                    if (tr.name.Equals(sustravName, StringComparison.Ordinal))
                    {
                        susTrav = tr;
                    }
                }
                foreach (Transform tr in this.part.GetComponentsInChildren<Transform>())
                {
                    if (tr.name.Equals(wheelName, StringComparison.Ordinal))
                    {
                        wheel = tr;
                    }
                }
                foreach (Transform tr in this.part.GetComponentsInChildren<Transform>())
                {
                    if (tr.name.Equals(steeringName, StringComparison.Ordinal))
                    {
                        trackSteering = tr;
                    }
                }

                track = this.part.GetComponentInChildren<ModuleTrack>();
                master = this.part.GetComponentInChildren<ModuleWheelMaster>();

                susTravIndex = Extensions.SetAxisIndex(susTravAxis);
                steeringIndex = Extensions.SetAxisIndex(steeringAxis);

                initialTraverse = susTrav.transform.localPosition;
                initialSteeringAngles = trackSteering.transform.localEulerAngles;
                print(initialSteeringAngles);
                
                lastTempTraverse = initialTraverse[susTravIndex] - wheelCollider.suspensionDistance; //sets it to a default value for the sprockets and wheels

                if (useDirectionCorrector)
                    directionCorrector = master.directionCorrector;
                else directionCorrector = 1;
                print(directionCorrector);

                wheelRotation = new Vector3(wheelRotationX, wheelRotationY, wheelRotationZ);
            }
            currentAngles = initialSteeringAngles;
            //end find named objects
            base.OnStart(state);
        }//end OnStart
        //OnUpdate
        public override void OnUpdate()
        {
            base.OnUpdate();
            wheel.transform.Rotate(wheelRotation, track.degreesPerTick / wheelCollider.radius * directionCorrector * rotationCorrection); //rotate wheel
            //suspension movement
            WheelHit hit;
            Vector3 tempTraverse = initialTraverse;
            bool grounded = wheelCollider.GetGroundHit(out hit); //set up to pass out wheelhit coordinates
            if (grounded && !isSprocket) //is it on the ground
            {
                tempTraverse[susTravIndex] -= (-wheelCollider.transform.InverseTransformPoint(hit.point).y + track.raycastError) - wheelCollider.radius;// / wheelCollider.suspensionDistance; //out hit does not take wheel radius into account
                lastTempTraverse = tempTraverse[susTravIndex];
            }
            else
            {
                tempTraverse[susTravIndex] = lastTempTraverse;
            } //movement defaults back to zero when not grounded
            susTrav.transform.localPosition = tempTraverse; //move the suspensioTraverse object

           
            //end suspension mvoement
        }//end OnUpdate

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (hasSteering)
            {
                Vector3 newSteeringAngle = initialSteeringAngles;
                newSteeringAngle[steeringIndex] = initialSteeringAngles[steeringIndex] + track.steeringAngleSmoothed;

                currentAngles = Vector3.Lerp(currentAngles, newSteeringAngle, Time.deltaTime);

                //newSteeringAngle[steeringIndex] = Mathf.Lerp(newSteeringAngle[steeringIndex] + track.steeringAngle, trackSteering.transform.localEulerAngles[steeringIndex], Time.deltaTime * smoothSpeed);
                //print(currentAngles);
                trackSteering.transform.localEulerAngles = newSteeringAngle;
            }
        }

    }//end modele
}//end class