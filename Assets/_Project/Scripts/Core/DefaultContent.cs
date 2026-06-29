using System.Collections.Generic;
using UnityEngine;
using CarFactoryIdle.Data;

namespace CarFactoryIdle.Core
{
    /// <summary>Builds the entire GameConfig in code from the locked prototype values. This makes
    /// the game runnable with zero authored .asset files. Use the editor menu
    /// "CFI/Generate Default Content Assets" to bake these into real ScriptableObjects when you
    /// want to tune them in the inspector.</summary>
    public static class DefaultContent
    {
        public static GameConfig BuildConfig()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            BuildItems(cfg);
            BuildStations(cfg);
            BuildVehicles(cfg);
            BuildRaceCars(cfg);
            BuildGacha(cfg);
            BuildFactoryTiers(cfg);
            BuildContracts(cfg);
            BuildTrophyUpgrades(cfg);
            cfg.Init();
            return cfg;
        }

        // ---------- Items ----------
        private static ItemDefinition Item(string id, string name, string icon, ItemKind kind, int tier)
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>();
            d.id = id; d.displayName = name; d.icon = icon; d.kind = kind; d.tier = tier;
            return d;
        }

        private static void BuildItems(GameConfig cfg)
        {
            // Production model v2. Item `tier` for materials mirrors the extraction unlock tier
            // (T1: steel/rubber/copper; T2: aluminum/silicon; T3: carbon). The `icon` field is left
            // blank on purpose: the Unity developer assigns real sprites/icons in the inspector.
            cfg.items = new List<ItemDefinition>
            {
                // --- Materials (6) ---
                Item("steel",    "Steel",    "", ItemKind.Raw, 1),
                Item("rubber",   "Rubber",   "", ItemKind.Raw, 1),
                Item("copper",   "Copper",   "", ItemKind.Raw, 1),
                Item("aluminum", "Aluminum", "", ItemKind.Raw, 2),
                Item("silicon",  "Silicon",  "", ItemKind.Raw, 2),
                Item("carbon",   "Carbon",   "", ItemKind.Raw, 3),

                // --- Engines (3) ---
                Item("engineV4", "V4 Engine", "", ItemKind.Component, 1),
                Item("engineV6", "V6 Engine", "", ItemKind.Component, 2),
                Item("engineV8", "V8 Engine", "", ItemKind.Component, 3),
                // --- Chassis (3) ---
                Item("chassisSteel",    "Steel Chassis",    "", ItemKind.Component, 1),
                Item("chassisAluminum", "Aluminum Chassis", "", ItemKind.Component, 2),
                Item("chassisCarbon",   "Carbon Chassis",   "", ItemKind.Component, 3),
                // --- Wheels (3) ---
                Item("wheelStandard",    "Standard Wheels",    "", ItemKind.Component, 1),
                Item("wheelPerformance", "Performance Wheels", "", ItemKind.Component, 2),
                Item("wheelHyper",       "Hyper Wheels",       "", ItemKind.Component, 3),
                // --- Wiring (3) ---
                Item("wiringStandard", "Standard Wiring", "", ItemKind.Component, 1),
                Item("wiringAdvanced", "Advanced Wiring", "", ItemKind.Component, 2),
                Item("wiringPremium",  "Premium Wiring",  "", ItemKind.Component, 3),
            };
        }

        // ---------- Stations ----------
        private static StationDefinition Station(string id, string name, StationCategory cat, int tier,
            List<ItemCost> inputs, string output, float interval, long automation,
            bool unlocked, long unlockCost, int baseOutput = 1)
        {
            var d = ScriptableObject.CreateInstance<StationDefinition>();
            d.id = id; d.displayName = name; d.category = cat; d.tier = tier;
            d.inputs = inputs ?? new List<ItemCost>(); d.outputItemId = output;
            d.baseOutput = baseOutput; d.baseIntervalSeconds = interval; d.automationCost = automation;
            d.unlockedByDefault = unlocked; d.unlockCost = unlockCost;
            return d;
        }

        private static List<ItemCost> Costs(params (string id, int qty)[] items)
        {
            var list = new List<ItemCost>();
            foreach (var c in items) list.Add(new ItemCost(c.id, c.qty));
            return list;
        }

        private static void BuildStations(GameConfig cfg)
        {
            cfg.stations = new List<StationDefinition>
            {
                // --- Extraction (6), one material each. Tier gating:
                //     T1 (steel/rubber/copper) free; T2 (aluminum/silicon) & T3 (carbon) unlock-gated.
                // baseOutput tuned so supply/s = baseOutput/interval >= ~1.2x each material's PEAK
                // demand across all chains (peaks: steel 2.5 @T1, rubber 1.0 @T1, copper 1.143 @T3,
                // aluminum 2.1 @T2, silicon 0.857 @T3, carbon 1.4 @T3). Extractor-only balance bump.
                Station("steelWorks",        "Steel Works",        StationCategory.Extractor, 1, null, "steel",    2.0f, 500,  true,  0, 7), // 3.50/s (1.40x)
                Station("rubberPlantation",  "Rubber Plantation",  StationCategory.Extractor, 1, null, "rubber",   2.0f, 500,  true,  0, 3), // 1.50/s (1.50x)
                Station("copperMine",        "Copper Mine",        StationCategory.Extractor, 1, null, "copper",   2.5f, 500,  true,  0, 4), // 1.60/s (1.40x)
                Station("aluminumMine",      "Aluminum Mine",      StationCategory.Extractor, 2, null, "aluminum", 2.5f, 1500, false, 8000, 7), // 2.80/s (1.33x)
                Station("siliconQuarry",     "Silicon Quarry",     StationCategory.Extractor, 2, null, "silicon",  2.5f, 1500, false, 8000, 3), // 1.20/s (1.40x)
                Station("carbonFiberLab",    "Carbon-Fiber Lab",   StationCategory.Extractor, 3, null, "carbon",   3.0f, 6000, false, 80000, 6), // 2.00/s (1.43x)

                // --- Crafting (4 families x 3 tiers). v2 recipes consume RAW MATERIALS ONLY.
                //     Kept as one station per tiered output (the simulation keys production off a fixed
                //     outputItemId; SimSmokeTest derives a vehicle's chain from these outputs).
                // Engine Plant: V4={Steel} V6={Steel,Aluminum} V8={Steel,Aluminum,Carbon}
                Station("enginePlantV4", "Engine Plant - V4", StationCategory.Manufacturing, 1, Costs(("steel",5)),                        "engineV4", 4.0f, 1500,  true,  0),
                Station("enginePlantV6", "Engine Plant - V6", StationCategory.Manufacturing, 2, Costs(("steel",3),("aluminum",3)),         "engineV6", 5.0f, 5000,  false, 10000),
                Station("enginePlantV8", "Engine Plant - V8", StationCategory.Manufacturing, 3, Costs(("steel",4),("aluminum",3),("carbon",2)), "engineV8", 6.0f, 20000, false, 100000),
                // Chassis Shop: Steel={Steel} Aluminum={Aluminum} Carbon={Carbon}
                Station("chassisShopSteel",    "Chassis Shop - Steel",    StationCategory.Manufacturing, 1, Costs(("steel",5)),    "chassisSteel",    4.0f, 1500,  true,  0),
                Station("chassisShopAluminum", "Chassis Shop - Aluminum", StationCategory.Manufacturing, 2, Costs(("aluminum",5)), "chassisAluminum", 5.0f, 5000,  false, 10000),
                Station("chassisShopCarbon",   "Chassis Shop - Carbon",   StationCategory.Manufacturing, 3, Costs(("carbon",4)),   "chassisCarbon",   6.0f, 20000, false, 100000),
                // Wheel Factory: Standard={Rubber} Performance={Rubber,Aluminum} Hyper={Rubber,Carbon}
                Station("wheelFactoryStandard",    "Wheel Factory - Standard",    StationCategory.Manufacturing, 1, Costs(("rubber",3)),               "wheelStandard",    3.0f, 1200,  true,  0),
                Station("wheelFactoryPerformance", "Wheel Factory - Performance", StationCategory.Manufacturing, 2, Costs(("rubber",3),("aluminum",2)), "wheelPerformance", 4.0f, 4000,  false, 8000),
                Station("wheelFactoryHyper",       "Wheel Factory - Hyper",       StationCategory.Manufacturing, 3, Costs(("rubber",4),("carbon",2)),   "wheelHyper",       5.0f, 15000, false, 80000),
                // Electronics Lab: Standard={Copper} Advanced={Copper,Silicon} Premium={Copper x2,Silicon x2}
                Station("electronicsLabStandard", "Electronics Lab - Standard", StationCategory.Manufacturing, 1, Costs(("copper",4)),               "wiringStandard", 5.0f, 1800,  true,  0),
                Station("electronicsLabAdvanced", "Electronics Lab - Advanced", StationCategory.Manufacturing, 2, Costs(("copper",4),("silicon",3)),  "wiringAdvanced", 6.0f, 6000,  false, 12000),
                Station("electronicsLabPremium",  "Electronics Lab - Premium",  StationCategory.Manufacturing, 3, Costs(("copper",8),("silicon",6)),  "wiringPremium",  7.0f, 25000, false, 120000),

                // Assembly stations, one per vehicle tier. Builds the selected vehicle's recipe.
                Station("assemblyTier1", "Assembly Line I",   StationCategory.Assembly, 1, null, null, 6.0f, 3500,   true,  0),
                Station("assemblyTier2", "Assembly Line II",  StationCategory.Assembly, 2, null, null, 6.0f, 12000,  false, 10000),
                Station("assemblyTier3", "Assembly Line III", StationCategory.Assembly, 3, null, null, 6.0f, 60000,  false, 100000),
                Station("assemblyTier4", "Assembly Line IV",  StationCategory.Assembly, 4, null, null, 6.0f, 400000, false, 1000000),

                // Sales office (passive cash path; main selling is the auction).
                Station("salesOffice", "Sales Office", StationCategory.Sales, 1, null, null, 3.0f, 2000, true, 0),
            };
        }

        // ---------- Vehicles ----------
        private static VehicleDefinition Vehicle(string id, string name, int tier, long price,
            RaceStats stats, List<ItemCost> recipe)
        {
            var d = ScriptableObject.CreateInstance<VehicleDefinition>();
            d.id = id; d.displayName = name; d.tier = tier; d.basePrice = price;
            d.baseRaceStats = stats; d.recipe = recipe;
            return d;
        }

        private static RaceStats RS(float ts, float ac, float tr, float la)
            => new RaceStats { topSpeed = ts, acceleration = ac, traction = tr, launch = la };

        private static void BuildVehicles(GameConfig cfg)
        {
            // Base prices match the production economy (2026-06-18).
            cfg.vehicles = new List<VehicleDefinition>
            {
                // Tier 1: engineV4 + chassisSteel + 4x wheelStandard + wiringStandard
                Vehicle("tokyoCommuter",  "Tokyo Commuter",   1, 800,    RS(10,8,8,8),     Costs(("engineV4",1),("chassisSteel",1),("wheelStandard",4),("wiringStandard",1))),
                Vehicle("tokyoTrekker",   "Tokyo Trekker",    1, 1200,   RS(12,9,10,8),    Costs(("engineV4",1),("chassisSteel",1),("wheelStandard",4),("wiringStandard",2))),
                // Tier 2: engineV6 + chassisAluminum + 4x wheelPerformance + wiringAdvanced
                Vehicle("hiroshimaBreeze","Hiroshima Breeze", 2, 3500,   RS(24,22,20,18),  Costs(("engineV6",1),("chassisAluminum",1),("wheelPerformance",4),("wiringAdvanced",1))),
                Vehicle("bavarianSeries3","Bavarian Series 3",2, 4500,   RS(20,18,16,16),  Costs(("engineV6",1),("chassisAluminum",1),("wheelPerformance",4),("wiringAdvanced",1))),
                Vehicle("britanniaRover", "Britannia Rover",  2, 6000,   RS(22,16,18,16),  Costs(("engineV6",1),("chassisAluminum",2),("wheelPerformance",4),("wiringAdvanced",1))),
                // Tier 3: engineV8 + chassisCarbon + 4x wheelHyper + wiringPremium
                Vehicle("stuttgartSClass","Stuttgart S-Class",3, 18000,  RS(30,26,24,24),  Costs(("engineV8",1),("chassisCarbon",1),("wheelHyper",4),("wiringPremium",2))),
                Vehicle("stuttgartGBox",  "Stuttgart G-Box",  3, 25000,  RS(28,24,26,22),  Costs(("engineV8",1),("chassisCarbon",2),("wheelHyper",4),("wiringPremium",2))),
                Vehicle("autobahn911",    "Autobahn 911",     3, 30000,  RS(40,38,34,34),  Costs(("engineV8",1),("chassisCarbon",1),("wheelHyper",4),("wiringPremium",2))),
                Vehicle("milanoToro",     "Milano Toro",      3, 50000,  RS(55,52,48,46),  Costs(("engineV8",2),("chassisCarbon",1),("wheelHyper",4),("wiringPremium",1))),
                // Tier 4 (hypercars): T3 parts at higher quantities
                Vehicle("maranelloRosso", "Maranello Rosso",  4, 120000, RS(52,50,46,44),  Costs(("engineV8",2),("chassisCarbon",1),("wheelHyper",4),("wiringPremium",2))),
                Vehicle("molsheimRoyale", "Molsheim Royale",  4, 350000, RS(70,66,60,58),  Costs(("engineV8",4),("chassisCarbon",2),("wheelHyper",4),("wiringPremium",3))),
                Vehicle("angelholmApex",  "Angelholm Apex",   4, 800000, RS(80,78,70,68),  Costs(("engineV8",4),("chassisCarbon",2),("wheelHyper",4),("wiringPremium",4))),
            };
        }

        // ---------- Race cars (dedicated race-only production) ----------
        // Reuses VehicleDefinition: recipe = parts cost, basePrice = cash build cost,
        // tier = minimum factory tier (1-based) required to build. Ids have NO underscore so they
        // never collide with graded showroom-car keys ("<id>_<grade>"). Stats are flat (no gacha).
        private static void BuildRaceCars(GameConfig cfg)
        {
            cfg.raceCars = new List<VehicleDefinition>
            {
                Vehicle("rcVulcan",   "Vulcan GT",      2, 8000,   RS(60,58,52,40),
                    Costs(("engineV6",2),("chassisAluminum",2),("wheelPerformance",2),("wiringAdvanced",2))),
                Vehicle("rcThunder",  "Thunderbolt R",  3, 40000,  RS(85,82,72,58),
                    Costs(("engineV8",2),("chassisCarbon",2),("wheelHyper",2),("wiringPremium",1))),
                Vehicle("rcHypernova","Hypernova X",    4, 150000, RS(110,105,92,80),
                    Costs(("engineV8",4),("chassisCarbon",3),("wheelHyper",4),("wiringPremium",3))),
            };
        }

        // ---------- Gacha ----------
        private static GachaTable.Entry G(Grade g, float w, float m)
            => new GachaTable.Entry { grade = g, weight = w, priceMultiplier = m };

        private static void BuildGacha(GameConfig cfg)
        {
            var t = ScriptableObject.CreateInstance<GachaTable>();
            t.baseEntries = new List<GachaTable.Entry>
            {
                G(Grade.D, 15f, 0.8f), G(Grade.C, 50f, 1.0f), G(Grade.B, 20f, 1.2f),
                G(Grade.A, 10f, 1.5f), G(Grade.S, 4f, 2.5f),  G(Grade.SPlus, 1f, 5.0f),
            };
            t.qualityControlEntries = new List<GachaTable.Entry>
            {
                G(Grade.D, 10f, 0.8f), G(Grade.C, 45f, 1.0f), G(Grade.B, 20f, 1.2f),
                G(Grade.A, 15f, 1.5f), G(Grade.S, 7f, 2.5f),  G(Grade.SPlus, 3f, 5.0f),
            };
            cfg.gachaTable = t;
        }

        // ---------- Factory tiers ----------
        private static FactoryTierDefinition Tier(int tier, string name, long cost, float mult)
        {
            var d = ScriptableObject.CreateInstance<FactoryTierDefinition>();
            d.tier = tier; d.displayName = name; d.cost = cost; d.productionMultiplier = mult;
            return d;
        }

        private static void BuildFactoryTiers(GameConfig cfg)
        {
            cfg.factoryTiers = new List<FactoryTierDefinition>
            {
                Tier(1, "Garage Workshop", 0, 1f),
                Tier(2, "Small Factory", 10000, 2f),
                Tier(3, "Industrial Plant", 100000, 5f),
                Tier(4, "Mega Factory", 1000000, 15f),
            };
        }

        // ---------- Contracts ----------
        private static ContractTypeDefinition Contract(ContractType type, string name, int minTier,
            int minQty, int maxBase, int maxPerTier, float payout, float baseTimer, float timerPerTier,
            int trophyMin, int trophyMax, int trophyPerTier)
        {
            var d = ScriptableObject.CreateInstance<ContractTypeDefinition>();
            d.type = type; d.displayName = name; d.minFactoryTier = minTier;
            d.minQty = minQty; d.maxQtyBase = maxBase; d.maxQtyPerTier = maxPerTier;
            d.payoutMultiplier = payout; d.baseTimerSeconds = baseTimer; d.timerPerTier = timerPerTier;
            d.trophyMin = trophyMin; d.trophyMax = trophyMax; d.trophyPerTier = trophyPerTier;
            return d;
        }

        private static void BuildContracts(GameConfig cfg)
        {
            // Premium pays trophies instead of cash. VIP is simplified to a single vehicle for now;
            // revisit if a multi-vehicle VIP contract is wanted later.
            cfg.contractTypes = new List<ContractTypeDefinition>
            {
                Contract(ContractType.Standard, "Standard Sale", 1, 1, 3, 1, 1.5f, 0, 0, 0, 0, 0),
                Contract(ContractType.Bulk,     "Bulk Order",    1, 5, 10, 3, 1.3f, 0, 0, 0, 0, 0),
                Contract(ContractType.Rush,     "Rush Order",    1, 2, 4, 0, 2.5f, 60f, 30f, 0, 0, 0),
                Contract(ContractType.Premium,  "Premium Buyer", 2, 1, 2, 0, 0f, 0, 0, 1, 3, 1),
                Contract(ContractType.VIP,      "VIP Contract",  3, 1, 2, 1, 3.0f, 0, 0, 1, 2, 0),
            };
        }

        // ---------- Trophy upgrades ----------
        private static TrophyUpgradeDefinition Trophy(string id, string name, int cost, string fx)
        {
            var d = ScriptableObject.CreateInstance<TrophyUpgradeDefinition>();
            d.id = id; d.displayName = name; d.trophyCost = cost; d.effectDescription = fx;
            return d;
        }

        private static void BuildTrophyUpgrades(GameConfig cfg)
        {
            cfg.trophyUpgrades = new List<TrophyUpgradeDefinition>
            {
                Trophy("nitroKit",      "Nitro Kit",            5,  "2s burst at 1.35x accel, one use per race"),
                Trophy("turboTuning",   "Turbo Engine Tuning", 10, "+acceleration in races"),
                Trophy("racingTires",   "Racing Tires",        8,  "+traction in races"),
                Trophy("chromeFinish",  "Chrome Finish",       3,  "cosmetic / minor sale bonus"),
                Trophy("sponsorDeal",   "Sponsor Deal",        15, "passive cash bonus"),
            };
        }
    }
}
