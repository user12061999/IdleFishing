#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using IdleFishing.World;

namespace IdleFishing.Editor
{
    public static class ToonDistantOceanGenerator
    {
        private const string ModelsDir = "Assets/Models/Gameplay/Toon";
        private const string PrefabsDir = "Assets/Prefabs/DistantOcean";
        private const string AssetBasePath = "Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite";

        [MenuItem("IdleFishing/Generate All Distant Ocean Assets", false, 11)]
        public static void GenerateAllDistantOceanAssets()
        {
            if (!Directory.Exists(ModelsDir)) Directory.CreateDirectory(ModelsDir);
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);

            Debug.Log("[ToonDistantOceanGenerator] Starting generation of all 16 Distant Ocean 3D Models...");

            // Theme 1: Temperate Lighthouse & Tall Ship
            BuildDistantBeaconIsland();
            BuildDistantMerchantTallShip();
            BuildDistantSeaBuoy();

            // Theme 2: Tropical Sunken Galleon & Coral Reef
            BuildDistantSunkenGalleon();
            BuildDistantCoralAtoll();
            BuildDistantLeapingDolphin();
            BuildDistantPalmAtoll();

            // Theme 3: Autumn Cove Beacon & Timber Barge
            BuildDistantAutumnBeacon();
            BuildDistantTimberBarge();
            BuildDistantFjordStack();

            // Theme 4: Arctic Iceberg Range & Breaching Whale
            BuildDistantGlacialIcebergRange();
            BuildDistantBreachingWhale();
            BuildDistantPenguinPackIce();

