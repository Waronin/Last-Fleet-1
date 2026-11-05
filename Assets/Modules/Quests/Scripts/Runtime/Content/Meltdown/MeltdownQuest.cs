using System;
using LastFleet.Dialogue;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests.Meltdown
{
    /// <summary>
    /// The Meltdown chain: an exploding station points to the scientist behind its experimental power
    /// source, who has plans for a generator that can set off a supernova. Steps 1 and 2 are playable;
    /// steps 3 to 5 are written down and wait for their mechanics (see the Quests README).
    /// </summary>
    public static class MeltdownQuest
    {
        public const string Id = "meltdown";

        // Proper names, shown as they are in every language.
        const string PlatformName = "Ascalon";
        const string ResearchStationName = "Meridian";
        const string ScientistName = "Dr. Ilka Morrow";
        const string ManufacturerName = "Halvorsen Kinetics";

        // Step 1: the fuse leaves time to undock and jump out (a shuttle charges a zone jump in about 5 s);
        // the blast destroys any small ship and dents capital ships (hull 1800–3500).
        const float FuseSeconds = 30f;
        const float BlastDamage = 150f;
        const int PlatformWreckCount = 6;
        const float MetalPerPlatformWreck = 80f;

        // Step 2: the scientist travels with a small crew; the Pronin need a moment to notice the launch.
        const int ShuttlePassengers = 4;
        const int AttackerCount = 3;
        const float AttackDelaySeconds = 8f;

        public static QuestChainDefinition Create()
        {
            return new QuestChainDefinition(Id, MeltdownTexts.ChainTitle, new[]
            {
                new QuestStepDefinition(MeltdownTexts.PlatformTitle, MeltdownTexts.PlatformObjective, QuestFailure.EndsChain,
                    () => new SearchTrapStep(PlatformSettings())),
                new QuestStepDefinition(MeltdownTexts.ScientistTitle, MeltdownTexts.ScientistObjective, QuestFailure.EndsChain,
                    () => new EscortStep(ScientistSettings())),
                new QuestStepDefinition(MeltdownTexts.SacredRockTitle, MeltdownTexts.SacredRockObjective, QuestFailure.EndsChain, null),
                new QuestStepDefinition(MeltdownTexts.RefiningTitle, MeltdownTexts.RefiningObjective, QuestFailure.EndsChain, null),
                new QuestStepDefinition(MeltdownTexts.IgnitionTitle, MeltdownTexts.IgnitionObjective, QuestFailure.EndsChain, null)
            });
        }

        /// <summary>An abandoned, walkable platform, so the search can start and set off the trap.</summary>
        static SearchTrapSettings PlatformSettings()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.None, StationCondition.Damaged,
                StationRadiation.None);
            return new SearchTrapSettings(new QuestStationSpec(PlatformName, StationType.Industrial, profile), FuseSeconds, BlastDamage,
                PlatformWreckCount, MetalPerPlatformWreck, PlatformAlarm);
        }

        static DialogueScript PlatformAlarm(ZoneStation station)
        {
            return DialogueBuilder.Message(MeltdownTexts.SearchPartySpeaker.Format(("station", GameText.Verbatim(station.Name))),
                MeltdownTexts.PlatformAlarm.Format(("seconds", (int)FuseSeconds), ("manufacturer", GameText.Verbatim(ManufacturerName))));
        }

        /// <summary>A friendly research station; hailing it reaches the scientist.</summary>
        static EscortSettings ScientistSettings()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, 24, StationAttitude.Friendly, StationCondition.Intact,
                StationRadiation.None);
            return new EscortSettings(new QuestStationSpec(ResearchStationName, StationType.Research, profile), ShuttlePassengers,
                AttackerCount, AttackDelaySeconds, ScientistOffer, ScientistArrived);
        }

        static DialogueScript ScientistOffer(ZoneStation station, Action accept)
        {
            GameText scientist = GameText.Verbatim(ScientistName);
            return new DialogueBuilder()
                .Say("offer", scientist, MeltdownTexts.ScientistOffer.Format(("station", GameText.Verbatim(station.Name)),
                    ("scientist", scientist)))
                .Choice(MeltdownTexts.ScientistAccept, "launching", accept)
                .Choice(MeltdownTexts.ScientistDecline, "waiting")
                .Say("launching", scientist, MeltdownTexts.ScientistLaunching)
                .Say("waiting", scientist, MeltdownTexts.ScientistWaiting)
                .Build();
        }

        static DialogueScript ScientistArrived(ShipAgent shuttle)
        {
            return DialogueBuilder.Message(GameText.Verbatim(ScientistName), MeltdownTexts.ScientistArrived);
        }
    }
}
