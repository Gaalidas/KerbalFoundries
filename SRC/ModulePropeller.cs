﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("ModulePropeller")]
    class ModulePropeller : PartModule
    {
        ModuleWheelMaster master;

        [KSPField]
        public float propellerForce = 5;


        public override void OnStart(PartModule.StartState state)
        {
            print("ModulePropeller called");
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight) 
            {
                master = this.part.GetComponentInChildren<ModuleWheelMaster>();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (this.part.Splashed)
            {
                float forwardPropellorForce = master.directionCorrector * propellerForce * this.vessel.ctrlState.wheelThrottle;
                float turningPropellorForce = (propellerForce / 3) * this.vessel.ctrlState.wheelSteer;
                part.rigidbody.AddForce(this.part.GetReferenceTransform().forward * (forwardPropellorForce - turningPropellorForce));
            }
        }
    }
}