#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace IdleFishing.Editor
{
    public static class ToonGroundTerrainGenerator
    {
        private const string ModelsDir = "Assets/Art/Models/Generated";
        private const string PrefabsDir = "Assets/Prefabs/Terrain";
        private const string AssetBasePath = "Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite";

        [MenuItem("Tools/Idle Fishing/Generate Ground Terrain Assets", false, 12)]
        public static void GenerateAllTerrainAssets()
        {
            if (!Directory.Exists(ModelsDir)) Directory.CreateDirectory(ModelsDir);
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);

            // 0. Universal Island Ground Base (Organic rounded island terrain)
            BuildIslandGroundBase();

            // 1. Level 1: Cobblestone Paths, Plaza, Fences, Garden & Shoreline
            BuildCobblestonePathStraight();
            BuildCobblestoneCurve();
            BuildCobblestonePlaza();
            BuildRusticFence();
            BuildGardenPatch();
            BuildBeachShorelineTemperate();

            // 2. Level 2: Tropical White Beach & Wooden Boardwalk
            BuildBeachShorelineTropical();
            BuildTimberBoardwalk();

            // 3. Level 3: Autumn Slate Roadway, Stone Curb & Fallen Leaf Patches
            BuildSlateRoadway();
            BuildStoneCurb();
            BuildAutumnLeafPatches();

            // 4. Level 4: Arctic Glacial Ice Shelf (5 Aligned Fishing Holes), Snow Trail & Snow Drift
            BuildGlacialIceShelfWithFishingHole();
            BuildSnowTrail();
            BuildSnowDrift();

            // 5. Level 5: Desert Sandstone Pavers, Dune Ridges & Oasis Turf
            BuildDesertSandstonePavers();
            BuildDesertDuneRidge();
            BuildOasisTurfPatch();

            AssetDatabase.Refresh();
            CreateAllTerrainPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("[ToonGroundTerrainGenerator] All ground terrain assets generated and prefabs created!");
        }

        // =========================================================================
        // 0. UNIVERSAL ORGANIC ISLAND GROUND BASE
        // =========================================================================
        private static void BuildIslandGroundBase()
        {
            var b = new ToonObjBuilder("Toon_Ground_Island_Base");

            // Layer 1: Sub-Island Sandy Shoreline Base (Y = -0.18m down to -0.32m)
            // A sweeping rounded island perimeter dipping into the ocean water
            // Width = 27.5m, Depth = 26.5m, rounded corners radius = 4.2m
            Vector3 baseCenter = new Vector3(0f, -0.16f, -1.2f);
            b.AddRoundedRectPrism(baseCenter, 27.5f, 26.5f, 4.2f, 0.32f, 1.25f, ToonColor.TanBamboo, ToonColor.SunlightGold, 8);

            // Layer 2: Main Island Turf Plateau (Y = 0.0f to 0.025m)
            // Core playable landmass, mapped to ToonColor.GrassGreen
            // Adapts across all themes: Spring=Lush green, Summer=Tropical lime, Fall=Golden amber, Winter=Pure crisp snow, Desert=Sunlit sand
            Vector3 turfCenter = new Vector3(0f, 0.012f, -1.2f);
            b.AddRoundedRectPrism(turfCenter, 24.8f, 24.0f, 3.6f, 0.035f, 0.18f, ToonColor.WarmWood, ToonColor.GrassGreen, 8);

            // Layer 3: Flanking Raised Bluffs (Tree grove contour knolls)
            // West Bluff (under trees -10.5m, -8.2m)
            Vector3 westBluff = new Vector3(-8.8f, 0.035f, -1.0f);
            b.AddRoundedRectPrism(westBluff, 5.0f, 9.5f, 2.0f, 0.05f, 0.28f, ToonColor.LightStone, ToonColor.GrassGreen, 6);

            // East Bluff (under trees +9.8m, +8.0m)
            Vector3 eastBluff = new Vector3(8.8f, 0.035f, -1.0f);
            b.AddRoundedRectPrism(eastBluff, 5.0f, 9.5f, 2.0f, 0.05f, 0.28f, ToonColor.LightStone, ToonColor.GrassGreen, 6);

            // Rear Horizon Ridge (framing south perimeter -12.5m)
            Vector3 southRidge = new Vector3(0.0f, 0.035f, -11.5f);
            b.AddRoundedRectPrism(southRidge, 18.0f, 3.8f, 1.6f, 0.05f, 0.25f, ToonColor.MediumStone, ToonColor.GrassGreen, 6);

            // Layer 4: Pier Gateway Threshold (meeting the wooden dock ramp at Z = 11.0m to 11.25m)
            b.AddBox(new Vector3(0f, 0.02f, 11.1f), new Vector3(6.5f, 0.04f, 0.35f), ToonColor.WarmWood);

            b.SaveObj($"{ModelsDir}/Toon_Ground_Island_Base.obj");
        }

        // =========================================================================
        // 1. LEVEL 1: COBBLESTONE PATHS, FENCES, GARDEN & TEMPERATE BEACH
        // =========================================================================
        private static void BuildCobblestonePathStraight()
        {
            var b = new ToonObjBuilder("Toon_Ground_Cobblestone_Straight");
            // Under-layer earthen soil bed (raised 0.01m above ground)
            b.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(2.6f, 0.02f, 4.0f), ToonColor.WarmWood);

            // Flagstones / Cobblestones on top (Y = 0.02m to 0.04m)
            float[] zSteps = new float[] { -1.5f, -0.75f, 0f, 0.75f, 1.5f };
            for (int r = 0; r < zSteps.Length; r++)
            {
                float z = zSteps[r];
                if (r % 2 == 0)
                {
                    // 3 stones across
                    b.AddRoundedRectPrism(new Vector3(-0.85f, 0.025f, z), 0.75f, 0.65f, 0.15f, 0.03f, 0.02f, ToonColor.MediumStone, ToonColor.LightStone);
                    b.AddRoundedRectPrism(new Vector3(0.0f, 0.028f, z + 0.05f), 0.80f, 0.60f, 0.15f, 0.035f, 0.02f, ToonColor.LightStone, ToonColor.MediumStone);
                    b.AddRoundedRectPrism(new Vector3(0.85f, 0.025f, z), 0.75f, 0.65f, 0.15f, 0.03f, 0.02f, ToonColor.MediumStone, ToonColor.LightStone);
                }
                else
                {
                    // 2 large central stones with edge pebbles
                    b.AddRoundedRectPrism(new Vector3(-0.55f, 0.026f, z), 0.95f, 0.65f, 0.15f, 0.032f, 0.02f, ToonColor.LightStone, ToonColor.MediumStone);
                    b.AddRoundedRectPrism(new Vector3(0.55f, 0.027f, z), 0.95f, 0.65f, 0.15f, 0.034f, 0.02f, ToonColor.MediumStone, ToonColor.LightStone);
                    // Edge pebbles
                    b.AddCylinder(new Vector3(-1.15f, 0.022f, z + 0.1f), 0.12f, 0.025f, ToonColor.MediumStone, 6);
                    b.AddCylinder(new Vector3(1.15f, 0.022f, z - 0.1f), 0.14f, 0.025f, ToonColor.LightStone, 6);
                }
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Cobblestone_Straight.obj");
        }

        private static void BuildCobblestoneCurve()
        {
            var b = new ToonObjBuilder("Toon_Ground_Cobblestone_Curve");
            // A 90-degree curved cobblestone quadrant connecting avenue to side cabins
            // Inner radius = 1.2m, Outer radius = 3.8m (Width = 2.6m)
            int segs = 8;
            for (int i = 0; i < segs; i++)
            {
                float a0 = (i / (float)segs) * (Mathf.PI * 0.5f);
                float a1 = ((i + 1) / (float)segs) * (Mathf.PI * 0.5f);

                Vector3 v0 = new Vector3(Mathf.Cos(a0) * 1.2f, 0.01f, Mathf.Sin(a0) * 1.2f);
                Vector3 v1 = new Vector3(Mathf.Cos(a1) * 1.2f, 0.01f, Mathf.Sin(a1) * 1.2f);
                Vector3 v2 = new Vector3(Mathf.Cos(a1) * 3.8f, 0.01f, Mathf.Sin(a1) * 3.8f);
                Vector3 v3 = new Vector3(Mathf.Cos(a0) * 3.8f, 0.01f, Mathf.Sin(a0) * 3.8f);

                // Soil underlay
                b.AddQuad(v0, v1, v2, v3, Vector3.up, ToonColor.WarmWood);

                // Cobblestone stones on top
                float aMid = (a0 + a1) * 0.5f;
                Vector3 pIn = new Vector3(Mathf.Cos(aMid) * 1.85f, 0.026f, Mathf.Sin(aMid) * 1.85f);
                Vector3 pOut = new Vector3(Mathf.Cos(aMid) * 3.10f, 0.026f, Mathf.Sin(aMid) * 3.10f);

                b.AddRoundedRectPrism(pIn, 0.70f, 0.60f, 0.15f, 0.03f, 0.02f, ToonColor.MediumStone, ToonColor.LightStone);
                b.AddRoundedRectPrism(pOut, 0.75f, 0.65f, 0.15f, 0.03f, 0.02f, ToonColor.LightStone, ToonColor.MediumStone);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Cobblestone_Curve.obj");
        }

        private static void BuildCobblestonePlaza()
        {
            var b = new ToonObjBuilder("Toon_Ground_Cobblestone_Plaza");
            // Central plaza round base
            b.AddCylinder(new Vector3(0f, 0.01f, 0f), 3.2f, 0.02f, ToonColor.WarmWood, 16);

            // Center compass/star stone medallion
            b.AddCylinder(new Vector3(0f, 0.025f, 0f), 1.0f, 0.03f, ToonColor.LightStone, 12);
            b.AddCylinder(new Vector3(0f, 0.035f, 0f), 0.45f, 0.02f, ToonColor.SunlightGold, 8);

            // Ring 1 cobblestones
            int count1 = 8;
            for (int i = 0; i < count1; i++)
            {
                float ang = (i / (float)count1) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * 1.6f, 0.026f, Mathf.Sin(ang) * 1.6f);
                b.AddRoundedRectPrism(pos, 0.70f, 0.60f, 0.15f, 0.03f, 0.02f, ToonColor.MediumStone, ToonColor.LightStone);
            }

            // Ring 2 cobblestones
            int count2 = 14;
            for (int i = 0; i < count2; i++)
            {
                float ang = (i / (float)count2) * Mathf.PI * 2f + 0.2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * 2.5f, 0.024f, Mathf.Sin(ang) * 2.5f);
                b.AddRoundedRectPrism(pos, 0.75f, 0.65f, 0.15f, 0.028f, 0.02f, ToonColor.LightStone, ToonColor.MediumStone);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Cobblestone_Plaza.obj");
        }

        private static void BuildRusticFence()
        {
            var b = new ToonObjBuilder("Toon_Ground_Rustic_Fence");
            // Stylized wooden split-rail fence (Length = 3.2m, Height = 0.65m)
            // 3 round timber posts
            float[] postXs = new float[] { -1.45f, 0.0f, 1.45f };
            foreach (var px in postXs)
            {
                b.AddCylinder(new Vector3(px, 0.35f, 0f), 0.075f, 0.72f, ToonColor.WarmWood, 6);
                b.AddBox(new Vector3(px, 0.72f, 0f), new Vector3(0.12f, 0.06f, 0.12f), ToonColor.DarkTimber);
            }
            // Upper and lower horizontal rails
            b.AddBox(new Vector3(0f, 0.52f, 0f), new Vector3(3.2f, 0.07f, 0.07f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(0f, 0.24f, 0f), new Vector3(3.2f, 0.07f, 0.07f), ToonColor.HoneyWood);

            b.SaveObj($"{ModelsDir}/Toon_Ground_Rustic_Fence.obj");
        }

        private static void BuildGardenPatch()
        {
            var b = new ToonObjBuilder("Toon_Ground_Garden_Patch");
            // Raised rectangular garden crop bed (Length = 2.8m, Width = 2.0m, Height = 0.10m)
            // Wooden retaining frame border
            b.AddBox(new Vector3(0f, 0.05f, 0f), new Vector3(2.8f, 0.10f, 2.0f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(2.55f, 0.08f, 1.75f), ToonColor.WarmWood);

            // 3 rows of raised furrow mounds with crops
            float[] rowZs = new float[] { -0.55f, 0.0f, 0.55f };
            for (int r = 0; r < rowZs.Length; r++)
            {
                float z = rowZs[r];
                b.AddBox(new Vector3(0f, 0.11f, z), new Vector3(2.4f, 0.06f, 0.35f), ToonColor.WarmWood);

                for (int c = 0; c < 5; c++)
                {
                    float x = -0.95f + c * 0.48f;
                    if (r % 2 == 0) // Cabbages
                    {
                        b.AddCylinder(new Vector3(x, 0.16f, z), 0.12f, 0.08f, ToonColor.SproutGreen, 6);
                        b.AddCylinder(new Vector3(x, 0.20f, z), 0.08f, 0.08f, ToonColor.GrassGreen, 6);
                    }
                    else // Carrots with foliage
                    {
                        b.AddCylinder(new Vector3(x, 0.14f, z), 0.05f, 0.05f, ToonColor.Terracotta, 6);
                        b.AddBox(new Vector3(x, 0.20f, z), new Vector3(0.12f, 0.08f, 0.12f), ToonColor.SproutGreen);
                    }
                }
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Garden_Patch.obj");
        }

        private static void BuildBeachShorelineTemperate()
        {
            var b = new ToonObjBuilder("Toon_Ground_Beach_Temperate");
            // A sweeping curved golden beach band (Width = 24m, Depth = 3.5m)
            float width = 24.0f;
            float depth = 3.2f;
            int segments = 16;
            float stepX = width / segments;

            for (int i = 0; i < segments; i++)
            {
                float x0 = -width * 0.5f + i * stepX;
                float x1 = x0 + stepX;

                float u0 = (x0 / (width * 0.5f));
                float u1 = (x1 / (width * 0.5f));
                float zCurve0 = (1f - u0 * u0) * 0.8f;
                float zCurve1 = (1f - u1 * u1) * 0.8f;

                Vector3 v0 = new Vector3(x0, 0.03f, -depth * 0.5f + zCurve0);
                Vector3 v1 = new Vector3(x1, 0.03f, -depth * 0.5f + zCurve1);
                Vector3 v2 = new Vector3(x1, -0.12f, depth * 0.5f + zCurve1);
                Vector3 v3 = new Vector3(x0, -0.12f, depth * 0.5f + zCurve0);

                // Golden sand upper beach
                b.AddQuad(v0, v1, v2, v3, Vector3.up, ToonColor.SunlightGold);

                // Shore edge strip (wet sand)
                Vector3 w2 = new Vector3(x1, -0.18f, depth * 0.5f + 0.6f + zCurve1);
                Vector3 w3 = new Vector3(x0, -0.18f, depth * 0.5f + 0.6f + zCurve0);
                b.AddQuad(v3, v2, w2, w3, Vector3.up, ToonColor.TanBamboo);
            }

            // Scattered smooth coastal pebbles
            for (int p = 0; p < 12; p++)
            {
                float px = -10.0f + p * 1.8f + (p % 3) * 0.3f;
                float pz = (p % 2 == 0) ? 0.4f : -0.3f;
                b.AddCylinder(new Vector3(px, 0.02f, pz), 0.18f, 0.05f, ToonColor.LightStone, 6);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Beach_Temperate.obj");
        }

        // =========================================================================
        // 2. LEVEL 2: TROPICAL WHITE BEACH & WOODEN BOARDWALK
        // =========================================================================
        private static void BuildBeachShorelineTropical()
        {
            var b = new ToonObjBuilder("Toon_Ground_Beach_Tropical");
            float width = 24.0f;
            float depth = 3.6f;
            int segments = 16;
            float stepX = width / segments;

            for (int i = 0; i < segments; i++)
            {
                float x0 = -width * 0.5f + i * stepX;
                float x1 = x0 + stepX;

                float u0 = (x0 / (width * 0.5f));
                float u1 = (x1 / (width * 0.5f));
                float zCurve0 = (1f - u0 * u0) * 1.1f;
                float zCurve1 = (1f - u1 * u1) * 1.1f;

                Vector3 v0 = new Vector3(x0, 0.04f, -depth * 0.5f + zCurve0);
                Vector3 v1 = new Vector3(x1, 0.04f, -depth * 0.5f + zCurve1);
                Vector3 v2 = new Vector3(x1, -0.10f, depth * 0.5f + zCurve1);
                Vector3 v3 = new Vector3(x0, -0.10f, depth * 0.5f + zCurve0);

                // Pristine white coral sand
                b.AddQuad(v0, v1, v2, v3, Vector3.up, ToonColor.PlasterWhite);

                // Wet sand tideline
                Vector3 w2 = new Vector3(x1, -0.18f, depth * 0.5f + 0.8f + zCurve1);
                Vector3 w3 = new Vector3(x0, -0.18f, depth * 0.5f + 0.8f + zCurve0);
                b.AddQuad(v3, v2, w2, w3, Vector3.up, ToonColor.SunlightGold);
            }

            // Clustered tropical shells & coral rubble
            for (int s = 0; s < 10; s++)
            {
                float sx = -9.0f + s * 2.0f;
                float sz = (s % 2 == 0) ? 0.35f : -0.25f;
                ToonColor col = (s % 2 == 0) ? ToonColor.PlasterWhite : ToonColor.SunlightGold;
                b.AddCylinder(new Vector3(sx, 0.02f, sz), 0.14f, 0.04f, col, 6);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Beach_Tropical.obj");
        }

        private static void BuildTimberBoardwalk()
        {
            var b = new ToonObjBuilder("Toon_Ground_Timber_Boardwalk");
            // Weathered timber plank walkway (Length = 5.0m, Width = 2.4m)
            b.AddBox(new Vector3(-0.9f, 0.02f, 0f), new Vector3(0.18f, 0.04f, 5.0f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.9f, 0.02f, 0f), new Vector3(0.18f, 0.04f, 5.0f), ToonColor.DarkTimber);

            int plankCount = 14;
            float plankStep = 5.0f / plankCount;
            for (int p = 0; p < plankCount; p++)
            {
                float z = -2.5f + (p + 0.5f) * plankStep;
                float pW = 2.30f + (p % 2 == 0 ? 0.06f : -0.06f);
                ToonColor plankCol = (p % 2 == 0) ? ToonColor.WarmWood : ToonColor.HoneyWood;
                b.AddRoundedRectPrism(new Vector3(0f, 0.045f, z), pW, plankStep * 0.88f, 0.04f, 0.035f, 0.015f, ToonColor.DarkTimber, plankCol);

                b.AddCylinder(new Vector3(-0.9f, 0.065f, z), 0.035f, 0.01f, ToonColor.DarkIron, 6);
                b.AddCylinder(new Vector3( 0.9f, 0.065f, z), 0.035f, 0.01f, ToonColor.DarkIron, 6);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Timber_Boardwalk.obj");
        }

        // =========================================================================
        // 3. LEVEL 3: AUTUMN SLATE ROADWAYS, STONE CURB & FALLEN LEAVES
        // =========================================================================
        private static void BuildSlateRoadway()
        {
            var b = new ToonObjBuilder("Toon_Ground_Slate_Roadway");
            // Dark rustic flagstone road (Length = 4.5m, Width = 2.6m)
            b.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(2.6f, 0.02f, 4.5f), ToonColor.DarkTimber);

            float[] zSteps = new float[] { -1.8f, -0.9f, 0.0f, 0.9f, 1.8f };
            for (int r = 0; r < zSteps.Length; r++)
            {
                float z = zSteps[r];
                if (r % 2 == 0)
                {
                    b.AddRoundedRectPrism(new Vector3(-0.75f, 0.028f, z), 0.90f, 0.75f, 0.16f, 0.035f, 0.02f, ToonColor.CastIron, ToonColor.MediumStone);
                    b.AddRoundedRectPrism(new Vector3( 0.75f, 0.028f, z), 0.90f, 0.75f, 0.16f, 0.035f, 0.02f, ToonColor.CastIron, ToonColor.DarkIron);
                }
                else
                {
                    b.AddRoundedRectPrism(new Vector3( 0.0f, 0.030f, z), 1.30f, 0.75f, 0.18f, 0.038f, 0.02f, ToonColor.DarkIron, ToonColor.MediumStone);
                    b.AddRoundedRectPrism(new Vector3(-1.05f, 0.026f, z), 0.50f, 0.65f, 0.12f, 0.030f, 0.02f, ToonColor.CastIron, ToonColor.DarkIron);
                    b.AddRoundedRectPrism(new Vector3( 1.05f, 0.026f, z), 0.50f, 0.65f, 0.12f, 0.030f, 0.02f, ToonColor.CastIron, ToonColor.DarkIron);
                }
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Slate_Roadway.obj");
        }

        private static void BuildStoneCurb()
        {
            var b = new ToonObjBuilder("Toon_Ground_Stone_Curb");
            // Curved stone retaining curb (Length = 3.6m, Height = 0.18m)
            int blocks = 6;
            float stepX = 3.6f / blocks;
            for (int i = 0; i < blocks; i++)
            {
                float x = -1.8f + (i + 0.5f) * stepX;
                ToonColor col = (i % 2 == 0) ? ToonColor.MediumStone : ToonColor.LightStone;
                b.AddRoundedRectPrism(new Vector3(x, 0.09f, 0f), stepX * 0.92f, 0.35f, 0.08f, 0.18f, 0.02f, ToonColor.DarkIron, col);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Stone_Curb.obj");
        }

        private static void BuildAutumnLeafPatches()
        {
            var b = new ToonObjBuilder("Toon_Ground_Autumn_LeafPatch");
            int leafCount = 18;
            for (int i = 0; i < leafCount; i++)
            {
                float r = Mathf.Sqrt((i + 0.5f) / leafCount) * 1.1f;
                float ang = i * 2.39996f;
                float lx = Mathf.Cos(ang) * r;
                float lz = Mathf.Sin(ang) * r;

                ToonColor leafCol = (i % 3 == 0) ? ToonColor.Terracotta :
                                    (i % 3 == 1) ? ToonColor.SunlightGold : ToonColor.TanBamboo;

                Vector3 center = new Vector3(lx, 0.022f + (i * 0.001f), lz);
                b.AddBox(center, new Vector3(0.24f, 0.015f, 0.24f), leafCol);
                b.AddBox(center + new Vector3(0.06f, 0.005f, 0.06f), new Vector3(0.14f, 0.012f, 0.14f), leafCol);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Autumn_LeafPatch.obj");
        }

        // =========================================================================
        // 4. LEVEL 4: ARCTIC GLACIAL ICE SHELF (5 HOLES), SNOW TRAIL & SNOW DRIFT
        // =========================================================================
        private static void BuildGlacialIceShelfWithFishingHole()
        {
            var b = new ToonObjBuilder("Toon_Ground_Glacial_IceShelf");
            // Broad crystalline ice shelf extending out into the water across all 5 fishing slots
            // Width = 14.8m (X = -7.4m to +7.4m), Length = 4.8m (Z = -2.4m to +2.4m)
            // Placed at world (0.0f, -0.02f, 13.8f) in ToonEnvironmentBuilder.cs
            Vector3 shelfCenter = new Vector3(0f, -0.06f, 0f);
            b.AddRoundedRectPrism(shelfCenter, 14.8f, 4.8f, 0.8f, 0.28f, 0.12f, ToonColor.PlasterWhite, ToonColor.IceBlue, 8);

            // Subtle stepped ice lip along outer ocean edge
            b.AddRoundedRectPrism(shelfCenter + new Vector3(0f, -0.08f, 1.8f), 13.8f, 1.6f, 0.6f, 0.18f, 0.08f, ToonColor.IceBlue, ToonColor.IceBlue, 6);

            // 5 DISTINCT ICE-FISHING HOLES accurately aligned with the 5 fishing rods/lines:
            // Hole 4 (Slot 4): X = -3.50f
            // Hole 2 (Slot 2): X = -1.20f
            // Hole 1 (Slot 1): X = +1.10f
            // Hole 3 (Slot 3): X = +3.40f
            // Hole 5 (Slot 5): X = +5.70f
            float[] holeXs = new float[] { -3.50f, -1.20f, 1.10f, 3.40f, 5.70f };
            float holeZ = 0.0f; // local Z = 0.0f -> world Z = 13.8f
            float holeRadius = 0.65f;

            for (int i = 0; i < holeXs.Length; i++)
            {
                float hx = holeXs[i];
                Vector3 holePos = new Vector3(hx, 0.08f, holeZ);

                // 1. Carved open circular cylinder into the ice
                b.AddCylinder(holePos + new Vector3(0f, -0.16f, 0f), holeRadius, 0.32f, ToonColor.DarkIron, 12);
                // Deep blue-black open arctic water disc inside hole
                b.AddCylinder(holePos + new Vector3(0f, -0.22f, 0f), holeRadius * 0.94f, 0.04f, ToonColor.CastIron, 12);

                // 2. Raised frosted snow rim around the ice hole
                int rimSegs = 12;
                for (int r = 0; r < rimSegs; r++)
                {
                    float ang = (r / (float)rimSegs) * Mathf.PI * 2f;
                    Vector3 rimP = holePos + new Vector3(Mathf.Cos(ang) * (holeRadius + 0.12f), 0.035f, Mathf.Sin(ang) * (holeRadius + 0.12f));
                    b.AddBox(rimP, new Vector3(0.22f, 0.10f, 0.22f), ToonColor.PlasterWhite);
                }

                // 3. Radial ice fracture cracks radiating from each hole across turquoise ice
                float[] crackAngles = (i % 2 == 0)
                    ? new float[] { 20f, 80f, 140f, 210f, 270f, 330f }
                    : new float[] { 45f, 110f, 165f, 235f, 290f, 350f };

                foreach (var deg in crackAngles)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                    Vector3 mid = holePos + dir * 1.35f;
                    b.AddBox(new Vector3(mid.x, 0.09f, mid.z), new Vector3(Mathf.Abs(dir.x) * 1.5f + 0.06f, 0.018f, Mathf.Abs(dir.z) * 1.5f + 0.06f), ToonColor.PlasterWhite);
                }

                // 4. Distinct ice-fishing gear setup beside each hole:
                if (i == 0) // Hole 4: Wooden stool and iron tackle box
                {
                    Vector3 stoolPos = holePos + new Vector3(-0.95f, 0.0f, -0.4f);
                    b.AddCylinder(stoolPos + new Vector3(0f, 0.20f, 0f), 0.25f, 0.06f, ToonColor.WarmWood, 8);
                    b.AddCylinder(stoolPos + new Vector3(-0.15f, 0.10f, -0.15f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    b.AddCylinder(stoolPos + new Vector3( 0.15f, 0.10f, -0.15f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    b.AddCylinder(stoolPos + new Vector3( 0.00f, 0.10f,  0.18f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    // Tackle box
                    b.AddBox(holePos + new Vector3(-0.9f, 0.08f, 0.45f), new Vector3(0.38f, 0.16f, 0.24f), ToonColor.DarkIron);
                    b.AddBox(holePos + new Vector3(-0.9f, 0.17f, 0.45f), new Vector3(0.12f, 0.04f, 0.04f), ToonColor.SunlightGold);
                }
                else if (i == 1) // Hole 2: Ice bucket with caught arctic fish
                {
                    Vector3 bucketPos = holePos + new Vector3(0.95f, 0.12f, 0.35f);
                    b.AddCylinder(bucketPos, 0.22f, 0.28f, ToonColor.DarkIron, 8);
                    b.AddBox(bucketPos + new Vector3(0.04f, 0.18f, 0f), new Vector3(0.10f, 0.18f, 0.06f), ToonColor.IceBlue);
                    b.AddBox(bucketPos + new Vector3(-0.06f, 0.16f, 0.05f), new Vector3(0.08f, 0.15f, 0.06f), ToonColor.PlasterWhite);
                }
                else if (i == 2) // Hole 1 (Center): Wooden tripod rig with hanging iron cauldron
                {
                    Vector3 tripodBase = holePos + new Vector3(-1.0f, 0f, 0.7f);
                    b.AddCylinder(tripodBase + new Vector3(-0.35f, 0.6f, -0.35f), 0.04f, 1.3f, ToonColor.WarmWood, 5);
                    b.AddCylinder(tripodBase + new Vector3( 0.35f, 0.6f, -0.35f), 0.04f, 1.3f, ToonColor.WarmWood, 5);
                    b.AddCylinder(tripodBase + new Vector3( 0.00f, 0.6f,  0.45f), 0.04f, 1.3f, ToonColor.WarmWood, 5);
                    b.AddCylinder(tripodBase + new Vector3(0f, 0.7f, 0f), 0.18f, 0.22f, ToonColor.CastIron, 8);
                }
                else if (i == 3) // Hole 3: Wooden fish crate and lantern
                {
                    Vector3 cratePos = holePos + new Vector3(0.95f, 0.10f, -0.35f);
                    b.AddBox(cratePos, new Vector3(0.42f, 0.22f, 0.32f), ToonColor.HoneyWood);
                    Vector3 lanternPos = holePos + new Vector3(0.90f, 0.08f, 0.40f);
                    b.AddCylinder(lanternPos, 0.08f, 0.18f, ToonColor.SunlightGold, 6);
                    b.AddBox(lanternPos + new Vector3(0f, 0.12f, 0f), new Vector3(0.10f, 0.04f, 0.10f), ToonColor.CastIron);
                }
                else if (i == 4) // Hole 5: Stool and bait tin
                {
                    Vector3 stoolPos = holePos + new Vector3(0.95f, 0.0f, -0.4f);
                    b.AddCylinder(stoolPos + new Vector3(0f, 0.20f, 0f), 0.25f, 0.06f, ToonColor.WarmWood, 8);
                    b.AddCylinder(stoolPos + new Vector3(-0.15f, 0.10f, -0.15f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    b.AddCylinder(stoolPos + new Vector3( 0.15f, 0.10f, -0.15f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    b.AddCylinder(stoolPos + new Vector3( 0.00f, 0.10f,  0.18f), 0.035f, 0.20f, ToonColor.HoneyWood, 4);
                    b.AddCylinder(holePos + new Vector3(0.85f, 0.05f, 0.35f), 0.12f, 0.10f, ToonColor.LightStone, 8);
                }
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Glacial_IceShelf.obj");
        }

        private static void BuildSnowTrail()
        {
            var b = new ToonObjBuilder("Toon_Ground_Snow_Trail");
            b.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(2.4f, 0.02f, 5.0f), ToonColor.MediumStone);

            int bermSegs = 10;
            float stepZ = 5.0f / bermSegs;
            for (int i = 0; i < bermSegs; i++)
            {
                float z = -2.5f + (i + 0.5f) * stepZ;
                float hL = 0.08f + (i % 2) * 0.04f;
                float hR = 0.08f + ((i + 1) % 2) * 0.04f;
                b.AddRoundedRectPrism(new Vector3(-1.3f, hL * 0.5f, z), 0.6f, stepZ * 0.95f, 0.15f, hL, 0.05f, ToonColor.PlasterWhite, ToonColor.PlasterWhite);
                b.AddRoundedRectPrism(new Vector3( 1.3f, hR * 0.5f, z), 0.6f, stepZ * 0.95f, 0.15f, hR, 0.05f, ToonColor.PlasterWhite, ToonColor.PlasterWhite);

                if (i % 2 == 0)
                {
                    b.AddRoundedRectPrism(new Vector3(0f, 0.022f, z), 0.85f, 0.6f, 0.12f, 0.025f, 0.02f, ToonColor.LightStone, ToonColor.IceBlue);
                }
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Snow_Trail.obj");
        }

        private static void BuildSnowDrift()
        {
            var b = new ToonObjBuilder("Toon_Ground_Snow_Drift");
            // Soft sculpted snow bank / drift (Length = 4.0m, Width = 1.2m, Height = 0.35m)
            int segs = 8;
            float stepX = 4.0f / segs;
            for (int i = 0; i < segs; i++)
            {
                float x0 = -2.0f + i * stepX;
                float x1 = x0 + stepX;
                float u0 = (x0 / 2.0f);
                float u1 = (x1 / 2.0f);
                float h0 = (1f - u0 * u0) * 0.35f;
                float h1 = (1f - u1 * u1) * 0.35f;

                Vector3 v0 = new Vector3(x0, 0.01f, -0.6f);
                Vector3 v1 = new Vector3(x1, 0.01f, -0.6f);
                Vector3 c1 = new Vector3(x1, h1,    0.0f);
                Vector3 c0 = new Vector3(x0, h0,    0.0f);
                Vector3 v3 = new Vector3(x1, 0.01f,  0.6f);
                Vector3 v2 = new Vector3(x0, 0.01f,  0.6f);

                b.AddQuad(v0, v1, c1, c0, Vector3.up, ToonColor.PlasterWhite);
                b.AddQuad(c0, c1, v3, v2, Vector3.up, ToonColor.PlasterWhite);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Snow_Drift.obj");
        }

        // =========================================================================
        // 5. LEVEL 5: DESERT SANDSTONE PAVERS, DUNE RIDGES & OASIS TURF
        // =========================================================================
        private static void BuildDesertSandstonePavers()
        {
            var b = new ToonObjBuilder("Toon_Ground_Desert_SandstonePaver");
            b.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(2.6f, 0.02f, 4.5f), ToonColor.SunlightGold);

            float[] zSteps = new float[] { -1.8f, -0.9f, 0.0f, 0.9f, 1.8f };
            for (int r = 0; r < zSteps.Length; r++)
            {
                float z = zSteps[r];
                b.AddRoundedRectPrism(new Vector3(0f, 0.028f, z), 1.0f, 0.75f, 0.15f, 0.03f, 0.02f, ToonColor.TanBamboo, ToonColor.LightStone);
                b.AddBox(new Vector3(0f, 0.038f, z), new Vector3(0.35f, 0.015f, 0.35f), ToonColor.Terracotta);

                b.AddRoundedRectPrism(new Vector3(-0.95f, 0.026f, z), 0.65f, 0.75f, 0.12f, 0.028f, 0.02f, ToonColor.LightStone, ToonColor.SunlightGold);
                b.AddRoundedRectPrism(new Vector3( 0.95f, 0.026f, z), 0.65f, 0.75f, 0.12f, 0.028f, 0.02f, ToonColor.LightStone, ToonColor.SunlightGold);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Desert_SandstonePaver.obj");
        }

        private static void BuildDesertDuneRidge()
        {
            var b = new ToonObjBuilder("Toon_Ground_Desert_DuneRidge");
            int segments = 12;
            float stepZ = 8.0f / segments;
            for (int i = 0; i < segments; i++)
            {
                float z0 = -4.0f + i * stepZ;
                float z1 = z0 + stepZ;
                float u0 = (z0 / 4.0f);
                float u1 = (z1 / 4.0f);
                float h0 = (1f - u0 * u0) * 0.32f;
                float h1 = (1f - u1 * u1) * 0.32f;

                Vector3 v0 = new Vector3(-1.2f, 0.01f, z0);
                Vector3 v1 = new Vector3(-1.2f, 0.01f, z1);
                Vector3 c1 = new Vector3( 0.2f, h1,   z1);
                Vector3 c0 = new Vector3( 0.2f, h0,   z0);
                Vector3 v3 = new Vector3( 1.2f, 0.01f, z1);
                Vector3 v2 = new Vector3( 1.2f, 0.01f, z0);

                Vector3 nWind = Vector3.Cross(c0 - v0, v1 - v0).normalized;
                b.AddQuad(v0, v1, c1, c0, nWind, ToonColor.SunlightGold);

                Vector3 nLee = Vector3.Cross(v3 - c1, c0 - c1).normalized;
                b.AddQuad(c0, c1, v3, v2, nLee, ToonColor.TanBamboo);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Desert_DuneRidge.obj");
        }

        private static void BuildOasisTurfPatch()
        {
            var b = new ToonObjBuilder("Toon_Ground_Oasis_Turf");
            b.AddCylinder(new Vector3(0f, 0.02f, 0f), 3.2f, 0.03f, ToonColor.GrassGreen, 16);
            b.AddCylinder(new Vector3(0f, 0.03f, 0f), 2.2f, 0.025f, ToonColor.DeepMoss, 14);

            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(ang) * 2.6f, 0.038f, Mathf.Sin(ang) * 2.6f);
                b.AddRoundedRectPrism(p, 0.65f, 0.55f, 0.15f, 0.03f, 0.02f, ToonColor.LightStone, ToonColor.SunlightGold);
            }

            b.SaveObj($"{ModelsDir}/Toon_Ground_Oasis_Turf.obj");
        }

        // =========================================================================
        // PREFAB CREATION & MATERIAL ASSIGNMENT
        // =========================================================================
        public static void CreateAllTerrainPrefabs()
        {
            Material springMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Spring.mat");
            Material summerMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Summer.mat");
            Material fallMat   = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Fall.mat");
            Material winterMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Winter.mat");
            Material desertMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Desert.mat");

            // Universal Island Base
            CreatePrefabFromObj("Toon_Ground_Island_Base", springMat);

            // Level 1
            CreatePrefabFromObj("Toon_Ground_Cobblestone_Straight", springMat);
            CreatePrefabFromObj("Toon_Ground_Cobblestone_Curve", springMat);
            CreatePrefabFromObj("Toon_Ground_Cobblestone_Plaza", springMat);
            CreatePrefabFromObj("Toon_Ground_Rustic_Fence", springMat);
            CreatePrefabFromObj("Toon_Ground_Garden_Patch", springMat);
            CreatePrefabFromObj("Toon_Ground_Beach_Temperate", springMat);

            // Level 2
            CreatePrefabFromObj("Toon_Ground_Beach_Tropical", summerMat);
            CreatePrefabFromObj("Toon_Ground_Timber_Boardwalk", summerMat);

            // Level 3
            CreatePrefabFromObj("Toon_Ground_Slate_Roadway", fallMat);
            CreatePrefabFromObj("Toon_Ground_Stone_Curb", fallMat);
            CreatePrefabFromObj("Toon_Ground_Autumn_LeafPatch", fallMat);

            // Level 4
            CreatePrefabFromObj("Toon_Ground_Glacial_IceShelf", winterMat);
            CreatePrefabFromObj("Toon_Ground_Snow_Trail", winterMat);
            CreatePrefabFromObj("Toon_Ground_Snow_Drift", winterMat);

            // Level 5
            CreatePrefabFromObj("Toon_Ground_Desert_SandstonePaver", desertMat);
            CreatePrefabFromObj("Toon_Ground_Desert_DuneRidge", desertMat);
            CreatePrefabFromObj("Toon_Ground_Oasis_Turf", desertMat);
        }

        private static void CreatePrefabFromObj(string modelName, Material defaultMat)
        {
            string objPath = $"{ModelsDir}/{modelName}.obj";
            var modelObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (modelObj == null)
            {
                Debug.LogWarning($"[ToonGroundTerrainGenerator] Model not found: {objPath}");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(modelObj);
            instance.name = modelName;

            if (defaultMat != null)
            {
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = defaultMat;
                }
            }

            string prefabPath = $"{PrefabsDir}/{modelName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            Debug.Log($"[ToonGroundTerrainGenerator] Created Prefab: {prefabPath}");
        }
    }
}
#endif
