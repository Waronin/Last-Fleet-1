using System;
using LastFleet.Dialogue;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests.Meltdown
{
    /// <summary>
    /// The Meltdown chain: an exploding station points to the scientist behind its experimental power
    /// source, who has plans for a generator that can set off a supernova. Steps 1 to 4 are playable;
    /// step 5 is written down and waits for its mechanics (see the Quests README).
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

        // Step 3: one harvester load (8 per second, 120 hold) takes 15 s, so the item comes within the first load.
        // The guards cover the rock and the station around it.
        const string VigilName = "Kethran Vigil";
        const float SacredRockRadiusUnits = 8f;
        const float SacredRockAmount = 3000f;
        const float MiningSecondsForSolarite = 12f;
        const int GuardCount = 4;
        const float GuardRadiusUnits = 60f;

        // Step 4: a shuttle crew reaches the limit after 40 s docked and dies one by one after that; 60 s away clears the dose.
        const string ForgeName = "Tamsin Forge";
        const float RefiningSeconds = 300f;
        const float RadiationSecondsToLimit = 40f;
        const float RadiationSecondsToRecover = 60f;
        const float RadiationDeathIntervalSeconds = 8f;
        const float RadiationWarningLevel = 0.75f;

        static readonly QuestItem Solarite = new QuestItem("meltdown.solarite", MeltdownTexts.SolariteItem);
        static readonly QuestItem RefinedSolarite = new QuestItem("meltdown.refined-solarite", MeltdownTexts.RefinedSolariteItem);

        public static QuestChainDefinition Create()
        {
            return new QuestChainDefinition(Id, MeltdownTexts.ChainTitle, new[]
            {
                new QuestStepDefinition(MeltdownTexts.PlatformTitle, MeltdownTexts.PlatformObjective, QuestFailure.EndsChain,
                    () => new SearchTrapStep(PlatformSettings())),
                new QuestStepDefinition(MeltdownTexts.ScientistTitle, MeltdownTexts.ScientistObjective, QuestFailure.EndsChain,
                    () => new EscortStep(ScientistSettings())),
                new QuestStepDefinition(MeltdownTexts.SacredRockTitle, MeltdownTexts.SacredRockObjective, QuestFailure.EndsChain,
                    () => new GuardedDepositStep(SacredRockSettings())),
                new QuestStepDefinition(MeltdownTexts.RefiningTitle, MeltdownTexts.RefiningObjective, QuestFailure.EndsChain,
                    () => new StationProcessingStep(RefiningSettings())),
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

        /// <summary>A wary outpost of believers next to the rock; its fighters defend it once someone mines it.</summary>
        static GuardedDepositSettings SacredRockSettings()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, 12, StationAttitude.Wary, StationCondition.Intact,
                StationRadiation.None);
            return new GuardedDepositSettings(new QuestStationSpec(VigilName, StationType.Outpost, profile), ResourceKind.Ore,
                SacredRockRadiusUnits, SacredRockAmount, Solarite, MiningSecondsForSolarite, GuardCount, GuardRadiusUnits,
                VigilWarning, VigilOutrage, SolariteSecured);
        }

        static DialogueScript VigilWarning(ZoneStation station)
        {
            GameText keeper = MeltdownTexts.KeeperSpeaker.Format(("station", GameText.Verbatim(station.Name)));
            return new DialogueBuilder()
                .Say("warning", keeper, MeltdownTexts.VigilWarning.Format(("station", GameText.Verbatim(station.Name))))
                .Choice(MeltdownTexts.VigilUnderstood, "peace")
                .Choice(MeltdownTexts.VigilNeedIt, "refusal")
                .Say("peace", keeper, MeltdownTexts.VigilPeace)
                .Say("refusal", keeper, MeltdownTexts.VigilRefusal)
                .Build();
        }

        static DialogueScript VigilOutrage(ZoneStation station)
        {
            return DialogueBuilder.Message(MeltdownTexts.KeeperSpeaker.Format(("station", GameText.Verbatim(station.Name))),
                MeltdownTexts.VigilOutrage);
        }

        static DialogueScript SolariteSecured(ShipAgent carrier)
        {
            return DialogueBuilder.Message(GameText.Verbatim(ScientistName), MeltdownTexts.ScientistSolariteSecured);
        }

        /// <summary>
        /// An abandoned refinery: its radiation drove everyone out, so nobody lives aboard. The furnace only runs
        /// while a docked crew keeps it going; a hail reaches its automatic control system.
        /// </summary>
        static StationProcessingSettings RefiningSettings()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.None, StationCondition.Damaged,
                StationRadiation.Elevated);
            return new StationProcessingSettings(new QuestStationSpec(ForgeName, StationType.Industrial, profile), Solarite, RefinedSolarite,
                RefiningSeconds, RadiationSecondsToLimit, RadiationSecondsToRecover, RadiationDeathIntervalSeconds, RadiationWarningLevel,
                ForgeBriefing, ForgeReceived, ForgeFinished);
        }

        static DialogueScript ForgeBriefing(ZoneStation station)
        {
            GameText stationName = GameText.Verbatim(station.Name);
            return DialogueBuilder.Message(MeltdownTexts.ForgeControlSpeaker.Format(("station", stationName)),
                MeltdownTexts.ForgeBriefing.Format(("station", stationName), ("minutes", (int)(RefiningSeconds / 60f))));
        }

        static DialogueScript ForgeReceived(ZoneStation station, ShipAgent ship)
        {
            return DialogueBuilder.Message(MeltdownTexts.DockedCrewSpeaker.Format(("ship", ship.DisplayName)), MeltdownTexts.ForgeReceived);
        }

        static DialogueScript ForgeFinished(ZoneStation station, ShipAgent ship)
        {
            return DialogueBuilder.Message(MeltdownTexts.DockedCrewSpeaker.Format(("ship", ship.DisplayName)), MeltdownTexts.ForgeFinished);
        }
    }
}
