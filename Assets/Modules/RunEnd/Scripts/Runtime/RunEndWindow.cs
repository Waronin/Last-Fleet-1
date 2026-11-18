using System;
using System.Collections.Generic;
using System.Globalization;
using LastFleet.Localization;
using LastFleet.ShipControl;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// The end screen of a run: a golden title and the winning quest chain for a victory, a red title and the reason for
    /// a defeat, and the account (GDD: Wertung) building up line by line before the total counts up. Each line shows
    /// its change since the last run and marks records; the latest runs follow. A click skips the build-up. It covers
    /// the whole screen and blocks clicks to the ships; the only way on is a new run. Whoever owns the run shows it,
    /// stops the game behind it and starts the new run, so the build-up runs on real time. Prototype UI in IMGUI.
    /// </summary>
    public sealed class RunEndWindow : MonoBehaviour
    {
        const float WidthPixels = 640f;
        const float PointsColumnPixels = 80f;
        const float CompareColumnPixels = 160f;
        const string DateFormat = "yyyy-MM-dd HH:mm";

        /// <summary>Lower depth draws on top; the other panels keep the default of 0.</summary>
        const int BackdropDepth = -100;

        static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);
        static readonly Color VictoryColor = new Color(1f, 0.84f, 0.4f);
        static readonly Color DefeatColor = new Color(1f, 0.38f, 0.32f);
        static readonly Color PointsColor = new Color(0.6f, 0.95f, 0.6f);
        static readonly Color FactColor = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color BetterColor = new Color(0.55f, 0.9f, 0.55f);
        static readonly Color WorseColor = new Color(1f, 0.5f, 0.45f);

        [SerializeField] ShipCommandInput _shipInput;
        [SerializeField, Min(0.05f)] float _secondsPerLine = 0.35f;
        [SerializeField, Min(0.05f)] float _countUpSeconds = 1.2f;
        [SerializeField, Min(1)] int _latestRunsShown = 5;

        readonly List<string> _lineLabels = new List<string>();
        readonly List<string> _linePoints = new List<string>();
        readonly List<string> _lineComparisons = new List<string>();
        readonly List<Color> _lineComparisonColors = new List<Color>();
        readonly List<string> _latestRuns = new List<string>();

        // Not serialized: Unity would otherwise restore empty default styles (black, size 0) after a reload,
        // and the lazy creation below would keep them.
        [NonSerialized] GUIStyle _titleStyle;
        [NonSerialized] GUIStyle _moodStyle;
        [NonSerialized] GUIStyle _textStyle;
        [NonSerialized] GUIStyle _lineStyle;
        [NonSerialized] GUIStyle _pointsStyle;
        [NonSerialized] GUIStyle _compareStyle;
        [NonSerialized] GUIStyle _totalStyle;
        [NonSerialized] GUIStyle _historyStyle;
        [NonSerialized] GUIStyle _backdropStyle;
        Texture2D _backdropTexture;

        RunEndReveal _reveal;
        string _title;
        string _mood;
        string _cause;
        string _bestTotal;
        string _shipsAndTime;
        string _historyTitle;
        string _skipHint;
        string _newRunButton;
        bool _isRevealComplete;
        int _shownTotal = -1;
        string _totalText;

        /// <summary>The report shown, or null while the run goes on.</summary>
        public RunEndReport Report { get; private set; }

        public RunSummary Summary => Report?.Summary;

        public bool IsOpen => Report != null;

        /// <summary>The player asked for a new run.</summary>
        public event Action NewRunRequested;

        void Awake()
        {
            if (_shipInput == null)
            {
                Debug.LogError("RunEndWindow needs the ship command input to block clicks behind it.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_shipInput != null)
            {
                _shipInput.ClearBlockedScreenArea(this);
            }
        }

        void OnDestroy()
        {
            if (_backdropTexture != null)
            {
                Destroy(_backdropTexture);
            }
        }

        /// <summary>Opens the end screen; texts are resolved once in the current language.</summary>
        public void Show(RunEndReport report)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            RunSummary summary = report.Summary;
            _reveal = new RunEndReveal(summary.Lines.Count, _secondsPerLine, _countUpSeconds);
            _title = GameTextResolver.Resolve(RunEndTexts.Titles.For(summary.Outcome));
            _mood = GameTextResolver.Resolve(RunEndTexts.Moods.For(summary.Outcome));
            _cause = ResolveCause(summary);
            _shipsAndTime = GameTextResolver.Resolve(RunEndTexts.Ships.Format(("ships", summary.Ships))) + "   ·   " +
                            GameTextResolver.Resolve(RunEndTexts.Time.Format(("time", ClockFormat.MinutesSeconds(summary.ElapsedSeconds))));
            _bestTotal = ResolveBestTotal(report.Comparison);
            _historyTitle = GameTextResolver.Resolve(RunEndTexts.HistoryTitle);
            _skipHint = GameTextResolver.Resolve(RunEndTexts.SkipHint);
            _newRunButton = GameTextResolver.Resolve(RunEndTexts.NewRunButton);
            ResolveLines(report);
            ResolveLatestRuns(report);
            _shownTotal = -1;
            _isRevealComplete = false;
        }

        public void Close()
        {
            Report = null;
            _reveal = null;
            _shipInput.ClearBlockedScreenArea(this);
        }

        static string ResolveCause(RunSummary summary)
        {
            if (summary.WinningChainTitle != null)
            {
                return GameTextResolver.Resolve(RunEndTexts.WinningChain.Format(("chain", summary.WinningChainTitle)));
            }
            return summary.Reason != null ? GameTextResolver.Resolve(summary.Reason) : null;
        }

        static string ResolveBestTotal(RunComparison comparison)
        {
            if (!comparison.HasEarlierRuns)
            {
                return GameTextResolver.Resolve(RunEndTexts.FirstRun);
            }
            return comparison.IsNewBestTotal
                ? GameTextResolver.Resolve(RunEndTexts.NewBestTotal)
                : GameTextResolver.Resolve(RunEndTexts.BestTotal.Format(("points", comparison.BestEarlierTotal.Value)));
        }

        void ResolveLines(RunEndReport report)
        {
            _lineLabels.Clear();
            _linePoints.Clear();
            _lineComparisons.Clear();
            _lineComparisonColors.Clear();
            foreach (RunEndLine line in report.Summary.Lines)
            {
                _lineLabels.Add(GameTextResolver.Resolve(line.Label));
                _linePoints.Add(line.Scores ? GameTextResolver.Resolve(RunEndTexts.LinePoints.Format(("points", line.Points))) : string.Empty);
                RunLineComparison comparison = report.Comparison.For(line.Metric);
                _lineComparisons.Add(ResolveComparison(comparison));
                _lineComparisonColors.Add(comparison.IsImprovement || comparison.IsRecord
                    ? BetterColor
                    : comparison.ChangeSinceLastRun.GetValueOrDefault() != 0 ? WorseColor : FactColor);
            }
        }

        static string ResolveComparison(RunLineComparison comparison)
        {
            if (!comparison.ChangeSinceLastRun.HasValue)
            {
                return string.Empty;
            }
            int change = comparison.ChangeSinceLastRun.Value;
            GameText text = change > 0
                ? RunEndTexts.ChangeUp.Format(("change", change))
                : change < 0
                    ? RunEndTexts.ChangeDown.Format(("change", -change))
                    : RunEndTexts.ChangeNone.Format();
            string resolved = GameTextResolver.Resolve(text);
            return comparison.IsRecord ? resolved + "  " + GameTextResolver.Resolve(RunEndTexts.Record) : resolved;
        }

        void ResolveLatestRuns(RunEndReport report)
        {
            _latestRuns.Clear();
            for (int i = 0; i < report.LatestRuns.Count && i < _latestRunsShown; i++)
            {
                RunRecord run = report.LatestRuns[i];
                string date = run.FinishedAtUtc.ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
                GameText chain = run.WinningChainTitle;
                GameText entry = run.Outcome == RunOutcome.Won
                    ? RunEndTexts.HistoryWon.Format(("date", date), ("chain", chain ?? GameText.Verbatim(run.WinningChainId ?? "?")),
                        ("points", run.TotalPoints))
                    : RunEndTexts.HistoryLost.Format(("date", date), ("points", run.TotalPoints));
                _latestRuns.Add(GameTextResolver.Resolve(entry));
            }
        }

        void Update()
        {
            _reveal?.Advance(Time.unscaledDeltaTime);
        }

        void OnGUI()
        {
            if (Report == null)
            {
                return;
            }
            _shipInput.SetBlockedScreenArea(this, new Rect(0f, 0f, Screen.width, Screen.height));
            CreateStyles();

            // IMGUI needs the same controls in every event of a frame, so what appears at the end changes only on Layout.
            if (Event.current.type == EventType.Layout)
            {
                _isRevealComplete = _reveal.IsComplete;
            }
            if (Event.current.type == EventType.MouseDown && !_reveal.IsComplete)
            {
                _reveal.Skip();
                Event.current.Use();
            }

            // Drawn above the other IMGUI panels, on a dark veil so the HUD behind it does not distract.
            GUI.depth = BackdropDepth;
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, _backdropStyle);
            GUILayout.BeginArea(new Rect((Screen.width - WidthPixels) * 0.5f, 0f, WidthPixels, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);

            DrawHeader();
            GUILayout.Space(12f);
            DrawLines();
            GUILayout.Space(10f);
            DrawTotal();
            GUILayout.Label(_shipsAndTime, _textStyle);
            GUILayout.Space(8f);
            DrawLatestRuns();
            GUILayout.Space(8f);

            bool newRun = false;
            if (_isRevealComplete)
            {
                newRun = GUILayout.Button(_newRunButton, GUILayout.Height(30f));
            }
            else
            {
                GUILayout.Label(_skipHint, _textStyle, GUILayout.Height(30f));
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();

            if (newRun)
            {
                NewRunRequested?.Invoke();
            }
        }

        void DrawHeader()
        {
            Color previousContentColor = GUI.contentColor;
            Color outcomeColor = Summary.Outcome == RunOutcome.Won ? VictoryColor : DefeatColor;
            GUI.contentColor = outcomeColor;
            GUILayout.Label(_title, _titleStyle);
            GUI.contentColor = previousContentColor;
            GUILayout.Label(_mood, _moodStyle);
            if (_cause != null)
            {
                GUI.contentColor = Summary.Outcome == RunOutcome.Won ? outcomeColor : Color.white;
                GUILayout.Label(_cause, _textStyle);
                GUI.contentColor = previousContentColor;
            }
        }

        /// <summary>All lines take their place from the start, so the box does not grow while they appear.</summary>
        void DrawLines()
        {
            Color previousContentColor = GUI.contentColor;
            for (int i = 0; i < _lineLabels.Count; i++)
            {
                bool visible = i < _reveal.VisibleLineCount;
                GUILayout.BeginHorizontal();
                GUI.contentColor = Summary.Lines[i].Scores ? Color.white : FactColor;
                GUILayout.Label(visible ? _lineLabels[i] : string.Empty, _lineStyle);
                GUI.contentColor = PointsColor;
                GUILayout.Label(visible ? _linePoints[i] : string.Empty, _pointsStyle, GUILayout.Width(PointsColumnPixels));
                GUI.contentColor = _lineComparisonColors[i];
                GUILayout.Label(visible ? _lineComparisons[i] : string.Empty, _compareStyle, GUILayout.Width(CompareColumnPixels));
                GUILayout.EndHorizontal();
            }
            GUI.contentColor = previousContentColor;
        }

        void DrawTotal()
        {
            int shown = _reveal.DisplayedTotal(Summary.TotalPoints);
            if (shown != _shownTotal)
            {
                _shownTotal = shown;
                _totalText = GameTextResolver.Resolve(RunEndTexts.TotalPoints.Format(("points", shown)));
            }
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = Summary.Outcome == RunOutcome.Won ? VictoryColor : Color.white;
            GUILayout.Label(_reveal.IsTotalVisible ? _totalText : string.Empty, _totalStyle);
            GUI.contentColor = Report.Comparison.IsNewBestTotal ? VictoryColor : FactColor;
            GUILayout.Label(_isRevealComplete ? _bestTotal : string.Empty, _textStyle);
            GUI.contentColor = previousContentColor;
        }

        /// <summary>Shown once the build-up is done; the first entry is this run.</summary>
        void DrawLatestRuns()
        {
            if (!_isRevealComplete || _latestRuns.Count == 0)
            {
                return;
            }
            Color previousContentColor = GUI.contentColor;
            GUILayout.Label(_historyTitle, _textStyle);
            for (int i = 0; i < _latestRuns.Count; i++)
            {
                GUI.contentColor = i == 0 ? VictoryColor : FactColor;
                GUILayout.Label(_latestRuns[i], _historyStyle);
            }
            GUI.contentColor = previousContentColor;
        }

        void CreateStyles()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 28, alignment = TextAnchor.MiddleCenter };
            _moodStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, fontSize = 14, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _textStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _lineStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            _pointsStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _compareStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight };
            _totalStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _historyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            if (_backdropStyle == null)
            {
                _backdropTexture = new Texture2D(1, 1);
                _backdropTexture.SetPixel(0, 0, BackdropColor);
                _backdropTexture.Apply();
                _backdropStyle = new GUIStyle();
                _backdropStyle.normal.background = _backdropTexture;
            }
        }
    }
}
