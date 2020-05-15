using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Tap : TapHold
    {
        public override bool IsHold => false;

        public override int Duration{
            get{return 0;}
            set{return;}
        }
    }
}
