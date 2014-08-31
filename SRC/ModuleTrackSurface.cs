﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalFoundries
{
    [KSPModule("ModuleTrackSurface")]
    public class ModuleTrackSurface : PartModule
    {
        GameObject trackSurface;
        ModuleTrack track;
        [KSPField]
        public float trackLength = 10;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            foreach (SkinnedMeshRenderer Track in this.part.GetComponentsInChildren<SkinnedMeshRenderer>()) //this is the track
            {
                trackSurface = Track.gameObject;
            }

            track = this.part.GetComponentInChildren<ModuleTrack>();

        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            float degreesPerTick = (track.averageTrackRPM / 60) * Time.deltaTime * 360; //calculate how many degrees to rotate the wheel
            float distanceTravelled = (float)((track.averageTrackRPM * 2 * Math.PI) / 60) * Time.deltaTime; //calculate how far the track will need to move
            Material trackMaterial = trackSurface.renderer.material;    //set things up for changing the texture offset on the track
            Vector2 textureOffset = trackMaterial.mainTextureOffset;
            textureOffset = textureOffset + new Vector2(-distanceTravelled / trackLength, 0); //tracklength is used to fine tune the speed of movement.
            trackMaterial.SetTextureOffset("_MainTex", textureOffset);
            trackMaterial.SetTextureOffset("_BumpMap", textureOffset);
        }

    }
}
