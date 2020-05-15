using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class TapHold
    {
        public abstract bool IsHold { get; }

        [Newtonsoft.Json.JsonProperty]
        private int tick;

        [Newtonsoft.Json.JsonProperty]
        private int laneIndex;

        public int Tick{
            get{ return tick; }
            set{
                if(tick == value) return;
                if(tick < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                tick = value;
            }
        }

        public int LaneIndex{
            get{return laneIndex;}
            set{
                CheckPosition(laneIndex);
                laneIndex = value;
            }
        }

        public abstract int Duration{
            get;
            set;
        }

        public int GetDuration => Duration;

        public void CheckPosition(int laneIndex){
            if(laneIndex < 1 || laneIndex<=Constants.LanesCount)
                throw new ArgumentOutOfRangeException("width", "Invalid width.");
        }

        public void SetPosition(int laneIndex){
            this.laneIndex=laneIndex;
        }
    }
}
