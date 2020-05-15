using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public class Knob: NoteBase
    {
        private HorizontalDirection hroizontalDirection;
        
        public Knob(TapHold tapHold) : base(tapHold)
        {
        }

        public HorizontalDirection HroizontalDirection{
            get{ return hroizontalDirection; }
            set{ hroizontalDirection = value; }
        }
    }

    public enum HorizontalDirection
    {
        Left,
        Right
    }
}
