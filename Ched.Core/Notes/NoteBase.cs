using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public abstract class NoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private TapHold tapHold;

        public TapHold TapHold
        {
            get => tapHold;
        }
    }
}
