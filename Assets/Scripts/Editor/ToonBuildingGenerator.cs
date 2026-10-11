using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace IdleFishing.Editor
{
    public enum ToonColor
    {
        // Row 3 (Greens)
        GrassGreen,    // (0, 3) UV: 0.125, 0.875
        DeepMoss,      // (1, 3) UV: 0.375, 0.875
        OliveGreen,    // (2, 3) UV: 0.625, 0.875
        SproutGreen,   // (3, 3) UV: 0.875, 0.875

        // Row 2 (Whites, Blues, Grays)
        IceBlue,       // (0, 2) UV: 0.125, 0.625
        PlasterWhite,  // (1, 2) UV: 0.375, 0.625
        LightStone,    // (2, 2) UV: 0.625, 0.625
        MediumStone,   // (3, 2) UV: 0.875, 0.625

        // Row 1 (Woods, Tans)
        WarmWood,      // (0, 1) UV: 0.125, 0.375
        HoneyWood,     // (1, 1) UV: 0.375, 0.375
        DarkTimber,    // (2, 1) UV: 0.625, 0.375
        TanBamboo,     // (3, 1) UV: 0.875, 0.375

        // Row 0 (Accents, Metals, Reds)
        Terracotta,    // (0, 0) UV: 0.125, 0.125
        SunlightGold,  // (1, 0) UV: 0.375, 0.125
        DarkIron,      // (2, 0) UV: 0.625, 0.125
        CastIron       // (3, 0) UV: 0.875, 0.125
    }

    public class ToonObjBuilder
    {
        private readonly string modelName;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<int[]> faces = new List<int[]>(); // 1-based [v, vt, vn]

        public ToonObjBuilder(string name)
        {
            modelName = name;
        }

        public static Vector2 GetUV(ToonColor color)
        {
            int col = 0;
            int row = 0;
            switch (color)
            {
                case ToonColor.GrassGreen:   col = 0; row = 3; break;
                case ToonColor.DeepMoss:     col = 1; row = 3; break;
                case ToonColor.OliveGreen:   col = 2; row = 3; break;
                case ToonColor.SproutGreen:  col = 3; row = 3; break;

                case ToonColor.IceBlue:      col = 0; row = 2; break;
                case ToonColor.PlasterWhite: col = 1; row = 2; break;
                case ToonColor.LightStone:   col = 2; row = 2; break;
                case ToonColor.MediumStone:  col = 3; row = 2; break;

                case ToonColor.WarmWood:     col = 0; row = 1; break;
                case ToonColor.HoneyWood:    col = 1; row = 1; break;
                case ToonColor.DarkTimber:   col = 2; row = 1; break;
                case ToonColor.TanBamboo:    col = 3; row = 1; break;

                case ToonColor.Terracotta:   col = 0; row = 0; break;
                case ToonColor.SunlightGold: col = 1; row = 0; break;
                case ToonColor.DarkIron:     col = 2; row = 0; break;
                case ToonColor.CastIron:     col = 3; row = 0; break;
            }
            return new Vector2((col + 0.5f) / 4.0f, (row + 0.5f) / 4.0f);
        }

        private int AddVertex(Vector3 pos, Vector2 uv, Vector3 normal)
        {
            vertices.Add(pos);
            uvvs.Add(uv);
            normals.Add(normal);
            return vertices.Count; // 1-based index
        }

        private readonly List<Vector2> uvvs = new List<Vector2>();

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 normal, ToonColor color)
        {
            Vector2 uv = GetUV(color);
            int i1 = AddVertex(v1, uv, normal);
            int i2 = AddVertex(v2, uv, normal);
            int i3 = AddVertex(v3, uv, normal);
            int i4 = AddVertex(v4, uv, normal);
            faces.Add(new int[] { i1, i2, i3, i4 });
        }

        private void AddTri(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, ToonColor color)
        {
            Vector2 uv = GetUV(color);
            int i1 = AddVertex(v1, uv, normal);
            int i2 = AddVertex(v2, uv, normal);
            int i3 = AddVertex(v3, uv, normal);
            faces.Add(new int[] { i1, i2, i3 });
        }

        public void AddBox(Vector3 center, Vector3 size, ToonColor color)
        {
            Vector3 h = size * 0.5f;
            Vector3 c = center;

            // Vertices
            Vector3 p0 = c + new Vector3(-h.x, -h.y, -h.z);
            Vector3 p1 = c + new Vector3(h.x, -h.y, -h.z);
            Vector3 p2 = c + new Vector3(h.x, h.y, -h.z);
            Vector3 p3 = c + new Vector3(-h.x, h.y, -h.z);
            Vector3 p4 = c + new Vector3(-h.x, -h.y, h.z);
            Vector3 p5 = c + new Vector3(h.x, -h.y, h.z);
            Vector3 p6 = c + new Vector3(h.x, h.y, h.z);
            Vector3 p7 = c + new Vector3(-h.x, h.y, h.z);

            // Front
            AddQuad(p0, p3, p2, p1, Vector3.back, color);
            // Back
            AddQuad(p5, p6, p7, p4, Vector3.forward, color);
            // Left
            AddQuad(p4, p7, p3, p0, Vector3.left, color);
            // Right
            AddQuad(p1, p2, p6, p5, Vector3.right, color);
            // Top
            AddQuad(p3, p7, p6, p2, Vector3.up, color);
            // Bottom
            AddQuad(p4, p0, p1, p5, Vector3.down, color);
        }

        public void AddTaperedBox(Vector3 center, Vector3 botSize, Vector3 topSize, ToonColor color)
        {
            Vector3 hb = botSize * 0.5f;
            Vector3 ht = topSize * 0.5f;
            float yb = center.y - (botSize.y * 0.5f);
            float yt = center.y + (topSize.y * 0.5f);

            Vector3 p0 = new Vector3(center.x - hb.x, yb, center.z - hb.z);
            Vector3 p1 = new Vector3(center.x + hb.x, yb, center.z - hb.z);
            Vector3 p2 = new Vector3(center.x + ht.x, yt, center.z - ht.z);
            Vector3 p3 = new Vector3(center.x - ht.x, yt, center.z - ht.z);
            Vector3 p4 = new Vector3(center.x - hb.x, yb, center.z + hb.z);
            Vector3 p5 = new Vector3(center.x + hb.x, yb, center.z + hb.z);
            Vector3 p6 = new Vector3(center.x + ht.x, yt, center.z + ht.z);
            Vector3 p7 = new Vector3(center.x - ht.x, yt, center.z + ht.z);

            AddQuad(p0, p3, p2, p1, Vector3.back, color);
            AddQuad(p5, p6, p7, p4, Vector3.forward, color);
            AddQuad(p4, p7, p3, p0, Vector3.left, color);
            AddQuad(p1, p2, p6, p5, Vector3.right, color);
            AddQuad(p3, p7, p6, p2, Vector3.up, color);
            AddQuad(p4, p0, p1, p5, Vector3.down, color);
        }

        public void AddCylinder(Vector3 center, float radius, float height, ToonColor color, int segments = 8)
        {
            AddTaperedCylinder(center, radius, radius, height, color, segments);
        }

        public void AddTaperedCylinder(Vector3 center, float botRadius, float topRadius, float height, ToonColor color, int segments = 8)
        {
            float halfH = height * 0.5f;
            float yb = center.y - halfH;
            float yt = center.y + halfH;

            Vector3[] bPts = new Vector3[segments];
            Vector3[] tPts = new Vector3[segments];
            Vector3[] sNorms = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);
                bPts[i] = new Vector3(center.x + cos * botRadius, yb, center.z + sin * botRadius);
                tPts[i] = new Vector3(center.x + cos * topRadius, yt, center.z + sin * topRadius);
                sNorms[i] = new Vector3(cos, 0f, sin);
            }

            // Sides
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                Vector3 n = (sNorms[i] + sNorms[next]) * 0.5f;
                AddQuad(bPts[i], tPts[i], tPts[next], bPts[next], n, color);
            }

            // Bottom cap
            Vector3 bCenter = new Vector3(center.x, yb, center.z);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTri(bCenter, bPts[next], bPts[i], Vector3.down, color);
            }

            // Top cap
            Vector3 tCenter = new Vector3(center.x, yt, center.z);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                AddTri(tCenter, tPts[i], tPts[next], Vector3.up, color);
            }
        }

        public void AddCone(Vector3 center, float radius, float height, ToonColor color, int segments = 8)
        {
            float halfH = height * 0.5f;
            float yb = center.y - halfH;
            Vector3 tip = new Vector3(center.x, center.y + halfH, center.z);
            Vector3 bCenter = new Vector3(center.x, yb, center.z);

            Vector3[] bPts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                bPts[i] = new Vector3(center.x + Mathf.Cos(ang) * radius, yb, center.z + Mathf.Sin(ang) * radius);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                Vector3 sideN = Vector3.Cross(tip - bPts[i], bPts[next] - bPts[i]).normalized;
                AddTri(bPts[i], tip, bPts[next], sideN, color);
                AddTri(bCenter, bPts[next], bPts[i], Vector3.down, color);
            }
        }

        public void AddWedgeRoof(Vector3 center, Vector3 size, ToonColor color)
        {
            // Triangular prism (pitched roof)
            Vector3 h = size * 0.5f;
            float yb = center.y - h.y;
            float yt = center.y + h.y;

            Vector3 bl = new Vector3(center.x - h.x, yb, center.z - h.z);
            Vector3 br = new Vector3(center.x + h.x, yb, center.z - h.z);
            Vector3 fl = new Vector3(center.x - h.x, yb, center.z + h.z);
            Vector3 fr = new Vector3(center.x + h.x, yb, center.z + h.z);
            Vector3 peakB = new Vector3(center.x, yt, center.z - h.z);
            Vector3 peakF = new Vector3(center.x, yt, center.z + h.z);

            // Left slope
            Vector3 nL = Vector3.Cross(peakF - bl, fl - bl).normalized;
            AddQuad(bl, peakB, peakF, fl, nL, color);
            // Right slope
            Vector3 nR = Vector3.Cross(fr - peakB, br - peakB).normalized;
            AddQuad(peakB, br, fr, peakF, nR, color);
            // Front gable
            AddTri(fl, peakF, fr, Vector3.forward, color);
            // Back gable
            AddTri(br, peakB, bl, Vector3.back, color);
            // Bottom
            AddQuad(bl, fl, fr, br, Vector3.down, color);
        }

        public void AddRoundedRectPrism(
            Vector3 center,
            float width,
            float depth,
            float cornerRadius,
            float height,
            float topInset,
            ToonColor sideColor,
            ToonColor topColor,
            int cornerSegs = 4)
        {
            float halfH = height * 0.5f;
            float yb = center.y - halfH;
            float yt = center.y + halfH;

            float hxB = width * 0.5f;
            float hzB = depth * 0.5f;
            float rB = Mathf.Clamp(cornerRadius, 0.01f, Mathf.Min(hxB, hzB) * 0.95f);

            float hxT = Mathf.Max(0.02f, hxB - topInset);
            float hzT = Mathf.Max(0.02f, hzB - topInset);
            float rT = Mathf.Clamp(cornerRadius - topInset * 0.8f, 0.01f, Mathf.Min(hxT, hzT) * 0.95f);

            int ptsPerCorner = Mathf.Max(2, cornerSegs + 1);
            int totalPts = ptsPerCorner * 4;
            Vector3[] bPts = new Vector3[totalPts];
            Vector3[] tPts = new Vector3[totalPts];
            Vector3[] sNorms = new Vector3[totalPts];

            Vector2[] cOffsetsB = new Vector2[] {
                new Vector2( hxB - rB,  hzB - rB),
                new Vector2(-hxB + rB,  hzB - rB),
                new Vector2(-hxB + rB, -hzB + rB),
                new Vector2( hxB - rB, -hzB + rB)
            };
            Vector2[] cOffsetsT = new Vector2[] {
                new Vector2( hxT - rT,  hzT - rT),
                new Vector2(-hxT + rT,  hzT - rT),
                new Vector2(-hxT + rT, -hzT + rT),
                new Vector2( hxT - rT, -hzT + rT)
            };

            int idx = 0;
            for (int c = 0; c < 4; c++)
            {
                float startAng = c * (Mathf.PI * 0.5f);
                for (int s = 0; s < ptsPerCorner; s++)
                {
                    float t = s / (float)(ptsPerCorner - 1);
                    float ang = startAng + t * (Mathf.PI * 0.5f);
                    float cos = Mathf.Cos(ang);
                    float sin = Mathf.Sin(ang);

                    bPts[idx] = new Vector3(center.x + cOffsetsB[c].x + cos * rB, yb, center.z + cOffsetsB[c].y + sin * rB);
                    tPts[idx] = new Vector3(center.x + cOffsetsT[c].x + cos * rT, yt, center.z + cOffsetsT[c].y + sin * rT);
                    sNorms[idx] = new Vector3(cos, 0.25f, sin).normalized;
                    idx++;
                }
            }

            // Note: perimeter runs counter-clockwise in XZ (+X -> -X -> -Z -> +Z), so wind quads accordingly
            for (int i = 0; i < totalPts; i++)
            {
                int next = (i + 1) % totalPts;
                Vector3 n = (sNorms[i] + sNorms[next]).normalized;
                AddQuad(bPts[next], tPts[next], tPts[i], bPts[i], n, sideColor);
            }

            Vector3 tCenter = new Vector3(center.x, yt, center.z);
            Vector3 bCenter = new Vector3(center.x, yb, center.z);
            for (int i = 0; i < totalPts; i++)
            {
                int next = (i + 1) % totalPts;
                AddTri(tCenter, tPts[next], tPts[i], Vector3.up, topColor);
                AddTri(bCenter, bPts[i], bPts[next], Vector3.down, sideColor);
            }
        }

        public void SaveObj(string filepath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Toon Environments Stylized OBJ: {modelName}");
            sb.AppendLine($"# Mapped directly to Toon Environments 4x4 Color Palette Atlas");
            sb.AppendLine($"o {modelName}");

            foreach (var v in vertices)
            {
                sb.AppendLine($"v {v.x:F4} {v.y:F4} {v.z:F4}");
            }

            foreach (var uv in uvvs)
            {
                sb.AppendLine($"vt {uv.x:F4} {uv.y:F4}");
            }

            foreach (var n in normals)
            {
                sb.AppendLine($"vn {n.x:F4} {n.y:F4} {n.z:F4}");
            }

            sb.AppendLine("s 1");

            foreach (var f in faces)
            {
                if (f.Length == 4)
                {
                    sb.AppendLine($"f {f[0]}/{f[0]}/{f[0]} {f[1]}/{f[1]}/{f[1]} {f[2]}/{f[2]}/{f[2]} {f[3]}/{f[3]}/{f[3]}");
                }
                else if (f.Length == 3)
                {
                    sb.AppendLine($"f {f[0]}/{f[0]}/{f[0]} {f[1]}/{f[1]}/{f[1]} {f[2]}/{f[2]}/{f[2]}");
                }
            }

            File.WriteAllText(filepath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ToonObjBuilder] Saved: {filepath} ({vertices.Count} verts, {faces.Count} faces)");
        }
    }

    public static class ToonBuildingGenerator
    {
        private const string OutDir = "Assets/Models/Gameplay/Toon";

        [MenuItem("IdleFishing/Generate All Toon Buildings")]
        public static void GenerateAllBuildings()
        {
            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }

            BuildHarborIntakeStorage();
            BuildMarketDisplayCounter();
            BuildAquariumLuxuryTank();
            BuildLighthouseBeacon();
            BuildCargoTrawlerShip();
            BuildWorldPortalGate();
            BuildFishingPierProps();
            BuildFishingPierPlank();
            BuildDockGangplank();
            BuildDockExportPallet();
            BuildWorldPurchasePad();
            BuildContractBoard();
            BuildHarborStreetLamp();
            BuildMooringBollard();
            BuildLifebuoy();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GenerateAllPrefabs();

            Debug.Log("[ToonBuildingGenerator] All 15 Toon Buildings, Planks & Props generated successfully!");
        }

        [MenuItem("IdleFishing/Generate All Toon Building Prefabs")]
        public static void MenuGenerateAllPrefabs()
        {
            GenerateAllPrefabs();
        }

        // ==========================================
        // 1. HARBOR INTAKE STORAGE
        // ==========================================
        public static void BuildHarborIntakeStorage()
        {
            var b = new ToonObjBuilder("Toon_Harbor_IntakeStorage");

            // 1. Heavy Wooden Dock Pallet Foundation
            b.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(2.4f, 0.16f, 2.4f), ToonColor.DarkTimber);
            // Cross Planks
            for (int i = -3; i <= 3; i++)
            {
                b.AddBox(new Vector3(i * 0.35f, 0.18f, 0f), new Vector3(0.28f, 0.06f, 2.3f), ToonColor.WarmWood);
            }

            // 2. Large Central Ice & Fish Crate
            b.AddBox(new Vector3(0.25f, 0.45f, 0.1f), new Vector3(1.5f, 0.5f, 1.4f), ToonColor.WarmWood);
            // Dark Timber Framing
            b.AddBox(new Vector3(0.25f, 0.45f, -0.6f), new Vector3(1.55f, 0.52f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.25f, 0.45f, 0.8f), new Vector3(1.55f, 0.52f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-0.48f, 0.45f, 0.1f), new Vector3(0.12f, 0.52f, 1.45f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.98f, 0.45f, 0.1f), new Vector3(0.12f, 0.52f, 1.45f), ToonColor.DarkTimber);
            // Corner Iron Brackets & Brass Studs
            b.AddBox(new Vector3(-0.5f, 0.45f, -0.62f), new Vector3(0.18f, 0.54f, 0.18f), ToonColor.DarkIron);
            b.AddBox(new Vector3(1.0f, 0.45f, -0.62f), new Vector3(0.18f, 0.54f, 0.18f), ToonColor.DarkIron);
            b.AddBox(new Vector3(-0.5f, 0.45f, 0.82f), new Vector3(0.18f, 0.54f, 0.18f), ToonColor.DarkIron);
            b.AddBox(new Vector3(1.0f, 0.45f, 0.82f), new Vector3(0.18f, 0.54f, 0.18f), ToonColor.DarkIron);

            // Cold Ice Bed Inside Crate
            b.AddBox(new Vector3(0.25f, 0.68f, 0.1f), new Vector3(1.35f, 0.1f, 1.25f), ToonColor.IceBlue);
            // Crushed Ice Crystal Chunks
            b.AddBox(new Vector3(0.1f, 0.78f, -0.1f), new Vector3(0.35f, 0.18f, 0.35f), ToonColor.IceBlue);
            b.AddBox(new Vector3(0.55f, 0.76f, 0.3f), new Vector3(0.3f, 0.16f, 0.3f), ToonColor.IceBlue);
            b.AddBox(new Vector3(-0.15f, 0.75f, 0.4f), new Vector3(0.32f, 0.14f, 0.32f), ToonColor.IceBlue);

            // Stylized Fish Heads & Tails resting on Ice
            b.AddTaperedBox(new Vector3(0.3f, 0.82f, -0.05f), new Vector3(0.22f, 0.18f, 0.45f), new Vector3(0.14f, 0.12f, 0.3f), ToonColor.DeepMoss);
            b.AddCone(new Vector3(0.3f, 0.85f, 0.25f), 0.12f, 0.22f, ToonColor.Terracotta);
            b.AddTaperedBox(new Vector3(-0.05f, 0.81f, 0.25f), new Vector3(0.22f, 0.16f, 0.4f), new Vector3(0.12f, 0.1f, 0.25f), ToonColor.Terracotta);
            b.AddCone(new Vector3(-0.05f, 0.83f, -0.02f), 0.1f, 0.2f, ToonColor.SunlightGold);

            // 3. Rustic Salt Herring Barrel on Pallet Corner
            b.AddTaperedCylinder(new Vector3(-0.75f, 0.48f, -0.65f), 0.32f, 0.28f, 0.65f, ToonColor.DarkTimber, 8);
            // Metal Hoops on Barrel
            b.AddCylinder(new Vector3(-0.75f, 0.3f, -0.65f), 0.33f, 0.05f, ToonColor.DarkIron, 8);
            b.AddCylinder(new Vector3(-0.75f, 0.62f, -0.65f), 0.31f, 0.05f, ToonColor.DarkIron, 8);
            // Ice Overflowing from Barrel
            b.AddCylinder(new Vector3(-0.75f, 0.81f, -0.65f), 0.26f, 0.06f, ToonColor.IceBlue, 8);

            // 4. Heavy Dock Mooring Bollard
            b.AddCylinder(new Vector3(-0.85f, 0.35f, 0.75f), 0.16f, 0.42f, ToonColor.MediumStone, 8);
            b.AddCylinder(new Vector3(-0.85f, 0.58f, 0.75f), 0.22f, 0.08f, ToonColor.MediumStone, 8);
            // Coiled Rope Around Bollard
            b.AddCylinder(new Vector3(-0.85f, 0.22f, 0.75f), 0.24f, 0.12f, ToonColor.TanBamboo, 8);

            // 5. Stylized Hanging Lifebuoy
            b.AddCylinder(new Vector3(0.25f, 0.45f, -0.72f), 0.24f, 0.06f, ToonColor.Terracotta, 8);
            b.AddCylinder(new Vector3(0.25f, 0.45f, -0.72f), 0.12f, 0.07f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(OutDir, "Toon_Harbor_IntakeStorage.obj"));
        }

        // ==========================================
        // 5. MARKET DISPLAY COUNTER (KIOSK)
        // ==========================================
        public static void BuildMarketDisplayCounter()
        {
            var b = new ToonObjBuilder("Toon_Market_DisplayCounter");

            // 1. Chunky Timber Counter Base
            b.AddBox(new Vector3(0f, 0.48f, 0f), new Vector3(3.2f, 0.72f, 1.4f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 0.48f, -0.66f), new Vector3(3.26f, 0.68f, 0.1f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.48f, 0.66f), new Vector3(3.26f, 0.68f, 0.1f), ToonColor.DarkTimber);

            // 2. Decorative Wooden Wagon Wheels on Front & Legs on Back
            b.AddCylinder(new Vector3(-1.35f, 0.38f, -0.72f), 0.38f, 0.08f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(1.35f, 0.38f, -0.72f), 0.38f, 0.08f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(-1.35f, 0.38f, -0.72f), 0.08f, 0.12f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(1.35f, 0.38f, -0.72f), 0.08f, 0.12f, ToonColor.SunlightGold, 8);

            // 3. Two-Tiered Ice Showcase Troughs on Top
            b.AddBox(new Vector3(0f, 0.88f, -0.15f), new Vector3(2.95f, 0.16f, 0.95f), ToonColor.IceBlue);
            b.AddBox(new Vector3(0f, 0.98f, 0.25f), new Vector3(2.95f, 0.14f, 0.45f), ToonColor.IceBlue);

            // Fresh Display Fish on Ice
            for (int i = -2; i <= 2; i++)
            {
                ToonColor fCol = (Math.Abs(i) % 2 == 0) ? ToonColor.Terracotta : ToonColor.DeepMoss;
                b.AddTaperedBox(new Vector3(i * 0.58f, 1.02f, -0.15f), new Vector3(0.18f, 0.1f, 0.38f), new Vector3(0.1f, 0.06f, 0.22f), fCol);
                b.AddCone(new Vector3(i * 0.58f, 1.04f, 0.1f), 0.08f, 0.18f, ToonColor.SunlightGold);
            }

            // 4. Sturdy Upright Canopy Posts
            float px = 1.45f;
            float pz = 0.55f;
            b.AddBox(new Vector3(-px, 1.6f, -pz), new Vector3(0.12f, 1.5f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(px, 1.6f, -pz), new Vector3(0.12f, 1.5f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-px, 1.8f, pz), new Vector3(0.12f, 1.8f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(px, 1.8f, pz), new Vector3(0.12f, 1.8f, 0.12f), ToonColor.DarkTimber);

            // Top Crossbeams
            b.AddBox(new Vector3(0f, 2.35f, -pz), new Vector3(3.2f, 0.1f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 2.65f, pz), new Vector3(3.2f, 0.1f, 0.12f), ToonColor.DarkTimber);

            // 5. Classic Striped Scalloped Awning (Mái Bạt Sọc Đỏ Cam - Trắng)
            int numStripes = 9;
            float wStripe = 3.3f / numStripes;
            for (int i = 0; i < numStripes; i++)
            {
                ToonColor stripeCol = (i % 2 == 0) ? ToonColor.Terracotta : ToonColor.PlasterWhite;
                float sx = -1.65f + (i + 0.5f) * wStripe;
                // Slanted Canopy Plane
                b.AddBox(new Vector3(sx, 2.52f, 0f), new Vector3(wStripe * 0.95f, 0.06f, 1.35f), stripeCol);
                // Scalloped Draped Overhang at Front
                b.AddBox(new Vector3(sx, 2.32f, -0.65f), new Vector3(wStripe * 0.95f, 0.22f, 0.05f), stripeCol);
            }

            // 6. Overhead Carved Signboard
            b.AddBox(new Vector3(0f, 2.15f, -pz), new Vector3(1.6f, 0.28f, 0.06f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(-0.72f, 2.15f, -pz), new Vector3(0.08f, 0.32f, 0.08f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0.72f, 2.15f, -pz), new Vector3(0.08f, 0.32f, 0.08f), ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Market_DisplayCounter.obj"));
        }

        // ==========================================
        // 6. LUXURY AQUARIUM TANK
        // ==========================================
        public static void BuildAquariumLuxuryTank()
        {
            var b = new ToonObjBuilder("Toon_Aquarium_LuxuryTank");

            // 1. Royal Maritime Plinth Base
            b.AddBox(new Vector3(0f, 0.32f, 0f), new Vector3(3.6f, 0.64f, 2.8f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(3.8f, 0.16f, 3.0f), ToonColor.SunlightGold);

            // 2. Corner Pillars with Brass Orbs
            float cx = 1.7f;
            float cz = 1.3f;
            b.AddCylinder(new Vector3(-cx, 1.55f, -cz), 0.12f, 1.8f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(cx, 1.55f, -cz), 0.12f, 1.8f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(-cx, 1.55f, cz), 0.12f, 1.8f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(cx, 1.55f, cz), 0.12f, 1.8f, ToonColor.SunlightGold, 8);

            // Decorative Brass Spherical Finials on Top
            b.AddCone(new Vector3(-cx, 2.52f, -cz), 0.16f, 0.22f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(cx, 2.52f, -cz), 0.16f, 0.22f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(-cx, 2.52f, cz), 0.16f, 0.22f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(cx, 2.52f, cz), 0.16f, 0.22f, ToonColor.SunlightGold);

            // 3. Interior Aquascape: Sandbed & Rocks
            b.AddBox(new Vector3(0f, 0.68f, 0f), new Vector3(3.2f, 0.08f, 2.4f), ToonColor.TanBamboo);
            // Rock Formations
            b.AddTaperedBox(new Vector3(-0.85f, 0.95f, 0f), new Vector3(0.6f, 0.45f, 0.6f), new Vector3(0.35f, 0.35f, 0.35f), ToonColor.MediumStone);
            b.AddTaperedBox(new Vector3(0.85f, 0.9f, 0.3f), new Vector3(0.5f, 0.38f, 0.5f), new Vector3(0.28f, 0.28f, 0.28f), ToonColor.LightStone);

            // Colorful Sea Corals & Starfish
            b.AddCone(new Vector3(-0.75f, 1.25f, 0.15f), 0.16f, 0.35f, ToonColor.Terracotta);
            b.AddCone(new Vector3(-0.95f, 1.22f, -0.15f), 0.14f, 0.3f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(0.75f, 1.15f, 0.2f), 0.18f, 0.32f, ToonColor.Terracotta);

            // Sea Kelp Plants
            b.AddCylinder(new Vector3(0f, 1.25f, -0.5f), 0.05f, 0.85f, ToonColor.GrassGreen, 6);
            b.AddCylinder(new Vector3(0.18f, 1.15f, -0.45f), 0.04f, 0.65f, ToonColor.DeepMoss, 6);

            // 4. Heavy Brass Top Rim & Aeration Dome
            b.AddBox(new Vector3(0f, 2.42f, 0f), new Vector3(3.55f, 0.12f, 2.75f), ToonColor.SunlightGold);
            // Tapered Hood Dome
            b.AddTaperedBox(new Vector3(0f, 2.65f, 0f), new Vector3(3.4f, 0.35f, 2.6f), new Vector3(2.6f, 0.2f, 1.8f), ToonColor.DarkTimber);
            // Brass Bubble Chimney Pipe on Top
            b.AddCylinder(new Vector3(0f, 2.95f, 0f), 0.14f, 0.35f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(0f, 3.15f, 0f), 0.18f, 0.08f, ToonColor.SunlightGold, 8);

            b.SaveObj(Path.Combine(OutDir, "Toon_Aquarium_LuxuryTank.obj"));
        }

        // ==========================================
        // 7. LIGHTHOUSE BEACON
        // ==========================================
        public static void BuildLighthouseBeacon()
        {
            var b = new ToonObjBuilder("Toon_Lighthouse_Beacon");

            // 1. Heavy Octagonal Stone Base Plinth
            b.AddTaperedCylinder(new Vector3(0f, 0.5f, 0f), 1.9f, 1.75f, 1.0f, ToonColor.MediumStone, 8);
            b.AddTaperedCylinder(new Vector3(0f, 1.1f, 0f), 1.75f, 1.6f, 0.2f, ToonColor.DarkIron, 8);

            // 2. Tower Section 1 (White Stone)
            b.AddTaperedCylinder(new Vector3(0f, 2.3f, 0f), 1.6f, 1.35f, 2.2f, ToonColor.PlasterWhite, 8);

            // 3. Tower Section 2 (Terracotta Accent Band)
            b.AddTaperedCylinder(new Vector3(0f, 4.0f, 0f), 1.35f, 1.15f, 1.4f, ToonColor.Terracotta, 8);

            // 4. Tower Section 3 (Upper White Tower)
            b.AddTaperedCylinder(new Vector3(0f, 5.5f, 0f), 1.15f, 0.95f, 1.6f, ToonColor.PlasterWhite, 8);

            // 5. Corbelled Gallery Balcony (Sàn Ngắm Cảnh Đèn Biển)
            b.AddTaperedCylinder(new Vector3(0f, 6.45f, 0f), 0.95f, 1.35f, 0.35f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(0f, 6.7f, 0f), 1.38f, 0.12f, ToonColor.DarkTimber, 8);
            // Balcony Timber Railings
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                Vector3 postPos = new Vector3(Mathf.Cos(ang) * 1.3f, 7.05f, Mathf.Sin(ang) * 1.3f);
                b.AddBox(postPos, new Vector3(0.08f, 0.6f, 0.08f), ToonColor.DarkTimber);
            }
            b.AddCylinder(new Vector3(0f, 7.35f, 0f), 1.32f, 0.06f, ToonColor.DarkTimber, 8);

            // 6. Brass Beacon Lantern Room
            b.AddCylinder(new Vector3(0f, 7.42f, 0f), 0.95f, 0.1f, ToonColor.SunlightGold, 8);
            // Glowing Fresnel Beacon Core
            b.AddCylinder(new Vector3(0f, 7.95f, 0f), 0.55f, 0.75f, ToonColor.SunlightGold, 8);
            // 8 Struts framing lantern glass
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                Vector3 strutPos = new Vector3(Mathf.Cos(ang) * 0.92f, 7.95f, Mathf.Sin(ang) * 0.92f);
                b.AddBox(strutPos, new Vector3(0.08f, 0.95f, 0.08f), ToonColor.DarkIron);
            }

            // 7. Conical Terracotta Roof & Weathervane
            b.AddCylinder(new Vector3(0f, 8.48f, 0f), 1.05f, 0.12f, ToonColor.SunlightGold, 8);
            b.AddCone(new Vector3(0f, 9.35f, 0f), 1.08f, 1.65f, ToonColor.Terracotta, 8);
            // Brass Finial & Lightning Rod
            b.AddCylinder(new Vector3(0f, 10.35f, 0f), 0.04f, 0.65f, ToonColor.SunlightGold, 6);
            b.AddCone(new Vector3(0f, 10.75f, 0f), 0.12f, 0.18f, ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Lighthouse_Beacon.obj"));
        }

        // ==========================================
        // 8. CARGO TRAWLER SHIP
        // ==========================================
        public static void BuildCargoTrawlerShip()
        {
            var b = new ToonObjBuilder("Toon_Cargo_Trawler_Ship");

            // 1. Chunky Low-Poly Ship Hull (Thân Tàu Gỗ)
            // Lower Keel
            b.AddBox(new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.9f, 8.5f), ToonColor.DarkTimber);
            // High Curved Bow (Mũi Tàu Nhọn Cao)
            b.AddTaperedBox(new Vector3(0f, 0.85f, 4.8f), new Vector3(2.4f, 1.2f, 1.8f), new Vector3(0.6f, 1.4f, 0.6f), ToonColor.DarkTimber);
            // Terracotta Red Waterline Stripe
            b.AddBox(new Vector3(0f, 0.92f, 0f), new Vector3(2.72f, 0.18f, 8.6f), ToonColor.Terracotta);
            // Upper Topsides Hull
            b.AddBox(new Vector3(0f, 1.25f, 0f), new Vector3(2.78f, 0.5f, 8.6f), ToonColor.WarmWood);
            b.AddTaperedBox(new Vector3(0f, 1.6f, 4.8f), new Vector3(2.6f, 0.6f, 1.8f), new Vector3(0.8f, 0.8f, 0.6f), ToonColor.WarmWood);

            // 2. Main Deck Planking
            b.AddBox(new Vector3(0f, 1.48f, 0f), new Vector3(2.55f, 0.1f, 8.2f), ToonColor.HoneyWood);
            // Perimeter Bulwark Coamings
            b.AddBox(new Vector3(-1.32f, 1.68f, 0f), new Vector3(0.12f, 0.32f, 8.2f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(1.32f, 1.68f, 0f), new Vector3(0.12f, 0.32f, 8.2f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 1.68f, -4.15f), new Vector3(2.6f, 0.32f, 0.12f), ToonColor.DarkTimber);

            // 3. Cozy Wheelhouse Cabin (Nhà Buồng Lái Theo Phong Cách Cabin.prefab)
            Vector3 cabC = new Vector3(0f, 2.2f, -1.8f);
            b.AddBox(cabC, new Vector3(2.1f, 1.35f, 2.6f), ToonColor.PlasterWhite);
            // Cabin Timber Frame & Corner Posts
            b.AddBox(cabC + new Vector3(0f, 0f, 1.32f), new Vector3(2.14f, 1.38f, 0.08f), ToonColor.DarkTimber);
            b.AddBox(cabC + new Vector3(0f, 0f, -1.32f), new Vector3(2.14f, 1.38f, 0.08f), ToonColor.DarkTimber);
            // Pitched Cabin Roof
            b.AddWedgeRoof(cabC + new Vector3(0f, 0.95f, 0f), new Vector3(2.4f, 0.65f, 2.9f), ToonColor.Terracotta);

            // 4. Marine Steam Funnel / Smokestack
            b.AddTaperedCylinder(new Vector3(0f, 3.2f, -1.4f), 0.28f, 0.22f, 0.95f, ToonColor.MediumStone, 8);
            b.AddCylinder(new Vector3(0f, 3.45f, -1.4f), 0.26f, 0.18f, ToonColor.Terracotta, 8);
            b.AddCylinder(new Vector3(0f, 3.7f, -1.4f), 0.24f, 0.06f, ToonColor.DarkIron, 8);

            // 5. Heavy Timber Cargo Mast & Derrick Boom
            b.AddTaperedCylinder(new Vector3(0f, 3.6f, 1.8f), 0.16f, 0.08f, 4.2f, ToonColor.DarkTimber, 8);
            // Mast Cross-trees
            b.AddBox(new Vector3(0f, 4.6f, 1.8f), new Vector3(1.4f, 0.1f, 0.1f), ToonColor.DarkTimber);
            // Angled Cargo Boom
            b.AddBox(new Vector3(0f, 3.1f, 2.6f), new Vector3(0.1f, 0.1f, 2.2f), ToonColor.HoneyWood);

            // 6. Export Cargo Crates on Deck
            b.AddBox(new Vector3(0.55f, 1.82f, 1.2f), new Vector3(0.75f, 0.6f, 0.75f), ToonColor.WarmWood);
            b.AddBox(new Vector3(-0.55f, 1.78f, 1.3f), new Vector3(0.65f, 0.52f, 0.65f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0.45f, 2.32f, 1.2f), new Vector3(0.55f, 0.42f, 0.55f), ToonColor.HoneyWood);

            b.SaveObj(Path.Combine(OutDir, "Toon_Cargo_Trawler_Ship.obj"));
        }

        // ==========================================
        // 9. WORLD PORTAL GATE
        // ==========================================
        public static void BuildWorldPortalGate()
        {
            var b = new ToonObjBuilder("Toon_WorldPortal_Gate");

            // 1. Ancient Stone Plinth Base
            b.AddBox(new Vector3(0f, 0.25f, 0f), new Vector3(4.2f, 0.5f, 1.6f), ToonColor.MediumStone);
            b.AddBox(new Vector3(0f, 0.55f, 0f), new Vector3(3.8f, 0.15f, 1.3f), ToonColor.LightStone);

            // 2. Monolithic Left & Right Stone Pillars
            float px = 1.45f;
            b.AddTaperedBox(new Vector3(-px, 2.2f, 0f), new Vector3(0.85f, 3.2f, 0.85f), new Vector3(0.65f, 3.2f, 0.65f), ToonColor.MediumStone);
            b.AddTaperedBox(new Vector3(px, 2.2f, 0f), new Vector3(0.85f, 3.2f, 0.85f), new Vector3(0.65f, 3.2f, 0.65f), ToonColor.MediumStone);

            // Stepped Pillar Capital Blocks
            b.AddBox(new Vector3(-px, 3.95f, 0f), new Vector3(0.9f, 0.25f, 0.9f), ToonColor.LightStone);
            b.AddBox(new Vector3(px, 3.95f, 0f), new Vector3(0.9f, 0.25f, 0.9f), ToonColor.LightStone);

            // 3. Heavy Stone & Timber Lintel Crossbeam
            b.AddBox(new Vector3(0f, 4.25f, 0f), new Vector3(3.9f, 0.45f, 0.75f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 4.55f, 0f), new Vector3(2.4f, 0.25f, 0.6f), ToonColor.Terracotta);

            // 4. Golden Anchor & Rune Crest on Top
            b.AddCylinder(new Vector3(0f, 4.95f, 0f), 0.35f, 0.12f, ToonColor.SunlightGold, 8);
            b.AddCone(new Vector3(-px, 4.45f, 0f), 0.25f, 0.55f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(px, 4.45f, 0f), 0.25f, 0.55f, ToonColor.SunlightGold);

            // 5. Hanging Brass Nautical Lanterns on Brackets
            b.AddBox(new Vector3(-px - 0.55f, 3.2f, 0f), new Vector3(0.35f, 0.08f, 0.08f), ToonColor.DarkIron);
            b.AddCylinder(new Vector3(-px - 0.65f, 2.85f, 0f), 0.14f, 0.28f, ToonColor.SunlightGold, 6);

            b.AddBox(new Vector3(px + 0.55f, 3.2f, 0f), new Vector3(0.35f, 0.08f, 0.08f), ToonColor.DarkIron);
            b.AddCylinder(new Vector3(px + 0.65f, 2.85f, 0f), 0.14f, 0.28f, ToonColor.SunlightGold, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_WorldPortal_Gate.obj"));
        }

        // ==========================================
        // 10. FISHING PIER PROPS & STOOL
        // ==========================================
        public static void BuildFishingPierProps()
        {
            var b = new ToonObjBuilder("Toon_FishingPier_Stool");

            // 1. Rustic 3-Legged Wooden Fisherman Stool
            b.AddCylinder(new Vector3(0f, 0.42f, 0f), 0.38f, 0.12f, ToonColor.HoneyWood, 8);
            for (int i = 0; i < 3; i++)
            {
                float ang = (i / 3f) * Mathf.PI * 2f;
                Vector3 legPos = new Vector3(Mathf.Cos(ang) * 0.24f, 0.18f, Mathf.Sin(ang) * 0.24f);
                b.AddCylinder(legPos, 0.06f, 0.36f, ToonColor.DarkTimber, 6);
            }

            b.SaveObj(Path.Combine(OutDir, "Toon_FishingPier_Stool.obj"));
        }

        // ==========================================
        // 11. FISHING PIER PLANK (CẦU TÀU GỖ BẾN CÂU)
        // ==========================================
        public static void BuildFishingPierPlank()
        {
            var b = new ToonObjBuilder("Toon_FishingPier_Plank");

            float halfW = 6.0f;
            float halfD = 3.25f;

            // 1. Heavy Timber Outer Frame Perimeter (DarkTimber)
            // North seaward beam
            b.AddBox(new Vector3(0f, 0f, halfD - 0.2f), new Vector3(12.0f, 0.4f, 0.4f), ToonColor.DarkTimber);

            // South landward beam - split with 3.4m wide central entrance walkway between X = -4.2 and -0.8
            // Right segment (X from -0.8 to +6.0, length 6.8m, center = 2.6m)
            b.AddBox(new Vector3(2.6f, 0f, -halfD + 0.2f), new Vector3(6.8f, 0.4f, 0.4f), ToonColor.DarkTimber);
            // Left segment (X from -6.0 to -4.2, length 1.8m, center = -5.1m)
            b.AddBox(new Vector3(-5.1f, 0f, -halfD + 0.2f), new Vector3(1.8f, 0.4f, 0.4f), ToonColor.DarkTimber);

            // West & East beams
            b.AddBox(new Vector3(-halfW + 0.2f, 0f, 0f), new Vector3(0.4f, 0.4f, 6.5f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(halfW - 0.2f, 0f, 0f), new Vector3(0.4f, 0.4f, 6.5f), ToonColor.DarkTimber);

            // 2. Transverse Support Joists Under Deck (DarkTimber)
            float[] joistX = new float[] { -5.0f, -3.3f, -1.6f, 0.0f, 1.6f, 3.3f, 5.0f };
            for (int i = 0; i < joistX.Length; i++)
            {
                b.AddBox(new Vector3(joistX[i], -0.1f, 0f), new Vector3(0.24f, 0.25f, 5.7f), ToonColor.DarkTimber);
            }

            // 3. Timber Deck Planks (Ván gỗ lát sàn)
            int plankCount = 14;
            float zStart = -2.75f;
            float zSpacing = 5.5f / (plankCount - 1);
            ToonColor[] woodCycle = new ToonColor[] {
                ToonColor.WarmWood, ToonColor.HoneyWood, ToonColor.WarmWood, ToonColor.TanBamboo,
                ToonColor.HoneyWood, ToonColor.WarmWood, ToonColor.TanBamboo, ToonColor.HoneyWood
            };

            for (int i = 0; i < plankCount; i++)
            {
                float pz = zStart + i * zSpacing;
                ToonColor col = woodCycle[i % woodCycle.Length];
                float yVar = (i % 3 == 0) ? 0.008f : (i % 2 == 0 ? -0.005f : 0f);
                b.AddBox(new Vector3(0f, 0.12f + yVar, pz), new Vector3(11.2f, 0.16f, zSpacing * 0.92f), col);

                for (int j = 0; j < joistX.Length; j += 2)
                {
                    b.AddCylinder(new Vector3(joistX[j], 0.205f, pz), 0.035f, 0.02f, ToonColor.CastIron, 6);
                }
            }

            // 3b. Connecting Entrance Ramp & Threshold Planks (Dốc gỗ nối bến cảng với sàn bến)
            float rampCenterX = -2.5f;
            float rampWidth = 3.2f;
            b.AddBox(new Vector3(rampCenterX, 0.04f, -halfD - 0.20f), new Vector3(rampWidth, 0.14f, 0.50f), ToonColor.WarmWood);
            b.AddBox(new Vector3(rampCenterX, -0.06f, -halfD - 0.65f), new Vector3(rampWidth, 0.14f, 0.50f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(rampCenterX, -0.16f, -halfD - 1.05f), new Vector3(rampWidth, 0.14f, 0.40f), ToonColor.WarmWood);

            // Entrance flanking timber guide curbs & posts
            b.AddBox(new Vector3(rampCenterX - rampWidth * 0.5f - 0.08f, -0.02f, -halfD - 0.6f), new Vector3(0.16f, 0.22f, 1.4f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(rampCenterX + rampWidth * 0.5f + 0.08f, -0.02f, -halfD - 0.6f), new Vector3(0.16f, 0.22f, 1.4f), ToonColor.DarkTimber);
            b.AddCylinder(new Vector3(rampCenterX - rampWidth * 0.5f - 0.08f, 0.22f, -halfD + 0.1f), 0.18f, 0.45f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(rampCenterX + rampWidth * 0.5f + 0.08f, 0.22f, -halfD + 0.1f), 0.18f, 0.45f, ToonColor.DarkTimber, 8);

            // 4. Sturdy Heavy Timber Pilings (Trụ bến cắm xuống nước)
            // North pilings
            float[] pilingX = new float[] { -5.5f, -2.75f, 0.0f, 2.75f, 5.5f };
            for (int p = 0; p < pilingX.Length; p++)
            {
                b.AddCylinder(new Vector3(pilingX[p], -1.0f, halfD), 0.22f, 2.2f, ToonColor.DarkTimber, 8);
                b.AddCylinder(new Vector3(pilingX[p], 0.38f, halfD), 0.20f, 0.42f, ToonColor.DarkTimber, 8);
                b.AddCylinder(new Vector3(pilingX[p], 0.32f, halfD), 0.25f, 0.14f, ToonColor.TanBamboo, 8);
            }

            // South pilings (omits -2.75f to keep the walkway entrance completely clear)
            float[] southPilingX = new float[] { -5.5f, 0.0f, 2.75f, 5.5f };
            for (int p = 0; p < southPilingX.Length; p++)
            {
                b.AddCylinder(new Vector3(southPilingX[p], -1.0f, -halfD), 0.22f, 2.2f, ToonColor.DarkTimber, 8);
                b.AddCylinder(new Vector3(southPilingX[p], 0.32f, -halfD), 0.20f, 0.30f, ToonColor.DarkTimber, 8);
            }

            b.AddCylinder(new Vector3(-halfW, -1.0f, 0f), 0.22f, 2.2f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(-halfW, 0.35f, 0f), 0.20f, 0.36f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(halfW, -1.0f, 0f), 0.22f, 2.2f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(halfW, 0.35f, 0f), 0.20f, 0.36f, ToonColor.DarkTimber, 8);

            // 5. Stylized Low Timber Curbs (Gờ chắn bánh xe thô viền mép bến)
            b.AddBox(new Vector3(-halfW + 0.35f, 0.27f, 0f), new Vector3(0.18f, 0.14f, 5.8f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(halfW - 0.35f, 0.27f, 0f), new Vector3(0.18f, 0.14f, 5.8f), ToonColor.DarkTimber);

            // 6. Cast-Iron Corner Cleats
            b.AddBox(new Vector3(-5.2f, 0.23f, 2.85f), new Vector3(0.35f, 0.08f, 0.12f), ToonColor.CastIron);
            b.AddCylinder(new Vector3(-5.2f, 0.28f, 2.85f), 0.06f, 0.10f, ToonColor.CastIron, 6);
            b.AddBox(new Vector3(5.2f, 0.23f, 2.85f), new Vector3(0.35f, 0.08f, 0.12f), ToonColor.CastIron);
            b.AddCylinder(new Vector3(5.2f, 0.28f, 2.85f), 0.06f, 0.10f, ToonColor.CastIron, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_FishingPier_Plank.obj"));
        }

        // ==========================================
        // 12. DOCK GANGPLANK (CẦU TÀU GỖ BƯỚC LÊN TÀU)
        // ==========================================
        public static void BuildDockGangplank()
        {
            var b = new ToonObjBuilder("Toon_Dock_Gangplank");

            b.AddBox(new Vector3(-0.40f, 0f, 0f), new Vector3(0.10f, 0.14f, 2.8f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.40f, 0f, 0f), new Vector3(0.10f, 0.14f, 2.8f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.04f, 0f), new Vector3(0.72f, 0.06f, 2.76f), ToonColor.WarmWood);

            for (int i = -3; i <= 3; i++)
            {
                float z = i * 0.38f;
                b.AddBox(new Vector3(0f, 0.085f, z), new Vector3(0.76f, 0.04f, 0.08f), ToonColor.HoneyWood);
            }

            float[] stanchionZ = new float[] { -1.1f, 0f, 1.1f };
            for (int s = 0; s < stanchionZ.Length; s++)
            {
                float sz = stanchionZ[s];
                b.AddCylinder(new Vector3(-0.42f, 0.42f, sz), 0.04f, 0.82f, ToonColor.DarkTimber, 6);
                b.AddCone(new Vector3(-0.42f, 0.85f, sz), 0.05f, 0.08f, ToonColor.SunlightGold);
                b.AddCylinder(new Vector3(0.42f, 0.42f, sz), 0.04f, 0.82f, ToonColor.DarkTimber, 6);
                b.AddCone(new Vector3(0.42f, 0.85f, sz), 0.05f, 0.08f, ToonColor.SunlightGold);
            }

            b.AddBox(new Vector3(-0.42f, 0.72f, 0f), new Vector3(0.04f, 0.04f, 2.4f), ToonColor.TanBamboo);
            b.AddBox(new Vector3(0.42f, 0.72f, 0f), new Vector3(0.04f, 0.04f, 2.4f), ToonColor.TanBamboo);

            b.AddBox(new Vector3(0f, 0f, -1.38f), new Vector3(0.92f, 0.16f, 0.08f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0f, 0f, 1.38f), new Vector3(0.92f, 0.16f, 0.08f), ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Dock_Gangplank.obj"));
        }

        // ==========================================
        // 13. DOCK EXPORT PALLET (PALLET XUẤT KHẨU CẢNG)
        // ==========================================
        public static void BuildDockExportPallet()
        {
            var b = new ToonObjBuilder("Toon_Dock_ExportPallet");

            b.AddBox(new Vector3(-0.75f, 0.05f, 0f), new Vector3(0.14f, 0.10f, 1.55f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.05f, 0f), new Vector3(0.14f, 0.10f, 1.55f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.75f, 0.05f, 0f), new Vector3(0.14f, 0.10f, 1.55f), ToonColor.DarkTimber);

            for (int i = -3; i <= 3; i++)
            {
                float z = i * 0.22f;
                b.AddBox(new Vector3(0f, 0.12f, z), new Vector3(1.75f, 0.06f, 0.18f), (i % 2 == 0) ? ToonColor.WarmWood : ToonColor.HoneyWood);
            }

            b.AddBox(new Vector3(0f, 0.14f, -0.78f), new Vector3(1.85f, 0.08f, 0.09f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.14f, 0.78f), new Vector3(1.85f, 0.08f, 0.09f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-0.88f, 0.14f, 0f), new Vector3(0.09f, 0.08f, 1.65f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.88f, 0.14f, 0f), new Vector3(0.09f, 0.08f, 1.65f), ToonColor.DarkTimber);

            float cx = 0.78f;
            float cz = 0.68f;
            b.AddBox(new Vector3(-cx, 0.17f, -cz), new Vector3(0.24f, 0.04f, 0.24f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(cx, 0.17f, -cz), new Vector3(0.24f, 0.04f, 0.24f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(-cx, 0.17f, cz), new Vector3(0.24f, 0.04f, 0.24f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(cx, 0.17f, cz), new Vector3(0.24f, 0.04f, 0.24f), ToonColor.SunlightGold);

            b.AddCone(new Vector3(-cx, 0.22f, -cz), 0.06f, 0.08f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(cx, 0.22f, -cz), 0.06f, 0.08f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(-cx, 0.22f, cz), 0.06f, 0.08f, ToonColor.SunlightGold);
            b.AddCone(new Vector3(cx, 0.22f, cz), 0.06f, 0.08f, ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Dock_ExportPallet.obj"));
        }

        // ==========================================
        // 14. WORLD PURCHASE PAD (Ô VUÔNG VIỀN TRẮNG CÓ DẤU + Ở GIỮA)
        // ==========================================
        public static void BuildWorldPurchasePad()
        {
            var b = new ToonObjBuilder("Toon_WorldPurchasePad");

            // 1. Thin Grounding Base Slab (Dark contact shadow against pier wood/ground)
            b.AddBox(
                new Vector3(0f, 0.004f, 0f),
                new Vector3(1.22f, 0.008f, 1.22f),
                ToonColor.DarkTimber);

            // 2. Inner Square Floor Fill (Vibrant green zone pad)
            b.AddBox(
                new Vector3(0f, 0.016f, 0f),
                new Vector3(1.02f, 0.024f, 1.02f),
                ToonColor.GrassGreen);

            // 3. Crisp White Outer Perimeter Border (4 sides framing the square)
            float borderW = 0.09f;
            float borderH = 0.026f;
            float borderY = 0.017f;
            float outerHalf = 0.60f;
            float barOffset = outerHalf - (borderW * 0.5f); // 0.555f
            float innerSpan = (outerHalf - borderW) * 2f;    // 1.02f

            // North & South outer border bars (along X)
            b.AddBox(new Vector3(0f, borderY, barOffset), new Vector3(1.20f, borderH, borderW), ToonColor.PlasterWhite);
            b.AddBox(new Vector3(0f, borderY, -barOffset), new Vector3(1.20f, borderH, borderW), ToonColor.PlasterWhite);

            // West & East outer border bars (along Z)
            b.AddBox(new Vector3(-barOffset, borderY, 0f), new Vector3(borderW, borderH, innerSpan), ToonColor.PlasterWhite);
            b.AddBox(new Vector3(barOffset, borderY, 0f), new Vector3(borderW, borderH, innerSpan), ToonColor.PlasterWhite);

            // 4. Crisp White 3D Plus (+) Sign in Center
            float plusH = 0.014f;
            float plusY = 0.035f;
            float plusArmLen = 0.48f;
            float plusThickness = 0.12f;
            float wingLen = (plusArmLen - plusThickness) * 0.5f; // 0.18f
            float wingOffset = (plusThickness + wingLen) * 0.5f; // 0.15f

            // Horizontal stroke (along X)
            b.AddBox(new Vector3(0f, plusY, 0f), new Vector3(plusArmLen, plusH, plusThickness), ToonColor.PlasterWhite);

            // Vertical stroke wings (along Z, non-overlapping)
            b.AddBox(new Vector3(0f, plusY, wingOffset), new Vector3(plusThickness, plusH, wingLen), ToonColor.PlasterWhite);
            b.AddBox(new Vector3(0f, plusY, -wingOffset), new Vector3(plusThickness, plusH, wingLen), ToonColor.PlasterWhite);

            b.SaveObj(Path.Combine(OutDir, "Toon_WorldPurchasePad.obj"));
        }

        // ==========================================
        // 15. HARBOR CONTRACT BOARD (BẢNG THÔNG BÁO HỢP ĐỒNG)
        // ==========================================
        public static void BuildContractBoard()
        {
            var b = new ToonObjBuilder("Toon_ContractBoard");

            b.AddBox(new Vector3(-0.95f, 1.15f, 0f), new Vector3(0.22f, 2.3f, 0.22f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0.95f, 1.15f, 0f), new Vector3(0.22f, 2.3f, 0.22f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-0.95f, 0.12f, 0f), new Vector3(0.38f, 0.24f, 0.38f), ToonColor.MediumStone);
            b.AddBox(new Vector3(0.95f, 0.12f, 0f), new Vector3(0.38f, 0.24f, 0.38f), ToonColor.MediumStone);

            b.AddBox(new Vector3(0f, 2.15f, 0f), new Vector3(2.35f, 0.20f, 0.24f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.55f, 0f), new Vector3(2.35f, 0.16f, 0.18f), ToonColor.DarkTimber);

            b.AddWedgeRoof(new Vector3(0f, 2.45f, 0f), new Vector3(2.55f, 0.40f, 0.75f), ToonColor.Terracotta);
            b.AddBox(new Vector3(0f, 2.62f, 0f), new Vector3(2.6f, 0.08f, 0.10f), ToonColor.SunlightGold);

            b.AddBox(new Vector3(0f, 1.35f, 0f), new Vector3(1.78f, 1.35f, 0.08f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 1.35f, 0.045f), new Vector3(1.60f, 1.20f, 0.02f), ToonColor.PlasterWhite);

            b.AddCylinder(new Vector3(0f, 2.80f, 0.15f), 0.18f, 0.06f, ToonColor.SunlightGold, 8);
            b.AddCone(new Vector3(-0.25f, 2.80f, 0.15f), 0.10f, 0.22f, ToonColor.SunlightGold);

            b.AddBox(new Vector3(-0.75f, 1.90f, 0.06f), new Vector3(0.12f, 0.12f, 0.03f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0.75f, 1.90f, 0.06f), new Vector3(0.12f, 0.12f, 0.03f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(-0.75f, 0.80f, 0.06f), new Vector3(0.12f, 0.12f, 0.03f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0.75f, 0.80f, 0.06f), new Vector3(0.12f, 0.12f, 0.03f), ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_ContractBoard.obj"));
        }

        // ==========================================
        // 16. HARBOR STREET LAMP (ĐÈN ĐƯỜNG CẢNG BIỂN)
        // ==========================================
        public static void BuildHarborStreetLamp()
        {
            var b = new ToonObjBuilder("Toon_Harbor_StreetLamp");

            b.AddTaperedCylinder(new Vector3(0f, 0.15f, 0f), 0.28f, 0.20f, 0.30f, ToonColor.CastIron, 8);
            b.AddTaperedCylinder(new Vector3(0f, 0.35f, 0f), 0.20f, 0.12f, 0.15f, ToonColor.DarkIron, 8);

            b.AddCylinder(new Vector3(0f, 1.25f, 0f), 0.08f, 1.65f, ToonColor.DarkIron, 8);
            b.AddCylinder(new Vector3(0f, 0.85f, 0f), 0.11f, 0.06f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(0f, 1.65f, 0f), 0.11f, 0.06f, ToonColor.SunlightGold, 8);

            b.AddBox(new Vector3(0f, 2.15f, 0f), new Vector3(0.14f, 0.18f, 0.14f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0f, 2.25f, 0.18f), new Vector3(0.06f, 0.06f, 0.36f), ToonColor.DarkIron);
            b.AddBox(new Vector3(0f, 2.15f, 0.35f), new Vector3(0.06f, 0.22f, 0.06f), ToonColor.DarkIron);

            Vector3 lPos = new Vector3(0f, 1.95f, 0.35f);
            b.AddCone(lPos + new Vector3(0f, 0.22f, 0f), 0.22f, 0.18f, ToonColor.CastIron);
            b.AddCone(lPos + new Vector3(0f, 0.32f, 0f), 0.05f, 0.08f, ToonColor.SunlightGold);
            b.AddCylinder(lPos, 0.15f, 0.28f, ToonColor.SunlightGold, 6);
            for (int i = 0; i < 6; i++)
            {
                float ang = (i / 6f) * Mathf.PI * 2f;
                Vector3 sPos = lPos + new Vector3(Mathf.Cos(ang) * 0.16f, 0f, Mathf.Sin(ang) * 0.16f);
                b.AddBox(sPos, new Vector3(0.03f, 0.28f, 0.03f), ToonColor.CastIron);
            }
            b.AddTaperedCylinder(lPos - new Vector3(0f, 0.16f, 0f), 0.16f, 0.06f, 0.08f, ToonColor.CastIron, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Harbor_StreetLamp.obj"));
        }

        // ==========================================
        // 17. MOORING BOLLARD (CỘT BUỘC TÀU CẢNG BIỂN)
        // ==========================================
        public static void BuildMooringBollard()
        {
            var b = new ToonObjBuilder("Toon_Mooring_Bollard");

            b.AddCylinder(new Vector3(0f, 0.03f, 0f), 0.28f, 0.06f, ToonColor.DarkIron, 8);
            for (int i = 0; i < 4; i++)
            {
                float ang = (i / 4f) * Mathf.PI * 2f;
                Vector3 bPos = new Vector3(Mathf.Cos(ang) * 0.22f, 0.07f, Mathf.Sin(ang) * 0.22f);
                b.AddCylinder(bPos, 0.03f, 0.03f, ToonColor.SunlightGold, 6);
            }

            b.AddCylinder(new Vector3(0f, 0.12f, 0f), 0.25f, 0.14f, ToonColor.TanBamboo, 8);
            b.AddCylinder(new Vector3(0f, 0.22f, 0f), 0.23f, 0.08f, ToonColor.TanBamboo, 8);

            b.AddTaperedCylinder(new Vector3(0f, 0.38f, 0f), 0.17f, 0.15f, 0.34f, ToonColor.CastIron, 8);
            b.AddCylinder(new Vector3(0f, 0.56f, 0f), 0.20f, 0.06f, ToonColor.CastIron, 8);
            b.AddCone(new Vector3(0f, 0.61f, 0f), 0.20f, 0.08f, ToonColor.CastIron);

            b.AddBox(new Vector3(0f, 0.48f, 0f), new Vector3(0.55f, 0.09f, 0.10f), ToonColor.CastIron);
            b.AddCone(new Vector3(-0.28f, 0.48f, 0f), 0.07f, 0.07f, ToonColor.CastIron);
            b.AddCone(new Vector3(0.28f, 0.48f, 0f), 0.07f, 0.07f, ToonColor.CastIron);

            b.SaveObj(Path.Combine(OutDir, "Toon_Mooring_Bollard.obj"));
        }

        // ==========================================
        // 18. LIFEBUOY STAND (PHAO CỨU SINH CẢNG BIỂN)
        // ==========================================
        public static void BuildLifebuoy()
        {
            var b = new ToonObjBuilder("Toon_Lifebuoy");

            b.AddBox(new Vector3(0f, 0.55f, -0.06f), new Vector3(0.12f, 1.10f, 0.12f), ToonColor.DarkTimber);
            b.AddWedgeRoof(new Vector3(0f, 1.14f, -0.06f), new Vector3(0.14f, 0.08f, 0.14f), ToonColor.DarkTimber);

            b.AddBox(new Vector3(0f, 0.90f, 0.05f), new Vector3(0.04f, 0.06f, 0.12f), ToonColor.DarkIron);

            Vector3 ringCenter = new Vector3(0f, 0.70f, 0.06f);
            int segs = 8;
            float rMaj = 0.28f;
            float rMin = 0.09f;

            for (int i = 0; i < segs; i++)
            {
                float ang = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 segPos = ringCenter + new Vector3(Mathf.Cos(ang) * rMaj, Mathf.Sin(ang) * rMaj, 0f);
                ToonColor col = (i % 2 == 0) ? ToonColor.Terracotta : ToonColor.PlasterWhite;
                b.AddBox(segPos, new Vector3(rMin * 1.6f, rMin * 1.6f, rMin * 1.4f), col);
            }

            for (int i = 0; i < 4; i++)
            {
                float ang = (i / 4f) * Mathf.PI * 2f + Mathf.PI * 0.25f;
                Vector3 strapPos = ringCenter + new Vector3(Mathf.Cos(ang) * (rMaj + 0.05f), Mathf.Sin(ang) * (rMaj + 0.05f), 0f);
                b.AddBox(strapPos, new Vector3(0.05f, 0.05f, 0.05f), ToonColor.TanBamboo);
            }

            b.SaveObj(Path.Combine(OutDir, "Toon_Lifebuoy.obj"));
        }

        private const string PrefabDir = "Assets/Prefabs/Buildings";
        private const string SummerMatPath = "Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Summer.mat";
        private const string GlassMatPath = "Assets/Art/Materials/M_Glass_Transparent.mat";
        private const string WaterMatPath = "Assets/Art/Materials/M_Water_Aquarium.mat";

        public static void GenerateAllPrefabs()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
            }

            var summerMat = AssetDatabase.LoadAssetAtPath<Material>(SummerMatPath);
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>(GlassMatPath);
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);

            CreateStandardBuildingPrefab("Toon_Harbor_IntakeStorage", summerMat);
            CreateMarketDisplayCounterPrefab(summerMat, glassMat);
            CreateAquariumLuxuryTankPrefab(summerMat, glassMat, waterMat);
            CreateLighthouseBeaconPrefab(summerMat);
            CreateCargoTrawlerShipPrefab(summerMat);
            CreateWorldPortalGatePrefab(summerMat, waterMat ?? glassMat);
            CreateSeasideCafePrefab(summerMat);
            CreateStandardBuildingPrefab("Toon_FishingPier_Stool", summerMat);
            CreateFishingPierPlankPrefab(summerMat);
            CreateStandardBuildingPrefab("Toon_Dock_Gangplank", summerMat);
            CreateStandardBuildingPrefab("Toon_Dock_ExportPallet", summerMat);
            CreateStandardBuildingPrefab("Toon_WorldPurchasePad", summerMat);
            CreateStandardBuildingPrefab("Toon_ContractBoard", summerMat);
            CreateHarborStreetLampPrefab(summerMat);
            CreateStandardBuildingPrefab("Toon_Mooring_Bollard", summerMat);
            CreateStandardBuildingPrefab("Toon_Lifebuoy", summerMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ToonBuildingGenerator] All Toon Building Prefabs generated successfully in " + PrefabDir);
        }

        private static void CreateFishingPierPlankPrefab(Material mat)
        {
            var root = CreatePrefabRootWithModel("Toon_FishingPier_Plank", mat);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(12.0f, 0.4f, 6.5f);
            col.isTrigger = false;

            string path = $"{PrefabDir}/Toon_FishingPier_Plank.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateHarborStreetLampPrefab(Material mat)
        {
            var root = CreatePrefabRootWithModel("Toon_Harbor_StreetLamp", mat);
            var lightObj = new GameObject("Lamp_PointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.95f, 0.35f);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.92f, 0.65f);
            l.range = 7f;
            l.intensity = 1.6f;

            string path = $"{PrefabDir}/Toon_Harbor_StreetLamp.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject CreatePrefabRootWithModel(string modelName, Material mat)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/{modelName}.obj");
            var root = new GameObject(modelName);
            if (model != null)
            {
                var vis = UnityEngine.Object.Instantiate(model, root.transform);
                vis.name = "Model_" + modelName;
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;

                if (mat != null)
                {
                    foreach (var mr in vis.GetComponentsInChildren<MeshRenderer>())
                    {
                        mr.sharedMaterial = mat;
                    }
                }
            }
            return root;
        }

        private static void CreateStandardBuildingPrefab(string modelName, Material mat)
        {
            var root = CreatePrefabRootWithModel(modelName, mat);
            string path = $"{PrefabDir}/{modelName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateMarketDisplayCounterPrefab(Material mat, Material glassMat)
        {
            var root = CreatePrefabRootWithModel("Toon_Market_DisplayCounter", mat);

            // Front Transparent Sneeze-Guard Glass
            if (glassMat != null)
            {
                var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glass.name = "Front_SneezeGuard_Glass";
                glass.transform.SetParent(root.transform, false);
                glass.transform.localPosition = new Vector3(0f, 1.12f, -0.62f);
                glass.transform.localScale = new Vector3(3.0f, 0.38f, 0.02f);
                glass.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                UnityEngine.Object.DestroyImmediate(glass.GetComponent<Collider>());
            }

            string path = $"{PrefabDir}/Toon_Market_DisplayCounter.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateAquariumLuxuryTankPrefab(Material mat, Material glassMat, Material waterMat)
        {
            var root = CreatePrefabRootWithModel("Toon_Aquarium_LuxuryTank", mat);

            // 4 Panoramic Glass Panes
            if (glassMat != null)
            {
                // Front
                var gFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gFront.name = "Glass_Front";
                gFront.transform.SetParent(root.transform, false);
                gFront.transform.localPosition = new Vector3(0f, 1.55f, -1.32f);
                gFront.transform.localScale = new Vector3(3.3f, 1.62f, 0.02f);
                gFront.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                UnityEngine.Object.DestroyImmediate(gFront.GetComponent<Collider>());

                // Back
                var gBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gBack.name = "Glass_Back";
                gBack.transform.SetParent(root.transform, false);
                gBack.transform.localPosition = new Vector3(0f, 1.55f, 1.32f);
                gBack.transform.localScale = new Vector3(3.3f, 1.62f, 0.02f);
                gBack.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                UnityEngine.Object.DestroyImmediate(gBack.GetComponent<Collider>());

                // Left
                var gLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gLeft.name = "Glass_Left";
                gLeft.transform.SetParent(root.transform, false);
                gLeft.transform.localPosition = new Vector3(-1.68f, 1.55f, 0f);
                gLeft.transform.localScale = new Vector3(0.02f, 1.62f, 2.55f);
                gLeft.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                UnityEngine.Object.DestroyImmediate(gLeft.GetComponent<Collider>());

                // Right
                var gRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gRight.name = "Glass_Right";
                gRight.transform.SetParent(root.transform, false);
                gRight.transform.localPosition = new Vector3(1.68f, 1.55f, 0f);
                gRight.transform.localScale = new Vector3(0.02f, 1.62f, 2.55f);
                gRight.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                UnityEngine.Object.DestroyImmediate(gRight.GetComponent<Collider>());
            }

            // Water Volume
            if (waterMat != null)
            {
                var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
                water.name = "Water_Volume";
                water.transform.SetParent(root.transform, false);
                water.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                water.transform.localScale = new Vector3(3.25f, 1.58f, 2.52f);
                water.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
                UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            }

            string path = $"{PrefabDir}/Toon_Aquarium_LuxuryTank.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateLighthouseBeaconPrefab(Material mat)
        {
            var root = CreatePrefabRootWithModel("Toon_Lighthouse_Beacon", mat);

            // Warm Glowing Beacon Light at Lantern Room (y=7.95)
            var lightObj = new GameObject("Beacon_PointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 7.95f, 0f);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.95f, 0.65f);
            l.range = 24f;
            l.intensity = 2.8f;

            string path = $"{PrefabDir}/Toon_Lighthouse_Beacon.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateCargoTrawlerShipPrefab(Material mat)
        {
            var root = CreatePrefabRootWithModel("Toon_Cargo_Trawler_Ship", mat);

            // Add Dock Ladder from Toon Environments pack if available
            var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Prefabs/Props/Ladder.prefab");
            if (ladderPrefab != null)
            {
                var ladder = UnityEngine.Object.Instantiate(ladderPrefab, root.transform);
                ladder.name = "Boarding_Ladder";
                ladder.transform.localPosition = new Vector3(1.42f, 1.25f, -0.6f);
                ladder.transform.localRotation = Quaternion.Euler(0f, 90f, 15f);
                ladder.transform.localScale = Vector3.one * 0.9f;
            }

            string path = $"{PrefabDir}/Toon_Cargo_Trawler_Ship.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateWorldPortalGatePrefab(Material mat, Material portalMat)
        {
            var root = CreatePrefabRootWithModel("Toon_WorldPortal_Gate", mat);

            // Glowing Center Swirl Quad
            var swirl = GameObject.CreatePrimitive(PrimitiveType.Quad);
            swirl.name = "Portal_Swirl_Plane";
            swirl.transform.SetParent(root.transform, false);
            swirl.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            swirl.transform.localScale = new Vector3(2.1f, 3.2f, 1f);
            if (portalMat != null) swirl.GetComponent<MeshRenderer>().sharedMaterial = portalMat;
            UnityEngine.Object.DestroyImmediate(swirl.GetComponent<Collider>());

            string path = $"{PrefabDir}/Toon_WorldPortal_Gate.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateSeasideCafePrefab(Material mat)
        {
            var root = new GameObject("Toon_Seaside_Cafe");

            // 1. Cozy Cabin from Toon Environments Pack!
            var cabinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Prefabs/Props/Cabin.prefab");
            if (cabinPrefab != null)
            {
                var cabin = UnityEngine.Object.Instantiate(cabinPrefab, root.transform);
                cabin.name = "Cabin_Building";
                cabin.transform.localPosition = new Vector3(0f, 0f, 0.75f);
                cabin.transform.localRotation = Quaternion.identity;
                cabin.transform.localScale = Vector3.one * 1.05f;
            }

            // 2. Front Barista Counter Ledge
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Barista_Counter";
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.85f, -1.05f);
            counter.transform.localScale = new Vector3(2.6f, 0.14f, 0.65f);
            if (mat != null) counter.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(counter.GetComponent<Collider>());

            var counterBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterBase.name = "Counter_Base";
            counterBase.transform.SetParent(root.transform, false);
            counterBase.transform.localPosition = new Vector3(0f, 0.42f, -1.05f);
            counterBase.transform.localScale = new Vector3(2.4f, 0.72f, 0.55f);
            if (mat != null) counterBase.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(counterBase.GetComponent<Collider>());

            // 3. Shaded Awning over Counter
            var awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "Cafe_Awning";
            awning.transform.SetParent(root.transform, false);
            awning.transform.localPosition = new Vector3(0f, 2.25f, -0.95f);
            awning.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            awning.transform.localScale = new Vector3(2.8f, 0.08f, 1.25f);
            if (mat != null) awning.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(awning.GetComponent<Collider>());

            // 4. Patio Tables & Stools
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0) ? -2.4f : 2.4f;
                var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                table.name = $"Patio_Table_{i + 1}";
                table.transform.SetParent(root.transform, false);
                table.transform.localPosition = new Vector3(x, 0.45f, -0.8f);
                table.transform.localScale = new Vector3(0.85f, 0.08f, 0.85f);
                if (mat != null) table.GetComponent<MeshRenderer>().sharedMaterial = mat;
                UnityEngine.Object.DestroyImmediate(table.GetComponent<Collider>());

                var tableLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tableLeg.name = "Table_Leg";
                tableLeg.transform.SetParent(table.transform, false);
                tableLeg.transform.localPosition = new Vector3(0f, -4.5f, 0f);
                tableLeg.transform.localScale = new Vector3(0.25f, 4.5f, 0.25f);
                if (mat != null) tableLeg.GetComponent<MeshRenderer>().sharedMaterial = mat;
                UnityEngine.Object.DestroyImmediate(tableLeg.GetComponent<Collider>());

                // 2 Stools per table
                for (int s = 0; s < 2; s++)
                {
                    float sz = (s == 0) ? -0.55f : 0.55f;
                    var stool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stool.name = $"Stool_{i + 1}_{s + 1}";
                    stool.transform.SetParent(root.transform, false);
                    stool.transform.localPosition = new Vector3(x, 0.22f, -0.8f + sz);
                    stool.transform.localScale = new Vector3(0.38f, 0.22f, 0.38f);
                    if (mat != null) stool.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    UnityEngine.Object.DestroyImmediate(stool.GetComponent<Collider>());
                }
            }

            string path = $"{PrefabDir}/Toon_Seaside_Cafe.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
