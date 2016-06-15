﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CrazyStorm.Core;

namespace CrazyStorm_Player.CrazyStorm
{
    abstract class Emitter : Component
    {
        public Vector2 EmitPosition { get; set; }
        public int EmitCount { get; set; }
        public int EmitCycle { get; set; }
        public float EmitAngle { get; set; }
        public float EmitRange { get; set; }
        public ParticleBase Template { get; protected set; }
        public IList<ParticleBase> Particles { get; private set; }
        public IList<EventGroup> EmitterEventGroups { get; private set; }
        public Emitter()
        {
            Particles = new List<ParticleBase>();
        }
        public override void LoadPlayData(BinaryReader reader)
        {
            base.LoadPlayData(reader);
            using (BinaryReader emitterReader = PlayDataHelper.GetBlockReader(reader))
            {
                using (BinaryReader dataReader = PlayDataHelper.GetBlockReader(emitterReader))
                {
                    EmitPosition = PlayDataHelper.ReadVector2(dataReader);
                    EmitCount = dataReader.ReadInt32();
                    EmitCycle = dataReader.ReadInt32();
                    EmitAngle = dataReader.ReadSingle();
                    EmitRange = dataReader.ReadSingle();
                }
                //particle
                Template.LoadPlayData(emitterReader);
                //emitterEventGroups
                PlayDataHelper.LoadObjectList(EmitterEventGroups, emitterReader);
                Template.ParticleEventGroups = EmitterEventGroups;
            }
        }
        public override bool PushProperty(string propertyName)
        {
            if (base.PushProperty(propertyName))
                return true;

            switch (propertyName)
            {
                case "EmitPosition": 
                    VM.PushVector2(EmitPosition);
                    return true;
                case "EmitPosition.x":
                    VM.PushFloat(EmitPosition.x);
                    return true;
                case "EmitPosition.y":
                    VM.PushFloat(EmitPosition.y);
                    return true;
                case "EmitCount":
                    VM.PushInt(EmitCount);
                    return true;
                case "EmitCycle":
                    VM.PushInt(EmitCycle);
                    return true;
                case "EmitAngle":
                    VM.PushFloat(EmitAngle);
                    return true;
                case "EmitRange":
                    VM.PushFloat(EmitRange);
                    return true;
            }
            return Template.PushProperty(propertyName);
        }
        public override bool SetProperty(string propertyName)
        {
            if (base.SetProperty(propertyName))
                return true;

            switch (propertyName)
            {
                case "EmitPosition":
                    EmitPosition = VM.PopVector2();
                    return true;
                case "EmitPosition.x":
                    EmitPosition = new Vector2(VM.PopFloat(), EmitPosition.y);
                    return true;
                case "EmitPosition.y":
                    EmitPosition = new Vector2(EmitPosition.x, VM.PopFloat());
                    return true;
                case "EmitCount":
                    EmitCount = VM.PopInt();
                    return true;
                case "EmitCycle":
                    EmitCycle = VM.PopInt();
                    return true;
                case "EmitAngle":
                    EmitAngle = VM.PopFloat();
                    return true;
                case "EmitRange":
                    EmitRange = VM.PopFloat();
                    return true;
            }
            return Template.SetProperty(propertyName);
        }
        public override bool Update(int currentFrame)
        {
            if (!base.Update(currentFrame))
                return false;

            if (currentFrame % EmitCycle == 0)
                Emit();
            
            return true;
        }
        public override void Reset()
        {
            base.Reset();
            var initialState = base.initialState as Emitter;
            EmitPosition = initialState.EmitPosition;
            EmitCount = initialState.EmitCount;
            EmitCycle = initialState.EmitCycle;
            EmitAngle = initialState.EmitAngle;
            EmitRange = initialState.EmitRange;
        }
        public void Emit()
        {
            base.ExecuteExpression("EmitCycle");
            base.ExecuteExpression("EmitRange");
            base.ExecuteExpression("EmitCount");
            base.ExecuteExpression("EmitAngle");
            base.ExecuteExpression("EmitPosition");
            Template.PPosition = EmitPosition;
            float increment = EmitRange / EmitCount;
            float angle = EmitAngle;
            for (int i = 0; i < EmitCount; ++i)
            {
                angle += increment;
                Template.PSpeedAngle = angle;
                ParticleBase newParticle = ParticleManager.GetParticle(Template);
                newParticle.Emitter = this;
                Particles.Add(newParticle);
            }
        }
    }
}