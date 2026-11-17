using System;
using System.Collections.Generic;
using LastFleet.Pursuit;
using LastFleet.Shared.Text;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.LateSignals
{
    /// <summary>
    /// Picks up the late signals of the current system as the countdown to the enemy's arrival runs
    /// (GDD: late signals), so they follow time compression. Whoever shows the system tells it which system
    /// to listen to and shows each place once its signal is in.
    /// </summary>
    public sealed class LateSignalReceiver : MonoBehaviour
    {
        [SerializeField] PursuitClock _pursuit;

        readonly List<ZoneRecord> _received = new List<ZoneRecord>();
        SystemZones _system;

        public event Action<GameText> Notice;

        /// <summary>The zone of a place whose signal just came in.</summary>
        public event Action<ZoneRecord> SignalReceived;

        /// <summary>The system listened to, or null.</summary>
        public SystemZones ListenedSystem => _system;

        void Awake()
        {
            if (_pursuit == null)
            {
                Debug.LogError("LateSignalReceiver needs the pursuit clock.", this);
                enabled = false;
            }
        }

        /// <summary>Listens for the late signals of this system; null stops listening.</summary>
        public void Listen(SystemZones system)
        {
            _system = system;
        }

        void Update()
        {
            PursuitCountdown countdown = _pursuit.Countdown;
            if (_system == null || countdown == null)
            {
                return;
            }

            LateSignalSchedule.ReceiveDue(_system, countdown.ElapsedSeconds / countdown.ArrivalSeconds, _received);
            foreach (ZoneRecord zone in _received)
            {
                SignalReceived?.Invoke(zone);
                Notice?.Invoke(NoticeFor(zone.PointOfInterest));
            }
        }

        static GameText NoticeFor(PointOfInterest point)
        {
            GameTextKey key = point.Kind switch
            {
                PointOfInterestKind.StrandedShip => LateSignalTexts.DistressCall,
                PointOfInterestKind.Station => LateSignalTexts.RadioSilenceBroken,
                PointOfInterestKind.WreckField => LateSignalTexts.WreckFieldInRange,
                _ => LateSignalTexts.SignalPickedUp
            };
            return key.Format(("zone", point.Name));
        }
    }
}
