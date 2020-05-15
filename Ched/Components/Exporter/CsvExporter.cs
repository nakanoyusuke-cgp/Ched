using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ched.Core;
using Ched.Core.Events;
using Ched.Core.Notes;
using Un4seen.Bass.AddOn.Vst;

/*
MIT License

Copyright (c) 2017 Paralleltree

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

namespace Ched.Components.Exporter
{
    class CsvExporter : IExtendedExpoerter<SusArgs>
    {
        enum NoteAttribute
        {
            Knob,
            Fader,
            Pad
        }

        private class Note
        {
            public float startTime;
            public int lane;
            public NoteAttribute attribute;
            public int durationByTick;
            public float durationBySec;
            public int direction;

            public override string ToString()
            {
                var ary = new string[]
                {
                    startTime.ToString(),
                    lane.ToString(),
                    ((int)attribute).ToString(),
                    durationByTick.ToString(),
                    durationBySec.ToString(),
                    direction.ToString()
                };
                return string.Join(",", ary);
            }
        }

        public string FormatName => "CSV File";
        public SusArgs CustomArgs { get; set; }

        private Score score;

        public void Export(string path, ScoreBook book)
        {
            score = book.Score;
            var notes = book.Score.Notes;
            var noteList = new List<Note>();

            noteList.AddRange(notes.Pads.Select(ConvertToNote));
            noteList.AddRange(notes.Faders.Select(ConvertToNote));
            noteList.AddRange(notes.Knobs.Select(ConvertToNote));

            // 並び替えて文字列に整形
            var noteString = noteList
                .OrderBy(x => x.startTime)
                .Select(x => x.ToString());
            // 
            // convert from
            // Pads : Taps, Holds
            // Knobs : ExTaps, AirActions
            // Faders : Flicks, Sliders

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("#TickPerBeat:{0}", score.TicksPerBeat);

                foreach (var note in noteString) writer.WriteLine(note);
            }
        }

        private Note ConvertToNote(Pad note) => ConvertToNote(note.TapHold, NoteAttribute.Pad, 0);

        private Note ConvertToNote(Fader note) => ConvertToNote(note.TapHold, NoteAttribute.Fader, (int)note.VerticalDirection);

        private Note ConvertToNote(Knob note) => ConvertToNote(note.TapHold, NoteAttribute.Knob, (int)note.HroizontalDirection);

        private Note ConvertToNote(TapHold note, NoteAttribute attribute, int direction)
        {
            return new Note
            {
                lane = note.LaneIndex,
                startTime = TickToSecond(note.Tick),
                attribute = attribute,
                direction = direction,
                durationBySec = TickToSecond(note.Tick),
                durationByTick = note.Duration
            };
        }


        /// <summary>
        /// Tickを秒に変換する。
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        private float TickToSecond(int tick)
        {
            var bpmChanges = score.Events.BPMChangeEvents;
            var curSec = (decimal)0;
            var curTick = bpmChanges.First().Tick;
            var curBpm = bpmChanges.First().BPM;
            decimal spt = 60 / (curBpm * score.TicksPerBeat);

            foreach (var bpmChange in bpmChanges.Skip(1))
            {
                if (bpmChange.Tick > tick) break;
                curSec += spt * (bpmChange.Tick - curTick);
                curTick = bpmChange.Tick;
                curBpm = bpmChange.BPM;
                spt = 60 / (curBpm * score.TicksPerBeat);
            }

            curSec += spt * (tick - curTick);
            return (float)curSec;
        }
    }
}