            // Theme 5: Desert Oasis Sunken Pharaoh & Mirage Dunes
            BuildDistantSunkenPharaoh();
            BuildDistantSunkenObelisk();
            BuildDistantMirageDune();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ToonDistantOceanGenerator] All 16 Distant Ocean Models generated successfully! Now creating prefabs...");
            CreateAllPrefabs();
            Debug.Log("[ToonDistantOceanGenerator] All Distant Ocean Prefabs created successfully!");
        }

        // =========================================================================
        // 1. THEME 1: DISTANT BEACON ISLAND (Level 1)
        // =========================================================================
        public static void BuildDistantBeaconIsland()
        {
            var b = new ToonObjBuilder("Toon_Distant_Beacon_Island");

            // Rocky Islet Foundation (Stepped coastal basalt plinth)
            b.AddTaperedCylinder(new Vector3(0f, 0.4f, 0f), 4.8f, 4.3f, 0.8f, ToonColor.MediumStone, 8);
            b.AddTaperedCylinder(new Vector3(0f, 1.0f, 0f), 4.3f, 3.8f, 0.6f, ToonColor.LightStone, 8);
            // Grassy Island Plateau
            b.AddTaperedCylinder(new Vector3(0f, 1.45f, 0f), 3.8f, 3.5f, 0.4f, ToonColor.GrassGreen, 8);

            // Natural Crags & Shoreline Boulders
            b.AddBox(new Vector3(-3.2f, 0.4f,  1.8f), new Vector3(1.8f, 1.1f, 1.6f), ToonColor.MediumStone);
            b.AddBox(new Vector3( 3.0f, 0.4f, -1.6f), new Vector3(1.9f, 1.2f, 1.7f), ToonColor.DarkIron);
            b.AddBox(new Vector3(-2.6f, 0.3f, -2.4f), new Vector3(1.6f, 0.9f, 1.5f), ToonColor.LightStone);
            b.AddBox(new Vector3( 2.4f, 0.3f,  2.5f), new Vector3(1.7f, 1.0f, 1.5f), ToonColor.MediumStone);

            // Stone Jetty / Boat Landing Steps (South side)
            b.AddBox(new Vector3(0f, 0.25f, -3.8f), new Vector3(2.2f, 0.5f, 1.4f), ToonColor.LightStone);
            b.AddBox(new Vector3(0f, 0.55f, -3.1f), new Vector3(2.0f, 0.5f, 1.2f), ToonColor.LightStone);
            b.AddCylinder(new Vector3(-0.9f, 0.7f, -4.2f), 0.12f, 0.6f, ToonColor.CastIron, 6);
            b.AddCylinder(new Vector3( 0.9f, 0.7f, -4.2f), 0.12f, 0.6f, ToonColor.CastIron, 6);

            // Keeper's Stone Cottage (West side of islet)
            Vector3 cottagePos = new Vector3(-1.8f, 2.1f, 0.2f);
            b.AddBox(cottagePos, new Vector3(2.4f, 1.3f, 1.8f), ToonColor.LightStone);
            // Pitched Roof
            b.AddWedgeRoof(cottagePos + new Vector3(0f, 0.95f, 0f), new Vector3(2.6f, 0.8f, 2.0f), ToonColor.Terracotta);
            // Stone Chimney
            b.AddBox(cottagePos + new Vector3(-0.7f, 1.2f, -0.4f), new Vector3(0.45f, 1.2f, 0.45f), ToonColor.MediumStone);
            b.AddBox(cottagePos + new Vector3(-0.7f, 1.85f, -0.4f), new Vector3(0.55f, 0.15f, 0.55f), ToonColor.DarkIron);
            // Cottage Door & Warm Window
            b.AddBox(cottagePos + new Vector3(0.4f, -0.15f, -0.92f), new Vector3(0.5f, 0.85f, 0.08f), ToonColor.WarmWood);
            b.AddBox(cottagePos + new Vector3(-0.5f, 0.1f, -0.92f), new Vector3(0.45f, 0.45f, 0.08f), ToonColor.SunlightGold);

            // Tall Ancient Lighthouse Tower (Centered slightly east)
            Vector3 ltPos = new Vector3(1.2f, 1.6f, 0.4f);
            // Base Plinth
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 0.4f, 0f), 1.6f, 1.45f, 0.8f, ToonColor.MediumStone, 8);
            b.AddCylinder(ltPos + new Vector3(0f, 0.85f, 0f), 1.48f, 0.15f, ToonColor.DarkIron, 8);
            // Section 1: White
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 1.9f, 0f), 1.42f, 1.22f, 2.0f, ToonColor.PlasterWhite, 8);
            // Section 2: Red Band
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 3.4f, 0f), 1.22f, 1.05f, 1.2f, ToonColor.Terracotta, 8);
            // Section 3: White
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 4.6f, 0f), 1.05f, 0.90f, 1.4f, ToonColor.PlasterWhite, 8);
            // Balcony Gallery
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 5.45f, 0f), 0.90f, 1.25f, 0.35f, ToonColor.DarkTimber, 8);
            b.AddCylinder(ltPos + new Vector3(0f, 5.68f, 0f), 1.28f, 0.12f, ToonColor.DarkTimber, 8);
            // Railing posts
            for (int i = 0; i < 8; i++)
            {
                float a = (i / 8f) * Mathf.PI * 2f;
                b.AddBox(ltPos + new Vector3(Mathf.Cos(a) * 1.2f, 6.0f, Mathf.Sin(a) * 1.2f), new Vector3(0.06f, 0.55f, 0.06f), ToonColor.DarkIron);
            }
            b.AddCylinder(ltPos + new Vector3(0f, 6.3f, 0f), 1.22f, 0.05f, ToonColor.DarkIron, 8);
            // Beacon Lantern Room
            b.AddCylinder(ltPos + new Vector3(0f, 6.38f, 0f), 0.88f, 0.1f, ToonColor.SunlightGold, 8);
            b.AddCylinder(ltPos + new Vector3(0f, 6.85f, 0f), 0.55f, 0.75f, ToonColor.SunlightGold, 8);
            // Struts
            for (int i = 0; i < 8; i++)
            {
                float a = (i / 8f) * Mathf.PI * 2f;
                b.AddBox(ltPos + new Vector3(Mathf.Cos(a) * 0.85f, 6.85f, Mathf.Sin(a) * 0.85f), new Vector3(0.06f, 0.85f, 0.06f), ToonColor.DarkIron);
            }
            // Roof Cone & Weathervane
            b.AddCone(ltPos + new Vector3(0f, 8.0f, 0f), 0.98f, 1.4f, ToonColor.Terracotta, 8);
            b.AddCylinder(ltPos + new Vector3(0f, 8.9f, 0f), 0.04f, 0.6f, ToonColor.SunlightGold, 6);

            // Coastal Pine Trees on Islet
            Vector3 pine1 = new Vector3(-0.6f, 1.6f, 2.2f);
            b.AddCylinder(pine1 + new Vector3(0f, 0.6f, 0f), 0.16f, 1.2f, ToonColor.DarkTimber, 6);
            b.AddCone(pine1 + new Vector3(0f, 1.6f, 0f), 1.2f, 1.5f, ToonColor.GrassGreen, 7);
            b.AddCone(pine1 + new Vector3(0f, 2.5f, 0f), 0.95f, 1.3f, ToonColor.DeepMoss, 7);
            b.AddCone(pine1 + new Vector3(0f, 3.3f, 0f), 0.65f, 1.1f, ToonColor.GrassGreen, 6);

            Vector3 pine2 = new Vector3(2.5f, 1.6f, -1.8f);
            b.AddCylinder(pine2 + new Vector3(0f, 0.45f, 0f), 0.14f, 0.9f, ToonColor.DarkTimber, 6);
            b.AddCone(pine2 + new Vector3(0f, 1.2f, 0f), 0.95f, 1.2f, ToonColor.DeepMoss, 7);
            b.AddCone(pine2 + new Vector3(0f, 1.9f, 0f), 0.70f, 1.0f, ToonColor.GrassGreen, 6);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Beacon_Island.obj"));
        }

        // =========================================================================
        // 2. THEME 1: DISTANT 3-MASTED MERCHANT TALL SHIP
        // =========================================================================
        public static void BuildDistantMerchantTallShip()
        {
            var b = new ToonObjBuilder("Toon_Distant_Merchant_TallShip");

            // Main Hull (Chunky stylized merchant galleon/tall ship, length ~8.8m, width ~2.8m)
            b.AddLoftedBoatHull(Vector3.zero, 8.8f, 2.8f, 1.6f, 0.65f, 0.35f, ToonColor.WarmWood, ToonColor.HoneyWood, ToonColor.HoneyWood);

            // Dark Timber Keel & Waterline Stripe
            b.AddBox(new Vector3(0f, -0.6f, 0f), new Vector3(0.4f, 0.35f, 8.2f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(2.85f, 0.12f, 8.0f), ToonColor.SunlightGold);

            // Raised Quarterdeck & Poop Deck at Stern (Z negative)
            Vector3 quarterdeckPos = new Vector3(0f, 0.75f, -2.6f);
            b.AddBox(quarterdeckPos, new Vector3(2.4f, 0.8f, 2.6f), ToonColor.WarmWood);
            // Stern Transom Cabin Gallery with glowing windows
            b.AddBox(quarterdeckPos + new Vector3(0f, 0.2f, -1.32f), new Vector3(2.1f, 0.45f, 0.08f), ToonColor.SunlightGold);
            b.AddBox(quarterdeckPos + new Vector3(0f, 0.7f, -1.25f), new Vector3(0.2f, 0.35f, 0.2f), ToonColor.SunlightGold); // Stern Lantern

            // Bowsprit Spar jutting forward from bow (Z positive)
            Vector3 bowspritPos = new Vector3(0f, 1.25f, 4.8f);
            b.AddCylinder(bowspritPos, 0.11f, 2.6f, ToonColor.HoneyWood, 6);
            // Jib Sails (Triangular stay sails between bowsprit and foremast)
            Vector3 jib1 = new Vector3(0f, 1.8f, 4.4f);
            Vector3 jib2 = new Vector3(0f, 1.1f, 5.6f);
            Vector3 jib3 = new Vector3(0f, 3.8f, 2.2f);
            b.AddTri(jib1, jib2, jib3, Vector3.right, ToonColor.PlasterWhite);
            b.AddTri(jib1, jib3, jib2, Vector3.left, ToonColor.PlasterWhite);

            // 3 TALL MASTS: Foremast (+Z), Mainmast (Center), Mizzenmast (-Z)
            float[] mastZ = new float[] { 2.0f, -0.3f, -2.4f };
            float[] mastHeights = new float[] { 6.2f, 7.5f, 5.2f };
            float[] mastRadius = new float[] { 0.13f, 0.15f, 0.12f };

            for (int m = 0; m < 3; m++)
            {
                float z = mastZ[m];
                float h = mastHeights[m];
                float r = mastRadius[m];

                // Vertical Mast
                b.AddCylinder(new Vector3(0f, h * 0.5f, z), r, h, ToonColor.WarmWood, 8);
                // Crow's Nest Lookout
                if (m < 2)
                {
                    b.AddCylinder(new Vector3(0f, h * 0.72f, z), 0.55f, 0.38f, ToonColor.DarkTimber, 8);
                }

                // 2 Yardarms (crossbars) per mast with billowing square sails
                float[] yardY = new float[] { h * 0.42f, h * 0.75f };
                float[] yardW = new float[] { 2.6f - m * 0.3f, 2.0f - m * 0.25f };

                for (int y = 0; y < 2; y++)
                {
                    float yPos = yardY[y];
                    float w = yardW[y];
                    // Yardarm beam
                    b.AddBox(new Vector3(0f, yPos, z), new Vector3(w, 0.10f, 0.10f), ToonColor.HoneyWood);

                    // Billowing Stylized Sail (curved forward)
                    float sailH = 1.25f;
                    Vector3 sTL = new Vector3(-w * 0.46f, yPos, z);
                    Vector3 sTR = new Vector3( w * 0.46f, yPos, z);
                    Vector3 sBL = new Vector3(-w * 0.42f, yPos - sailH, z + 0.35f);
                    Vector3 sBR = new Vector3( w * 0.42f, yPos - sailH, z + 0.35f);

                    b.AddQuad(sTL, sTR, sBR, sBL, new Vector3(0f, 0.2f, 0.95f), ToonColor.PlasterWhite);
                    b.AddQuad(sTL, sBL, sBR, sTR, new Vector3(0f, -0.2f, -0.95f), ToonColor.PlasterWhite);
                }
            }

            // Deck Cargo Barrels & Crates
            b.AddCylinder(new Vector3(0.5f, 0.5f, 0.8f), 0.25f, 0.6f, ToonColor.HoneyWood, 8);
            b.AddCylinder(new Vector3(-0.5f, 0.5f, 0.9f), 0.22f, 0.55f, ToonColor.HoneyWood, 8);
            b.AddBox(new Vector3(0.0f, 0.55f, 1.2f), new Vector3(0.6f, 0.5f, 0.6f), ToonColor.DarkTimber);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Merchant_TallShip.obj"));
        }

        // =========================================================================
        // 3. THEME 1: DISTANT NAVIGATIONAL SEA BUOY
        // =========================================================================
        public static void BuildDistantSeaBuoy()
        {
            var b = new ToonObjBuilder("Toon_Distant_Sea_Buoy");

            // Submerged Counterweight Ring
            b.AddCylinder(new Vector3(0f, -0.45f, 0f), 0.75f, 0.3f, ToonColor.DarkIron, 8);
            // Floating Conical Buoy Body (Red bottom, White middle, Red top band)
            b.AddTaperedCylinder(new Vector3(0f, 0.0f, 0f), 0.75f, 0.60f, 0.6f, ToonColor.Terracotta, 8);
            b.AddTaperedCylinder(new Vector3(0f, 0.5f, 0f), 0.60f, 0.45f, 0.5f, ToonColor.PlasterWhite, 8);
            b.AddTaperedCylinder(new Vector3(0f, 0.9f, 0f), 0.45f, 0.32f, 0.4f, ToonColor.Terracotta, 8);

            // Metal Cage Superstructure & Warning Bell
            b.AddCylinder(new Vector3(0f, 1.15f, 0f), 0.38f, 0.1f, ToonColor.DarkIron, 8);
            for (int i = 0; i < 4; i++)
            {
                float a = (i / 4f) * Mathf.PI * 2f;
                b.AddBox(new Vector3(Mathf.Cos(a) * 0.32f, 1.55f, Mathf.Sin(a) * 0.32f), new Vector3(0.06f, 0.7f, 0.06f), ToonColor.DarkIron);
            }
            // Warning Lantern Core
            b.AddCylinder(new Vector3(0f, 1.55f, 0f), 0.20f, 0.45f, ToonColor.SunlightGold, 8);
            // Conical Roof & Radar Reflector
            b.AddCone(new Vector3(0f, 2.05f, 0f), 0.40f, 0.45f, ToonColor.Terracotta, 8);
            b.AddCylinder(new Vector3(0f, 2.4f, 0f), 0.03f, 0.35f, ToonColor.DarkIron, 6);

            // Water Ripple Foam Ring at Base
            b.AddCylinder(new Vector3(0f, -0.05f, 0f), 1.05f, 0.06f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Sea_Buoy.obj"));
        }

        // =========================================================================
        // 4. THEME 2: DISTANT SUNKEN PIRATE GALLEON (Level 2)
        // =========================================================================
        public static void BuildDistantSunkenGalleon()
        {
            var b = new ToonObjBuilder("Toon_Distant_Sunken_Galleon");

            // Galleon Hull (Listing 22 degrees on shallow reef)
            // Model generated with built-in tilt for dramatic shipwreck pose
            b.AddLoftedBoatHull(new Vector3(0f, 0.4f, 0f), 8.5f, 3.2f, 1.8f, 0.7f, 0.4f, ToonColor.DarkTimber, ToonColor.WarmWood, ToonColor.DarkTimber);

            // Moss / Algae on water line
            b.AddBox(new Vector3(0f, 0.1f, 0f), new Vector3(3.3f, 0.25f, 8.2f), ToonColor.DeepMoss);

            // High Ornate Sterncastle (Broken Pirate Galleon Gallery)
            Vector3 sternPos = new Vector3(0f, 1.2f, -2.8f);
            b.AddBox(sternPos, new Vector3(2.6f, 1.4f, 2.4f), ToonColor.DarkTimber);
            // Broken window frames & decorative gold moulding
            b.AddBox(sternPos + new Vector3(0f, 0.3f, -1.22f), new Vector3(2.2f, 0.6f, 0.08f), ToonColor.SunlightGold);
            b.AddBox(sternPos + new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 0.15f, 2.2f), ToonColor.WarmWood);

            // 6 Cannon Ports along flanks (3 left, 3 right)
            float[] gunZ = new float[] { -1.5f, 0.2f, 1.9f };
            for (int i = 0; i < 3; i++)
            {
                // Port cannons
                b.AddCylinder(new Vector3(-1.65f, 0.6f, gunZ[i]), 0.12f, 0.65f, ToonColor.CastIron, 6);
                // Starboard cannons
                b.AddCylinder(new Vector3( 1.65f, 0.6f, gunZ[i]), 0.12f, 0.65f, ToonColor.CastIron, 6);
            }

            // Shattered Tilted Mainmast (Split at height, leaning backward)
            Vector3 mainmastBase = new Vector3(0.2f, 0.8f, -0.4f);
            b.AddCylinder(mainmastBase + new Vector3(0f, 1.8f, 0f), 0.18f, 3.6f, ToonColor.DarkTimber, 8);
            // Tilted fallen upper mast
            Vector3 fallenStart = mainmastBase + new Vector3(0f, 3.6f, 0f);
            Vector3 fallenEnd   = fallenStart + new Vector3(-0.8f, 1.8f, -2.2f);
            b.AddAngledBar(fallenStart, fallenEnd, 0.12f, 0.12f, ToonColor.WarmWood);
            // Tattered Hanging Canvas Sail Scraps
            Vector3 sailP1 = fallenStart + new Vector3(0.4f, -0.2f, -0.3f);
            Vector3 sailP2 = fallenEnd + new Vector3(0.6f, -0.5f, 0.2f);
            Vector3 sailP3 = fallenStart + new Vector3(-0.3f, -1.6f, -0.8f);
            b.AddTri(sailP1, sailP2, sailP3, Vector3.up, ToonColor.PlasterWhite);
            b.AddTri(sailP1, sailP3, sailP2, Vector3.down, ToonColor.PlasterWhite);

            // Broken Foremast Stump (+Z)
            b.AddCylinder(new Vector3(0f, 1.5f, 2.2f), 0.16f, 2.0f, ToonColor.DarkTimber, 8);
            b.AddCone(new Vector3(0f, 2.65f, 2.2f), 0.17f, 0.4f, ToonColor.HoneyWood, 6); // Splintered top

            // Snapped Bowsprit jutting into water
            b.AddAngledBar(new Vector3(0f, 1.2f, 4.4f), new Vector3(0.3f, 0.1f, 6.2f), 0.12f, 0.12f, ToonColor.DarkTimber);

            // Sunken Treasure Chest lodged in broken deck
            b.AddTreasureChest(new Vector3(0.5f, 0.7f, 0.6f), new Vector3(0.65f, 0.45f, 0.45f), ToonColor.WarmWood, ToonColor.SunlightGold);

            // Encrusted Marine Barnacles & Seaweed on Reef Base
            b.AddBox(new Vector3(-1.8f, 0.1f, 0.5f), new Vector3(1.2f, 0.5f, 1.4f), ToonColor.LightStone);
            b.AddBox(new Vector3( 1.7f, 0.1f, -1.2f), new Vector3(1.4f, 0.6f, 1.5f), ToonColor.Terracotta); // Orange coral growth
            b.AddBox(new Vector3(-0.8f, 0.15f, 3.2f), new Vector3(1.1f, 0.4f, 1.1f), ToonColor.IceBlue);    // Cyan reef patch

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Sunken_Galleon.obj"));
        }

        // =========================================================================
        // 5. THEME 2: DISTANT SHALLOW CORAL ATOLL REEF
        // =========================================================================
        public static void BuildDistantCoralAtoll()
        {
            var b = new ToonObjBuilder("Toon_Distant_Coral_Atoll");

            // Submerged Reef Sandbank Plateau
            b.AddTaperedCylinder(new Vector3(0f, -0.1f, 0f), 3.8f, 3.4f, 0.4f, ToonColor.SunlightGold, 8);

            // Ring of Colorful Stylized Low-Poly Coral Formations
            // 1. Terracotta / Red Staghorn Coral Cluster
            Vector3 c1 = new Vector3(-1.8f, 0.1f, 1.2f);
            b.AddCylinder(c1 + new Vector3(0f, 0.4f, 0f), 0.35f, 0.7f, ToonColor.Terracotta, 6);
            b.AddCone(c1 + new Vector3(-0.25f, 0.95f, 0f), 0.18f, 0.65f, ToonColor.Terracotta, 5);
            b.AddCone(c1 + new Vector3( 0.25f, 0.90f, 0.15f), 0.16f, 0.60f, ToonColor.Terracotta, 5);
            b.AddCone(c1 + new Vector3( 0.05f, 1.05f, -0.2f), 0.14f, 0.70f, ToonColor.Terracotta, 5);

            // 2. IceBlue / Turquoise Branching Coral Cluster
            Vector3 c2 = new Vector3(1.6f, 0.1f, -1.4f);
            b.AddCylinder(c2 + new Vector3(0f, 0.35f, 0f), 0.32f, 0.6f, ToonColor.IceBlue, 6);
            b.AddCone(c2 + new Vector3(-0.2f, 0.85f, -0.15f), 0.16f, 0.55f, ToonColor.IceBlue, 5);
            b.AddCone(c2 + new Vector3( 0.2f, 0.90f,  0.10f), 0.18f, 0.60f, ToonColor.IceBlue, 5);

            // 3. SproutGreen / DeepMoss Brain Coral Mounds
            Vector3 c3 = new Vector3(2.0f, 0.15f, 1.0f);
            b.AddOvoid(c3 + new Vector3(0f, 0.35f, 0f), new Vector3(0.65f, 0.45f, 0.60f), ToonColor.SproutGreen, 8, 4);
            Vector3 c4 = new Vector3(-1.4f, 0.12f, -1.8f);
            b.AddOvoid(c4 + new Vector3(0f, 0.40f, 0f), new Vector3(0.70f, 0.50f, 0.65f), ToonColor.DeepMoss, 8, 4);

            // 4. Starfish & Shell Accents on Reef Rim
            b.AddBox(new Vector3(0.2f, 0.15f, 2.2f), new Vector3(0.45f, 0.06f, 0.45f), ToonColor.Terracotta);
            b.AddBox(new Vector3(-2.2f, 0.15f, -0.2f), new Vector3(0.40f, 0.06f, 0.40f), ToonColor.SunlightGold);

            // Translucent Surf Break Water Rings
            b.AddCylinder(new Vector3(0f, 0.02f, 0f), 4.2f, 0.04f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Coral_Atoll.obj"));
        }

        // =========================================================================
        // 6. THEME 2: DISTANT LEAPING DOLPHINS
        // =========================================================================
        public static void BuildDistantLeapingDolphin()
        {
            var b = new ToonObjBuilder("Toon_Distant_Leaping_Dolphin");

            // 1. Lead Dolphin (Mid-air arch leap, peak at Y = 1.6m)
            Vector3 dCenter = new Vector3(0f, 1.4f, 0f);
            // Aerodynamic Torpedo Body
            b.AddOvoid(dCenter, new Vector3(0.42f, 0.38f, 1.35f), ToonColor.IceBlue, 8, 5);
            // Ventral White Belly
            b.AddOvoid(dCenter + new Vector3(0f, -0.10f, 0.05f), new Vector3(0.34f, 0.22f, 1.15f), ToonColor.PlasterWhite, 8, 4);
            // Bottle Snout / Beak
            b.AddCone(dCenter + new Vector3(0f, -0.05f, 1.55f), 0.16f, 0.55f, ToonColor.IceBlue, 6);
            // Curved Dorsal Fin on Back
            b.AddTri(dCenter + new Vector3(0f, 0.32f, -0.05f),
                     dCenter + new Vector3(0f, 0.78f, -0.32f),
                     dCenter + new Vector3(0f, 0.30f, -0.45f), Vector3.right, ToonColor.IceBlue);
            b.AddTri(dCenter + new Vector3(0f, 0.32f, -0.05f),
                     dCenter + new Vector3(0f, 0.30f, -0.45f),
                     dCenter + new Vector3(0f, 0.78f, -0.32f), Vector3.left, ToonColor.IceBlue);
            // Left & Right Pectoral Flippers
            b.AddAngledBar(dCenter + new Vector3(-0.35f, -0.1f, 0.3f), dCenter + new Vector3(-0.85f, -0.3f, 0.05f), 0.06f, 0.18f, ToonColor.IceBlue);
            b.AddAngledBar(dCenter + new Vector3( 0.35f, -0.1f, 0.3f), dCenter + new Vector3( 0.85f, -0.3f, 0.05f), 0.06f, 0.18f, ToonColor.IceBlue);
            // Fluked Horizontal Tail Fin
            Vector3 tailPos = dCenter + new Vector3(0f, -0.05f, -1.45f);
            b.AddBox(tailPos, new Vector3(0.95f, 0.06f, 0.32f), ToonColor.IceBlue);

            // Water Splash / Foam Ripple Rings at Entry & Exit Points
            // Entry point splash (+Z)
            b.AddCylinder(new Vector3(0f, 0.05f, 2.2f), 0.75f, 0.08f, ToonColor.PlasterWhite, 8);
            b.AddCylinder(new Vector3(0f, 0.08f, 2.2f), 0.45f, 0.12f, ToonColor.IceBlue, 6);
            // Exit point splash (-Z)
            b.AddCylinder(new Vector3(0f, 0.05f, -1.8f), 0.85f, 0.08f, ToonColor.PlasterWhite, 8);
            b.AddCylinder(new Vector3(0f, 0.08f, -1.8f), 0.50f, 0.12f, ToonColor.IceBlue, 6);

            // 2. Accompanying Calf Dolphin (Slightly smaller, flanking left)
            Vector3 cCenter = new Vector3(-1.6f, 0.95f, 0.5f);
            b.AddOvoid(cCenter, new Vector3(0.28f, 0.25f, 0.90f), ToonColor.IceBlue, 7, 4);
            b.AddCone(cCenter + new Vector3(0f, -0.03f, 1.05f), 0.11f, 0.35f, ToonColor.IceBlue, 6);
            b.AddBox(cCenter + new Vector3(0f, -0.04f, -0.95f), new Vector3(0.65f, 0.05f, 0.22f), ToonColor.IceBlue);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Leaping_Dolphin.obj"));
        }

        // =========================================================================
        // 7. THEME 2: DISTANT PALM ATTOLL SANDBAR
        // =========================================================================
        public static void BuildDistantPalmAtoll()
        {
            var b = new ToonObjBuilder("Toon_Distant_Palm_Atoll");

            // Smooth Rounded Sandbar Island Mound
            b.AddTaperedCylinder(new Vector3(0f, 0.3f, 0f), 3.2f, 2.6f, 0.6f, ToonColor.SunlightGold, 8);
            b.AddTaperedCylinder(new Vector3(0f, 0.75f, 0f), 2.6f, 2.0f, 0.4f, ToonColor.HoneyWood, 8);

            // Leaning Tropical Coconut Palm Tree
            Vector3 palmBase = new Vector3(-0.4f, 0.95f, 0.2f);
            // Curved Segmented Trunk
            Vector3 p0 = palmBase;
            Vector3 p1 = palmBase + new Vector3(0.25f, 1.2f, 0.15f);
            Vector3 p2 = palmBase + new Vector3(0.70f, 2.4f, 0.35f);
            Vector3 p3 = palmBase + new Vector3(1.25f, 3.5f, 0.60f);

            b.AddAngledBar(p0, p1, 0.22f, 0.20f, ToonColor.WarmWood);
            b.AddAngledBar(p1, p2, 0.20f, 0.18f, ToonColor.WarmWood);
            b.AddAngledBar(p2, p3, 0.18f, 0.16f, ToonColor.HoneyWood);

            // Crown of Arching Palm Fronds
            Vector3 crown = p3 + new Vector3(0f, 0.1f, 0f);
            int frondCount = 6;
            for (int f = 0; f < frondCount; f++)
            {
                float ang = (f / (float)frondCount) * Mathf.PI * 2f;
                b.AddArchingPalmFrond(crown, ang, 2.4f, 0.65f, 0.85f, 0.65f, ToonColor.GrassGreen, ToonColor.SproutGreen, 6);
            }
            // Cluster of Coconuts
            b.AddOvoid(crown + new Vector3(-0.15f, -0.2f,  0.10f), new Vector3(0.18f, 0.22f, 0.18f), ToonColor.DarkTimber, 6, 3);
            b.AddOvoid(crown + new Vector3( 0.15f, -0.2f, -0.10f), new Vector3(0.18f, 0.22f, 0.18f), ToonColor.DarkTimber, 6, 3);

            // Small Sandbar Driftwood & Starfish
            b.AddCylinder(new Vector3(1.2f, 0.98f, -0.8f), 0.12f, 1.4f, ToonColor.WarmWood, 6);
            b.AddBox(new Vector3(1.4f, 1.0f, 0.6f), new Vector3(0.35f, 0.05f, 0.35f), ToonColor.Terracotta);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Palm_Atoll.obj"));
        }

        // =========================================================================
        // 8. THEME 3: DISTANT AUTUMN BEACON ISLET (Level 3)
        // =========================================================================
        public static void BuildDistantAutumnBeacon()
        {
            var b = new ToonObjBuilder("Toon_Distant_Autumn_Beacon");

            // Dark Basalt Stone Islet
            b.AddTaperedCylinder(new Vector3(0f, 0.45f, 0f), 4.6f, 4.0f, 0.9f, ToonColor.DarkIron, 8);
            b.AddTaperedCylinder(new Vector3(0f, 1.15f, 0f), 4.0f, 3.4f, 0.6f, ToonColor.MediumStone, 8);
            b.AddTaperedCylinder(new Vector3(0f, 1.60f, 0f), 3.4f, 3.0f, 0.4f, ToonColor.LightStone, 8);

            // Granite Weathered Lighthouse Tower
            Vector3 ltPos = new Vector3(-1.0f, 1.8f, 0.2f);
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 0.45f, 0f), 1.55f, 1.35f, 0.9f, ToonColor.DarkIron, 8);
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 1.9f, 0f), 1.35f, 1.12f, 2.0f, ToonColor.LightStone, 8);
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 3.3f, 0f), 1.12f, 0.95f, 1.0f, ToonColor.Terracotta, 8); // Scarlet autumn band
            b.AddTaperedCylinder(ltPos + new Vector3(0f, 4.3f, 0f), 0.95f, 0.82f, 1.2f, ToonColor.LightStone, 8);
            // Cast Iron Balcony & Lamp
            b.AddCylinder(ltPos + new Vector3(0f, 5.0f, 0f), 1.15f, 0.15f, ToonColor.DarkIron, 8);
            b.AddCylinder(ltPos + new Vector3(0f, 5.5f, 0f), 0.50f, 0.70f, ToonColor.SunlightGold, 8);
            b.AddCone(ltPos + new Vector3(0f, 6.4f, 0f), 0.92f, 1.25f, ToonColor.DarkIron, 8);

            // Keeper's Autumn Timber Cabin
            Vector3 cabPos = new Vector3(1.6f, 2.2f, -0.4f);
            b.AddBox(cabPos, new Vector3(2.2f, 1.2f, 1.8f), ToonColor.WarmWood);
            b.AddWedgeRoof(cabPos + new Vector3(0f, 0.85f, 0f), new Vector3(2.4f, 0.75f, 2.0f), ToonColor.DarkTimber);
            // Stone Chimney with smoke top
            b.AddBox(cabPos + new Vector3(-0.6f, 1.0f, 0.3f), new Vector3(0.4f, 1.1f, 0.4f), ToonColor.MediumStone);
            // Stack of firewood logs
            b.AddCylinder(cabPos + new Vector3(1.2f, -0.3f, 0.6f), 0.15f, 0.8f, ToonColor.HoneyWood, 6);
            b.AddCylinder(cabPos + new Vector3(1.2f, -0.05f, 0.6f), 0.14f, 0.75f, ToonColor.HoneyWood, 6);

            // Scarlet & Golden Autumn Maple Trees on Islet
            Vector3 treePos1 = new Vector3(0.8f, 1.8f, 1.8f);
            b.AddCylinder(treePos1 + new Vector3(0f, 0.6f, 0f), 0.16f, 1.2f, ToonColor.DarkTimber, 6);
            b.AddOvoid(treePos1 + new Vector3(0f, 1.8f, 0f), new Vector3(1.1f, 1.2f, 1.1f), ToonColor.Terracotta, 8, 4); // Scarlet foliage
            b.AddOvoid(treePos1 + new Vector3(0.2f, 2.3f, 0f), new Vector3(0.85f, 0.9f, 0.85f), ToonColor.SunlightGold, 7, 4);

            Vector3 treePos2 = new Vector3(-2.2f, 1.8f, -1.6f);
            b.AddCylinder(treePos2 + new Vector3(0f, 0.5f, 0f), 0.14f, 1.0f, ToonColor.DarkTimber, 6);
            b.AddOvoid(treePos2 + new Vector3(0f, 1.5f, 0f), new Vector3(0.95f, 1.0f, 0.95f), ToonColor.SunlightGold, 8, 4); // Golden foliage

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Autumn_Beacon.obj"));
        }

        // =========================================================================
        // 9. THEME 3: DISTANT TIMBER LOGGING BARGE
        // =========================================================================
        public static void BuildDistantTimberBarge()
        {
            var b = new ToonObjBuilder("Toon_Distant_Timber_Barge");

            // Heavy Timber Flat-Bottom Barge Hull (Length ~8.0m, Width ~3.6m, Height ~1.1m)
            b.AddTaperedBox(new Vector3(0f, 0.5f, 0f), new Vector3(3.4f, 1.0f, 7.6f), new Vector3(3.8f, 1.0f, 8.2f), ToonColor.DarkTimber);
            // Thick Wooden Gunwale / Bulwark Frame
            b.AddBox(new Vector3(-1.8f, 1.1f, 0f), new Vector3(0.25f, 0.35f, 8.2f), ToonColor.WarmWood);
            b.AddBox(new Vector3( 1.8f, 1.1f, 0f), new Vector3(0.25f, 0.35f, 8.2f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 1.1f,  4.0f), new Vector3(3.8f, 0.35f, 0.25f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 1.1f, -4.0f), new Vector3(3.8f, 0.35f, 0.25f), ToonColor.WarmWood);

            // Giant Stacks of Harvested Forest Timber Logs (Bound with dark iron cables)
            float[] logZ = new float[] { -0.8f, 0.7f, 2.2f };
            for (int s = 0; s < 3; s++)
            {
                float z = logZ[s];
                // Bottom tier logs (3 logs across)
                b.AddCylinder(new Vector3(-0.95f, 1.25f, z), 0.42f, 1.4f, ToonColor.HoneyWood, 8);
                b.AddCylinder(new Vector3( 0.00f, 1.25f, z), 0.44f, 1.4f, ToonColor.WarmWood, 8);
                b.AddCylinder(new Vector3( 0.95f, 1.25f, z), 0.42f, 1.4f, ToonColor.HoneyWood, 8);

                // Middle tier logs (2 logs)
                b.AddCylinder(new Vector3(-0.50f, 1.85f, z), 0.42f, 1.35f, ToonColor.WarmWood, 8);
                b.AddCylinder(new Vector3( 0.50f, 1.85f, z), 0.42f, 1.35f, ToonColor.HoneyWood, 8);

                // Top tier log (1 log)
                b.AddCylinder(new Vector3( 0.00f, 2.45f, z), 0.40f, 1.3f, ToonColor.HoneyWood, 8);

                // Dark Iron Cable Tie-Down Straps
                b.AddBox(new Vector3(0f, 1.85f, z), new Vector3(3.1f, 0.08f, 0.08f), ToonColor.CastIron);
            }

            // Aft Tug / Barge Pilot Cabin (At stern -Z)
            Vector3 cabPos = new Vector3(0f, 1.7f, -2.8f);
            b.AddBox(cabPos, new Vector3(2.2f, 1.4f, 1.8f), ToonColor.WarmWood);
            b.AddBox(cabPos + new Vector3(0f, 0.8f, 0f), new Vector3(2.4f, 0.15f, 2.0f), ToonColor.DarkTimber); // Roof
            // Pilot Windows
            b.AddBox(cabPos + new Vector3(0f, 0.2f, 0.92f), new Vector3(1.8f, 0.45f, 0.06f), ToonColor.SunlightGold);
            // Tall Vertical Smokestack
            b.AddCylinder(cabPos + new Vector3(0.6f, 1.4f, -0.3f), 0.15f, 1.2f, ToonColor.DarkIron, 8);
            b.AddTaperedCylinder(cabPos + new Vector3(0.6f, 2.05f, -0.3f), 0.15f, 0.25f, 0.2f, ToonColor.Terracotta, 8); // Smoke rim

            // Bow Heavy Towing Bollards
            b.AddCylinder(new Vector3(-1.2f, 1.35f, 3.8f), 0.14f, 0.55f, ToonColor.CastIron, 6);
            b.AddCylinder(new Vector3( 1.2f, 1.35f, 3.8f), 0.14f, 0.55f, ToonColor.CastIron, 6);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Timber_Barge.obj"));
        }

        // =========================================================================
        // 10. THEME 3: DISTANT FJORD ROCK SEA STACK
        // =========================================================================
        public static void BuildDistantFjordStack()
        {
            var b = new ToonObjBuilder("Toon_Distant_Fjord_Stack");

            // Towering Stepped Basalt Sea Stack (Height ~7.8m, Base ~3.6m)
            b.AddTaperedBox(new Vector3(0f, 1.2f, 0f), new Vector3(3.8f, 2.4f, 3.4f), new Vector3(3.2f, 2.4f, 2.8f), ToonColor.DarkIron);
            b.AddTaperedBox(new Vector3(0.2f, 3.2f, 0.1f), new Vector3(3.2f, 2.0f, 2.8f), new Vector3(2.4f, 2.0f, 2.1f), ToonColor.MediumStone);
            b.AddTaperedBox(new Vector3(0.1f, 5.0f, 0f), new Vector3(2.4f, 1.8f, 2.1f), new Vector3(1.6f, 1.8f, 1.4f), ToonColor.LightStone);
            b.AddTaperedBox(new Vector3(0f, 6.4f, -0.1f), new Vector3(1.6f, 1.4f, 1.4f), new Vector3(0.9f, 1.4f, 0.8f), ToonColor.DarkIron);

            // Basalt Column Side Buttresses
            b.AddBox(new Vector3(-1.8f, 1.4f,  0.6f), new Vector3(1.2f, 2.8f, 1.4f), ToonColor.MediumStone);
            b.AddBox(new Vector3( 1.7f, 1.8f, -0.5f), new Vector3(1.3f, 3.6f, 1.5f), ToonColor.DarkIron);
            b.AddBox(new Vector3( 0.4f, 0.9f,  1.7f), new Vector3(1.8f, 1.8f, 1.1f), ToonColor.LightStone);

            // Pine Sapling clinging to rock ledge
            Vector3 treeP = new Vector3(-0.4f, 5.8f, 0.4f);
            b.AddCylinder(treeP + new Vector3(0f, 0.4f, 0f), 0.10f, 0.8f, ToonColor.DarkTimber, 6);
            b.AddCone(treeP + new Vector3(0f, 1.1f, 0f), 0.70f, 1.1f, ToonColor.GrassGreen, 6);
            b.AddCone(treeP + new Vector3(0f, 1.8f, 0f), 0.45f, 0.8f, ToonColor.DeepMoss, 6);

            // Water Foam Ring at Base
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 4.4f, 0.08f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Fjord_Stack.obj"));
        }

        // =========================================================================
        // 11. THEME 4: DISTANT GLACIAL ICEBERG MOUNTAIN RANGE (Level 4)
        // =========================================================================
        // 11. THEME 4: DISTANT GLACIAL ICEBERG MOUNTAIN RANGE (Level 4)
        // =========================================================================
        public static void BuildDistantGlacialIcebergRange()
        {
            var b = new ToonObjBuilder("Toon_Distant_Glacial_IcebergRange");

            // Majestic Multi-Peaked Iceberg Massif (Width ~12.0m, Depth ~6.5m, Height ~8.0m)
            // In Winter.png: IceBlue = Deep Glacial Cyan (#13577B), DeepMoss/GrassGreen = Crisp Snow White (#D6E0E7)
            // 1. Central Grand Ice Peak
            Vector3 pCenter = new Vector3(0f, 3.8f, 0.4f);
            b.AddTaperedBox(pCenter, new Vector3(4.8f, 7.6f, 3.8f), new Vector3(1.2f, 7.6f, 0.8f), ToonColor.IceBlue);
            // Sharp Chiseled Summit Spires
            b.AddCone(new Vector3(0.2f, 8.0f, 0.3f), 0.9f, 2.0f, ToonColor.DeepMoss, 6);
            b.AddCone(new Vector3(-0.6f, 7.2f, 0.1f), 0.75f, 1.6f, ToonColor.IceBlue, 6);

            // 2. West Ice Peak (-X)
            Vector3 pWest = new Vector3(-3.8f, 2.9f, -0.3f);
            b.AddTaperedBox(pWest, new Vector3(3.8f, 5.8f, 3.2f), new Vector3(0.9f, 5.8f, 0.7f), ToonColor.IceBlue);
            b.AddCone(pWest + new Vector3(0f, 3.2f, 0f), 0.75f, 1.8f, ToonColor.DeepMoss, 6);

            // 3. East Ice Peak (+X)
            Vector3 pEast = new Vector3(4.0f, 3.2f, -0.2f);
            b.AddTaperedBox(pEast, new Vector3(4.0f, 6.4f, 3.4f), new Vector3(1.0f, 6.4f, 0.8f), ToonColor.DeepMoss);
            b.AddCone(pEast + new Vector3(0f, 3.5f, 0f), 0.85f, 1.9f, ToonColor.IceBlue, 6);

            // 4. Carved Glacial Sea-Arch Tunnel (Through center base)
            // Arch pillars left & right of tunnel
            b.AddBox(new Vector3(-1.8f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 3.6f), ToonColor.IceBlue);
            b.AddBox(new Vector3( 1.8f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 3.6f), ToonColor.IceBlue);
            // Arch lintel spanning above water
            b.AddBox(new Vector3(0f, 1.8f, 0f), new Vector3(2.4f, 0.6f, 3.2f), ToonColor.IceBlue);

            // 5. Powdery Snow Shelves on Mountain Ledges
            b.AddSnowCap(new Vector3(-2.6f, 4.4f, 0.2f), 2.2f, 1.4f, 0.35f, ToonColor.DeepMoss);
            b.AddSnowCap(new Vector3( 2.8f, 4.8f, 0.1f), 2.4f, 1.5f, 0.35f, ToonColor.DeepMoss);

            // 6. Submerged Crystalline Ice Shelf Apron
            b.AddCylinder(new Vector3(0f, -0.25f, 0f), 6.5f, 0.5f, ToonColor.IceBlue, 8);
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 6.8f, 0.08f, ToonColor.DeepMoss, 8); // Water ice rim

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Glacial_IcebergRange.obj"));
        }

        // =========================================================================
        // 12. THEME 4: DISTANT BREACHING HUMPBACK WHALE
        // =========================================================================
        public static void BuildDistantBreachingWhale()
        {
            var b = new ToonObjBuilder("Toon_Distant_Breaching_Whale");

            // Breaching Humpback Whale body (Angled upward at 42-degree pitch)
            // Total length ~6.5m, emerging dramatically out of sea waves
            Vector3 wCenter = new Vector3(0f, 2.0f, 0f);

            // Upper Dorsal Body (Dark Slate Iron)
            b.AddOvoid(wCenter, new Vector3(0.95f, 1.15f, 2.6f), ToonColor.DarkIron, 8, 5);
            // Ventral Throat Grooves (Bright Crisp White pleated belly in Winter palette)
            b.AddOvoid(wCenter + new Vector3(0f, -0.45f, 0.2f), new Vector3(0.82f, 0.70f, 2.2f), ToonColor.DeepMoss, 8, 4);

            // Humpback Head & Nodules
            b.AddOvoid(wCenter + new Vector3(0f, 0.2f, 2.2f), new Vector3(0.70f, 0.65f, 1.1f), ToonColor.DarkIron, 7, 4);

            // Characteristic Curved Humpback Dorsal Fin
            Vector3 dorsalPos = wCenter + new Vector3(0f, 1.10f, -0.6f);
            b.AddTri(dorsalPos + new Vector3(0f, 0f, 0.2f),
                     dorsalPos + new Vector3(0f, 0.65f, -0.2f),
                     dorsalPos + new Vector3(0f, 0f, -0.4f), Vector3.right, ToonColor.DarkIron);
            b.AddTri(dorsalPos + new Vector3(0f, 0f, 0.2f),
                     dorsalPos + new Vector3(0f, 0f, -0.4f),
                     dorsalPos + new Vector3(0f, 0.65f, -0.2f), Vector3.left, ToonColor.DarkIron);

            // Long Wing-Like Pectoral Flippers (Humpback signature!)
            Vector3 flipperL_Start = wCenter + new Vector3(-0.85f, -0.2f, 0.8f);
            Vector3 flipperL_End   = flipperL_Start + new Vector3(-1.85f, 0.6f, -0.6f);
            b.AddAngledBar(flipperL_Start, flipperL_End, 0.10f, 0.45f, ToonColor.DeepMoss);

            Vector3 flipperR_Start = wCenter + new Vector3( 0.85f, -0.2f, 0.8f);
            Vector3 flipperR_End   = flipperR_Start + new Vector3( 1.75f, -0.5f, -0.5f);
            b.AddAngledBar(flipperR_Start, flipperR_End, 0.10f, 0.45f, ToonColor.DeepMoss);

            // Blowhole Mist Spout Plume (Shooting upward)
            Vector3 blowholePos = wCenter + new Vector3(0f, 1.0f, 1.8f);
            b.AddCone(blowholePos + new Vector3(0f, 1.0f, 0f), 0.35f, 1.8f, ToonColor.IceBlue, 6);
            b.AddOvoid(blowholePos + new Vector3(0f, 2.0f, 0f), new Vector3(0.55f, 0.45f, 0.55f), ToonColor.DeepMoss, 7, 4);

            // Surging Circular Splash Foam Rings on Water Surface
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 2.8f, 0.10f, ToonColor.DeepMoss, 8);
            b.AddCylinder(new Vector3(0f, 0.08f, 0f), 1.9f, 0.14f, ToonColor.IceBlue, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Breaching_Whale.obj"));
        }

        // =========================================================================
        // 13. THEME 4: DISTANT PENGUIN PACK ICE FLOE COLONY
        // =========================================================================
        public static void BuildDistantPenguinPackIce()
        {
            var b = new ToonObjBuilder("Toon_Distant_Penguin_PackIce");

            // Broad Floating Ice Floe Sheet (4.6m x 3.8m x 0.4m)
            b.AddTaperedBox(new Vector3(0f, 0.2f, 0f), new Vector3(4.8f, 0.45f, 3.8f), new Vector3(4.4f, 0.45f, 3.4f), ToonColor.IceBlue);
            // Crisp Snow Layer on Top
            b.AddBox(new Vector3(0f, 0.45f, 0f), new Vector3(4.2f, 0.12f, 3.2f), ToonColor.DeepMoss);

            // Stylized Cute Toon Penguins standing in a colony (3 penguins)
            Vector3[] penPos = new Vector3[]
            {
                new Vector3(-0.9f, 0.52f,  0.5f), // Penguin 1 (Looking out to sea)
                new Vector3( 0.6f, 0.52f,  0.7f), // Penguin 2 (Looking slightly right)
                new Vector3( 0.1f, 0.52f, -0.6f)  // Penguin 3 (Rear observer)
            };

            for (int p = 0; p < penPos.Length; p++)
            {
                Vector3 pos = penPos[p];
                // Black Tuxedo Teardrop Body
                b.AddOvoid(pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.24f, 0.42f, 0.22f), ToonColor.DarkIron, 7, 4);
                // White Belly
                b.AddOvoid(pos + new Vector3(0f, 0.40f, 0.12f), new Vector3(0.18f, 0.32f, 0.10f), ToonColor.DeepMoss, 7, 3);
                // Head
                b.AddOvoid(pos + new Vector3(0f, 0.88f, 0f), new Vector3(0.18f, 0.18f, 0.18f), ToonColor.DarkIron, 7, 4);
                // Orange Beak
                b.AddCone(pos + new Vector3(0f, 0.86f, 0.24f), 0.08f, 0.18f, ToonColor.Terracotta, 5);
                // Orange Webbed Feet
                b.AddBox(pos + new Vector3(-0.09f, 0.04f, 0.08f), new Vector3(0.09f, 0.06f, 0.16f), ToonColor.Terracotta);
                b.AddBox(pos + new Vector3( 0.09f, 0.04f, 0.08f), new Vector3(0.09f, 0.06f, 0.16f), ToonColor.Terracotta);
                // Side Flipper Wings
                b.AddAngledBar(pos + new Vector3(-0.22f, 0.60f, 0f), pos + new Vector3(-0.35f, 0.35f, -0.05f), 0.04f, 0.12f, ToonColor.DarkIron);
                b.AddAngledBar(pos + new Vector3( 0.22f, 0.60f, 0f), pos + new Vector3( 0.35f, 0.35f, -0.05f), 0.04f, 0.12f, ToonColor.DarkIron);
            }

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Penguin_PackIce.obj"));
        }

        // =========================================================================
        // 14. THEME 5: DISTANT SUNKEN PHARAOH STATUE (Level 5)
        // =========================================================================
        public static void BuildDistantSunkenPharaoh()
        {
            var b = new ToonObjBuilder("Toon_Distant_Sunken_Pharaoh");

            // Colossal Ancient Sandstone Pharaoh Bust (Emerging at 12-degree tilt)
            // In Desert.png: LightStone = Warm Sandstone (#E6C580), PlasterWhite = White Marble (#F2F1EC), IceBlue = Turquoise (#89C7E6)
            Vector3 pCenter = new Vector3(0f, 2.5f, 0f);

            // Monumental Shoulders / Torso (Sandstone Plinth)
            b.AddTaperedBox(new Vector3(0f, 1.0f, 0f), new Vector3(4.8f, 2.0f, 2.4f), new Vector3(4.2f, 2.0f, 2.1f), ToonColor.LightStone);
            // Weathered Ocean Waterline Barnacles & Cyan Mineral Patina
            b.AddBox(new Vector3(0f, 0.25f, 0f), new Vector3(5.1f, 0.45f, 2.7f), ToonColor.IceBlue);

            // Ancient Egyptian Broad Collar Necklace (Stepped golden & white bands)
            b.AddBox(new Vector3(0f, 2.1f, 0.2f), new Vector3(3.4f, 0.25f, 1.8f), ToonColor.PlasterWhite);
            b.AddBox(new Vector3(0f, 2.35f, 0.2f), new Vector3(2.8f, 0.22f, 1.5f), ToonColor.LightStone);

            // Regal Face & Cheeks
            Vector3 facePos = new Vector3(0f, 3.4f, 0.4f);
            b.AddBox(facePos, new Vector3(1.6f, 1.8f, 1.4f), ToonColor.LightStone);
            // Noble Carved Nose & Brow
            b.AddBox(facePos + new Vector3(0f, 0.1f, 0.75f), new Vector3(0.32f, 0.70f, 0.22f), ToonColor.HoneyWood);
            b.AddBox(facePos + new Vector3(0f, 0.55f, 0.65f), new Vector3(1.1f, 0.22f, 0.18f), ToonColor.HoneyWood);

            // Braided Royal Chin Beard (Osiris Beard)
            b.AddTaperedBox(facePos + new Vector3(0f, -1.2f, 0.5f), new Vector3(0.40f, 1.2f, 0.35f), new Vector3(0.28f, 1.2f, 0.25f), ToonColor.DarkTimber);

            // Iconic Nemes Headcloth (Royal Striped Headdress)
            // Flared Side Wings framing the face
            b.AddBox(facePos + new Vector3(-1.3f, -0.2f, 0.1f), new Vector3(0.85f, 2.2f, 1.2f), ToonColor.LightStone);
            b.AddBox(facePos + new Vector3( 1.3f, -0.2f, 0.1f), new Vector3(0.85f, 2.2f, 1.2f), ToonColor.LightStone);
            // Alternating White Marble stripes on side wings
            b.AddBox(facePos + new Vector3(-1.32f, -0.1f, 0.1f), new Vector3(0.88f, 0.28f, 1.24f), ToonColor.PlasterWhite);
            b.AddBox(facePos + new Vector3( 1.32f, -0.1f, 0.1f), new Vector3(0.88f, 0.28f, 1.24f), ToonColor.PlasterWhite);

            // Crown Dome of Nemes
            b.AddTaperedBox(facePos + new Vector3(0f, 1.4f, -0.1f), new Vector3(2.6f, 1.2f, 1.8f), new Vector3(2.0f, 1.2f, 1.4f), ToonColor.LightStone);
            // Uraeus Cobra Crest on Brow (Crimson silk accent)
            b.AddBox(facePos + new Vector3(0f, 1.1f, 0.82f), new Vector3(0.22f, 0.55f, 0.18f), ToonColor.SunlightGold);

            // Water Ripple Rings Surrounding the Sunken Colossus
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 4.2f, 0.08f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Sunken_Pharaoh.obj"));
        }

        // =========================================================================
        // 15. THEME 5: DISTANT SUNKEN OBELISK & TEMPLE RUINS
        // =========================================================================
        public static void BuildDistantSunkenObelisk()
        {
            var b = new ToonObjBuilder("Toon_Distant_Sunken_Obelisk");

            // Submerged Stepped Sandstone Temple Foundation
            b.AddTaperedBox(new Vector3(0f, 0.35f, 0f), new Vector3(5.2f, 0.7f, 3.8f), new Vector3(4.6f, 0.7f, 3.2f), ToonColor.LightStone);

            // Monolithic Tapered Egyptian Obelisk (Tilted 15 degrees in water)
            Vector3 obeliskBase = new Vector3(-0.6f, 0.7f, 0f);
            b.AddTaperedBox(obeliskBase + new Vector3(0f, 3.2f, 0f), new Vector3(1.3f, 6.4f, 1.3f), new Vector3(0.65f, 6.4f, 0.65f), ToonColor.LightStone);
            // Pyramidion Pointed Gold Cap
            b.AddCone(obeliskBase + new Vector3(0f, 6.9f, 0f), 0.55f, 1.0f, ToonColor.HoneyWood, 4);

            // Two Fluted Sandstone Temple Columns with Lotus Capitals
            Vector3 col1 = new Vector3(1.6f, 0.7f,  0.9f);
            Vector3 col2 = new Vector3(1.6f, 0.7f, -0.9f);

            // Column 1
            b.AddCylinder(col1 + new Vector3(0f, 1.4f, 0f), 0.38f, 2.8f, ToonColor.LightStone, 8);
            b.AddTaperedCylinder(col1 + new Vector3(0f, 3.0f, 0f), 0.38f, 0.65f, 0.5f, ToonColor.PlasterWhite, 8); // White marble Lotus capital
            // Column 2 (Broken top)
            b.AddCylinder(col2 + new Vector3(0f, 0.9f, 0f), 0.38f, 1.8f, ToonColor.LightStone, 8);
            b.AddTaperedCylinder(col2 + new Vector3(0f, 1.9f, 0f), 0.38f, 0.65f, 0.4f, ToonColor.PlasterWhite, 8);

            // Architrave Stone Beam connecting columns
            b.AddBox(new Vector3(1.6f, 3.4f, 0f), new Vector3(0.85f, 0.45f, 2.4f), ToonColor.LightStone);

            // Water Ripple Ring
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 4.5f, 0.08f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Sunken_Obelisk.obj"));
        }

        // =========================================================================
        // 16. THEME 5: DISTANT MIRAGE SAND DUNE ISLAND
        // =========================================================================
        public static void BuildDistantMirageDune()
        {
            var b = new ToonObjBuilder("Toon_Distant_Mirage_Dune");

            // Broad Rolling Sand Dune Island (8.0m x 4.8m x 1.2m)
            b.AddTaperedCylinder(new Vector3(0f, 0.35f, 0f), 4.6f, 3.8f, 0.7f, ToonColor.LightStone, 8);
            b.AddTaperedCylinder(new Vector3(0.3f, 0.85f, 0.2f), 3.4f, 2.4f, 0.5f, ToonColor.HoneyWood, 8);
            // Sinuous Dune Ridge Peak
            b.AddBox(new Vector3(-0.4f, 1.15f, 0.4f), new Vector3(3.2f, 0.35f, 1.2f), ToonColor.LightStone);

            // Two Royal Date Palms on Mirage Island
            Vector3 palm1 = new Vector3(-1.2f, 1.2f, -0.3f);
            b.AddAngledBar(palm1, palm1 + new Vector3(0.2f, 1.4f, 0.1f), 0.22f, 0.18f, ToonColor.WarmWood);
            b.AddAngledBar(palm1 + new Vector3(0.2f, 1.4f, 0.1f), palm1 + new Vector3(0.5f, 2.7f, 0.2f), 0.18f, 0.15f, ToonColor.HoneyWood);
            Vector3 crown1 = palm1 + new Vector3(0.5f, 2.8f, 0.2f);
            for (int f = 0; f < 6; f++)
            {
                float ang = (f / 6f) * Mathf.PI * 2f;
                b.AddArchingPalmFrond(crown1, ang, 2.0f, 0.55f, 0.75f, 0.55f, ToonColor.OliveGreen, ToonColor.SproutGreen, 5);
            }
            // Golden Date Fruit Clusters
            b.AddOvoid(crown1 + new Vector3(0f, -0.2f, 0f), new Vector3(0.22f, 0.28f, 0.22f), ToonColor.HoneyWood, 6, 3);

            // Palm 2 (Smaller sapling)
            Vector3 palm2 = new Vector3(1.4f, 1.0f, 0.5f);
            b.AddAngledBar(palm2, palm2 + new Vector3(-0.15f, 1.1f, 0.1f), 0.16f, 0.13f, ToonColor.WarmWood);
            b.AddAngledBar(palm2 + new Vector3(-0.15f, 1.1f, 0.1f), palm2 + new Vector3(-0.35f, 2.1f, 0.2f), 0.13f, 0.11f, ToonColor.HoneyWood);
            Vector3 crown2 = palm2 + new Vector3(-0.35f, 2.2f, 0.2f);
            for (int f = 0; f < 5; f++)
            {
                float ang = (f / 5f) * Mathf.PI * 2f;
                b.AddArchingPalmFrond(crown2, ang, 1.6f, 0.45f, 0.65f, 0.45f, ToonColor.OliveGreen, ToonColor.SproutGreen, 5);
            }

            // Ancient Sandstone Archway Ruin on the Dune
            Vector3 archPos = new Vector3(0.2f, 1.2f, -1.2f);
            b.AddBox(archPos + new Vector3(-0.8f, 0.8f, 0f), new Vector3(0.35f, 1.6f, 0.35f), ToonColor.LightStone);
            b.AddBox(archPos + new Vector3( 0.8f, 0.8f, 0f), new Vector3(0.35f, 1.6f, 0.35f), ToonColor.LightStone);
            b.AddBox(archPos + new Vector3( 0.0f, 1.7f, 0f), new Vector3(2.1f, 0.32f, 0.45f), ToonColor.PlasterWhite);

            b.SaveObj(Path.Combine(ModelsDir, "Toon_Distant_Mirage_Dune.obj"));
        }

        // =========================================================================
        // PREFAB CREATION
        // =========================================================================
        public static void CreateAllPrefabs()
        {
            Material springMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Spring.mat");
            Material summerMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Summer.mat");
            Material fallMat   = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Fall.mat");
            Material winterMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Winter.mat");
            Material desertMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Desert.mat");

            // Level 1: Beacon Island, Tall Ship, Sea Buoy
            CreatePrefabFromObj("Toon_Distant_Beacon_Island", springMat, false);
            CreatePrefabFromObj("Toon_Distant_Merchant_TallShip", springMat, true, heave: 0.10f, pitch: 1.8f, roll: 2.8f);
            CreatePrefabFromObj("Toon_Distant_Sea_Buoy", springMat, true, heave: 0.12f, pitch: 3.5f, roll: 4.0f, speed: 1.4f);

            // Level 2: Sunken Galleon, Coral Atoll, Leaping Dolphin, Palm Atoll
            CreatePrefabFromObj("Toon_Distant_Sunken_Galleon", summerMat, false);
            CreatePrefabFromObj("Toon_Distant_Coral_Atoll", summerMat, false);
            CreatePrefabFromObj("Toon_Distant_Leaping_Dolphin", summerMat, true, heave: 0.05f, pitch: 1.0f, roll: 1.5f, speed: 1.2f);
            CreatePrefabFromObj("Toon_Distant_Palm_Atoll", summerMat, false);

            // Level 3: Autumn Beacon, Timber Barge, Fjord Stack
            CreatePrefabFromObj("Toon_Distant_Autumn_Beacon", fallMat, false);
            CreatePrefabFromObj("Toon_Distant_Timber_Barge", fallMat, true, heave: 0.07f, pitch: 1.2f, roll: 2.0f);
            CreatePrefabFromObj("Toon_Distant_Fjord_Stack", fallMat, false);

            // Level 4: Iceberg Range, Breaching Whale, Penguin Pack Ice
            CreatePrefabFromObj("Toon_Distant_Glacial_IcebergRange", winterMat, false);
            CreatePrefabFromObj("Toon_Distant_Breaching_Whale", winterMat, true, heave: 0.08f, pitch: 1.5f, roll: 2.2f);
            CreatePrefabFromObj("Toon_Distant_Penguin_PackIce", winterMat, true, heave: 0.09f, pitch: 1.8f, roll: 2.5f);

            // Level 5: Sunken Pharaoh, Sunken Obelisk, Mirage Dune
            CreatePrefabFromObj("Toon_Distant_Sunken_Pharaoh", desertMat, false);
            CreatePrefabFromObj("Toon_Distant_Sunken_Obelisk", desertMat, false);
            CreatePrefabFromObj("Toon_Distant_Mirage_Dune", desertMat, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefabFromObj(string modelName, Material defaultMat, bool addBobbing, float heave = 0.08f, float pitch = 2.0f, float roll = 2.8f, float speed = 1.0f)
        {
            string objPath = $"{ModelsDir}/{modelName}.obj";
            string prefabPath = $"{PrefabsDir}/{modelName}.prefab";

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[ToonDistantOceanGenerator] Model not found: {objPath}");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.name = modelName;

            // Apply Material
            if (defaultMat != null)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = defaultMat;
                }
            }

            // Add Bobbing if applicable
            if (addBobbing)
            {
                var bobbing = instance.AddComponent<DistantOceanBobbing>();
                bobbing.Configure(heave, pitch, roll, speed);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            Debug.Log($"[ToonDistantOceanGenerator] Saved Prefab: {prefabPath}");
        }
    }
}
#endif
