using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Hold : TapHold
    {

        [Newtonsoft.Json.JsonProperty]
        private int duration = 1;

        [Newtonsoft.Json.JsonProperty]
        private StartTap startTap;

        [Newtonsoft.Json.JsonProperty]
        private EndTap endTap;

        public override bool IsHold{
            get{return true;}
        }


        /// <summary>
        /// ノートの長さを設定します。
        /// </summary>
        public override int Duration
        {
            get { return duration; }
            set
            {
                if (duration == value) return;
                if (duration <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
                duration = value;
            }
        }

        public void SetPosition(int laneIndex)
        {
            LaneIndex = laneIndex;
        }
        
        public StartTap StartNote { get { return startTap; } }
        public EndTap EndNote { get { return endTap; } }
        
        public Hold()
        {
            startTap = new StartTap(this);
            endTap = new EndTap(this);
        }
        
        public int GetDuration()
        {
            return Duration;
        }
        
        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public abstract class LongNoteTapBase
        {
            public abstract bool IsTap { get; }
            public abstract int Tick { get; }
            protected Hold parent;
        
            public int LaneIndex { get { return parent.LaneIndex; } }
        
            public LongNoteTapBase(Hold parent)
            {
                this.parent = parent;
            }
        }
        
        public class StartTap : LongNoteTapBase
        {
            public override int Tick { get { return parent.Tick; } }
        
            public override bool IsTap { get { return true; } }
        
            public StartTap(Hold parent) : base(parent)
            {
            }
        }
        
        public class EndTap : LongNoteTapBase
        {
            public override bool IsTap { get { return false; } }
        
            public override int Tick { get { return parent.Tick + parent.Duration; } }
        
            public EndTap(Hold parent) : base(parent)
            {
            }
        }
    }
}
