using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public class Fader: NoteBase
    {
        private VerticalDirection verticalDirection;

        public VerticalDirection VerticalDirection{
            get{ return verticalDirection; }
            set{ verticalDirection = value; }
        }
    }

    public enum VerticalDirection
    {
        UP,
        DOWND
    }
}
