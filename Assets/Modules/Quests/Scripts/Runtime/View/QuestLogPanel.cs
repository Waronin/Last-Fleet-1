using LastFleet.Localization;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Quest log at the bottom middle of the screen, below the dialogue window: every chain of the run with
    /// its current step, what to do, where, and a live line such as a countdown. Finished and failed
    /// chains stay listed as such. Prototype UI in IMGUI.
    /// </summary>
    public sealed class QuestLogPanel : MonoBehaviour
    {
        const float WidthPixels = 360f;
        const float MarginPixels = 10f;

        [SerializeField] QuestDirector _quests;
        [SerializeField] ShipCommandInput _shipInput;
        [Tooltip("Optional. Lists the accepted delivery requests of stations.")]
        [SerializeField] StationRequestBoard _requests;

        // Not serialized: Unity would otherwise restore an empty default style after a reload.
        [System.NonSerialized] GUIStyle _wrapStyle;

        void Awake()
        {
            if (_quests == null || _shipInput == null)
            {
                Debug.LogError("QuestLogPanel needs the quest director and the ship command input to block clicks behind it.", this);
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

        void OnGUI()
        {
            if (_quests.Chains.Count == 0)
            {
                _shipInput.ClearBlockedScreenArea(this);
                return;
            }

            _wrapStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            Rect area = new Rect((Screen.width - WidthPixels) * 0.5f, 0f, WidthPixels, Screen.height - MarginPixels);
            GUILayout.BeginArea(area);
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogHeader));
            foreach (QuestChain chain in _quests.Chains)
            {
                DrawChain(chain);
            }
            DrawRequests();
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect box = GUILayoutUtility.GetLastRect();
                _shipInput.SetBlockedScreenArea(this, new Rect(area.x + box.x, Screen.height - area.y - box.yMax, box.width, box.height));
            }
            GUILayout.EndArea();
        }

        /// <summary>Accepted deliveries: which station, what it wants against what the fleet has, and what it pays.</summary>
        void DrawRequests()
        {
            if (_requests == null || _requests.Accepted.Count == 0)
            {
                return;
            }
            GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogRequestsHeader));
            foreach (StationRequestEntry entry in _requests.Accepted)
            {
                SupplyRequest request = entry.Request;
                GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogRequest.Format(("station", entry.Station.Name),
                    ("wanted", SupplyRequestContact.WantedText(request)), ("reward", SupplyRequestContact.RewardText(request)),
                    ("stock", Mathf.FloorToInt(_requests.StockOf(request.Wanted))))), _wrapStyle);
            }
        }

        void DrawChain(QuestChain chain)
        {
            GameText title = chain.Definition.Title;
            if (chain.State == QuestChainState.Completed)
            {
                GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogCompleted.Format(("chain", title))));
                return;
            }
            if (chain.State == QuestChainState.Failed)
            {
                GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogFailed.Format(("chain", title))));
                return;
            }

            QuestStepDefinition step = chain.CurrentStep;
            GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogStep.Format(("chain", title), ("step", chain.StepNumber),
                ("count", chain.StepCount), ("title", step.Title))));
            GUILayout.Label(GameTextResolver.Resolve(step.Objective), _wrapStyle);
            GUILayout.Label(GameTextResolver.Resolve(_quests.IsFleetIn(chain.StepSystemIndex)
                ? QuestTexts.LogHere
                : QuestTexts.LogSystem.Format(("system", GameText.Verbatim(_quests.SystemName(chain.StepSystemIndex))))));
            string zone = _quests.MarkedZoneName(chain);
            if (zone != null)
            {
                GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogZone.Format(("zone", GameText.Verbatim(zone)))));
            }
            if (!step.IsPlayable)
            {
                GUILayout.Label(GameTextResolver.Resolve(QuestTexts.LogNotPlayable));
                return;
            }
            GameText status = _quests.StatusOf(chain);
            if (status != null)
            {
                GUILayout.Label(GameTextResolver.Resolve(status));
            }
        }
    }
}
