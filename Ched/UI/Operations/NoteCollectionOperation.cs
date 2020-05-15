using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;

namespace Ched.UI.Operations
{
    public abstract class NoteCollectionOperation<T> : IOperation
    {
        protected T Note { get; }
        protected NoteView.NoteCollection Collection { get; }
        public abstract string Description { get; }

        public NoteCollectionOperation(NoteView.NoteCollection collection, T note)
        {
            Collection = collection;
            Note = note;
        }

        public abstract void Undo();
        public abstract void Redo();
    }

    public class InsertPadOperation : NoteCollectionOperation<Pad>{
        public override string Description { get { return "Padの追加"; } }

        public InsertPadOperation(NoteView.NoteCollection collection, Pad note) : base(collection, note)
        {
        }

        public override void Redo(){
            Collection.Add(Note);
        }

        public override void Undo(){
            Collection.Remove(Note);
        }
    }

    public class RemovePadOperation : NoteCollectionOperation<Pad>
    {
        public override string Description { get { return "Padの削除"; } }

        public RemovePadOperation(NoteView.NoteCollection collection, Pad note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertFaderOperation : NoteCollectionOperation<Fader>{
        public override string Description { get { return "Faderの追加"; } }

        public InsertFaderOperation(NoteView.NoteCollection collection, Fader note) : base(collection, note)
        {
        }

        public override void Redo(){
            Collection.Add(Note);
        }

        public override void Undo(){
            Collection.Remove(Note);
        }
    }

    public class RemoveFaderOperation : NoteCollectionOperation<Fader>
    {
        public override string Description { get { return "Faderの削除"; } }

        public RemoveFaderOperation(NoteView.NoteCollection collection, Fader note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    public class InsertKnobOperation : NoteCollectionOperation<Knob>{
        public override string Description { get { return "Knobの追加"; } }

        public InsertKnobOperation(NoteView.NoteCollection collection, Knob note) : base(collection, note)
        {
        }

        public override void Redo(){
            Collection.Add(Note);
        }

        public override void Undo(){
            Collection.Remove(Note);
        }
    }

    public class RemoveKnobOperation : NoteCollectionOperation<Knob>
    {
        public override string Description { get { return "Knobの削除"; } }

        public RemoveKnobOperation(NoteView.NoteCollection collection, Knob note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }
}
