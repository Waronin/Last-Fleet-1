using LastFleet.Dialogue;
using LastFleet.Exploration;
using LastFleet.Shared.Text;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.LateSignals
{
    /// <summary>
    /// Puts a stranded ship's distress call through as a radio popup (GDD: events, "Notruf empfangen"): send a
    /// shuttle or ignore it. The game keeps running; without an answer the call is ignored after a while, and the
    /// place stays on the map either way. Sending picks a free small ship like the explore order does.
    /// </summary>
    public sealed class DistressCallRadio : MonoBehaviour
    {
        /// <summary>GDD: events give 30–60 seconds to answer.</summary>
        public const float AnswerSeconds = 45f;

        const int IgnoreChoiceIndex = 1;

        [SerializeField] LateSignalReceiver _receiver;
        [SerializeField] DialogueService _dialogue;
        [SerializeField] ScoutingService _scouting;

        void Awake()
        {
            if (_receiver == null || _dialogue == null || _scouting == null)
            {
                Debug.LogError("DistressCallRadio needs the late signal receiver, the dialogue service and the scouting service.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _receiver.SignalReceived += OnSignalReceived;
        }

        void OnDisable()
        {
            _receiver.SignalReceived -= OnSignalReceived;
        }

        void OnSignalReceived(ZoneRecord zone)
        {
            if (zone.PointOfInterest.Kind != PointOfInterestKind.StrandedShip)
            {
                return;
            }

            SystemZones system = _receiver.ListenedSystem;
            GameText zoneName = GameText.Verbatim(zone.PointOfInterest.Name);
            DialogueScript call = new DialogueBuilder()
                .Say("call", LateSignalTexts.DistressSpeaker.Format(("zone", zoneName)), LateSignalTexts.DistressLine)
                .Choice(LateSignalTexts.SendShuttle, null, () => SendShuttle(system, zone))
                .Choice(LateSignalTexts.IgnoreCall, null)
                .TimeLimit(AnswerSeconds, IgnoreChoiceIndex)
                .Build();
            _dialogue.Open(call);
        }

        void SendShuttle(SystemZones system, ZoneRecord zone)
        {
            // The fleet may have jumped on while the call waited; the place is out of reach then.
            if (_receiver.ListenedSystem != system)
            {
                return;
            }
            // The scouting service reports which ship flies, or that none is free.
            _scouting.RequestExploration(zone.PointOfInterestIndex, zone.PointOfInterest);
        }
    }
}
