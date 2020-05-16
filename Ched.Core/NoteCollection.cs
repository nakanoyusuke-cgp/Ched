using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;

namespace Ched.Core
{
    /// <summary>
    /// ノーツを格納するコレクションを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class NoteCollection
    {
        [Newtonsoft.Json.JsonProperty] private List<Pad> pads;

        [Newtonsoft.Json.JsonProperty] private List<Fader> faders;

        [Newtonsoft.Json.JsonProperty] private List<Knob> knobs;

        public List<Pad> Pads
        {
            get => pads;
            set => pads = value;
        }

        public List<Fader> Faders
        {
            get => faders;
            set => faders = value;
        }

        public List<Knob> Knobs
        {
            get => knobs;
            set => knobs = value;
        }


        public NoteCollection()
        {
            pads = new List<Pad>();
            faders = new List<Fader>();
            knobs = new List<Knob>();
        }

        public NoteCollection(NoteCollection collection)
        {
            pads = collection.Pads.ToList();
            faders = collection.faders.ToList();
            knobs = collection.knobs.ToList();
        }

        public IEnumerable<TapHold> GetTaps() => GetAllNotes().Where(x => !x.IsHold);

        public IEnumerable<TapHold> GetHolds() => GetAllNotes().Where(x => x.IsHold);

        public IEnumerable<NoteBase> GetTapsInNoteBase() => GetAllNotesInNoteBase().Where(x => !x.TapHold.IsHold);
        public IEnumerable<NoteBase> GetHoldsInNoteBase() => GetAllNotesInNoteBase().Where(x => x.TapHold.IsHold);
        
        public IEnumerable<NoteBase> GetAllNotesInNoteBase()
        {
            foreach (var tapHold in pads)
                yield return tapHold;

            foreach (var tapHold in faders)
                yield return tapHold;

            foreach (var tapHold in knobs)
                yield return tapHold;
        }

        public IEnumerable<TapHold> GetAllNotes()
        {
            foreach (var tapHold in pads.Select(x => x.TapHold))
                yield return tapHold;

            foreach (var tapHold in faders.Select(x => x.TapHold))
                yield return tapHold;

            foreach (var tapHold in knobs.Select(x => x.TapHold))
                yield return tapHold;
        }

        public void UpdateTicksPerBeat(double factor)
        {
            foreach (var note in GetTaps())
                note.Tick = (int) (note.Tick * factor);

            foreach (var hold in GetHolds())
            {
                hold.Tick = (int) (hold.Tick * factor);
                hold.Duration = (int) (hold.Duration * factor);
            }
        }
    }
}