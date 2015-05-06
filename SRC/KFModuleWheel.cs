﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public class KFModuleWheel : PartModule
    {
        //name definitions
        public string right = "right";
        public string forward = "forward";
        public string up = "up";

        //tweakables
        [KSPField(isPersistant = false, guiActive = true, guiName = "Wheel Settings")]
        public string settings = "";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Group Number"), UI_FloatRange(minValue = 0, maxValue = 10f, stepIncrement = 1f)]
        public float groupNumber = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque Ratio"), UI_FloatRange(minValue = 0, maxValue = 2f, stepIncrement = .25f)]
        public float torque = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Strength"), UI_FloatRange(minValue = 0, maxValue = 6.00f, stepIncrement = 0.2f)]
        public float springRate;        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Damping"), UI_FloatRange(minValue = 0, maxValue = 1.0f, stepIncrement = 0.025f)]
        public float damperRate;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Suspension Travel"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 5)]
        public float rideHeight = 100;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"), UI_Toggle(disabledText = "Enabled", enabledText = "Disabled")]
        public bool steeringDisabled;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Start"), UI_Toggle(disabledText = "Deployed", enabledText = "Retracted")]
        public bool startRetracted;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status = "Nominal";

        //config fields
        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();       //torque applied to wheel colliders
        [KSPField]
        public FloatCurve steeringCurve = new FloatCurve();     //degrees to turn wheels for rotational steering
        [KSPField]
        public FloatCurve torqueSteeringCurve = new FloatCurve();   //toruqe applied to wheelcolliders for low speed tank steering
        [KSPField]
        public FloatCurve brakeSteeringCurve = new FloatCurve();    //brake torque applied to wheel colliders for high speed tank steering
        [KSPField]
        public bool hasSteering = false;                //this enables wheel (turn based) steering
        [KSPField]
        public float brakingTorque;                     //torque to apply for brakes
        [KSPField]
        public FloatCurve rollingResistance = new FloatCurve();                 //constant brake value aplpied to simulate rolling resistance
        [KSPField]
        public FloatCurve loadCoefficient = new FloatCurve();                 //constant brake value aplpied to simulate rolling resistance
        [KSPField]
        public float smoothSpeed = 10;                  //steering speed
        [KSPField]
        public float raycastError;                      //used to compensate for error in the raycast
        [KSPField]
        public float maxRPM = 350;                      //rev limiter. Stop freewheel runaway and sets a speed limit
        [KSPField]
        public float chargeConsumptionRate = 1f;        //how fast to consume ElectricCharge
        [KSPField]
        public bool hasRetract = false;
        [KSPField]
        public bool hasRetractAnimation = false;
        [KSPField]
        public string boundsName = "Bounds";
        [KSPField]
        public bool passivePart = false;

        //persistent fields
        [KSPField(isPersistant = true)]
        public float steeringInvert = 1;                //will be minus one for inverted
        [KSPField(isPersistant = true)]
        public bool brakesApplied;                      //saves the brake state
        [KSPField(isPersistant = true)]
        public bool isRetracted = false;
        [KSPField(isPersistant = true, guiActive = false, guiName = "TS", guiFormat = "F1")] //debug only.
        public float tweakScaleCorrector = 1;

        //global variables
        int rootIndexLong;      
        int rootIndexLat;
        int rootIndexUp;
        int controlAxisIndex;  
        uint commandId;
        uint lastCommandId;
        float brakeTorque;
        float brakeSteering;
        float motorTorque;
        int groundedWheels = 0; 
        float effectPower;
        float trackRPM = 0;

        //stuff deliberately made available to other modules:
        public float steeringAngle;
        public float steeringAngleSmoothed;
        public float appliedRideHeight;
        public int wheelCount;
        [KSPField(isPersistant = false, guiActive = true, guiName = "RPM", guiFormat = "F1")]
        public float averageTrackRPM;
        public int directionCorrector = 1;
        public int steeringCorrector = 1;
        public float steeringRatio;
        public float degreesPerTick;
        public float currentRideHeight;
        public float smoothedRideHeight;

        public List<WheelCollider> wcList = new List<WheelCollider>();
        
        public override void OnStart(PartModule.StartState start)  //when started
        {
            base.OnStart(start);
            print(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            
            if (startRetracted)
            {
                isRetracted = true;
            }
            if (!isRetracted)
                currentRideHeight = rideHeight; //set up correct values from persistence
            else
                currentRideHeight = 0;
            /* SharpDevelop reports that these two "if" checks can be replaced with single line checks. - Gaalidas WRONG
            isRetracted |= startRetracted;
            currentRideHeight = !isRetracted ? rideHeight : 0;
            smoothedRideHeight = currentRideHeight;
            appliedRideHeight = smoothedRideHeight / 100;
             * */
            //print(appliedRideHeight);
           
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (!hasRetract)
                {
                    Extensions.DisableAnimateButton(this.part);
                    Actions["AGToggleDeployed"].active = false;
                    Actions["Deploy"].active = false;
                    Actions["Retract"].active = false;
                    Fields["startRetracted"].guiActiveEditor = false;
                } 
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                maxRPM /= tweakScaleCorrector;
                startRetracted = false;
                if (!hasRetract)
                    Extensions.DisableAnimateButton(this.part);

                // wheel steering ratio setup
                rootIndexLong = WheelUtils.GetRefAxis(this.part.transform.forward, this.vessel.rootPart.transform); //Find the root part axis which matches each wheel axis.
                rootIndexLat = WheelUtils.GetRefAxis(this.part.transform.right, this.vessel.rootPart.transform);
                rootIndexUp = WheelUtils.GetRefAxis(this.part.transform.up, this.vessel.rootPart.transform);

                steeringRatio = WheelUtils.SetupRatios(rootIndexLong, this.part, this.vessel, groupNumber); //use the axis which corresponds to the forward axis of the wheel.
                GetControlAxis(); // sets up motor and steering direction direction

                if (torque > 2) //check if the torque value is using the old numbering system
                {
                    torque /= 100;
                }
                wheelCount = 0;
                this.part.force_activate(); //force the part active or OnFixedUpate is not called
                foreach (WheelCollider wc in this.part.GetComponentsInChildren<WheelCollider>()) //set colliders to values chosen in editor and activate
                {
                    wheelCount++;
                    JointSpring userSpring = wc.suspensionSpring;
                    userSpring.spring = springRate * tweakScaleCorrector;
                    userSpring.damper = damperRate * tweakScaleCorrector;
                    wc.suspensionSpring = userSpring;
                    wc.suspensionDistance = wc.suspensionDistance * appliedRideHeight;
                    wcList.Add(wc);
                    wc.enabled = true;
                    wc.gameObject.layer = 27;
                }
                if (brakesApplied)
                {
                    brakeTorque = brakingTorque; //were the brakes left applied 
                }
                if (isRetracted)
                    UpdateColliders("retract");
            }//end scene is flight
            DestroyBounds(); //destroys the Bounds helper object if it is still in the model.
        }//end OnStart

        public void WheelSound()
        {
            part.Effect("WheelEffect", effectPower);
        }

        public override void OnFixedUpdate()
        {
            //User input
            float electricCharge;
            float unitLoad = 0;
            float forwardTorque = torqueCurve.Evaluate((float)this.vessel.srfSpeed /tweakScaleCorrector) * torque; //this is used a lot, so may as well calculate once
            float steeringTorque;
            float brakeSteeringTorque;

            Vector3 travelVector = this.vessel.GetSrfVelocity();

            float travelDirection = Vector3.Dot(this.part.transform.forward, travelVector); //compare travel velocity with the direction the part is pointed.
            //print(travelDirection);

            if (!steeringDisabled)
            {
                steeringTorque = torqueSteeringCurve.Evaluate((float)this.vessel.srfSpeed / tweakScaleCorrector) * torque * steeringInvert; //low speed steering mode. Differential motor torque
                brakeSteering = brakeSteeringCurve.Evaluate(travelDirection) / tweakScaleCorrector * steeringInvert; //high speed steering. Brake on inside track because Unity seems to weight reverse motor torque less at high speed.
                steeringAngle = (steeringCurve.Evaluate((float)this.vessel.srfSpeed)) * -this.vessel.ctrlState.wheelSteer * steeringRatio * steeringCorrector * steeringInvert; //low speed steering mode. Differential motor torque
            }
            else
            {
                steeringTorque = 0;
                brakeSteering = 0;
                steeringAngle = 0;
            }
    
            if (!isRetracted)
            {
                motorTorque = (forwardTorque * directionCorrector * this.vessel.ctrlState.wheelThrottle) - (steeringTorque * this.vessel.ctrlState.wheelSteer); //forward and low speed steering torque. Direction controlled by precalulated directioncorrector
                brakeSteeringTorque = Mathf.Clamp(brakeSteering * this.vessel.ctrlState.wheelSteer, 0, 1000); //if the calculated value is negative, disregard: Only brake on inside track. no need to direction correct as we are using the velocity or the part not the vessel.
                //chargeRequest = Math.Abs(motorTorque * 0.0005f); //calculate the requested charge
                
                float chargeConsumption = Time.deltaTime * chargeConsumptionRate * (Math.Abs(motorTorque) / 100);
                //print(chargeConsumption);
                electricCharge = part.RequestResource("ElectricCharge", chargeConsumption);
                float freeWheelRPM = 0;
                for(int i = 0; i < wcList.Count(); i++)
                {
                    if (electricCharge != chargeConsumption)
                    { 
                        motorTorque = 0;
                        status = "Low Charge";
                    }
                    else if (Math.Abs(averageTrackRPM) >= maxRPM)
                    {
                        motorTorque = 0;
                        status = "Rev Limit";
                    }
                    else
                    {
                        status = "Nominal";
                    }

                    WheelHit hit;
                    bool grounded = wcList[i].GetGroundHit(out hit); //set up to pass out wheelhit coordinates
                    unitLoad += hit.force;
                    unitLoad /= wcList.Count();
                    unitLoad *= 10;
                    print("unitLoad = " + unitLoad);
                    var rollingFriction = rollingResistance.Evaluate((float)this.vessel.srfSpeed) * loadCoefficient.Evaluate((float)unitLoad); // 
                    print("rollingfriction = "+ rollingFriction);

                    wcList[i].motorTorque = motorTorque;
                    wcList[i].brakeTorque = brakeTorque + brakeSteeringTorque + rollingFriction;

                    if (wcList[i].isGrounded) //only count wheels in contact with the floor. Others will be freewheeling and will wreck the calculation. 
                    {
                        groundedWheels++;
                        trackRPM += wcList[i].rpm;
                    }
                    else if (wcList[i].suspensionDistance != 0) //the sprocket colliders could be doing anything. Don't count them.
                    {
                        freeWheelRPM += wcList[i].rpm;
                    }

                    

                    if(hasSteering)
                        wcList[i].steerAngle = steeringAngleSmoothed;
                }

                if (groundedWheels >= 1)
                {
                    averageTrackRPM = trackRPM / groundedWheels;
                }
                else
                {
                    averageTrackRPM = freeWheelRPM / wheelCount;
                }
                trackRPM = 0;
                degreesPerTick = (averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel mesh
                groundedWheels = 0; //reset number of wheels.
            }
            else //if(isRetracted)
            {
                averageTrackRPM = 0;
                degreesPerTick = 0;
                steeringAngle = 0;
            
                for (int i = 0; i < wcList.Count(); i++)
                {
                    wcList[i].motorTorque = 0;
                    wcList[i].brakeTorque = 500;
                    wcList[i].steerAngle = 0;
                }
            }
            smoothedRideHeight = Mathf.Lerp(smoothedRideHeight, currentRideHeight, Time.deltaTime * 2);
            appliedRideHeight = smoothedRideHeight / 100;
            steeringAngleSmoothed = Mathf.Lerp(steeringAngleSmoothed, steeringAngle, Time.deltaTime * smoothSpeed);
            //print(smoothedRideHeight); //debugging
        }//end OnFixedUpdate

        public override void OnUpdate()
        {
            base.OnUpdate();
            commandId = this.vessel.referenceTransformId;
            if (commandId != lastCommandId)
            {
                print("Control Axis Changed");
                GetControlAxis();
            }
            lastCommandId = commandId;
            effectPower = Math.Abs(averageTrackRPM / maxRPM);
            WheelSound();
        } //end OnUpdate

        public void UpdateColliders(string mode)
        {
			// I am unsure if this is more efficient or what, but this format was suggested. - Gaalidas
			switch (mode) {
				case "retract":
                	isRetracted = true;
                	currentRideHeight = 0;
                	Events["ApplySettings"].guiActive = false;
                	Events["InvertSteering"].guiActive = false;
                	Fields["rideHeight"].guiActive = false;
                	Fields["torque"].guiActive = false;
                	//Fields["springRate"].guiActive = false;
                	//Fields["damperRate"].guiActive = false;
                	Fields["steeringDisabled"].guiActive = false;
                	status = "Retracted";
					break;
				case "deploy":
                	isRetracted = false;
                	currentRideHeight = rideHeight;
                	Events["ApplySettings"].guiActive = true;
                	Events["InvertSteering"].guiActive = true;
                	Fields["rideHeight"].guiActive = true;
                	Fields["torque"].guiActive = true;
                	//Fields["springRate"].guiActive = true;
                	//Fields["damperRate"].guiActive = true;
                	Fields["steeringDisabled"].guiActive = true;
                	status = "Nominal";
					break;
				case "update":
					break;
            }
        }

        public void GetControlAxis()
        {
            controlAxisIndex = WheelUtils.GetRefAxis(this.part.transform.forward, this.vessel.ReferenceTransform); //grab current values for the part and the control module, which may ahve changed.
            directionCorrector = WheelUtils.GetCorrector(this.part.transform.forward, this.vessel.ReferenceTransform, controlAxisIndex); // dets the motor direction correction again.
            if (controlAxisIndex == rootIndexLat)       //uses the precalulated forward (as far as this part is concerned) to determined the orientation of the control module
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexLat);
            if (controlAxisIndex == rootIndexLong)
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexLong);
            if (controlAxisIndex == rootIndexUp)
                steeringCorrector = WheelUtils.GetCorrector(this.vessel.ReferenceTransform.up, this.vessel.rootPart.transform, rootIndexUp);
        }

        public void PlayAnimation()
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing
            ModuleAnimateGeneric myAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
            if (!myAnimation)
            {
                return; //the Log.Error line fails syntax check with 'The name 'Log' does not appear in the current context.
            }
            else
            {
                myAnimation.Toggle();
            }
        }

        public void DestroyBounds()
        {
            Transform bounds = transform.Search(boundsName);
            if (bounds != null)
            {
                GameObject.Destroy(bounds.gameObject);
                //boundsDestroyed = true; //remove the bounds object to let the wheel colliders take over
                print("destroying Bounds");
            }
        }

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

        [KSPAction("Decrease Torque")]
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
        [KSPAction("Invert Steering")]
        public void InvertSteeringAG(KSPActionParam param)
        {
            InvertSteering();
        }//end toggle steering
        //end action groups
        
        [KSPEvent(guiActive = true, guiName = "Invert Steering", active = true)]
        public void InvertSteering()
        {
            steeringInvert *= -1;
        }

        [KSPEvent(guiActive = true, guiName = "Apply Wheel Settings", active = true)]
        public void applySettingsGUI()
        {
            ApplySettings(false);
        }
        
        public void ApplySettings(bool actionGroup)
        {
            foreach (KFModuleWheel mt in this.vessel.FindPartModulesImplementing<KFModuleWheel>())
            {
                if (groupNumber != 0 && groupNumber == mt.groupNumber && !actionGroup)
                {
                    currentRideHeight = rideHeight;
                    mt.currentRideHeight = rideHeight;
                    mt.rideHeight = rideHeight;
                    mt.torque = torque;
                }
                if (actionGroup || groupNumber == 0)
                {
                    currentRideHeight = rideHeight;
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Apply Steering Settings", active = true)]
        public void ApplySteeringSettings()
        {
            foreach (KFModuleWheel mt in this.vessel.FindPartModulesImplementing<KFModuleWheel>())
            {
                if (groupNumber != 0 && groupNumber == mt.groupNumber)
                {
                    mt.steeringRatio = WheelUtils.SetupRatios(mt.rootIndexLong, mt.part, this.vessel, groupNumber);
                    mt.steeringInvert = steeringInvert;
                }
            }
        }

        [KSPAction("Toggle Deployed")]
        public void AGToggleDeployed(KSPActionParam param)
        {
            if (isRetracted)
            {
                Deploy(param);
            }
            else
            {
                Retract(param);
            }
        }//End Deploy toggle

        [KSPAction("Deploy")]
        public void Deploy(KSPActionParam param)
        {
            if (isRetracted == true)
            {
                if(hasRetractAnimation)
                    PlayAnimation();
                UpdateColliders("deploy");
            }
        }//end Reploy

        [KSPAction("Retract")]
        public void Retract(KSPActionParam param)
        {
            if (isRetracted == false)
            {
                if (hasRetractAnimation)
                    PlayAnimation();
                UpdateColliders("retract");
            }
        }//end Retract

        //Addons by Gaalidas
        [KSPAction("Lower Suspension")]
        public void LowerRideHeight(KSPActionParam param)
        {
            if (rideHeight > 0)
            {
                rideHeight -= 5;
            }
            ApplySettings(true);
        }//end decrease
  
        [KSPAction("Raise Suspension")]
        public void RaiseRideHeight(KSPActionParam param)
        {
            if (rideHeight < 100)
            {
                rideHeight += 5;
            }
            ApplySettings(true);
        }//end increase

        [KSPAction("Apply Wheel")]
        public void ApplyWheelAction(KSPActionParam param)
        {
            ApplySettings(true);
        }
  
        [KSPAction("Apply Steering")]
        public void ApplySteeringAction(KSPActionParam param)
        {
            ApplySteeringSettings();
        }
        //End Addons by Gaalidas
    }//end class
}//end namespaces
