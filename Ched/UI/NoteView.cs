using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Windows.Forms.VisualStyles;
using Ched.Core;
using Ched.Core.Notes;
using Ched.Drawing;
using Ched.UI.Operations;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        public event EventHandler HeadTickChanged;
        public event EventHandler EditModeChanged;
        public event EventHandler SelectedRangeChanged;
        public event EventHandler NewNoteTypeChanged;
        public event EventHandler AirDirectionChanged;

        public event EventHandler FaderDirectionChanged;
        public event EventHandler KnobDirectionChanged;
        public event EventHandler FaderHoldDirectionChanged;
        public event EventHandler KnobHoldDirectionChanged;
        public event EventHandler DragScroll;

        private Color barLineColor = Color.FromArgb(160, 160, 160);
        private Color beatLineColor = Color.FromArgb(80, 80, 80);
        private Color laneBorderLightColor = Color.FromArgb(60, 60, 60);
        private Color laneBorderDarkColor = Color.FromArgb(30, 30, 30);
        private ColorProfile colorProfile;
        private int unitLaneWidth = 12;
        private int shortNoteHeight = 5;
        private int unitBeatTick = 480;
        private float unitBeatHeight = 120;

        private int headTick = 0;
        private bool editable = true;
        private EditMode editMode = EditMode.Edit;
        private int currentTick = 0;
        private SelectionRange selectedRange = SelectionRange.Empty;
        private NoteType newNoteType = NoteType.Pad;
        private bool isNewSlideStepVisible = true;
        private FaderDirection faderDirection = new FaderDirection(VerticalDirection.UP);
        private KnobDirection knobDirection = new KnobDirection(HorizontalDirection.Left);
        private FaderDirection faderHoldDirection = new FaderDirection(VerticalDirection.UP);
        private KnobDirection knobHoldDirection = new KnobDirection(HorizontalDirection.Left);

        public FaderDirection FaderDirection
        {
            get { return faderDirection; }
            set
            {
                faderDirection = value;
                FaderDirectionChanged?.Invoke(this,EventArgs.Empty);
            }
        }
        public KnobDirection KnobDirection
        {
            get { return knobDirection; }
            set
            {
                knobDirection = value;
                FaderDirectionChanged?.Invoke(this,EventArgs.Empty);
            }
        }
        public FaderDirection FaderHoldDirection
        {
            get { return faderHoldDirection; }
            set
            {
                faderHoldDirection = value;
                FaderDirectionChanged?.Invoke(this,EventArgs.Empty);
            }
        }
        public KnobDirection KnobHoldDirection
        {
            get { return knobHoldDirection; }
            set
            {
                knobHoldDirection = value;
                FaderDirectionChanged?.Invoke(this,EventArgs.Empty);
            }
        }
        
        
        /// <summary>
        /// 小節の区切り線の色を設定します。
        /// </summary>
        public Color BarLineColor
        {
            get { return barLineColor; }
            set
            {
                barLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍のガイド線の色を設定します。
        /// </summary>
        public Color BeatLineColor
        {
            get { return beatLineColor; }
            set
            {
                beatLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンのガイド線のメインカラーを設定します。
        /// </summary>
        public Color LaneBorderLightColor
        {
            get { return laneBorderLightColor; }
            set
            {
                laneBorderLightColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンのガイド線のサブカラーを設定します。
        /// </summary>
        public Color LaneBorderDarkColor
        {
            get { return laneBorderDarkColor; }
            set
            {
                laneBorderDarkColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// ノーツの描画に利用する<see cref="Ched.Drawing.ColorProfile"/>を取得します。
        /// </summary>
        public ColorProfile ColorProfile
        {
            get { return colorProfile; }
        }

        /// <summary>
        /// 1レーンあたりの表示幅を設定します。
        /// </summary>
        public int UnitLaneWidth
        {
            get { return unitLaneWidth; }
            set
            {
                unitLaneWidth = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンの表示幅を取得します。
        /// </summary>
        public int LaneWidth
        {
            get { return UnitLaneWidth * Constants.LanesCount + BorderThickness * (Constants.LanesCount - 1); }
        }

        /// <summary>
        /// レーンのガイド線の幅を取得します。
        /// </summary>
        public int BorderThickness => UnitLaneWidth < 5 ? 0 : 1;

        /// <summary>
        /// ショートノーツの表示高さを設定します。
        /// </summary>
        public int ShortNoteHeight
        {
            get { return shortNoteHeight; }
            set
            {
                shortNoteHeight = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍あたりのTick数を設定します。
        /// </summary>
        public int UnitBeatTick
        {
            get { return unitBeatTick; }
            set
            {
                unitBeatTick = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍あたりの表示高さを設定します。
        /// </summary>
        public float UnitBeatHeight
        {
            get { return unitBeatHeight; }
            set
            {
                // 6の倍数でいい感じに描画してくれる
                unitBeatHeight = value;
                Invalidate();
            }
        }

        /// <summary>
        /// クォンタイズを行うTick数を指定します。
        /// </summary>
        public double QuantizeTick { get; set; }

        /// <summary>
        /// 表示始端のTickを設定します。
        /// </summary>
        public int HeadTick
        {
            get { return headTick; }
            set
            {
                if (headTick == value) return;
                headTick = value;
                HeadTickChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// 表示終端のTickを取得します。
        /// </summary>
        public int TailTick
        {
            get { return HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight); }
        }

        /// <summary>
        /// 譜面始端の表示余白に充てるTickを取得します。
        /// </summary>
        public int PaddingHeadTick
        {
            get { return UnitBeatTick / 8; }
        }

        /// <summary>
        /// ノーツが編集可能かどうかを示す値を設定します。
        /// </summary>
        public bool Editable
        {
            get { return editable; }
            set
            {
                editable = value;
                Cursor = value ? Cursors.Default : Cursors.No;
            }
        }

        /// <summary>
        /// 編集モードを設定します。
        /// </summary>
        public EditMode EditMode
        {
            get { return editMode; }
            set
            {
                editMode = value;
                EditModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 現在のTickを設定します。
        /// </summary>
        public int CurrentTick
        {
            get { return currentTick; }
            set
            {
                currentTick = value;
                if (currentTick < HeadTick || currentTick > TailTick)
                {
                    HeadTick = currentTick;
                    DragScroll?.Invoke(this, EventArgs.Empty);
                }
                Invalidate();
            }
        }

        /// <summary>
        /// 現在の選択範囲を設定します。
        /// </summary>
        public SelectionRange SelectedRange
        {
            get { return selectedRange; }
            set
            {
                selectedRange = value;
                SelectedRangeChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// 追加するノート種別を設定します。
        /// </summary>
        public NoteType NewNoteType
        {
            get { return newNoteType; }
            set
            {
                int bits = (int)value;
                bool isSingle = bits != 0 && (bits & (bits - 1)) == 0;
                if (!isSingle) throw new ArgumentException("value", "value must be single bit.");
                newNoteType = value;
                NewNoteTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 新たに追加するSlideのStepノートの可視性を設定します。
        /// </summary>
        public bool IsNewSlideStepVisible
        {
            get { return isNewSlideStepVisible; }
            set
            {
                isNewSlideStepVisible = value;
                NewNoteTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// AIR-ACTION挿入時に未追加のAIRを追加するかどうか指定します。
        /// </summary>
        public bool InsertAirWithAirAction { get; set; }

        /// <summary>
        /// ノート幅に対するノート端の当たり判定に含める割合を設定します。
        /// </summary>
        public float EdgeHitWidthRate { get; set; } = 0.2f;

        /// <summary>
        /// ノート端の当たり判定幅の下限を取得します。
        /// </summary>
        public float MinimumEdgeHitWidth => UnitLaneWidth * 0.4f;

        protected int LastWidth { get; set; } = 4;

        public bool CanUndo { get { return OperationManager.CanUndo; } }

        public bool CanRedo { get { return OperationManager.CanRedo; } }

        public NoteCollection Notes { get; private set; } = new NoteCollection(new Core.NoteCollection());

        public EventCollection ScoreEvents { get; set; } = new EventCollection();

        protected OperationManager OperationManager { get; }

        protected CompositeDisposable Subscriptions { get; } = new CompositeDisposable();

        private Dictionary<Score, NoteCollection> NoteCollectionCache { get; } = new Dictionary<Score, NoteCollection>();

        public NoteView(OperationManager manager)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Opaque, true);

            OperationManager = manager;

            QuantizeTick = UnitBeatTick;

            colorProfile = new ColorProfile()
            {
                BorderColor = new GradientColor(Color.FromArgb(160, 160, 160), Color.FromArgb(208, 208, 208)),
                TapColor = new GradientColor(Color.FromArgb(138, 0, 0), Color.FromArgb(255, 128, 128)),
                ExTapColor = new GradientColor(Color.FromArgb(204, 192, 0), Color.FromArgb(255, 236, 68)),
                FlickColor = Tuple.Create(new GradientColor(Color.FromArgb(68, 68, 68), Color.FromArgb(186, 186, 186)), new GradientColor(Color.FromArgb(0, 96, 138), Color.FromArgb(122, 216, 252))),
                DamageColor = new GradientColor(Color.FromArgb(8, 8, 116), Color.FromArgb(22, 40, 180)),
                HoldColor = new GradientColor(Color.FromArgb(196, 86, 0), Color.FromArgb(244, 156, 102)),
                HoldBackgroundColor = new GradientColor(Color.FromArgb(196, 166, 44, 168), Color.FromArgb(196, 216, 216, 0)),
                SlideColor = new GradientColor(Color.FromArgb(0, 16, 138), Color.FromArgb(86, 106, 255)),
                SlideLineColor = Color.FromArgb(196, 0, 214, 192),
                SlideBackgroundColor = new GradientColor(Color.FromArgb(196, 166, 44, 168), Color.FromArgb(196, 0, 164, 146)),
                AirUpColor = Color.FromArgb(28, 206, 22),
                AirDownColor = Color.FromArgb(192, 21, 216),
                AirActionColor = new GradientColor(Color.FromArgb(146, 0, 192), Color.FromArgb(212, 92, 255)),
                AirHoldLineColor = Color.FromArgb(216, 0, 196, 0),
                AirStepColor = new GradientColor(Color.FromArgb(6, 180, 10), Color.FromArgb(80, 224, 64))
            };

            var mouseDown = this.MouseDownAsObservable();
            var mouseMove = this.MouseMoveAsObservable();
            var mouseUp = this.MouseUpAsObservable();

            // マウスをクリックしているとき以外
            var mouseMoveSubscription = mouseMove.TakeUntil(mouseDown).Concat(mouseMove.SkipUntil(mouseUp).TakeUntil(mouseDown).Repeat())
                .Where(p => EditMode == EditMode.Edit && Editable)
                .Do(p =>
                {
                    var pos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(p.Location);
                    int tailTick = TailTick;
                    Func<int, bool> visibleTick = t => t >= HeadTick && t <= tailTick;

                    var shortNotes = Enumerable.Empty<NoteBase>()
                        .Concat(Notes.Faders.Reverse())
                        .Concat(Notes.Pads.Reverse())
                        .Concat(Notes.Knobs.Reverse())
                        .Where(q => visibleTick(q.TapHold.Tick))
                        .Select(q => GetClickableRectFromNotePosition(q.TapHold.Tick, q.TapHold.LaneIndex));

                    foreach (Hold hold in Notes.GetHolds().AsEnumerable().Reverse())
                    {
                        if (GetClickableRectFromNotePosition(hold.EndNote.Tick, hold.LaneIndex).Contains(pos))
                        {
                            Cursor = Cursors.SizeNS;
                            return;
                        }

                        RectangleF rect = GetClickableRectFromNotePosition(hold.Tick, hold.LaneIndex);
                        if (!rect.Contains(pos)) continue;
                        RectangleF left = rect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        RectangleF right = rect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        Cursor = (left.Contains(pos) || right.Contains(pos)) ? Cursors.SizeWE : Cursors.SizeAll;
                        return;
                    }

                    Cursor = Cursors.Default;
                })
                .Subscribe();

            var dragSubscription = mouseDown
                .SelectMany(p => mouseMove.TakeUntil(mouseUp).TakeUntil(mouseUp)
                    .CombineLatest(Observable.Interval(TimeSpan.FromMilliseconds(200)).TakeUntil(mouseUp), (q, r) => q)
                    .Sample(TimeSpan.FromMilliseconds(200), new ControlScheduler(this))
                    .Do(q =>
                    {
                        // コントロール端にドラッグされたらスクロールする
                        if (q.Y <= ClientSize.Height * 0.1)
                        {
                            HeadTick += UnitBeatTick;
                            DragScroll?.Invoke(this, EventArgs.Empty);
                        }
                        else if (q.Y >= ClientSize.Height * 0.9)
                        {
                            HeadTick -= HeadTick + PaddingHeadTick < UnitBeatTick ? HeadTick + PaddingHeadTick : UnitBeatTick;
                            DragScroll?.Invoke(this, EventArgs.Empty);
                        }
                    })).Subscribe();

            var editSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Edit)
                .SelectMany(p =>
                {
                    int tailTick = TailTick;
                    var from = p.Location;
                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Location);

                    // そもそも描画領域外であれば何もしない
                    RectangleF scoreRect = new RectangleF(0, GetYPositionFromTick(HeadTick), LaneWidth, GetYPositionFromTick(TailTick) - GetYPositionFromTick(HeadTick));
                    if (!scoreRect.Contains(scorePos)) return Observable.Empty<MouseEventArgs>();


                    Func<TapHold, IObservable<MouseEventArgs>> moveTappableNoteHandler = note =>
                    {
                        int beforeLaneIndex = note.LaneIndex;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                note.Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndex = beforeLaneIndex + xdiff;
                                note.LaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, laneIndex));
                                Cursor.Current = Cursors.SizeAll;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    };


                    Func<TapHold, IObservable<MouseEventArgs>> shortNoteHandler = note =>
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex);
                     
                    // ノート本体
                    if (rect.Contains(scorePos))
                    {
                        var beforePos = new MoveShortNoteOperation.NotePosition(note.Tick, note.LaneIndex);
                        return moveTappableNoteHandler(note)
                            .Finally(() =>
                            {
                                var afterPos = new MoveShortNoteOperation.NotePosition(note.Tick, note.LaneIndex);
                                if (beforePos == afterPos) return;
                                OperationManager.Push(new MoveShortNoteOperation(note, beforePos, afterPos));
                            });
                    }

                    return null;
                    };

                    Func<Hold, IObservable<MouseEventArgs>> holdDurationHandler = hold =>
                    {
                        return mouseMove.TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                hold.Duration = (int)Math.Max(QuantizeTick, GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - hold.Tick);
                                Cursor.Current = Cursors.SizeNS;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    };

                    Func<Hold, IObservable<MouseEventArgs>> holdHandler = hold =>
                    {
                        // HOLD長さ変更
                        if (GetClickableRectFromNotePosition(hold.EndNote.Tick, hold.LaneIndex).Contains(scorePos))
                        {
                            int beforeDuration = hold.Duration;
                            return holdDurationHandler(hold)
                                .Finally(() =>
                                {
                                    if (beforeDuration == hold.Duration) return;
                                    OperationManager.Push(new ChangeHoldDurationOperation(hold, beforeDuration, hold.Duration));
                                });
                        }

                        RectangleF startRect = GetClickableRectFromNotePosition(hold.Tick, hold.LaneIndex);

                        var beforePos = new MoveHoldOperation.NotePosition(hold.Tick, hold.LaneIndex);
                        if (startRect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    xdiff = Math.Min(0, Math.Max(-beforePos.LaneIndex, xdiff));
                                    int width = 1 - xdiff;
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    width = Math.Min(Constants.LanesCount - laneIndex, Math.Max(1, width));
                                    laneIndex = Math.Min(Constants.LanesCount - width, Math.Max(0, laneIndex));
                                    hold.SetPosition(laneIndex);
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.Tick, hold.LaneIndex);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        if (startRect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int width = 1 + xdiff;
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.Tick, hold.LaneIndex);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        if (startRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    hold.Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    hold.LaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, laneIndex));
                                    Cursor.Current = Cursors.SizeAll;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.Tick, hold.LaneIndex);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        return null;
                    };

                    Func<IObservable<MouseEventArgs>> surfaceNotesHandler = () =>
                    {
                        foreach (var note in Notes.GetTaps().AsEnumerable().Reverse().Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.GetHolds().Cast<Hold>().AsEnumerable().Reverse().Where(q => q.Tick <= tailTick && q.Tick + q.Duration >= HeadTick))
                        {
                            var subscription2 = holdHandler(note);
                            if (subscription2 != null) return subscription2;
                        }

                        return null;
                    };

                    var subscription3 = surfaceNotesHandler();
                    if (subscription3 != null) return subscription3;

                    // なんもねえなら追加だァ！
                    if ((NoteType.Pad | NoteType.Knob | NoteType.Fader).HasFlag(NewNoteType))
                    {
                        NoteBase newNote = null;
                        IOperation op = null;
                        switch (NewNoteType)
                        {
                            case NoteType.Pad:
                                var tap = new Pad(new Tap());
                                Notes.Add(tap);
                                newNote = tap;
                                op = new InsertPadOperation(Notes, tap);
                                break;

                            case NoteType.Knob:
                                var extap = new Knob(new Tap());
                                extap.HroizontalDirection = KnobDirection.HorizontalDirection;
                                Notes.Add(extap);
                                newNote = extap;
                                op = new InsertKnobOperation(Notes, extap);
                                break;

                            case NoteType.Fader:
                                var flick = new Fader(new Tap());
                                flick.VerticalDirection = FaderDirection.VerticalDirection;
                                Notes.Add(flick);
                                newNote = flick;
                                op = new InsertFaderOperation(Notes, flick);
                                break;

                        }
                        newNote.TapHold.Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0);
                        int newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - 1 / 2;
                        newNoteLaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, newNoteLaneIndex));
                        newNote.TapHold.LaneIndex = newNoteLaneIndex;
                        Invalidate();
                        return moveTappableNoteHandler(newNote.TapHold)
                            .Finally(() => OperationManager.Push(op));
                    }
                    else
                    {
                        int newNoteLaneIndex;

                        switch (NewNoteType)
                        {
                            case NoteType.FaderHold:
                                var hold = new Fader(new Hold()
                                {
                                    Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Duration = (int) QuantizeTick,
                                })
                                        {VerticalDirection = FaderHoldDirection.VerticalDirection,}
                                    ;
                                newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - 1 / 2;
                                hold.TapHold.LaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, newNoteLaneIndex));
                                Notes.Add(hold);
                                Invalidate();
                                return holdDurationHandler((Hold)hold.TapHold)
                                    .Finally(() => OperationManager.Push(new InsertFaderOperation(Notes, hold)));
                            
                            case NoteType.PadHold:
                                    var pad = new Pad(new Hold()
                                    {
                                        Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                        Duration = (int) QuantizeTick,
                                    });
                                    newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - 1 / 2;
                                    pad.TapHold.LaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, newNoteLaneIndex));
                                    Notes.Add(pad);
                                    Invalidate();
                                    return holdDurationHandler((Hold)pad.TapHold)
                                        .Finally(() => OperationManager.Push(new InsertPadOperation(Notes, pad)));
                            
                            case NoteType.KnobHold:
                                    var knob = new Knob(new Hold()
                                    {
                                        Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                        Duration = (int) QuantizeTick,
                                    })
                                    {
                                        HroizontalDirection = KnobHoldDirection.HorizontalDirection,
                                    };
                                    newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - 1 / 2;
                                    knob.TapHold.LaneIndex = Math.Min(Constants.LanesCount - 1, Math.Max(0, newNoteLaneIndex));
                                    Notes.Add(knob);
                                    Invalidate();
                                    return holdDurationHandler((Hold)knob.TapHold)
                                        .Finally(() => OperationManager.Push(new InsertKnobOperation(Notes, knob)));
                            }
                    }
                    return Observable.Empty<MouseEventArgs>();
                }).Subscribe(p => Invalidate());

            Func<PointF, IObservable<MouseEventArgs>> rangeSelection = startPos =>
            {
                SelectedRange = new SelectionRange()
                {
                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startPos.Y)), 0),
                    Duration = 0,
                    StartLaneIndex = 0,
                    SelectedLanesCount = 0
                };

                return mouseMove.TakeUntil(mouseUp)
                    .Do(q =>
                    {
                        Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                        currentMatrix.Invert();
                        var scorePos = currentMatrix.TransformPoint(q.Location);

                        int startLaneIndex = Math.Min(Math.Max((int)startPos.X / (UnitLaneWidth + BorderThickness), 0), Constants.LanesCount - 1);
                        int endLaneIndex = Math.Min(Math.Max((int)scorePos.X / (UnitLaneWidth + BorderThickness), 0), Constants.LanesCount - 1);
                        int endTick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y));

                        SelectedRange = new SelectionRange()
                        {
                            StartTick = SelectedRange.StartTick,
                            Duration = endTick - SelectedRange.StartTick,
                            StartLaneIndex = Math.Min(startLaneIndex, endLaneIndex),
                            SelectedLanesCount = Math.Abs(endLaneIndex - startLaneIndex) + 1
                        };
                    });
            };

            var eraseSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Erase)
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);
                    return rangeSelection(startScorePos)
                        .Count()
                        .Zip(mouseUp, (q, r) => new { Pos = r.Location, Count = q });
                })
                .Do(p =>
                {
                    if (p.Count > 0) // ドラッグで範囲選択された
                    {
                        RemoveSelectedNotes();
                        SelectedRange = SelectionRange.Empty;
                        return;
                    }

                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Pos);
                    
                    foreach (var note in Notes.Pads.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.TapHold.Tick, note.TapHold.LaneIndex);
                        if (rect.Contains(scorePos))
                        {
                            var op = new RemovePadOperation(Notes, note);
                            Notes.Remove(note);
                            OperationManager.Push(op);
                            return;
                        }
                    }
                    
                    foreach (var note in Notes.Faders.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.TapHold.Tick, note.TapHold.LaneIndex);
                        if (rect.Contains(scorePos))
                        {
                            var op = new RemoveFaderOperation(Notes, note);
                            Notes.Remove(note);
                            OperationManager.Push(op);
                            return;
                        }
                    }
                    
                    foreach (var note in Notes.Knobs.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.TapHold.Tick, note.TapHold.LaneIndex);
                        if (rect.Contains(scorePos))
                        {
                            var op = new RemoveKnobOperation(Notes, note);
                            Notes.Remove(note);
                            OperationManager.Push(op);
                            return;
                        }
                    }

                })
                .Subscribe(p => Invalidate());

            var selectSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Select)
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);

                    if (GetSelectionRect().Contains(Point.Ceiling(startScorePos)))
                    {
                        int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
                        int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
                        int startTick = SelectedRange.StartTick;
                        int startLaneIndex = SelectedRange.StartLaneIndex;
                        int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

                        var selectedNotes = GetSelectedNotes();
                        var dicShortNotes = selectedNotes.GetTaps().ToDictionary(q => q, q => new MoveShortNoteOperation.NotePosition(q.Tick, q.LaneIndex));
                        var dicHolds = selectedNotes.GetHolds().ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.Tick, q.LaneIndex));

                        // 選択範囲移動
                        return mouseMove.TakeUntil(mouseUp).Do(q =>
                        {
                            Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                            currentMatrix.Invert();
                            var scorePos = currentMatrix.TransformPoint(q.Location);

                            int xdiff = (int)((scorePos.X - startScorePos.X) / (UnitLaneWidth + BorderThickness));
                            int laneIndex = startLaneIndex + xdiff;

                            SelectedRange = new SelectionRange()
                            {
                                StartTick = startTick + Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y) - GetTickFromYPosition(startScorePos.Y)), -startTick - (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0)),
                                Duration = SelectedRange.Duration,
                                StartLaneIndex = Math.Min(Math.Max(laneIndex, 0), Constants.LanesCount - SelectedRange.SelectedLanesCount),
                                SelectedLanesCount = SelectedRange.SelectedLanesCount
                            };

                            foreach (var item in dicShortNotes)
                            {
                                item.Key.Tick = item.Value.Tick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            // ロングノーツは全体が範囲内に含まれているもののみを対象にするので範囲外移動は考えてない
                            foreach (var item in dicHolds)
                            {
                                item.Key.Tick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            // AIR-ACTIONはOffsetの管理面倒で実装できませんでした。許せ

                            Invalidate();
                        })
                        .Finally(() =>
                        {
                            var opShortNotes = dicShortNotes.Select(q =>
                            {
                                var after = new MoveShortNoteOperation.NotePosition(q.Key.Tick, q.Key.LaneIndex);
                                return new MoveShortNoteOperation(q.Key, q.Value, after);
                            });

                            var opHolds = dicHolds.Select(q =>
                            {
                                var after = new MoveHoldOperation.NotePosition(q.Key.Tick, q.Key.LaneIndex);
                                return new MoveHoldOperation(q.Key, q.Value, after);
                            });

                            // 同じ位置に戻ってきたら操作扱いにしない
                            if (startTick == SelectedRange.StartTick && startLaneIndex == SelectedRange.StartLaneIndex) return;
                            OperationManager.Push(new CompositeOperation("ノーツの移動", opShortNotes.Cast<IOperation>().Concat(opHolds).ToList()));
                        });
                    }
                    else
                    {
                        // 範囲選択
                        CurrentTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startScorePos.Y)), 0);
                        return rangeSelection(startScorePos);
                    }
                }).Subscribe();

            Subscriptions.Add(mouseMoveSubscription);
            Subscriptions.Add(dragSubscription);
            Subscriptions.Add(editSubscription);
            Subscriptions.Add(eraseSubscription);
            Subscriptions.Add(selectSubscription);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Matrix matrix = GetDrawingMatrix(new Matrix());
            matrix.Invert();

            if (EditMode == EditMode.Select && Editable)
            {
                var scorePos = matrix.TransformPoint(e.Location);
                Cursor = GetSelectionRect().Contains(scorePos) ? Cursors.SizeAll : Cursors.Default;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button == MouseButtons.Right)
            {
                EditMode = EditMode == EditMode.Edit ? EditMode.Select : EditMode.Edit;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            // Y軸の正方向をTick増加方向として描画 (y = 0 はコントロール下端)
            // コントロールの中心に描画したいなら後でTranslateしといてね
            var prevMatrix = pe.Graphics.Transform;
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix);

            var dc = new DrawingContext(pe.Graphics, ColorProfile);

            float laneWidth = LaneWidth;
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // レーン分割線描画
            using (var lightPen = new Pen(LaneBorderLightColor, BorderThickness))
            using (var darkPen = new Pen(LaneBorderDarkColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(i % 2 == 0 ? lightPen : darkPen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // 時間ガイドの描画
            // そのイベントが含まれる小節(ただし[小節開始Tick, 小節開始Tick + 小節Tick)の範囲)からその拍子を適用
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            using (var beatPen = new Pen(BeatLineColor, BorderThickness))
            using (var barPen = new Pen(BarLineColor, BorderThickness))
            {
                // 最初の拍子
                int firstBarLength = UnitBeatTick * 4 * sigs[0].Numerator / sigs[0].Denominator;
                int barTick = UnitBeatTick * 4;

                for (int i = HeadTick / (barTick / sigs[0].Denominator); sigs.Count < 2 || i * barTick / sigs[0].Denominator < sigs[1].Tick / firstBarLength * firstBarLength; i++)
                {
                    int tick = i * barTick / sigs[0].Denominator;
                    float y = GetYPositionFromTick(tick);
                    pe.Graphics.DrawLine(i % sigs[0].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    if (tick > tailTick) break;
                }

                // その後の拍子
                int pos = 0;
                for (int j = 1; j < sigs.Count; j++)
                {
                    int prevBarLength = barTick * sigs[j - 1].Numerator / sigs[j - 1].Denominator;
                    int currentBarLength = barTick * sigs[j].Numerator / sigs[j].Denominator;
                    pos += (sigs[j].Tick - pos) / prevBarLength * prevBarLength;
                    if (pos > tailTick) break;
                    for (int i = HeadTick - pos < 0 ? 0 : (HeadTick - pos) / (barTick / sigs[j].Denominator); pos + i * (barTick / sigs[j].Denominator) < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * barTick / sigs[j].Denominator >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;
                        float y = GetYPositionFromTick(pos + i * barTick / sigs[j].Denominator);
                        pe.Graphics.DrawLine(i % sigs[j].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    }
                }
            }

            using (var posPen = new Pen(Color.FromArgb(196, 0, 0)))
            {
                float y = GetYPositionFromTick(CurrentTick);
                pe.Graphics.DrawLine(posPen, -UnitLaneWidth * 2, y, laneWidth, y);
            }

            // ノート描画
            var holds = Notes.GetHolds().Where(p => p.Tick <= tailTick && p.Tick + p.Duration >= HeadTick).ToList();
            // ロングノーツ背景
            // HOLD
            foreach (var hold in holds)
            {
                dc.DrawHoldBackground(new RectangleF(
                    (UnitLaneWidth + BorderThickness) * hold.LaneIndex + BorderThickness,
                    GetYPositionFromTick(hold.Tick),
                    (UnitLaneWidth + BorderThickness) - BorderThickness,
                    GetYPositionFromTick(hold.Duration)
                    ));
            }
            //
            // // SLIDE
            // var slides = Notes.Slides.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            // foreach (var slide in slides)
            // {
            //     var bg = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.OrderBy(p => p.Tick)).ToList();
            //     var visibleSteps = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.Where(p => p.IsVisible).OrderBy(p => p.Tick)).ToList();
            //
            //     int stepHead = bg.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? bg[0].Tick;
            //     int stepTail = bg.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? bg[bg.Count - 1].Tick;
            //     int visibleHead = visibleSteps.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? visibleSteps[0].Tick;
            //     int visibleTail = visibleSteps.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? visibleSteps[visibleSteps.Count - 1].Tick;
            //
            //     var steps = bg
            //         .Where(p => p.Tick >= stepHead && p.Tick <= stepTail)
            //         .Select(p => new SlideStepElement()
            //         {
            //             Point = new PointF((UnitLaneWidth + BorderThickness) * p.LaneIndex, GetYPositionFromTick(p.Tick)),
            //             Width = (UnitLaneWidth + BorderThickness) * p.Width - BorderThickness
            //         });
            //     var visibleStepPos = visibleSteps
            //         .Where(p => p.Tick >= visibleHead && p.Tick <= visibleTail)
            //         .Select(p => GetYPositionFromTick(p.Tick));
            //
            //     if (stepHead == stepTail) continue;
            //     dc.DrawSlideBackground(steps, visibleStepPos, ShortNoteHeight);
            // }
            //
            // var airs = Notes.Airs.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick).ToList();
            // var airActions = Notes.AirActions.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            //
            // // AIR-ACTION(ガイド線)
            // foreach (var note in airActions)
            // {
            //     dc.DrawAirHoldLine(
            //         (UnitLaneWidth + BorderThickness) * (note.ParentNote.LaneIndex + note.ParentNote.Width / 2f),
            //         GetYPositionFromTick(note.StartTick),
            //         GetYPositionFromTick(note.StartTick + note.GetDuration()),
            //         ShortNoteHeight);
            // }
            //
            // // ロングノーツ終点AIR
            // foreach (var note in airs)
            // {
            //     if (!(note.ParentNote is LongNoteTapBase)) continue;
            //     RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
            //     dc.DrawAirStep(rect);
            // }
            //
            // // 中継点
            // foreach (var hold in holds)
            // {
            //     if (Notes.GetReferencedAir(hold.EndNote).Count() > 0) continue; // AIR付き終点
            //     dc.DrawHoldEnd(GetRectFromNotePosition(hold.StartTick + hold.Duration, hold.LaneIndex, hold.Width));
            // }
            //
            // foreach (var slide in slides)
            // {
            //     foreach (var step in slide.StepNotes.OrderBy(p => p.TickOffset))
            //     {
            //         if (!Editable && !step.IsVisible) continue;
            //         if (Notes.GetReferencedAir(step).Count() > 0) break; // AIR付き終点
            //         RectangleF rect = GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
            //         if (step.IsVisible) dc.DrawSlideStep(rect);
            //         else dc.DrawBorder(rect);
            //     }
            // }

            // 始点
            foreach (var hold in holds)
            {
                dc.DrawHoldBegin(GetRectFromNotePosition(hold.Tick, hold.LaneIndex));
            }
            //
            // foreach (var slide in slides)
            // {
            //     dc.DrawSlideBegin(GetRectFromNotePosition(slide.StartTick, slide.StartNote.LaneIndex, slide.StartWidth));
            // }
            //
            // // TAP, ExTAP, FLICK, DAMAGE
            // foreach (var note in Notes.Flicks.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            // {
            //     dc.DrawFlick(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            // }
            //
            foreach (var note in Notes.GetTaps().Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawTap(GetRectFromNotePosition(note.Tick, note.LaneIndex));
            }
            //
            // foreach (var note in Notes.ExTaps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            // {
            //     dc.DrawExTap(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            // }
            //
            // foreach (var note in Notes.Damages.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            // {
            //     dc.DrawDamage(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            // }
            //
            // // AIR-ACTION(ActionNote)
            // foreach (var action in airActions)
            // {
            //     foreach (var note in action.ActionNotes)
            //     {
            //         dc.DrawAirAction(GetRectFromNotePosition(action.StartTick + note.Offset, action.ParentNote.LaneIndex, action.ParentNote.Width).Expand(-ShortNoteHeight * 0.28f));
            //     }
            // }
            //
            // // AIR
            // foreach (var note in airs)
            // {
            //     RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
            //     dc.DrawAir(rect, note.VerticalDirection, note.HorizontalDirection);
            // }

            // 選択範囲描画
            if (Editable) DrawSelectionRange(pe.Graphics);

            // Y軸反転させずにTick = 0をY軸原点とする座標系へ
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix, false);

            using (var font = new Font("MS Gothic", 8))
            {
                SizeF strSize = pe.Graphics.MeasureString("000", font);

                // 小節番号描画
                int barTick = UnitBeatTick * 4;
                int barCount = 0;
                int pos = 0;

                for (int j = 0; j < sigs.Count; j++)
                {
                    if (pos > tailTick) break;
                    int currentBarLength = (UnitBeatTick * 4) * sigs[j].Numerator / sigs[j].Denominator;
                    for (int i = 0; pos + i * currentBarLength < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * currentBarLength >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;

                        int tick = pos + i * currentBarLength;
                        barCount++;
                        if (tick < HeadTick) continue;
                        var point = new PointF(-strSize.Width, -GetYPositionFromTick(tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0:000}", barCount), font, Brushes.White, point);
                    }

                    if (j < sigs.Count - 1)
                        pos += (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength;
                }

                float rightBase = (UnitLaneWidth + BorderThickness) * Constants.LanesCount + strSize.Width / 3;

                // BPM描画
                using (var bpmBrush = new SolidBrush(Color.FromArgb(0, 192, 0)))
                {
                    foreach (var item in ScoreEvents.BPMChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(Regex.Replace(item.BPM.ToString(), @"\.0$", "").PadLeft(3), font, Brushes.Lime, point);
                    }
                }

                // 拍子記号描画
                using (var sigBrush = new SolidBrush(Color.FromArgb(216, 116, 0)))
                {
                    foreach (var item in sigs.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0}/{1}", item.Numerator, item.Denominator), font, sigBrush, point);
                    }
                }

                // ハイスピ描画
                using (var highSpeedBrush = new SolidBrush(Color.FromArgb(216, 0, 64)))
                {
                    foreach (var item in ScoreEvents.HighSpeedChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width * 2, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("x{0: 0.00;-0.00}", item.SpeedRatio), font, highSpeedBrush, point);
                    }
                }
            }

            pe.Graphics.Transform = prevMatrix;
        }

        private Matrix GetDrawingMatrix(Matrix baseMatrix)
        {
            return GetDrawingMatrix(baseMatrix, true);
        }

        private Matrix GetDrawingMatrix(Matrix baseMatrix, bool flipY)
        {
            Matrix matrix = baseMatrix.Clone();
            if (flipY)
            {
                // 反転してY軸増加方向を時間軸に
                matrix.Scale(1, -1);
            }
            // ずれたコントロール高さ分を補正
            matrix.Translate(0, ClientSize.Height - 1, MatrixOrder.Append);
            // さらにずらして下端とHeadTickを合わせる
            matrix.Translate(0, HeadTick * UnitBeatHeight / UnitBeatTick, MatrixOrder.Append);
            // 水平方向に対して中央に寄せる
            matrix.Translate((ClientSize.Width - LaneWidth) / 2, 0);

            return matrix;
        }

        private float GetYPositionFromTick(int tick)
        {
            return tick * UnitBeatHeight / UnitBeatTick;
        }

        protected int GetTickFromYPosition(float y)
        {
            return (int)(y * UnitBeatTick / UnitBeatHeight);
        }

        protected int GetQuantizedTick(int tick)
        {
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            int head = 0;
            for (int i = 0; i < sigs.Count; i++)
            {
                int barTick = UnitBeatTick * 4 * sigs[i].Numerator / sigs[i].Denominator;

                if (i < sigs.Count - 1)
                {
                    int nextHead = head + (sigs[i + 1].Tick - head) / barTick * barTick;
                    if (tick >= nextHead)
                    {
                        head = nextHead;
                        continue;
                    }
                }

                int headBarTick = head + (tick - head) / barTick * barTick;
                int offsetCount = (int)Math.Round((float)(tick - headBarTick) / QuantizeTick);
                int maxOffsetCount = (int)(barTick / QuantizeTick);
                int remnantTick = barTick - (int)(maxOffsetCount * QuantizeTick);
                return headBarTick + ((tick - headBarTick >= barTick - remnantTick / 2) ? barTick : (int)(offsetCount * QuantizeTick));
            }

            throw new InvalidOperationException();
        }

        private RectangleF GetRectFromNotePosition(int tick, int laneIndex)
        {
            return new RectangleF(
                (UnitLaneWidth + BorderThickness) * laneIndex + BorderThickness,
                GetYPositionFromTick(tick) - ShortNoteHeight / 2,
                (UnitLaneWidth + BorderThickness) - BorderThickness,
                ShortNoteHeight
                );
        }

        private RectangleF GetClickableRectFromNotePosition(int tick, int laneIndex)
        {
            return GetRectFromNotePosition(tick, laneIndex).Expand(1);
        }

        private Rectangle GetSelectionRect()
        {
            int minTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick + SelectedRange.Duration : SelectedRange.StartTick;
            int maxTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick : SelectedRange.StartTick + SelectedRange.Duration;
            var start = new Point(SelectedRange.StartLaneIndex * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(minTick) - ShortNoteHeight);
            var end = new Point((SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount) * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(maxTick) + ShortNoteHeight);
            return new Rectangle(start.X, start.Y, end.X - start.X, end.Y - start.Y);
        }

        protected void DrawSelectionRange(Graphics g)
        {
            Rectangle selectedRect = GetSelectionRect();
            g.DrawXorRectangle(PenStyles.Dot, g.Transform.TransformPoint(selectedRect.Location), g.Transform.TransformPoint(selectedRect.Location + selectedRect.Size));
        }

        public Core.NoteCollection GetSelectedNotes()
        {
            int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
            int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
            int startLaneIndex = SelectedRange.StartLaneIndex;
            int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

            var c = new Core.NoteCollection();

            Func<NoteBase, bool> contained = p =>
                p.TapHold.Tick >= minTick && p.TapHold.Tick + p.TapHold.Duration <= maxTick &&
                p.TapHold.LaneIndex >= startLaneIndex && p.TapHold.LaneIndex <= endLaneIndex;
            c.Pads.AddRange(Notes.Pads.Where(p=>contained(p)));
            c.Faders.AddRange(Notes.Faders.Where(p => contained(p)));
            c.Knobs.AddRange(Notes.Knobs.Where(p => contained(p)));

            return c;
        }

        public void CutSelectedNotes()
        {
            CopySelectedNotes();
            RemoveSelectedNotes();
        }

        public void CopySelectedNotes()
        {
            var data = new SelectionData(SelectedRange.StartTick + Math.Min(SelectedRange.Duration, 0), UnitBeatTick, GetSelectedNotes());
            Clipboard.SetDataObject(data, true);
        }

        public void PasteNotes()
        {
            var op = PasteNotes(p => { });
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        public void PasteFlippedNotes()
        {
            var op = PasteNotes(p => FlipNotes(p.SelectedNotes));
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// クリップボードにコピーされたノーツをペーストしてその操作を表す<see cref="IOperation"/>を返します。
        /// ペーストするノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="action">選択データに対して適用するアクション</param>
        /// <returns>ペースト操作を表す<see cref="IOperation"/></returns>
        protected IOperation PasteNotes(Action<SelectionData> action)
        {
            var obj = Clipboard.GetDataObject();
            if (obj == null || !obj.GetDataPresent(typeof(SelectionData))) return null;

            var data = obj.GetData(typeof(SelectionData)) as SelectionData;
            if (data.IsEmpty) return null;

            double tickFactor = UnitBeatTick / (double)data.TicksPerBeat;
            int originTick = (int)(data.StartTick * tickFactor);
            if (data.TicksPerBeat != UnitBeatTick)
                data.SelectedNotes.UpdateTicksPerBeat(tickFactor);

            foreach (var note in data.SelectedNotes.GetTaps())
            {
                note.Tick = note.Tick - originTick + CurrentTick;
            }

            foreach (var hold in data.SelectedNotes.GetHolds())
            {
                hold.Tick = hold.Tick - originTick + CurrentTick;
            }

            action(data);

            var op = data.SelectedNotes.Pads.Select(p => new InsertPadOperation(Notes, p)).Cast<IOperation>()
                .Concat(data.SelectedNotes.Faders.Select(p => new InsertFaderOperation(Notes, p)))
                .Concat(data.SelectedNotes.Knobs.Select(p => new InsertKnobOperation(Notes, p)));
            var composite = new CompositeOperation("クリップボードからペースト", op.ToList());
            composite.Redo(); // 追加書くの面倒になったので許せ
            return composite;
        }

        public void RemoveSelectedNotes()
        {
            var selected = GetSelectedNotes();

            var pads = selected.Pads.Select(p =>
            {
                Notes.Remove(p);
                return new RemovePadOperation(Notes, p);
            });

            var faders = selected.Faders.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveFaderOperation(Notes, p);
            });

            var knobs = selected.Knobs.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveKnobOperation(Notes, p);
            });

            var opList = pads.Cast<IOperation>().Concat(faders).Concat(knobs)
                .ToList();

            if (opList.Count == 0) return;
            OperationManager.Push(new CompositeOperation("選択範囲内ノーツ削除", opList));
            Invalidate();
        }

        public void FlipSelectedNotes()
        {
            var op = FlipNotes(GetSelectedNotes());
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// 指定のコレクション内のノーツを反転してその操作を表す<see cref="IOperation"/>を返します。
        /// 反転するノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="notes">反転対象となるノーツを含む<see cref="Core.NoteCollection"/></param>
        /// <returns>反転操作を表す<see cref="IOperation"/></returns>
        protected IOperation FlipNotes(Core.NoteCollection notes)
        {
            var dicShortNotes = notes.GetTaps().ToDictionary(q => q, q => new MoveShortNoteOperation.NotePosition(q.Tick, q.LaneIndex));
            var dicHolds = notes.GetHolds().ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.Tick, q.LaneIndex));
            //var dicSlides = notes.Slides;
            //var referenced = new NoteCollection(notes);
            // var airs = notes.GetShortNotes().Cast<IAirable>()
            //     .Concat(notes.Holds.Select(p => p.EndNote))
            //     .Concat(notes.Slides.Select(p => p.StepNotes.OrderByDescending(q => q.TickOffset).First()))
            //     .SelectMany(p => referenced.GetReferencedAir(p));

            var opShortNotes = dicShortNotes.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex;
                var after = new MoveShortNoteOperation.NotePosition(p.Key.Tick, p.Key.LaneIndex);
                return new MoveShortNoteOperation(p.Key, p.Value, after);
            });

            var opHolds = dicHolds.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex;
                var after = new MoveHoldOperation.NotePosition(p.Key.Tick, p.Key.LaneIndex);
                return new MoveHoldOperation(p.Key, p.Value, after);
            });

            var opList = opShortNotes.Cast<IOperation>().Concat(opHolds).ToList();
            return opList.Count == 0 ? null : new CompositeOperation("ノーツの反転", opList);
        }

        public void Undo()
        {
            if (!OperationManager.CanUndo) return;
            OperationManager.Undo();
            Invalidate();
        }

        public void Redo()
        {
            if (!OperationManager.CanRedo) return;
            OperationManager.Redo();
            Invalidate();
        }


        public void Initialize()
        {
            SelectedRange = SelectionRange.Empty;
            CurrentTick = SelectedRange.StartTick;
            Invalidate();
        }

        public void Initialize(Score score)
        {
            Initialize();
            UpdateScore(score);
        }

        public void UpdateScore(Score score)
        {
            UnitBeatTick = score.TicksPerBeat;
            if (NoteCollectionCache.ContainsKey(score))
            {
                Notes = NoteCollectionCache[score];
            }
            else
            {
                Notes = new NoteCollection(score.Notes);
                NoteCollectionCache.Add(score, Notes);
            }
            ScoreEvents = score.Events;
            Invalidate();
        }

        public class NoteCollection
        {
            public event EventHandler NoteChanged;

            private Core.NoteCollection source = new Core.NoteCollection();

            public IReadOnlyCollection<Pad> Pads => source.Pads;
            public IReadOnlyCollection<Fader> Faders => source.Faders;
            public IReadOnlyCollection<Knob> Knobs => source.Knobs;

            public NoteCollection(Core.NoteCollection src)
            {
                Load(src);
            }

            public List<TapHold> GetHolds()
            {
                return source.GetHolds().ToList();
            }

            public List<TapHold> GetTaps()
            {
                return source.GetTaps().ToList();
            }

            public List<TapHold> GetAllNotes()
            {
                return source.GetAllNotes().ToList();
            }

            public void Add(Pad note)
            {
                source.Pads.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Fader note)
            {
                source.Faders.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Knob note)
            {
                source.Knobs.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Remove(Pad note)
            {
                source.Pads.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Fader note)
            {
                source.Faders.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Knob note)
            {
                source.Knobs.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public int GetLastTick()
            {
                var shortNotes = source.GetTaps().ToList();
                var longNotes = source.GetHolds().ToList();
                int lastShortNoteTick = shortNotes.Count == 0 ? 0 : shortNotes.Max(p => p.Tick);
                int lastLongNoteTick = longNotes.Count == 0 ? 0 : longNotes.Max(p => p.Tick + p.Duration);
                return Math.Max(lastShortNoteTick, lastLongNoteTick);
            }


            public void Load(Core.NoteCollection collection)
            {
                Clear();

                foreach (var note in collection.Pads) Add(note);
                foreach (var note in collection.Faders) Add(note);
                foreach (var note in collection.Knobs) Add(note);
            }

            public void Clear()
            {
                source = new Core.NoteCollection();

                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void UpdateTicksPerBeat(double factor)
            {
                source.UpdateTicksPerBeat(factor);
            }
        }
    }

    public enum EditMode
    {
        Select,
        Edit,
        Erase
    }

    // 要修正
    [Flags]
    public enum NoteType
    {
        Pad = 1,
        PadHold = 1 << 1,
        Fader = 1 << 2,
        FaderHold = 1 << 3,
        Knob = 1 << 4,
        KnobHold = 1 << 5,
    }

    public struct FaderDirection
    {
        public VerticalDirection VerticalDirection { get; }

        public FaderDirection(VerticalDirection verticalDirection)
        {
            this.VerticalDirection = verticalDirection;
        }
    }

    public struct KnobDirection
    {
        public HorizontalDirection HorizontalDirection { get; }

        public KnobDirection(HorizontalDirection horizontalDirection)
        {
            this.HorizontalDirection = horizontalDirection;
        }
    }

    [Serializable]
    public class SelectionData
    {
        private string serializedText = null;

        [NonSerialized]
        private InnerData Data;

        public int StartTick
        {
            get
            {
                CheckRestored();
                return Data.StartTick;
            }
        }

        public Core.NoteCollection SelectedNotes
        {
            get
            {
                CheckRestored();
                return Data.SelectedNotes;
            }
        }

        public bool IsEmpty
        {
            get
            {
                CheckRestored();
                return SelectedNotes.GetAllNotes().Count() == 0;
            }
        }

        public int TicksPerBeat
        {
            get
            {
                CheckRestored();
                return Data.TicksPerBeat;
            }
        }

        public SelectionData()
        {
        }

        public SelectionData(int startTick, int ticksPerBeat, NoteCollection notes)
        {
            Data = new InnerData(startTick, ticksPerBeat, notes);
            serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(Data, SerializerSettings);
        }

        protected void CheckRestored()
        {
            if (Data == null) Restore();
        }

        protected void Restore()
        {
            Data = Newtonsoft.Json.JsonConvert.DeserializeObject<InnerData>(serializedText, SerializerSettings);
        }

        protected static Newtonsoft.Json.JsonSerializerSettings SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { IgnoreSerializableAttribute = true }
        };

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        protected class InnerData
        {
            [Newtonsoft.Json.JsonProperty]
            private int startTick;

            [Newtonsoft.Json.JsonProperty]
            private int ticksPerBeat;

            [Newtonsoft.Json.JsonProperty]
            private NoteCollection selectedNotes;

            public int StartTick => startTick;
            public int TicksPerBeat => ticksPerBeat;
            public NoteCollection SelectedNotes => selectedNotes;

            public InnerData(int startTick, int ticksPerBeat, NoteCollection notes)
            {
                this.startTick = startTick;
                this.ticksPerBeat = ticksPerBeat;
                selectedNotes = notes;
            }
        }
    }

    internal static class UIExtensions
    {
        public static Core.NoteCollection Reposit(this NoteView.NoteCollection collection)
        {
            var res = new NoteCollection();
            res.Pads = collection.Pads.ToList();
            res.Faders = collection.Faders.ToList();
            res.Knobs = collection.Knobs.ToList();
            return res;
        }
    }
}
