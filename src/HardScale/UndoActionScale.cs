using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;

namespace HardScale
{
    class UndoActionScale : UndoAction
    {
        protected Vector3 lastPos;
        protected Vector3 lastScale;
        protected Vector3 pos;
        protected Vector3 scale;

        public UndoActionScale(Machine m, Guid blockGuid, Vector3 p, Vector3 lPos, Vector3 scale, Vector3 lastScale)
        {
            this.pos = p;
            this.lastPos = lPos;
            this.scale = scale;
            this.lastScale = lastScale;

            this.guid = blockGuid;
            this.changesTransform = true;
            this.machine = m;

            this.machine.MoveBlock(this.guid, this.machine.BuildingMachine.TransformPoint(p));
            this.machine.ScaleBlock(this.guid, this.scale);
        }

        public override bool Redo()
        {
            this.machine.MoveBlock(this.guid, this.machine.BuildingMachine.TransformPoint(this.pos));
            this.machine.ScaleBlock(this.guid, this.scale);
            if (!this.isMultiAction)
            {
                this.machine.RebuildClusters();
                AdvancedBlockEditor.Instance.UpdateTool();
            }
            return true;
        }

        public override bool Undo()
        {
            this.machine.MoveBlock(this.guid, this.machine.BuildingMachine.TransformPoint(this.lastPos));
            this.machine.ScaleBlock(this.guid, this.lastScale);
            if (!this.isMultiAction)
            {
                this.machine.RebuildClusters();
                AdvancedBlockEditor.Instance.UpdateTool();
            }
            return true;
        }
    }
}
