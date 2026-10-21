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

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 normal, ToonColor color)
        {
            Vector2 uv = GetUV(color);
            int i1 = AddVertex(v1, uv, normal);
            int i2 = AddVertex(v2, uv, normal);
            int i3 = AddVertex(v3, uv, normal);
            int i4 = AddVertex(v4, uv, normal);
            faces.Add(new int[] { i1, i2, i3, i4 });
        }

        public void AddTri(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, ToonColor color)
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

        public void AddOvoid(Vector3 center, Vector3 radii, ToonColor color, int lonSegments = 8, int latSegments = 4)
        {
            Vector3 southPole = new Vector3(center.x, center.y - radii.y, center.z);
            Vector3 northPole = new Vector3(center.x, center.y + radii.y, center.z);

            Vector3[][] rings = new Vector3[latSegments - 1][];
            for (int lat = 1; lat < latSegments; lat++)
            {
                float a = Mathf.PI * (-0.5f + (float)lat / latSegments);
                float y = center.y + Mathf.Sin(a) * radii.y;
                float r = Mathf.Cos(a);
                rings[lat - 1] = new Vector3[lonSegments];

                for (int lon = 0; lon < lonSegments; lon++)
                {
                    float lonA = (float)lon / lonSegments * Mathf.PI * 2f;
                    rings[lat - 1][lon] = new Vector3(
                        center.x + Mathf.Cos(lonA) * radii.x * r,
                        y,
                        center.z + Mathf.Sin(lonA) * radii.z * r
                    );
                }
            }

            // Bottom cap (triangles from south pole to first ring)
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int next = (lon + 1) % lonSegments;
                Vector3 n = ((southPole + rings[0][lon] + rings[0][next]) / 3f - center).normalized;
                AddTri(southPole, rings[0][next], rings[0][lon], n, color);
            }

            // Middle bands (quads between ring[lat-1] and ring[lat])
            for (int lat = 0; lat < latSegments - 2; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int next = (lon + 1) % lonSegments;
                    Vector3 b1 = rings[lat][lon];
                    Vector3 b2 = rings[lat][next];
                    Vector3 t1 = rings[lat + 1][lon];
                    Vector3 t2 = rings[lat + 1][next];
                    Vector3 n = ((b1 + b2 + t1 + t2) * 0.25f - center).normalized;
                    AddQuad(b1, b2, t2, t1, n, color);
                }
            }

            // Top cap (triangles from last ring to north pole)
            int last = latSegments - 2;
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int next = (lon + 1) % lonSegments;
                Vector3 n = ((northPole + rings[last][lon] + rings[last][next]) / 3f - center).normalized;
                AddTri(northPole, rings[last][lon], rings[last][next], n, color);
            }
        }

        public void AddArchingPalmFrond(
            Vector3 crownCenter, 
            float azimuthAngle, 
            float length, 
            float archHeight, 
            float droopDepth, 
            float maxWidth, 
            ToonColor topColor, 
            ToonColor underColor, 
            int numSegs = 7)
        {
            // Radial direction vector
            Vector3 F = new Vector3(Mathf.Cos(azimuthAngle), 0f, Mathf.Sin(azimuthAngle));
            // Lateral side vector (perpendicular to F)
            Vector3 S = new Vector3(-Mathf.Sin(azimuthAngle), 0f, Mathf.Cos(azimuthAngle));

            // Generate spine points, widths, and 3D leaf vertices
            Vector3[] spineMid = new Vector3[numSegs + 1];
            Vector3[] leafLeft = new Vector3[numSegs + 1];
            Vector3[] leafRight = new Vector3[numSegs + 1];

            for (int i = 0; i <= numSegs; i++)
            {
                float u = (float)i / numSegs;

                // Radial reach: smooth natural expansion
                float r = 0.22f + length * Mathf.Pow(Mathf.Sin(u * Mathf.PI * 0.5f), 0.85f);

                // Height arch & droop curve: arches up then sweeps down
                float y = crownCenter.y + archHeight * Mathf.Sin(Mathf.Pow(u, 0.65f) * Mathf.PI) - droopDepth * Mathf.Pow(u, 1.8f);

                Vector3 c = crownCenter + F * r;
                c.y = y;

                // Width: starts narrow at stem, peaks around u = 0.45, tapers to point at tip
                float w = (i == numSegs) ? 0f : (maxWidth * Mathf.Sin(Mathf.Pow(u, 0.60f) * Mathf.PI) * 0.5f);
                if (i == 0) w = 0.06f;

                // Inverted V cross-section (rachis raised slightly, leaflets drooping to sides)
                spineMid[i] = c + Vector3.up * 0.04f;
                leafLeft[i] = c + S * w - Vector3.up * 0.05f;
                leafRight[i] = c - S * w - Vector3.up * 0.05f;
            }

            // Build leaf blade quads and serrated feathered leaflets
            for (int i = 0; i < numSegs; i++)
            {
                int next = i + 1;

                if (next == numSegs)
                {
                    // Tip triangles
                    Vector3 tip = spineMid[next];

                    // Left Top
                    Vector3 nLT = Vector3.Cross(leafLeft[i] - spineMid[i], tip - spineMid[i]).normalized;
                    AddTri(spineMid[i], leafLeft[i], tip, nLT, topColor);
                    // Left Under
                    AddTri(spineMid[i], tip, leafLeft[i], -nLT, underColor);

                    // Right Top
                    Vector3 nRT = Vector3.Cross(tip - spineMid[i], leafRight[i] - spineMid[i]).normalized;
                    AddTri(spineMid[i], tip, leafRight[i], nRT, topColor);
                    // Right Under
                    AddTri(spineMid[i], leafRight[i], tip, -nRT, underColor);
                }
                else
                {
                    // Left Wing
                    Vector3 nL = Vector3.Cross(leafLeft[i] - spineMid[i], spineMid[next] - spineMid[i]).normalized;
                    AddQuad(spineMid[i], leafLeft[i], leafLeft[next], spineMid[next], nL, topColor);
                    AddQuad(spineMid[i], spineMid[next], leafLeft[next], leafLeft[i], -nL, underColor);

                    // Right Wing
                    Vector3 nR = Vector3.Cross(spineMid[next] - spineMid[i], leafRight[i] - spineMid[i]).normalized;
                    AddQuad(spineMid[i], spineMid[next], leafRight[next], leafRight[i], nR, topColor);
                    AddQuad(spineMid[i], leafRight[i], leafRight[next], spineMid[next], -nR, underColor);

                    // Feathered Leaflet teeth (tạo độ xòe hình lông chim đặc trưng của lá dừa)
                    if (i >= 1 && i <= numSegs - 2)
                    {
                        float toothLen = maxWidth * 0.40f;
                        // Left leaflet tooth
                        Vector3 leftTooth = (leafLeft[i] + leafLeft[next]) * 0.5f + S * toothLen - F * 0.12f - Vector3.up * 0.07f;
                        Vector3 nToothL = Vector3.Cross(leftTooth - leafLeft[i], leafLeft[next] - leafLeft[i]).normalized;
                        AddTri(leafLeft[i], leftTooth, leafLeft[next], nToothL, topColor);
                        AddTri(leafLeft[i], leafLeft[next], leftTooth, -nToothL, underColor);

                        // Right leaflet tooth
                        Vector3 rightTooth = (leafRight[i] + leafRight[next]) * 0.5f - S * toothLen - F * 0.12f - Vector3.up * 0.07f;
                        Vector3 nToothR = Vector3.Cross(leafRight[next] - leafRight[i], rightTooth - leafRight[i]).normalized;
                        AddTri(leafRight[i], leafRight[next], rightTooth, nToothR, topColor);
                        AddTri(leafRight[i], rightTooth, leafRight[next], -nToothR, underColor);
                    }
                }
            }
        }

        public void AddWheelX(Vector3 center, float radius, float width, ToonColor rimColor, ToonColor hubColor, int segs = 8)
        {
            float halfW = width * 0.5f;
            float xl = center.x - halfW;
            float xr = center.x + halfW;
            Vector3[] lPts = new Vector3[segs];
            Vector3[] rPts = new Vector3[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                lPts[i] = new Vector3(xl, center.y + Mathf.Sin(a) * radius, center.z + Mathf.Cos(a) * radius);
                rPts[i] = new Vector3(xr, center.y + Mathf.Sin(a) * radius, center.z + Mathf.Cos(a) * radius);
            }
            for (int i = 0; i < segs; i++)
            {
                int next = (i + 1) % segs;
                Vector3 n = new Vector3(0f, Mathf.Sin((i + 0.5f) / segs * Mathf.PI * 2f), Mathf.Cos((i + 0.5f) / segs * Mathf.PI * 2f));
                AddQuad(lPts[i], lPts[next], rPts[next], rPts[i], n, rimColor);
                AddTri(new Vector3(xl, center.y, center.z), lPts[i], lPts[next], Vector3.left, hubColor);
                AddTri(new Vector3(xr, center.y, center.z), rPts[next], rPts[i], Vector3.right, hubColor);
            }
        }

        public void AddLifebuoy(Vector3 center, float rMaj, float rMin, ToonColor mainCol, ToonColor bandCol, bool faceX = false)
        {
            int segs = 8;
            for (int i = 0; i < segs; i++)
            {
                float ang = (i / (float)segs) * Mathf.PI * 2f;
                float c = Mathf.Cos(ang) * rMaj;
                float s = Mathf.Sin(ang) * rMaj;
                Vector3 segPos = faceX
                    ? center + new Vector3(0f, s, c)
                    : center + new Vector3(c, s, 0f);
                ToonColor col = (i % 2 == 0) ? bandCol : mainCol;
                Vector3 boxSize = faceX
                    ? new Vector3(rMin * 1.4f, rMin * 1.6f, rMin * 1.6f)
                    : new Vector3(rMin * 1.6f, rMin * 1.6f, rMin * 1.4f);
                AddBox(segPos, boxSize, col);
            }
        }

        public void AddAnchor(Vector3 center, float height, ToonColor metalCol, bool faceX = false)
        {
            // Vertical shank
            Vector3 shankSize = new Vector3(0.08f, height, 0.08f);
            AddBox(center, shankSize, metalCol);

            // Stock (crossbar near top)
            Vector3 stockPos = center + new Vector3(0f, height * 0.35f, 0f);
            Vector3 stockSize = faceX ? new Vector3(0.06f, 0.06f, height * 0.7f) : new Vector3(height * 0.7f, 0.06f, 0.06f);
            AddBox(stockPos, stockSize, metalCol);

            // Top shackle ring
            Vector3 ringPos = center + new Vector3(0f, height * 0.52f, 0f);
            AddBox(ringPos, new Vector3(0.12f, 0.08f, 0.12f), metalCol);

            // Curved crown & arms at bottom
            Vector3 crownPos = center - new Vector3(0f, height * 0.45f, 0f);
            Vector3 armSpan = faceX ? new Vector3(0.08f, 0.08f, height * 0.65f) : new Vector3(height * 0.65f, 0.08f, 0.08f);
            AddBox(crownPos, armSpan, metalCol);

            // Left & Right flukes curving up
            float flukeOffset = height * 0.30f;
            float flukeHeight = height * 0.25f;
            Vector3 leftPos = faceX ? crownPos + new Vector3(0f, flukeHeight * 0.5f, -flukeOffset) : crownPos + new Vector3(-flukeOffset, flukeHeight * 0.5f, 0f);
            Vector3 rightPos = faceX ? crownPos + new Vector3(0f, flukeHeight * 0.5f, flukeOffset) : crownPos + new Vector3(flukeOffset, flukeHeight * 0.5f, 0f);
            AddBox(leftPos, new Vector3(0.08f, flukeHeight, 0.08f), metalCol);
            AddBox(rightPos, new Vector3(0.08f, flukeHeight, 0.08f), metalCol);
        }

        public void AddPropeller(Vector3 center, float hubRadius, float bladeLength, ToonColor metalCol, int numBlades = 3)
        {
            // Center spinner hub
            AddCylinder(center, hubRadius, 0.22f, metalCol, 8);

            // Angled blades
            for (int i = 0; i < numBlades; i++)
            {
                float ang = (i / (float)numBlades) * Mathf.PI * 2f;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);
                Vector3 bladeCenter = center + new Vector3(cos * (hubRadius + bladeLength * 0.5f), sin * (hubRadius + bladeLength * 0.5f), 0f);
                AddBox(bladeCenter, new Vector3(0.12f, 0.05f, 0.08f), metalCol);
            }
        }

        public void AddBowWedge(Vector3 backCenter, float width, float height, float length, ToonColor color)
        {
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float yb = backCenter.y - halfH;
            float yt = backCenter.y + halfH;
            float zb = backCenter.z;
            float zf = backCenter.z + length;
            float xc = backCenter.x;

            Vector3 blBot = new Vector3(xc - halfW, yb, zb);
            Vector3 brBot = new Vector3(xc + halfW, yb, zb);
            Vector3 blTop = new Vector3(xc - halfW, yt, zb);
            Vector3 brTop = new Vector3(xc + halfW, yt, zb);
            Vector3 bowTipBot = new Vector3(xc, yb, zf);
            Vector3 bowTipTop = new Vector3(xc, yt, zf);

            // Đáy tam giác
            AddTri(blBot, bowTipBot, brBot, Vector3.down, color);
            // Nắp trên tam giác (deck mũi tàu)
            AddTri(blTop, brTop, bowTipTop, Vector3.up, color);
            // Mạn trái (Port side)
            Vector3 nL = Vector3.Cross(bowTipTop - blTop, blBot - blTop).normalized;
            AddQuad(blBot, blTop, bowTipTop, bowTipBot, nL, color);
            // Mạn phải (Starboard side)
            Vector3 nR = Vector3.Cross(brTop - bowTipTop, brBot - bowTipTop).normalized;
            AddQuad(bowTipBot, bowTipTop, brTop, brBot, nR, color);
            // Mặt sau (nối với thân)
            AddQuad(brBot, brTop, blTop, blBot, Vector3.back, color);
        }

        public void AddAngledBar(Vector3 p1, Vector3 p2, float halfW, float halfH, ToonColor color)
        {
            Vector3 dir = (p2 - p1).normalized;
            Vector3 upApprox = Mathf.Abs(dir.y) > 0.85f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(dir, upApprox).normalized * halfW;
            Vector3 up = Vector3.Cross(right, dir).normalized * halfH;

            Vector3 q0 = p1 - right - up;
            Vector3 q1 = p1 + right - up;
            Vector3 q2 = p1 + right + up;
            Vector3 q3 = p1 - right + up;

            Vector3 r0 = p2 - right - up;
            Vector3 r1 = p2 + right - up;
            Vector3 r2 = p2 + right + up;
            Vector3 r3 = p2 - right + up;

            Vector3 nBot = -up.normalized;
            AddQuad(q0, q1, r1, r0, nBot, color);

            Vector3 nRight = right.normalized;
            AddQuad(q1, q2, r2, r1, nRight, color);

            Vector3 nTop = up.normalized;
            AddQuad(q2, q3, r3, r2, nTop, color);

            Vector3 nLeft = -right.normalized;
            AddQuad(q3, q0, r0, r3, nLeft, color);

            AddQuad(q0, q3, q2, q1, -dir, color);
            AddQuad(r1, r2, r3, r0, dir, color);
        }

        public void AddAngledOar(Vector3 fulcrumOnGunwale, bool isRightSide, ToonColor handleCol, ToonColor bladeCol)
        {
            float sideSign = isRightSide ? 1f : -1f;
            // Cọc chèo (Oarlock pin) kim loại trên mạn thuyền
            AddCylinder(fulcrumOnGunwale + new Vector3(0f, 0.08f, 0f), 0.055f, 0.18f, ToonColor.CastIron, 6);

            // Điểm tay cầm trong lòng thuyền:
            Vector3 grip = fulcrumOnGunwale + new Vector3(-sideSign * 0.48f, 0.08f, -0.05f);
            // Điểm bắt đầu bản chèo ngoài mạn thuyền:
            Vector3 bladeStart = fulcrumOnGunwale + new Vector3(sideSign * 0.60f, -0.10f, 0.25f);
            // Đầu mút bản chèo vươn ra xa ngoài mặt nước:
            Vector3 oarDir = (bladeStart - grip).normalized;
            Vector3 bladeTip = bladeStart + oarDir * 0.68f;

            // 1. Cán chèo gỗ to bản chuẩn Chunky Toon (đường kính ~7cm)
            AddAngledBar(grip, bladeStart, 0.035f, 0.035f, handleCol);

            // 2. Bản chèo phẳng rộng Toon vươn ra ngoài nước (rộng ~26cm, dày ~4cm)
            AddAngledBar(bladeStart, bladeTip, 0.13f, 0.020f, bladeCol);

            // 3. Khớp gia cố đai đồng nối giữa cán và bản chèo
            AddAngledBar(bladeStart - oarDir * 0.03f, bladeStart + oarDir * 0.03f, 0.055f, 0.040f, ToonColor.SunlightGold);
        }

        public void AddLoftedBoatHull(
            Vector3 center,
            float length,
            float width,
            float height,
            float bowCurve,
            float flareFactor,
            ToonColor hullColor,
            ToonColor rimColor,
            ToonColor floorColor)
        {
            float[] zFracs = new float[] { -0.50f, -0.34f, -0.15f, 0.05f, 0.24f, 0.38f, 0.46f, 0.50f };
            float[] wRatios = new float[] { 0.72f,  0.89f,  1.00f, 0.98f, 0.88f, 0.65f, 0.34f, 0.06f };
            int n = zFracs.Length;

            Vector3[] topL = new Vector3[n];
            Vector3[] topR = new Vector3[n];
            Vector3[] botL = new Vector3[n];
            Vector3[] botR = new Vector3[n];

            Vector3[] inTopL = new Vector3[n];
            Vector3[] inTopR = new Vector3[n];
            Vector3[] inBotL = new Vector3[n];
            Vector3[] inBotR = new Vector3[n];

            float wallThick = Mathf.Min(0.12f, width * 0.08f);
            float floorThick = Mathf.Min(0.10f, height * 0.22f);

            for (int i = 0; i < n; i++)
            {
                float u = zFracs[i];
                float w = width * wRatios[i];
                float z = center.z + u * length;

                float sheer = 0f;
                if (u > 0f)
                {
                    sheer = (u * u * 4.0f) * bowCurve;
                }
                else if (u < -0.30f)
                {
                    float tu = (-0.30f - u) / 0.20f;
                    sheer = tu * tu * 0.06f * bowCurve;
                }

                float yTop = center.y + sheer + height * 0.5f;
                float yBot = center.y + sheer - height * 0.5f;
                float yFloor = yBot + floorThick;

                float halfW = w * 0.5f;
                float halfBotW = halfW * flareFactor;

                topL[i] = new Vector3(center.x - halfW, yTop, z);
                topR[i] = new Vector3(center.x + halfW, yTop, z);
                botL[i] = new Vector3(center.x - halfBotW, yBot, z);
                botR[i] = new Vector3(center.x + halfBotW, yBot, z);

                float inHalfW = Mathf.Max(0.02f, halfW - wallThick);
                float inHalfBotW = Mathf.Max(0.01f, halfBotW - wallThick * 0.7f);

                inTopL[i] = new Vector3(center.x - inHalfW, yTop, z);
                inTopR[i] = new Vector3(center.x + inHalfW, yTop, z);
                inBotL[i] = new Vector3(center.x - inHalfBotW, yFloor, z);
                inBotR[i] = new Vector3(center.x + inHalfBotW, yFloor, z);
            }

            // Quads along stations
            for (int i = 0; i < n - 1; i++)
            {
                int next = i + 1;

                // 1. Mạn ngoài bên trái (Port Outer Wall - Normal Left)
                Vector3 nL = Vector3.Cross(botL[i] - topL[i], botL[next] - botL[i]).normalized;
                AddQuad(topL[i], botL[i], botL[next], topL[next], nL, hullColor);

                // 2. Mạn ngoài bên phải (Starboard Outer Wall - Normal Right)
                Vector3 nR = Vector3.Cross(topR[next] - topR[i], botR[next] - topR[next]).normalized;
                AddQuad(topR[i], topR[next], botR[next], botR[i], nR, hullColor);

                // 3. Đáy thuyền ngoài (Bottom Keel - Normal Down)
                AddQuad(botL[i], botR[i], botR[next], botL[next], Vector3.down, hullColor);

                // 4. Mép mạn trên trái & phải (Gunwale Rims - Normal Up)
                AddQuad(topL[i], topL[next], inTopL[next], inTopL[i], Vector3.up, rimColor);
                AddQuad(inTopR[i], inTopR[next], topR[next], topR[i], Vector3.up, rimColor);

                // 5. Vách trong trái & phải
                AddQuad(inTopL[i], inTopL[next], inBotL[next], inBotL[i], -nL, hullColor);
                AddQuad(inTopR[i], inBotR[i], inBotR[next], inTopR[next], -nR, hullColor);

                // 6. Sàn lòng thuyền (Floor Deck - Normal Up)
                AddQuad(inBotL[i], inBotL[next], inBotR[next], inBotR[i], Vector3.up, floorColor);
            }

            // Đuôi thuyền ngoài (Stern Transom Outer - Normal Back)
            AddQuad(botL[0], topL[0], topR[0], botR[0], Vector3.back, hullColor);
            // Đuôi thuyền trong (Stern Transom Inner - Normal Forward)
            AddQuad(inBotR[0], inTopR[0], inTopL[0], inBotL[0], Vector3.forward, hullColor);
            // Vành đuôi trên (Stern Rim - Normal Up)
            AddQuad(topL[0], topR[0], inTopR[0], inTopL[0], Vector3.up, rimColor);

            // Mũi thuyền (Bow Stem Post - Normal Forward)
            AddQuad(botL[n - 1], botR[n - 1], topR[n - 1], topL[n - 1], Vector3.forward, rimColor);
            AddQuad(topL[n - 1], topR[n - 1], inTopR[n - 1], inTopL[n - 1], Vector3.up, rimColor);
        }

        public void AddWavyCanopy(Vector3 center, float width, float length, float archHeight, ToonColor stripe1, ToonColor stripe2)
        {
            int slices = 6;
            float sliceLen = length / slices;
            int arcSegs = 6;
            float halfW = width * 0.5f;

            for (int s = 0; s < slices; s++)
            {
                ToonColor col = (s % 2 == 0) ? stripe1 : stripe2;
                float z0 = center.z - length * 0.5f + s * sliceLen;
                float z1 = z0 + sliceLen;

                // Build curved roof quad strip
                for (int a = 0; a < arcSegs; a++)
                {
                    float u0 = -1f + 2f * a / arcSegs;
                    float u1 = -1f + 2f * (a + 1) / arcSegs;

                    float x0 = center.x + u0 * halfW;
                    float x1 = center.x + u1 * halfW;

                    // Parabolic smooth arch
                    float y0 = center.y + (1f - u0 * u0) * archHeight;
                    float y1 = center.y + (1f - u1 * u1) * archHeight;

                    Vector3 v1 = new Vector3(x0, y0, z0);
                    Vector3 v2 = new Vector3(x1, y1, z0);
                    Vector3 v3 = new Vector3(x1, y1, z1);
                    Vector3 v4 = new Vector3(x0, y0, z1);

                    Vector3 norm = Vector3.Cross(v2 - v1, v4 - v1).normalized;
                    AddQuad(v1, v2, v3, v4, norm, col);
                    // Underneath ceiling surface
                    AddQuad(v1, v4, v3, v2, -norm, col);
                }

                // Left & Right hanging wavy scalloped fringe flaps
                float flapDrop = 0.14f;
                float leftY = center.y;
                float rightY = center.y;

                // Left scalloped flap
                Vector3 lf1 = new Vector3(center.x - halfW, leftY, z0);
                Vector3 lf2 = new Vector3(center.x - halfW, leftY - flapDrop, (z0 + z1) * 0.5f);
                Vector3 lf3 = new Vector3(center.x - halfW, leftY, z1);
                Vector3 normL = Vector3.left;
                AddTri(lf1, lf2, lf3, normL, col);
                AddTri(lf1, lf3, lf2, -normL, col);

                // Right scalloped flap
                Vector3 rf1 = new Vector3(center.x + halfW, rightY, z0);
                Vector3 rf2 = new Vector3(center.x + halfW, rightY, z1);
                Vector3 rf3 = new Vector3(center.x + halfW, rightY - flapDrop, (z0 + z1) * 0.5f);
                Vector3 normR = Vector3.right;
                AddTri(rf1, rf2, rf3, normR, col);
                AddTri(rf1, rf3, rf2, -normR, col);
            }

            // Sturdy wooden perimeter frame underneath
            AddBox(new Vector3(center.x, center.y - 0.03f, center.z), new Vector3(width * 0.98f, 0.04f, length * 0.98f), ToonColor.WarmWood);
        }

        public void AddSnowCap(Vector3 center, float width, float length, float thickness, ToonColor snowCol)
        {
            AddRoundedRectPrism(center, width, length, 0.16f, thickness, 0.06f, snowCol, snowCol, 4);
            AddBox(center + new Vector3(0f, -thickness * 0.4f,  length * 0.48f), new Vector3(width * 0.85f, thickness * 0.5f, 0.08f), snowCol);
            AddBox(center + new Vector3(0f, -thickness * 0.4f, -length * 0.48f), new Vector3(width * 0.85f, thickness * 0.5f, 0.08f), snowCol);
        }

        public void AddTreasureChest(Vector3 center, Vector3 size, ToonColor woodCol, ToonColor goldCol)
        {
            AddBox(center, size, woodCol);
            AddWedgeRoof(center + new Vector3(0f, size.y * 0.65f, 0f), new Vector3(size.x, size.y * 0.4f, size.z), woodCol);
            AddBox(center + new Vector3(0f, size.y * 0.1f, size.z * 0.52f), new Vector3(0.12f, 0.14f, 0.04f), goldCol);
            AddBox(center + new Vector3(-size.x * 0.35f, 0f, 0f), new Vector3(0.04f, size.y * 1.1f, size.z * 1.04f), goldCol);
            AddBox(center + new Vector3( size.x * 0.35f, 0f, 0f), new Vector3(0.04f, size.y * 1.1f, size.z * 1.04f), goldCol);
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
            BuildShipLevel1Trawler();
            BuildShipLevel2Cruiser();
            BuildShipLevel3SteamTrawler();
            BuildShipLevel4Icebreaker();
            BuildShipLevel5RoyalFlagship();
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

            // 2. Interior Aquascape: Sandbed & Rocks
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
        // 8A. LEVEL 1: THUYỀN GỖ NHỎ 1 MÁI CHÈO (TEMPERATE BAY - SINGLE-OAR SKIFF)
        // Dài ~2.9m, Rộng ~1.30m, Cao ~0.48m. Thân gỗ bo tròn Chunky Toon mềm mại, ĐÚNG 1 MÁI CHÈO gác mạn.
        // ==========================================
        public static void BuildShipLevel1Trawler()
        {
            var b = new ToonObjBuilder("Toon_Ship_Level1_Trawler");

            // 1. Lofted Rounded Wooden Hull (Thân thuyền bo cong mềm mại chuẩn Concept Tier 1)
            b.AddLoftedBoatHull(
                new Vector3(0f, 0.28f, 0f),
                2.85f, 1.30f, 0.46f, 0.18f, 0.68f,
                ToonColor.WarmWood, ToonColor.HoneyWood, ToonColor.DarkTimber
            );

            // 2. Wooden Thwart Seat (Băng ghế gỗ đơn giữa lòng thuyền với nẹp đinh đồng)
            b.AddBox(new Vector3(0f, 0.28f, 0.05f), new Vector3(1.10f, 0.09f, 0.35f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(-0.48f, 0.33f, 0.05f), new Vector3(0.06f, 0.03f, 0.06f), ToonColor.SunlightGold);
            b.AddBox(new Vector3( 0.48f, 0.33f, 0.05f), new Vector3(0.06f, 0.03f, 0.06f), ToonColor.SunlightGold);

            // 3. Cuộn dây thừng vàng buộc mũi thuyền + sợi thừng vắt qua mạn trước hướng về camera
            b.AddCylinder(new Vector3(0f, 0.32f, 0.95f), 0.18f, 0.08f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(0f, 0.39f, 0.95f), 0.12f, 0.07f, ToonColor.SunlightGold, 8);
            // Sợi dây thừng vắt qua mạn trước rủ xuống phía người chơi (-X)
            b.AddAngledBar(new Vector3(0.05f, 0.36f, 1.02f), new Vector3(-0.35f, 0.48f, 1.25f), 0.05f, 0.05f, ToonColor.SunlightGold);
            b.AddAngledBar(new Vector3(-0.35f, 0.48f, 1.25f), new Vector3(-0.48f, 0.26f, 1.22f), 0.05f, 0.05f, ToonColor.SunlightGold);
            b.AddCylinder(new Vector3(0f, 0.46f, 1.32f), 0.04f, 0.10f, ToonColor.CastIron, 6);

            // 4. ĐÚNG 1 MÁI CHÈO TO BẢN DUY NHẤT VƯƠN RA MẠN THUYỀN HƯỚNG VỀ PHÍA NGƯỜI CHƠI (Mạn trái -X)
            b.AddAngledOar(new Vector3(-0.62f, 0.46f, 0.08f), false, ToonColor.HoneyWood, ToonColor.WarmWood);

            b.SaveObj(Path.Combine(OutDir, "Toon_Ship_Level1_Trawler.obj"));
        }

        // ==========================================
        // 8B. LEVEL 2: THUYỀN GỖ LỚN 2 MÁI CHÈO (TROPICAL CORAL - DUAL-OAR BOAT)
        // Dài ~4.0m, Rộng ~1.65m, Cao ~0.60m. To hơn Level 1 rõ rệt, 2 MÁI CHÈO ĐÔI 2 bên đối xứng.
        // ==========================================
        public static void BuildShipLevel2Cruiser()
        {
            var b = new ToonObjBuilder("Toon_Ship_Level2_Cruiser");

            // 1. Lofted Sturdy Wooden Hull (Thân thuyền lớn hơn, dáng bo cong kiên cố)
            b.AddLoftedBoatHull(
                new Vector3(0f, 0.34f, 0f),
                4.00f, 1.65f, 0.58f, 0.22f, 0.70f,
                ToonColor.WarmWood, ToonColor.HoneyWood, ToonColor.WarmWood
            );

            // 2. Hai Băng Ghế Gỗ (Rowing Bench & Passenger Bench)
            b.AddBox(new Vector3(0f, 0.34f, 0.30f), new Vector3(1.40f, 0.08f, 0.38f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(0f, 0.34f, -0.75f), new Vector3(1.35f, 0.08f, 0.38f), ToonColor.HoneyWood);

            // 3. HAI MÁI CHÈO TO BẢN ĐỐI XỨNG 2 BÊN (Dual Oars - Port & Starboard)
            b.AddAngledOar(new Vector3(-0.78f, 0.56f, 0.30f), false, ToonColor.HoneyWood, ToonColor.WarmWood);
            b.AddAngledOar(new Vector3( 0.78f, 0.56f, 0.30f), true, ToonColor.HoneyWood, ToonColor.WarmWood);

            // 4. Rổ mây chứa cá tươi sống ở khoang sau
            b.AddCylinder(new Vector3(0.38f, 0.38f, -1.25f), 0.24f, 0.24f, ToonColor.TanBamboo, 8);
            b.AddBox(new Vector3(0.38f, 0.52f, -1.25f), new Vector3(0.26f, 0.07f, 0.18f), ToonColor.IceBlue);
            b.AddBox(new Vector3(0.38f, 0.53f, -1.25f), new Vector3(0.12f, 0.07f, 0.26f), ToonColor.IceBlue);

            // 5. Phao Cứu Sinh Đỏ Trắng Treo Mạn Thuyền (Mạn trái -X hướng về người chơi)
            b.AddLifebuoy(new Vector3(-0.86f, 0.44f, -0.75f), 0.26f, 0.06f, ToonColor.PlasterWhite, ToonColor.Terracotta, true);

            b.SaveObj(Path.Combine(OutDir, "Toon_Ship_Level2_Cruiser.obj"));
        }

        // ==========================================
        // 8C. LEVEL 3: THUYỀN MÁY CÂU CÁ CÓ MÁI CHE TOON (PASTEL CANOPY MOTORBOAT)
        // Dài ~5.1m, Rộng ~1.85m, Cao ~2.20m. Mái che sọc pastel uốn lượn, ghế nệm, động cơ sau.
        // ==========================================
        public static void BuildShipLevel3SteamTrawler()
        {
            var b = new ToonObjBuilder("Toon_Ship_Level3_SteamTrawler");

            // 1. Lofted Motorboat Hull (Thân trắng kem, viền mạn hồng cam đào pastel, sàn gỗ ấm)
            b.AddLoftedBoatHull(
                new Vector3(0f, 0.40f, 0f),
                5.00f, 1.85f, 0.65f, 0.25f, 0.72f,
                ToonColor.PlasterWhite, ToonColor.Terracotta, ToonColor.WarmWood
            );

            // 2. Boong trước ván gỗ & Kính chắn gió Toon & Vô lăng
            b.AddBox(new Vector3(0f, 0.68f, 1.4f), new Vector3(1.45f, 0.08f, 1.4f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 1.02f, 0.75f), new Vector3(1.40f, 0.42f, 0.06f), ToonColor.IceBlue);
            b.AddCylinder(new Vector3(0.35f, 0.95f, 0.60f), 0.18f, 0.05f, ToonColor.CastIron, 8); // Vô lăng

            // 3. Ghế bọc nệm sọc Toon
            b.AddBox(new Vector3(0f, 0.52f, 0.10f), new Vector3(1.35f, 0.32f, 0.48f), ToonColor.IceBlue);
            b.AddBox(new Vector3(0f, 0.70f, 0.10f), new Vector3(1.30f, 0.06f, 0.44f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0f, 0.52f, -1.25f), new Vector3(1.40f, 0.32f, 0.48f), ToonColor.IceBlue);

            // 4. Mái Che Toon Vòm Lượn Sóng Sọc Xanh Trắng (Wavy Canopy Awning)
            b.AddCylinder(new Vector3(-0.65f, 1.30f,  0.55f), 0.045f, 1.40f, ToonColor.CastIron, 6);
            b.AddCylinder(new Vector3( 0.65f, 1.30f,  0.55f), 0.045f, 1.40f, ToonColor.CastIron, 6);
            b.AddCylinder(new Vector3(-0.65f, 1.30f, -0.95f), 0.045f, 1.40f, ToonColor.CastIron, 6);
            b.AddCylinder(new Vector3( 0.65f, 1.30f, -0.95f), 0.045f, 1.40f, ToonColor.CastIron, 6);
            b.AddWavyCanopy(new Vector3(0f, 1.95f, -0.20f), 1.65f, 2.10f, 0.28f, ToonColor.IceBlue, ToonColor.PlasterWhite);

            // 5. Động cơ chân vịt ngoài đuôi tàu (Outboard Motor)
            b.AddBox(new Vector3(0f, 0.65f, -2.60f), new Vector3(0.38f, 0.75f, 0.42f), ToonColor.DarkIron);
            b.AddPropeller(new Vector3(0f, 0.35f, -2.85f), 0.09f, 0.22f, ToonColor.SunlightGold, 3);

            // 6. Phao chống va (Fenders) bên mạn thuyền
            b.AddCylinder(new Vector3(-0.96f, 0.52f, -0.7f), 0.10f, 0.28f, ToonColor.PlasterWhite, 6);
            b.AddCylinder(new Vector3( 0.96f, 0.52f, -0.7f), 0.10f, 0.28f, ToonColor.PlasterWhite, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Ship_Level3_SteamTrawler.obj"));
        }

        // ==========================================
        // 8D. LEVEL 4: TÀU THÁM HIỂM BĂNG TUYẾT (ARCTIC EXPEDITION TRAWLER)
        // Dài ~6.2m, Rộng ~2.25m, Cao ~2.80m. Cabin phủ tuyết trắng, đèn pha, ống khói đỏ xả khói bồng bềnh.
        // ==========================================
        public static void BuildShipLevel4Icebreaker()
        {
            var b = new ToonObjBuilder("Toon_Ship_Level4_Icebreaker");

            // 1. Lofted Steel-Blue Arctic Hull (Thân xanh thép, viền tuyết trắng, sàn đá xám)
            b.AddLoftedBoatHull(
                new Vector3(0f, 0.42f, 0f),
                6.20f, 2.25f, 0.85f, 0.30f, 0.74f,
                ToonColor.IceBlue, ToonColor.PlasterWhite, ToonColor.MediumStone
            );

            // 2. Mũi bọc thép góc nhọn chống va băng
            b.AddBox(new Vector3(0f, 0.75f, 2.95f), new Vector3(0.25f, 0.85f, 0.25f), ToonColor.DarkIron);

            // 3. Buồng Lái Cabin Bánh Lái (Cabin Wheelhouse)
            Vector3 cabPos = new Vector3(0f, 1.45f, -0.35f);
            b.AddBox(cabPos, new Vector3(1.50f, 1.05f, 1.75f), ToonColor.PlasterWhite);
            b.AddBox(cabPos + new Vector3(0f, 0.15f, 0.88f), new Vector3(1.20f, 0.45f, 0.05f), ToonColor.IceBlue);
            b.AddBox(cabPos + new Vector3(-0.76f, 0.15f, 0f), new Vector3(0.05f, 0.45f, 1.15f), ToonColor.IceBlue);
            b.AddBox(cabPos + new Vector3( 0.76f, 0.15f, 0f), new Vector3(0.05f, 0.45f, 1.15f), ToonColor.IceBlue);

            // 4. Nóc Cabin Phủ Tuyết Trắng Bồng Bềnh (Snow Cap)
            b.AddSnowCap(cabPos + new Vector3(0f, 0.60f, 0f), 1.65f, 1.90f, 0.16f, ToonColor.PlasterWhite);

            // 5. Đèn pha tròn lớn (Giant Searchlight) trên nóc buồng lái
            b.AddCylinder(cabPos + new Vector3(0f, 0.82f, 0.55f), 0.20f, 0.24f, ToonColor.PlasterWhite, 8);
            b.AddCylinder(cabPos + new Vector3(0f, 0.82f, 0.68f), 0.16f, 0.04f, ToonColor.SunlightGold, 8);

            // 6. Ống khói đỏ cam viễn dương xả khói Toon
            b.AddTaperedCylinder(cabPos + new Vector3(0f, 1.05f, -0.45f), 0.18f, 0.14f, 0.75f, ToonColor.Terracotta, 8);
            b.AddCylinder(cabPos + new Vector3(0f, 1.35f, -0.45f), 0.15f, 0.08f, ToonColor.CastIron, 8);

            // 7. Làn Khói Trắng Toon Bồng Bềnh (Stylized Smoke Puffs)
            b.AddOvoid(cabPos + new Vector3(0f, 1.62f, -0.45f), new Vector3(0.24f, 0.22f, 0.24f), ToonColor.PlasterWhite, 6, 4);
            b.AddOvoid(cabPos + new Vector3(0.10f, 1.95f, -0.56f), new Vector3(0.32f, 0.28f, 0.32f), ToonColor.PlasterWhite, 6, 4);
            b.AddOvoid(cabPos + new Vector3(0.22f, 2.32f, -0.70f), new Vector3(0.38f, 0.34f, 0.38f), ToonColor.PlasterWhite, 6, 4);

            // 8. Mỏ neo & Lốp xe chống va bên mạn
            b.AddAnchor(new Vector3(-1.18f, 0.75f, 2.1f), 0.70f, ToonColor.DarkIron, true);
            b.AddLifebuoy(new Vector3(-1.18f, 0.75f, -0.6f), 0.22f, 0.06f, ToonColor.CastIron, ToonColor.CastIron, true);
            b.AddLifebuoy(new Vector3( 1.18f, 0.75f, -0.6f), 0.22f, 0.06f, ToonColor.CastIron, ToonColor.CastIron, true);

            // 9. Chân vịt 4 cánh ở đuôi tàu
            b.AddPropeller(new Vector3(0f, 0.40f, -3.20f), 0.12f, 0.28f, ToonColor.SunlightGold, 4);

            b.SaveObj(Path.Combine(OutDir, "Toon_Ship_Level4_Icebreaker.obj"));
        }

        // ==========================================
        // 8E. LEVEL 5: ĐẠI SOÁI HẠM HOÀNG GIA (LUXURY ROYAL FLAGSHIP)
        // Dài ~7.2m, Rộng ~2.50m, Cao ~3.50m. 2 tầng hoa lệ, viền vàng uốn lượn, ống khói đôi mạ vàng, cờ vương miện, rương báu.
        // ==========================================
        public static void BuildShipLevel5RoyalFlagship()
        {
            var b = new ToonObjBuilder("Toon_Ship_Level5_RoyalFlagship");

            // 1. Lofted Royal Timber Hull (Thân gỗ gụ nâu tối, toàn bộ viền mạn mạ vàng rực rỡ, sàn gỗ ấm)
            b.AddLoftedBoatHull(
                new Vector3(0f, 0.50f, 0f),
                7.20f, 2.50f, 1.05f, 0.32f, 0.75f,
                ToonColor.DarkTimber, ToonColor.SunlightGold, ToonColor.WarmWood
            );

            // 2. Mũi giáo vươn dài (Bowsprit) mạ vàng kim ở mũi tàu
            b.AddCylinder(new Vector3(0f, 1.15f, 3.85f), 0.06f, 0.95f, ToonColor.SunlightGold, 6);

            // 3. Buồng Hoa Tiêu Hoàng Gia 2 Tầng (2-Tier Stateroom Bridge)
            // Tầng 1
            b.AddBox(new Vector3(0f, 1.55f, -0.85f), new Vector3(1.85f, 1.05f, 2.50f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 1.65f, 0.42f), new Vector3(1.45f, 0.48f, 0.05f), ToonColor.IceBlue);
            b.AddBox(new Vector3(-0.94f, 1.65f, -0.85f), new Vector3(0.05f, 0.48f, 1.60f), ToonColor.IceBlue);
            b.AddBox(new Vector3( 0.94f, 1.65f, -0.85f), new Vector3(0.05f, 0.48f, 1.60f), ToonColor.IceBlue);

            // Cầu thang gỗ dẫn lên tầng 2 boong sau
            b.AddBox(new Vector3(0.68f, 1.40f, 0.65f), new Vector3(0.35f, 0.70f, 0.55f), ToonColor.HoneyWood);

            // Tầng 2: Boong quan sát trên
            b.AddBox(new Vector3(0f, 2.35f, -1.05f), new Vector3(1.60f, 0.65f, 1.85f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 2.72f, -1.05f), new Vector3(1.70f, 0.10f, 1.95f), ToonColor.SunlightGold);

            // Lan can con tiện mạ vàng hoàng gia quanh boong trên
            b.AddBox(new Vector3(-0.84f, 2.95f, -1.05f), new Vector3(0.05f, 0.36f, 1.90f), ToonColor.SunlightGold);
            b.AddBox(new Vector3( 0.84f, 2.95f, -1.05f), new Vector3(0.05f, 0.36f, 1.90f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0f, 2.95f, -1.98f), new Vector3(1.65f, 0.36f, 0.05f), ToonColor.SunlightGold);

            // 4. ỐNG KHÓI ĐÔI MẠ VÀNG HOÀNG GIA (Twin Gilded Funnels)
            b.AddTaperedCylinder(new Vector3(-0.35f, 3.15f, -1.15f), 0.16f, 0.12f, 0.80f, ToonColor.PlasterWhite, 8);
            b.AddCylinder(new Vector3(-0.35f, 3.42f, -1.15f), 0.14f, 0.06f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3(-0.35f, 3.56f, -1.15f), 0.13f, 0.08f, ToonColor.DarkIron, 8);

            b.AddTaperedCylinder(new Vector3( 0.35f, 3.15f, -1.15f), 0.16f, 0.12f, 0.80f, ToonColor.PlasterWhite, 8);
            b.AddCylinder(new Vector3( 0.35f, 3.42f, -1.15f), 0.14f, 0.06f, ToonColor.SunlightGold, 8);
            b.AddCylinder(new Vector3( 0.35f, 3.56f, -1.15f), 0.13f, 0.08f, ToonColor.DarkIron, 8);

            // 5. Cột Cờ Buồm Hoàng Gia & Cờ Vương Miện Đuôi Nheo
            b.AddTaperedCylinder(new Vector3(0f, 2.25f, 1.85f), 0.08f, 0.04f, 2.30f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3(0f, 3.42f, 1.85f), 0.06f, 0.08f, ToonColor.SunlightGold, 6);
            b.AddBox(new Vector3(0f, 3.20f, 1.50f), new Vector3(0.02f, 0.34f, 0.65f), ToonColor.Terracotta);
            b.AddCylinder(new Vector3(0f, 3.20f, 1.50f), 0.08f, 0.03f, ToonColor.SunlightGold, 6); // Vương miện vàng trên cờ

            // 6. Rương Báu Vật Dát Vàng Đặt Trên Boong Trước
            b.AddTreasureChest(new Vector3(0f, 1.05f, 1.15f), new Vector3(0.65f, 0.45f, 0.40f), ToonColor.DarkTimber, ToonColor.SunlightGold);

            // 7. Hai Mỏ Neo Vàng Hai Bên Mạn Mũi
            b.AddAnchor(new Vector3(-1.32f, 1.00f, 2.65f), 0.75f, ToonColor.SunlightGold, true);
            b.AddAnchor(new Vector3( 1.32f, 1.00f, 2.65f), 0.75f, ToonColor.SunlightGold, true);

            // 8. Chân vịt đôi mạ đồng ở đuôi tàu
            b.AddPropeller(new Vector3(0f, 0.45f, -3.70f), 0.14f, 0.32f, ToonColor.SunlightGold, 4);

            b.SaveObj(Path.Combine(OutDir, "Toon_Ship_Level5_RoyalFlagship.obj"));
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

        // ==========================================
        // 18. TROPICAL PALM TREE (CÂY CỌ / DỪA NHIỆT ĐỚI)
        // ==========================================
        public static void BuildTropicalPalmTree()
        {
            var b = new ToonObjBuilder("Toon_Tropical_PalmTree");

            // ========================================================
            // 1. ROOT BULB FLARE & GROUND MOUND (GỐC DỪA PHÌNH TỰ NHIÊN)
            // ========================================================
            b.AddTaperedCylinder(new Vector3(0f, 0.12f, 0f), 0.46f, 0.38f, 0.24f, ToonColor.DarkTimber, 10);
            b.AddTaperedCylinder(new Vector3(0f, 0.30f, 0f), 0.38f, 0.32f, 0.18f, ToonColor.WarmWood, 10);

            // ========================================================
            // 2. CURVING ORGANIC TRUNK WITH BARK NOTCHES (THÂN DỪA NGHIÊNG)
            // ========================================================
            Vector3 leanDir = new Vector3(0.55f, 0f, 0.35f).normalized;
            int numSegs = 10;
            Vector3 curPos = new Vector3(0f, 0.38f, 0f);
            float curR = 0.32f;
            float totalH = 5.2f;
            float segH = (totalH - 0.38f) / numSegs;

            for (int i = 0; i < numSegs; i++)
            {
                float u = (float)(i + 1) / numSegs;
                // Natural sweeping arc (u^1.6 gives gentle curve leaning towards beach)
                float leanDist = Mathf.Pow(u, 1.55f) * 1.30f + Mathf.Sin(u * Mathf.PI * 0.75f) * 0.12f;
                Vector3 nextPos = new Vector3(
                    leanDir.x * leanDist,
                    0.38f + u * (totalH - 0.38f),
                    leanDir.z * leanDist
                );

                // Trunk tapers in middle then flares slightly near crown
                float nextR = Mathf.Lerp(0.32f, 0.19f, Mathf.Sin(u * Mathf.PI * 0.65f));
                if (i >= numSegs - 2) nextR = Mathf.Lerp(nextR, 0.22f, 0.45f);

                Vector3 center = (curPos + nextPos) * 0.5f;
                ToonColor barkCol = (i % 2 == 0) ? ToonColor.WarmWood : ToonColor.HoneyWood;
                b.AddTaperedCylinder(center, curR, nextR, segH, barkCol, 8);

                // Distinct bark ring notch (gờ ngấn vỏ cây dừa)
                b.AddCylinder(nextPos, nextR * 1.10f, 0.055f, ToonColor.DarkTimber, 8);

                curPos = nextPos;
                curR = nextR;
            }

            // ========================================================
            // 3. GREEN CROWNSHAFT (CỔ HŨ DỪA / BẸ DỪA XANH)
            // ========================================================
            Vector3 crownBase = curPos;
            Vector3 crownTop = crownBase + new Vector3(leanDir.x * 0.12f, 0.45f, leanDir.z * 0.12f);
            Vector3 crownshaftCenter = (crownBase + crownTop) * 0.5f;
            b.AddTaperedCylinder(crownshaftCenter, curR * 1.05f, curR * 0.95f, 0.45f, ToonColor.OliveGreen, 8);
            b.AddCylinder(crownTop, curR * 1.08f, 0.05f, ToonColor.GrassGreen, 8);

            // ========================================================
            // 4. COCONUT CLUSTERS (BUỒNG DỪA TRĨU QUẢ HÌNH BẦU DỤC)
            // ========================================================
            Vector3 nutCenter = crownshaftCenter;

            // Cluster 1: Fresh Green Coconuts (Buồng dừa xiêm xanh mướt)
            for (int c = 0; c < 4; c++)
            {
                float ang = (c * 0.55f) + Mathf.Atan2(leanDir.z, leanDir.x) - 0.55f;
                float dist = 0.27f;
                Vector3 nutPos = nutCenter + new Vector3(Mathf.Cos(ang) * dist, -0.06f * (c % 2), Mathf.Sin(ang) * dist);
                b.AddOvoid(nutPos, new Vector3(0.16f, 0.22f, 0.16f), ToonColor.GrassGreen, 8, 4);
                b.AddCylinder((nutCenter + nutPos) * 0.5f, 0.035f, 0.16f, ToonColor.DarkTimber, 5);
            }

            // Cluster 2: Ripe Golden Coconuts (Buồng dừa già vàng óng)
            float oppAng = Mathf.Atan2(leanDir.z, leanDir.x) + Mathf.PI;
            for (int c = 0; c < 4; c++)
            {
                float ang = oppAng + (c - 1.5f) * 0.48f;
                float dist = 0.26f;
                Vector3 nutPos = nutCenter + new Vector3(Mathf.Cos(ang) * dist, 0.06f - 0.08f * (c % 2), Mathf.Sin(ang) * dist);
                b.AddOvoid(nutPos, new Vector3(0.15f, 0.20f, 0.15f), ToonColor.SunlightGold, 8, 4);
                b.AddCylinder((nutCenter + nutPos) * 0.5f, 0.032f, 0.15f, ToonColor.DarkTimber, 5);
            }

            // ========================================================
            // 5. THREE-TIER ARCHING PALM FRONDS (16 TÀN LÁ DỪA ĐA TẦNG)
            // ========================================================
            Vector3 crownCenter = crownTop + Vector3.up * 0.05f;

            // --- TIER 1: UPPER YOUNG FRONDS (5 Lá đọt non vươn cao đón nắng) ---
            int tier1Count = 5;
            for (int i = 0; i < tier1Count; i++)
            {
                float ang = (i * Mathf.PI * 2f / tier1Count) + 0.20f;
                b.AddArchingPalmFrond(
                    crownCenter: crownCenter + Vector3.up * 0.10f,
                    azimuthAngle: ang,
                    length: 2.6f,
                    archHeight: 0.72f,    // Arches high upward
                    droopDepth: 0.45f,    // Mild droop at tip
                    maxWidth: 0.48f,
                    topColor: ToonColor.SproutGreen,
                    underColor: ToonColor.GrassGreen,
                    numSegs: 7
                );
            }

            // --- TIER 2: MIDDLE MATURE FRONDS (6 Lá bánh tẻ xòe rộng rực rỡ) ---
            int tier2Count = 6;
            for (int i = 0; i < tier2Count; i++)
            {
                float ang = (i * Mathf.PI * 2f / tier2Count) + 0.65f;
                b.AddArchingPalmFrond(
                    crownCenter: crownCenter,
                    azimuthAngle: ang,
                    length: 3.4f,        // Long spreading reach
                    archHeight: 0.52f,   // Graceful arch
                    droopDepth: 1.30f,   // Cascades downward
                    maxWidth: 0.64f,     // Broad lush feather blade
                    topColor: ToonColor.GrassGreen,
                    underColor: ToonColor.DeepMoss,
                    numSegs: 8
                );
            }

            // --- TIER 3: LOWER DROOPING FRONDS (5 Lá già buông rủ tha thướt) ---
            int tier3Count = 5;
            for (int i = 0; i < tier3Count; i++)
            {
                float ang = (i * Mathf.PI * 2f / tier3Count) + 1.15f;
                b.AddArchingPalmFrond(
                    crownCenter: crownCenter - Vector3.up * 0.08f,
                    azimuthAngle: ang,
                    length: 2.9f,
                    archHeight: 0.18f,   // Starts drooping right away
                    droopDepth: 2.10f,   // Deep dramatic droop hanging down
                    maxWidth: 0.52f,
                    topColor: ToonColor.OliveGreen,
                    underColor: ToonColor.DeepMoss,
                    numSegs: 7
                );
            }

            b.SaveObj(Path.Combine(OutDir, "Toon_Tropical_PalmTree.obj"));
        }

        // ==========================================
        // 19. TROPICAL TIKI BUNGALOW (CHÒI NGHỈ BÃI BIỂN TIKI)
        // ==========================================
        public static void BuildTropicalTikiBungalow()
        {
            var b = new ToonObjBuilder("Toon_Tropical_TikiBungalow");

            // 4 Stilts
            float sw = 1.2f;
            float sd = 1.0f;
            b.AddCylinder(new Vector3(-sw, 0.6f, -sd), 0.12f, 1.2f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3( sw, 0.6f, -sd), 0.12f, 1.2f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3(-sw, 0.6f,  sd), 0.12f, 1.2f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3( sw, 0.6f,  sd), 0.12f, 1.2f, ToonColor.TanBamboo, 6);

            // Raised Bamboo Deck
            b.AddBox(new Vector3(0f, 1.25f, 0f), new Vector3(2.8f, 0.14f, 2.4f), ToonColor.HoneyWood);

            // Roof Posts
            b.AddCylinder(new Vector3(-sw * 0.9f, 2.1f, -sd * 0.9f), 0.08f, 1.6f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3( sw * 0.9f, 2.1f, -sd * 0.9f), 0.08f, 1.6f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3(-sw * 0.9f, 2.1f,  sd * 0.9f), 0.08f, 1.6f, ToonColor.TanBamboo, 6);
            b.AddCylinder(new Vector3( sw * 0.9f, 2.1f,  sd * 0.9f), 0.08f, 1.6f, ToonColor.TanBamboo, 6);

            // Thatched Pyramid Roof (Mái lá Tiki)
            b.AddWedgeRoof(new Vector3(0f, 3.2f, 0f), new Vector3(3.4f, 1.1f, 3.0f), ToonColor.TanBamboo);
            b.AddBox(new Vector3(0f, 2.75f, 0f), new Vector3(3.2f, 0.15f, 2.8f), ToonColor.WarmWood);

            // Surfboard leaning against front stilt
            b.AddTaperedBox(new Vector3(sw + 0.15f, 0.85f, -sd + 0.2f), new Vector3(0.12f, 1.6f, 0.42f), new Vector3(0.08f, 1.6f, 0.28f), ToonColor.IceBlue);
            b.AddBox(new Vector3(sw + 0.16f, 0.85f, -sd + 0.2f), new Vector3(0.02f, 1.4f, 0.12f), ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Tropical_TikiBungalow.obj"));
        }

        // ==========================================
        // 20. HARBOR PLAZA MONUMENT (TƯỢNG ĐÀI CÁ VÀNG & BỒN HOA TRUNG TÂM)
        // ==========================================
        public static void BuildHarborPlazaMonument()
        {
            var b = new ToonObjBuilder("Toon_Harbor_PlazaMonument");

            // 1. Octagonal / Circular Stepped Stone Plinth
            b.AddCylinder(new Vector3(0f, 0.08f, 0f), 1.65f, 0.16f, ToonColor.MediumStone, 12);
            b.AddCylinder(new Vector3(0f, 0.20f, 0f), 1.42f, 0.12f, ToonColor.LightStone, 12);

            // 2. Decorative Planter Ring around center pedestal
            b.AddCylinder(new Vector3(0f, 0.32f, 0f), 1.25f, 0.14f, ToonColor.DarkTimber, 12);
            b.AddCylinder(new Vector3(0f, 0.40f, 0f), 1.15f, 0.08f, ToonColor.GrassGreen, 12);

            // Flower dots in planter
            for (int i = 0; i < 8; i++)
            {
                float a = i * (Mathf.PI * 0.25f);
                ToonColor fCol = (i % 2 == 0) ? ToonColor.PlasterWhite : ToonColor.SunlightGold;
                b.AddBox(new Vector3(Mathf.Cos(a) * 0.95f, 0.46f, Mathf.Sin(a) * 0.95f), new Vector3(0.16f, 0.10f, 0.16f), fCol);
            }

            // 3. Central Sculptured Pedestal
            b.AddBox(new Vector3(0f, 0.75f, 0f), new Vector3(0.72f, 0.65f, 0.72f), ToonColor.MediumStone);
            b.AddBox(new Vector3(0f, 1.12f, 0f), new Vector3(0.82f, 0.12f, 0.82f), ToonColor.LightStone);

            // 4. Golden Leaping Fish Monument (Tượng Cá Vàng Cảng Biển)
            b.AddTaperedBox(new Vector3(0f, 1.55f, 0f), new Vector3(0.48f, 0.85f, 0.22f), new Vector3(0.24f, 0.85f, 0.12f), ToonColor.SunlightGold);
            b.AddCone(new Vector3(0f, 1.95f, 0f), 0.28f, 0.38f, ToonColor.SunlightGold, 6);
            // Tail fin
            b.AddWedgeRoof(new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.35f, 0.08f), ToonColor.SunlightGold);

            // 5. Rustic Harbor Props at base (thùng cá & cọc neo)
            b.AddCylinder(new Vector3(-0.95f, 0.35f, -0.65f), 0.24f, 0.48f, ToonColor.HoneyWood, 8);
            b.AddCylinder(new Vector3(-0.65f, 0.28f, -0.85f), 0.20f, 0.38f, ToonColor.DarkTimber, 8);

            b.SaveObj(Path.Combine(OutDir, "Toon_Harbor_PlazaMonument.obj"));
        }

        // ==========================================
        // 21. AUTUMN TIMBER LODGE (NHÀ GỖ RỪNG THU CÓ ỐNG KHÓI & CỦI ĐỐT)
        // ==========================================
        public static void BuildAutumnTimberLodge()
        {
            var b = new ToonObjBuilder("Toon_Autumn_TimberLodge");

            // 1. Cobblestone Foundation
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(3.6f, 0.36f, 3.2f), ToonColor.MediumStone);

            // 2. Heavy Timber Log Walls
            b.AddBox(new Vector3(0f, 1.25f, 0f), new Vector3(3.3f, 1.8f, 2.9f), ToonColor.WarmWood);
            // Corner log pilasters
            b.AddCylinder(new Vector3(-1.6f, 1.25f, -1.4f), 0.15f, 1.85f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3( 1.6f, 1.25f, -1.4f), 0.15f, 1.85f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3(-1.6f, 1.25f,  1.4f), 0.15f, 1.85f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3( 1.6f, 1.25f,  1.4f), 0.15f, 1.85f, ToonColor.DarkTimber, 6);

            // 3. Steep Terracotta Autumn Roof (Mái ngói dốc màu đỏ đất mùa thu)
            b.AddWedgeRoof(new Vector3(0f, 2.85f, 0f), new Vector3(3.9f, 1.4f, 3.4f), ToonColor.Terracotta);
            b.AddBox(new Vector3(0f, 2.18f, 0f), new Vector3(3.6f, 0.14f, 3.1f), ToonColor.DarkTimber);

            // 4. Stone Hearth Chimney (Ống khói đá lò sưởi mùa thu)
            b.AddBox(new Vector3(1.35f, 2.45f, 0.6f), new Vector3(0.55f, 2.6f, 0.55f), ToonColor.LightStone);
            b.AddBox(new Vector3(1.35f, 3.78f, 0.6f), new Vector3(0.68f, 0.14f, 0.68f), ToonColor.DarkIron);

            // 5. Firewood Lean-To Shed (Chái nhà cất củi đốt sưởi ấm)
            b.AddBox(new Vector3(-1.95f, 0.85f, 0f), new Vector3(0.7f, 0.08f, 2.0f), ToonColor.DarkTimber);
            // Stacked firewood logs
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    Vector3 logPos = new Vector3(-1.85f - c * 0.16f, 0.25f + r * 0.24f, -0.6f + c * 0.2f);
                    b.AddCylinder(logPos, 0.10f, 0.85f, ToonColor.HoneyWood, 6);
                }
            }

            // 6. Warm Amber Glowing Window
            b.AddBox(new Vector3(0f, 1.35f, -1.48f), new Vector3(0.65f, 0.65f, 0.08f), ToonColor.SunlightGold);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_TimberLodge.obj"));
        }

        // ==========================================
        // 22. ARCTIC SNOW CABIN (NHÀ GỖ BẮC CỰC MÁI PHỦ TUYẾT TRẮNG)
        // ==========================================
        public static void BuildArcticSnowCabin()
        {
            var b = new ToonObjBuilder("Toon_Arctic_SnowCabin");

            // 1. Frost-Resistant Timber Stilts (Cọc gỗ nâng cao chống đóng băng)
            float sw = 1.4f;
            float sd = 1.2f;
            b.AddCylinder(new Vector3(-sw, 0.35f, -sd), 0.16f, 0.7f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3( sw, 0.35f, -sd), 0.16f, 0.7f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3(-sw, 0.35f,  sd), 0.16f, 0.7f, ToonColor.DarkTimber, 6);
            b.AddCylinder(new Vector3( sw, 0.35f,  sd), 0.16f, 0.7f, ToonColor.DarkTimber, 6);

            // 2. Thick Insulated Nordic Cabin Walls
            b.AddBox(new Vector3(0f, 0.72f, 0f), new Vector3(3.2f, 0.18f, 2.8f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 1.65f, 0f), new Vector3(3.0f, 1.7f, 2.6f), ToonColor.IceBlue);

            // 3. Thick Snow-Blanket Roof (Mái dốc phủ lớp tuyết dày trắng muốt)
            b.AddWedgeRoof(new Vector3(0f, 3.1f, 0f), new Vector3(3.5f, 1.3f, 3.1f), ToonColor.PlasterWhite);
            // Thick pillowy snow eaves
            b.AddBox(new Vector3(0f, 2.52f, 0f), new Vector3(3.7f, 0.22f, 3.3f), ToonColor.PlasterWhite);

            // 4. Frost Hearth Stone Chimney (Ống khói đá sưởi ấm)
            b.AddBox(new Vector3(1.1f, 2.85f, 0.5f), new Vector3(0.52f, 2.4f, 0.52f), ToonColor.MediumStone);
            b.AddBox(new Vector3(1.1f, 4.08f, 0.5f), new Vector3(0.62f, 0.12f, 0.62f), ToonColor.PlasterWhite);

            // 5. Wooden Dog Sled leaning against cabin (Xe trượt tuyết gỗ)
            b.AddBox(new Vector3(-1.75f, 0.55f, -0.6f), new Vector3(0.08f, 1.1f, 0.45f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(-1.76f, 0.15f, -0.6f), new Vector3(0.06f, 0.08f, 1.3f), ToonColor.WarmWood);

            // 6. Frost Icicles at Eaves (Nhũ băng rủ mép mái)
            for (int i = 0; i < 4; i++)
            {
                float x = -1.2f + i * 0.8f;
                b.AddCone(new Vector3(x, 2.32f, -1.62f), 0.08f, 0.28f, ToonColor.IceBlue, 4);
            }

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_SnowCabin.obj"));
        }

        // ==========================================
        // 23. DESERT OASIS PAVILION (LẦU HÓNG MÁT SA MẠC CÁT VÀNG & CỘT SA THẠCH)
        // ==========================================
        public static void BuildDesertOasisPavilion()
        {
            var b = new ToonObjBuilder("Toon_Desert_OasisPavilion");

            // 1. Stepped Sandstone Plinth Foundation (Bệ đá sa thạch bậc thang nâng cao, không trùng mặt phẳng)
            // Lower Base Plinth (Y from 0.00 to 0.30, warm sandstone)
            b.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(4.3f, 0.30f, 4.3f), ToonColor.LightStone);
            // Upper Terrace (Y from 0.28 to 0.46, white alabaster terrace)
            b.AddBox(new Vector3(0f, 0.37f, 0f), new Vector3(3.6f, 0.18f, 3.6f), ToonColor.PlasterWhite);
            // Inset Arabian Silk Carpet (Y from 0.45 to 0.47, crimson red silk)
            b.AddBox(new Vector3(0f, 0.46f, 0f), new Vector3(2.4f, 0.02f, 2.4f), ToonColor.SunlightGold);

            // 2. 4 Carved Arabian Sandstone Columns (4 Cột đá hoa sa mạc Ai Cập)
            float pw = 1.35f;
            float pd = 1.35f;
            float[] px = new float[] { -pw, pw, -pw, pw };
            float[] pz = new float[] { -pd, -pd, pd, pd };

            for (int i = 0; i < 4; i++)
            {
                // Column base pedestals (Y from 0.44 to 0.58)
                b.AddBox(new Vector3(px[i], 0.51f, pz[i]), new Vector3(0.52f, 0.14f, 0.52f), ToonColor.LightStone);
                // Column shaft (Y from 0.55 to 2.85, height 2.30m)
                b.AddCylinder(new Vector3(px[i], 1.70f, pz[i]), 0.20f, 2.30f, ToonColor.LightStone, 8);
                // Column capital (Y from 2.82 to 2.98, height 0.16m, white plaster)
                b.AddBox(new Vector3(px[i], 2.90f, pz[i]), new Vector3(0.54f, 0.16f, 0.54f), ToonColor.PlasterWhite);
            }

            // 3. Arabesque Architrave & Frieze Cornice (Dầm đỡ & gờ chỉ mái vòm)
            // Main Architrave Frame (Y from 2.96 to 3.16)
            b.AddBox(new Vector3(0f, 3.06f, 0f), new Vector3(3.7f, 0.20f, 3.7f), ToonColor.LightStone);
            // Decorative Frieze Cornice (Y from 3.14 to 3.30, projecting outward)
            b.AddBox(new Vector3(0f, 3.22f, 0f), new Vector3(4.0f, 0.16f, 4.0f), ToonColor.PlasterWhite);

            // 4. Arabian Vaulted Onion Dome (Mái vòm ngọc lam sa mạc Isfahan vát cong quý phái)
            // Octagonal Dome Drum Base (Y from 3.28 to 3.56)
            b.AddCylinder(new Vector3(0f, 3.42f, 0f), 1.55f, 0.28f, ToonColor.LightStone, 8);
            // Vaulted Bulbous Dome Body (Y from 3.50 to 4.30, Cyan Oasis Turquoise Mosaic Dome)
            b.AddOvoid(new Vector3(0f, 3.90f, 0f), new Vector3(1.50f, 0.65f, 1.50f), ToonColor.IceBlue, 8, 4);
            // Sweeping Dome Spire Peak (Y from 4.15 to 5.25)
            b.AddCone(new Vector3(0f, 4.70f, 0f), 1.05f, 1.10f, ToonColor.IceBlue, 8);
            // Golden Spire Stem & Crescent Finial (Chóp nhọn hoàng gia sa mạc)
            b.AddCylinder(new Vector3(0f, 5.25f, 0f), 0.08f, 0.24f, ToonColor.LightStone, 6);
            b.AddCone(new Vector3(0f, 5.60f, 0f), 0.16f, 0.65f, ToonColor.LightStone, 6);

            // 5. Interior Oasis Elements & Clay Amphoras (Bàn khảm đá & bình gốm ướp nước mát)
            b.AddCylinder(new Vector3(0f, 0.62f, 0f), 0.65f, 0.36f, ToonColor.TanBamboo, 8);
            b.AddCylinder(new Vector3(0f, 0.81f, 0f), 0.72f, 0.04f, ToonColor.PlasterWhite, 8);

            b.AddOvoid(new Vector3( 1.30f, 0.68f, -0.60f), new Vector3(0.20f, 0.26f, 0.20f), ToonColor.SunlightGold, 8, 4);
            b.AddCylinder(new Vector3( 1.30f, 0.90f, -0.60f), 0.10f, 0.18f, ToonColor.SunlightGold, 6);
            b.AddOvoid(new Vector3( 1.10f, 0.58f, -0.88f), new Vector3(0.18f, 0.22f, 0.18f), ToonColor.WarmWood, 8, 4);
            b.AddCylinder(new Vector3( 1.10f, 0.76f, -0.88f), 0.08f, 0.14f, ToonColor.WarmWood, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Desert_OasisPavilion.obj"));
        }

        // ==========================================
        // 24. AUTUMN MAPLE TREE (CÂY PHONG MÙA THU LÁ ĐỎ VÀNG RỰC RỠ)
        // ==========================================
        public static void BuildAutumnMapleTree()
        {
            var b = new ToonObjBuilder("Toon_Autumn_MapleTree");

            // 1. Root Flare (Chân rễ xoè bám đất)
            b.AddTaperedCylinder(new Vector3( 0.35f, 0.12f,  0.00f), 0.24f, 0.14f, 0.24f, ToonColor.DarkTimber, 6);
            b.AddTaperedCylinder(new Vector3(-0.35f, 0.12f,  0.00f), 0.24f, 0.14f, 0.24f, ToonColor.DarkTimber, 6);
            b.AddTaperedCylinder(new Vector3( 0.00f, 0.12f,  0.35f), 0.24f, 0.14f, 0.24f, ToonColor.DarkTimber, 6);
            b.AddTaperedCylinder(new Vector3( 0.00f, 0.12f, -0.35f), 0.24f, 0.14f, 0.24f, ToonColor.DarkTimber, 6);

            // 2. Fallen Autumn Leaf Carpet (Thảm lá rụng vàng đỏ quanh gốc)
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                float dist = 0.95f + (i % 3) * 0.35f;
                float lr = 0.38f + (i % 2) * 0.18f;
                Vector3 lPos = new Vector3(Mathf.Cos(ang) * dist, 0.015f, Mathf.Sin(ang) * dist);
                ToonColor leafCol = (i % 2 == 0) ? ToonColor.SunlightGold : ToonColor.Terracotta;
                b.AddCylinder(lPos, lr, 0.02f, leafCol, 6);
            }

            // 3. Sturdy Organic Hardwood Trunk (Thân cây gỗ sồi/phong uốn lượn tự nhiên)
            b.AddTaperedCylinder(new Vector3(0.00f, 0.45f, 0.00f), 0.46f, 0.38f, 0.90f, ToonColor.DarkTimber, 8);
            b.AddTaperedCylinder(new Vector3(0.05f, 1.25f, 0.02f), 0.38f, 0.32f, 0.80f, ToonColor.DarkTimber, 8);
            b.AddTaperedCylinder(new Vector3(0.10f, 1.95f, 0.04f), 0.32f, 0.26f, 0.70f, ToonColor.DarkTimber, 8);
            b.AddTaperedCylinder(new Vector3(0.08f, 2.55f, 0.02f), 0.26f, 0.22f, 0.60f, ToonColor.DarkTimber, 8);

            // 4. Sturdy Spreading Branches (Các cành chính vươn ngang đỡ tán lá)
            b.AddTaperedCylinder(new Vector3(-0.45f, 2.95f, -0.20f), 0.18f, 0.13f, 0.90f, ToonColor.DarkTimber, 6);
            b.AddTaperedCylinder(new Vector3( 0.50f, 3.05f, -0.15f), 0.18f, 0.13f, 0.95f, ToonColor.DarkTimber, 6);
            b.AddTaperedCylinder(new Vector3( 0.10f, 3.10f,  0.50f), 0.17f, 0.12f, 0.85f, ToonColor.DarkTimber, 6);

            // 5. Volumetric Cloud Canopy - Tier 1: Lower Spreading Amber Gold (Tầng lá dưới vàng óng rực rỡ)
            b.AddOvoid(new Vector3(-1.15f, 3.40f, -0.35f), new Vector3(1.10f, 0.75f, 1.05f), ToonColor.SunlightGold, 8, 4);
            b.AddOvoid(new Vector3( 1.20f, 3.50f, -0.30f), new Vector3(1.15f, 0.80f, 1.10f), ToonColor.SunlightGold, 8, 4);
            b.AddOvoid(new Vector3( 0.20f, 3.55f,  1.10f), new Vector3(1.05f, 0.75f, 1.05f), ToonColor.HoneyWood, 8, 4);
            b.AddOvoid(new Vector3(-0.35f, 3.35f, -1.05f), new Vector3(1.00f, 0.70f, 0.95f), ToonColor.SunlightGold, 8, 4);
            b.AddOvoid(new Vector3( 0.00f, 3.50f,  0.00f), new Vector3(1.35f, 0.85f, 1.35f), ToonColor.SunlightGold, 8, 4);

            // 6. Volumetric Cloud Canopy - Tier 2: Mid Fiery Terracotta Red (Tầng lá giữa đỏ rực mùa thu)
            b.AddOvoid(new Vector3(-0.75f, 4.30f,  0.30f), new Vector3(1.15f, 0.85f, 1.10f), ToonColor.Terracotta, 8, 4);
            b.AddOvoid(new Vector3( 0.80f, 4.40f,  0.20f), new Vector3(1.20f, 0.85f, 1.15f), ToonColor.Terracotta, 8, 4);
            b.AddOvoid(new Vector3( 0.00f, 4.45f, -0.75f), new Vector3(1.10f, 0.80f, 1.05f), ToonColor.Terracotta, 8, 4);
            b.AddOvoid(new Vector3( 0.00f, 4.35f,  0.65f), new Vector3(1.15f, 0.80f, 1.10f), ToonColor.Terracotta, 8, 4);

            // 7. Volumetric Cloud Canopy - Tier 3: Crown Apex (Đỉnh vòm tán phong đỏ thắm)
            b.AddOvoid(new Vector3( 0.00f, 5.25f,  0.00f), new Vector3(1.05f, 0.85f, 1.05f), ToonColor.Terracotta, 8, 4);
            b.AddOvoid(new Vector3( 0.00f, 5.85f,  0.00f), new Vector3(0.65f, 0.55f, 0.65f), ToonColor.Terracotta, 7, 3);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_MapleTree.obj"));
        }

        // ==========================================
        // 25. ARCTIC FROSTED SPRUCE (CÂY THÔNG BẮC CỰC TUYẾT PHỦ NẶNG & NHŨ BĂNG)
        // ==========================================
        public static void BuildArcticFrostedSpruce()
        {
            var b = new ToonObjBuilder("Toon_Arctic_FrostedSpruce");

            // 1. Snow Drift Base Mound (Gò tuyết dày phủ quanh gốc thông)
            b.AddCylinder(new Vector3(0f, 0.12f, 0f), 1.25f, 0.24f, ToonColor.PlasterWhite, 8);

            // 2. Heavy Dark Timber Trunk (Thân cây thông gỗ tuyết tùng vùng cực)
            b.AddTaperedCylinder(new Vector3(0f, 2.4f, 0f), 0.36f, 0.14f, 4.8f, ToonColor.DarkTimber, 8);

            // 3. Five Cascading Tiers with Heavy Snow Blankets & Hanging Icicles
            // --- Tier 1 (Lowest, broadest) ---
            b.AddCone(new Vector3(0f, 1.35f, 0f), 2.3f, 1.2f, ToonColor.IceBlue, 8);
            b.AddCone(new Vector3(0f, 1.65f, 0f), 2.4f, 0.7f, ToonColor.PlasterWhite, 8);
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 2.05f, 0.95f, Mathf.Sin(a) * 2.05f);
                b.AddCone(pos, 0.08f, 0.35f, ToonColor.IceBlue, 4);
            }

            // --- Tier 2 ---
            b.AddCone(new Vector3(0f, 2.45f, 0f), 1.9f, 1.1f, ToonColor.IceBlue, 8);
            b.AddCone(new Vector3(0f, 2.75f, 0f), 2.0f, 0.65f, ToonColor.PlasterWhite, 8);
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * Mathf.PI * 2f + 0.3f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 1.65f, 2.05f, Mathf.Sin(a) * 1.65f);
                b.AddCone(pos, 0.07f, 0.30f, ToonColor.IceBlue, 4);
            }

            // --- Tier 3 ---
            b.AddCone(new Vector3(0f, 3.50f, 0f), 1.5f, 1.0f, ToonColor.IceBlue, 8);
            b.AddCone(new Vector3(0f, 3.80f, 0f), 1.55f, 0.60f, ToonColor.PlasterWhite, 8);
            for (int i = 0; i < 4; i++)
            {
                float a = (i / 4f) * Mathf.PI * 2f + 0.5f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 1.25f, 3.15f, Mathf.Sin(a) * 1.25f);
                b.AddCone(pos, 0.06f, 0.25f, ToonColor.IceBlue, 4);
            }

            // --- Tier 4 ---
            b.AddCone(new Vector3(0f, 4.45f, 0f), 1.1f, 0.85f, ToonColor.IceBlue, 8);
            b.AddCone(new Vector3(0f, 4.70f, 0f), 1.15f, 0.50f, ToonColor.PlasterWhite, 8);

            // --- Tier 5 (Crowning Snow Spire) ---
            b.AddCone(new Vector3(0f, 5.45f, 0f), 0.65f, 1.1f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_FrostedSpruce.obj"));
        }

        // ==========================================
        // 26. GLACIAL ICEBERG (TẢNG BĂNG TRÔI TINH THỂ BẮC CỰC)
        // ==========================================
        public static void BuildGlacialIceberg()
        {
            var b = new ToonObjBuilder("Toon_Glacial_Iceberg");

            // 1. Submerged Ice Shelf (Thềm băng chìm dưới nước)
            b.AddBox(new Vector3(0f, -0.6f, 0f), new Vector3(5.8f, 1.2f, 4.8f), ToonColor.IceBlue);

            // 2. Faceted Glacial Cliff Body (Thân vách băng tinh thể góc cạnh)
            b.AddTaperedBox(new Vector3(0f, 0.4f, 0f), new Vector3(4.8f, 1.6f, 3.8f), new Vector3(3.6f, 1.6f, 2.8f), ToonColor.IceBlue);

            // 3. Twin Crystalline Pinnacles with Snow Caps (2 Đỉnh băng nhọn bất đối xứng)
            // Left Main Pinnacle
            b.AddTaperedBox(new Vector3(-0.7f, 1.85f, -0.3f), new Vector3(2.2f, 2.2f, 1.8f), new Vector3(0.5f, 0.2f, 0.5f), ToonColor.IceBlue);
            b.AddCone(new Vector3(-0.7f, 3.1f, -0.3f), 0.55f, 0.85f, ToonColor.PlasterWhite, 5);

            // Right Secondary Pinnacle
            b.AddTaperedBox(new Vector3(1.1f, 1.45f, 0.4f), new Vector3(1.8f, 1.7f, 1.5f), new Vector3(0.4f, 0.2f, 0.4f), ToonColor.IceBlue);
            b.AddCone(new Vector3(1.1f, 2.4f, 0.4f), 0.45f, 0.65f, ToonColor.PlasterWhite, 5);

            // 4. Heavy Sloping Snow Mantles on Ledges (Lớp tuyết phủ dày trên các gờ băng)
            b.AddBox(new Vector3(-0.2f, 1.25f, 0.9f), new Vector3(2.2f, 0.25f, 1.4f), ToonColor.PlasterWhite);
            b.AddBox(new Vector3( 0.8f, 0.95f, -0.8f), new Vector3(1.6f, 0.22f, 1.2f), ToonColor.PlasterWhite);

            b.SaveObj(Path.Combine(OutDir, "Toon_Glacial_Iceberg.obj"));
        }

        // ==========================================
        // 27. FLOATING ICE FLOE (MẢNG BĂNG TRÔI NỔI TRÊN MẶT NƯỚC)
        // ==========================================
        public static void BuildFloatingIceFloe()
        {
            var b = new ToonObjBuilder("Toon_Floating_IceFloe");

            // 1. Ice Floe Translucent Submerged Rim (Vành băng ngập trong nước)
            b.AddTaperedCylinder(new Vector3(0f, -0.15f, 0f), 2.5f, 2.8f, 0.32f, ToonColor.IceBlue, 8);

            // 2. Snow Surface Crust (Mặt băng phủ lớp tuyết phẳng)
            b.AddCylinder(new Vector3(0f, 0.04f, 0f), 2.75f, 0.08f, ToonColor.PlasterWhite, 8);

            // 3. Small Ice Chunks on Rim (Các khối băng vụn bám mép)
            b.AddBox(new Vector3( 1.35f, 0.16f,  0.80f), new Vector3(0.60f, 0.24f, 0.50f), ToonColor.IceBlue);
            b.AddBox(new Vector3( 1.35f, 0.29f,  0.80f), new Vector3(0.62f, 0.06f, 0.52f), ToonColor.PlasterWhite);

            b.AddBox(new Vector3(-1.20f, 0.14f, -0.75f), new Vector3(0.50f, 0.20f, 0.45f), ToonColor.IceBlue);
            b.AddBox(new Vector3(-1.20f, 0.25f, -0.75f), new Vector3(0.52f, 0.06f, 0.47f), ToonColor.PlasterWhite);

            b.SaveObj(Path.Combine(OutDir, "Toon_Floating_IceFloe.obj"));
        }

        // ==========================================
        // 28. ARCTIC IGLOO (LỀU BĂNG TUYẾT VÙNG CỰC)
        // ==========================================
        public static void BuildArcticIgloo()
        {
            var b = new ToonObjBuilder("Toon_Arctic_Igloo");

            // 1. Snow Block Dome Tiers (Vòm tuyết hình bán cầu phân tầng)
            b.AddCylinder(new Vector3(0f, 0.45f, 0f), 2.2f, 0.90f, ToonColor.PlasterWhite, 10);
            b.AddCylinder(new Vector3(0f, 1.25f, 0f), 1.8f, 0.70f, ToonColor.PlasterWhite, 10);
            b.AddCylinder(new Vector3(0f, 1.85f, 0f), 1.2f, 0.50f, ToonColor.PlasterWhite, 10);
            b.AddCone(new Vector3(0f, 2.15f, 0f), 0.75f, 0.35f, ToonColor.PlasterWhite, 8);

            // 2. Tunnel Vault Entrance (Đường hầm vòm tuyết lối vào)
            b.AddBox(new Vector3(0f, 0.55f, -1.8f), new Vector3(1.3f, 1.1f, 1.4f), ToonColor.PlasterWhite);
            // Dark interior doorway opening
            b.AddBox(new Vector3(0f, 0.50f, -2.52f), new Vector3(0.75f, 0.95f, 0.06f), ToonColor.CastIron);

            // 3. Smoke Hole Vent on Top
            b.AddCylinder(new Vector3(0f, 2.32f, 0f), 0.18f, 0.12f, ToonColor.DarkIron, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_Igloo.obj"));
        }

        // ==========================================
        // 29. AUTUMN HARVEST BARREL (THÙNG GỖ TRÁI CÂY MÙA THU)
        // ==========================================
        public static void BuildAutumnHarvestBarrel()
        {
            var b = new ToonObjBuilder("Toon_Autumn_HarvestBarrel");

            // 1. Wooden Barrel Body
            b.AddTaperedCylinder(new Vector3(0f, 0.45f, 0f), 0.42f, 0.45f, 0.90f, ToonColor.WarmWood, 8);

            // 2. Cast Iron Hoops
            b.AddCylinder(new Vector3(0f, 0.18f, 0f), 0.46f, 0.06f, ToonColor.CastIron, 8);
            b.AddCylinder(new Vector3(0f, 0.72f, 0f), 0.46f, 0.06f, ToonColor.CastIron, 8);

            // 3. Overflowing Autumn Harvest Apples
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 0.22f, 0.94f, Mathf.Sin(a) * 0.22f);
                ToonColor c = (i % 2 == 0) ? ToonColor.Terracotta : ToonColor.SunlightGold;
                b.AddOvoid(pos, new Vector3(0.12f, 0.12f, 0.12f), c, 6, 3);
            }
            b.AddOvoid(new Vector3(0f, 0.98f, 0f), new Vector3(0.13f, 0.13f, 0.13f), ToonColor.Terracotta, 6, 3);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_HarvestBarrel.obj"));
        }

        // ==========================================
        // 30. AUTUMN PUMPKIN (BÍ NGÔ MÙA THU VÀNG CAM)
        // ==========================================
        public static void BuildAutumnPumpkin()
        {
            var b = new ToonObjBuilder("Toon_Autumn_Pumpkin");

            // Ribbed Gourd Body
            b.AddOvoid(new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.24f, 0.36f), ToonColor.Terracotta, 8, 4);

            // Stem
            b.AddCylinder(new Vector3(0f, 0.48f, 0f), 0.045f, 0.12f, ToonColor.DeepMoss, 5);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_Pumpkin.obj"));
        }

        // ==========================================
        // 31. AUTUMN HARVEST CART (XE KÉO NÔNG SẢN MÙA THU)
        // ==========================================
        public static void BuildAutumnHarvestCart()
        {
            var b = new ToonObjBuilder("Toon_Autumn_HarvestCart");

            // 1. Wooden Wagon Chassis & Floor Planks
            b.AddBox(new Vector3(0f, 0.42f, 0f), new Vector3(1.30f, 0.10f, 2.10f), ToonColor.WarmWood);

            // 2. Wagon Bed Slatted Side Walls
            b.AddBox(new Vector3(-0.62f, 0.70f, 0f), new Vector3(0.08f, 0.48f, 2.10f), ToonColor.WarmWood);
            b.AddBox(new Vector3( 0.62f, 0.70f, 0f), new Vector3(0.08f, 0.48f, 2.10f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 0.70f, -1.02f), new Vector3(1.30f, 0.48f, 0.08f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 0.70f,  1.02f), new Vector3(1.30f, 0.48f, 0.08f), ToonColor.WarmWood);

            // Corner Support Stakes
            b.AddBox(new Vector3(-0.62f, 0.74f, -1.02f), new Vector3(0.12f, 0.60f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.62f, 0.74f, -1.02f), new Vector3(0.12f, 0.60f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-0.62f, 0.74f,  1.02f), new Vector3(0.12f, 0.60f, 0.12f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.62f, 0.74f,  1.02f), new Vector3(0.12f, 0.60f, 0.12f), ToonColor.DarkTimber);

            // 3. Two Large Spoked Wheels with Iron Rims
            b.AddWheelX(new Vector3(-0.76f, 0.42f, -0.15f), 0.42f, 0.14f, ToonColor.CastIron, ToonColor.DarkTimber, 8);
            b.AddWheelX(new Vector3( 0.76f, 0.42f, -0.15f), 0.42f, 0.14f, ToonColor.CastIron, ToonColor.DarkTimber, 8);

            // Transverse Wheel Axle
            b.AddBox(new Vector3(0f, 0.42f, -0.15f), new Vector3(1.60f, 0.10f, 0.10f), ToonColor.CastIron);

            // 4. Tow Handle / Pulling Shaft
            b.AddBox(new Vector3(-0.35f, 0.28f, 1.65f), new Vector3(0.08f, 0.08f, 1.30f), ToonColor.HoneyWood);
            b.AddBox(new Vector3( 0.35f, 0.28f, 1.65f), new Vector3(0.08f, 0.08f, 1.30f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(0f, 0.28f, 2.30f), new Vector3(0.85f, 0.08f, 0.08f), ToonColor.HoneyWood);
            // Front Ground Kickstand Peg
            b.AddBox(new Vector3(0f, 0.18f, 1.05f), new Vector3(0.12f, 0.36f, 0.12f), ToonColor.DarkTimber);

            // 5. Overflowing Harvest Cargo (Golden Straw, Big Pumpkins, Apple Bushel)
            b.AddBox(new Vector3(0f, 0.58f, 0f), new Vector3(1.10f, 0.25f, 1.90f), ToonColor.SunlightGold);

            // Harvest Pumpkins nestled inside
            b.AddOvoid(new Vector3(-0.25f, 0.82f, -0.45f), new Vector3(0.28f, 0.22f, 0.28f), ToonColor.Terracotta, 6, 3);
            b.AddOvoid(new Vector3( 0.28f, 0.84f,  0.35f), new Vector3(0.30f, 0.24f, 0.30f), ToonColor.Terracotta, 6, 3);
            b.AddOvoid(new Vector3( 0.22f, 0.80f, -0.38f), new Vector3(0.25f, 0.20f, 0.25f), ToonColor.SunlightGold, 6, 3);
            b.AddOvoid(new Vector3(-0.28f, 0.85f,  0.42f), new Vector3(0.26f, 0.22f, 0.26f), ToonColor.Terracotta, 6, 3);

            // Bushel crate of apples
            b.AddBox(new Vector3(0f, 0.80f, 0.05f), new Vector3(0.48f, 0.30f, 0.48f), ToonColor.HoneyWood);
            b.AddOvoid(new Vector3(-0.10f, 0.98f,  0.05f), new Vector3(0.10f, 0.10f, 0.10f), ToonColor.Terracotta, 5, 3);
            b.AddOvoid(new Vector3( 0.10f, 0.98f,  0.05f), new Vector3(0.10f, 0.10f, 0.10f), ToonColor.SunlightGold, 5, 3);
            b.AddOvoid(new Vector3( 0.00f, 1.02f, -0.06f), new Vector3(0.10f, 0.10f, 0.10f), ToonColor.Terracotta, 5, 3);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_HarvestCart.obj"));
        }

        // ==========================================
        // 32. AUTUMN CAMPFIRE (BẾP LỬA TRẠI SƯỞI ẤM MÙA THU)
        // ==========================================
        public static void BuildAutumnCampfire()
        {
            var b = new ToonObjBuilder("Toon_Autumn_Campfire");

            // 1. Stone Hearth Ring (8 Cobblestones)
            for (int i = 0; i < 8; i++)
            {
                float a = (i / 8f) * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.80f, 0.12f, Mathf.Sin(a) * 0.80f);
                ToonColor c = (i % 2 == 0) ? ToonColor.LightStone : ToonColor.MediumStone;
                b.AddOvoid(p, new Vector3(0.18f, 0.14f, 0.18f), c, 5, 3);
            }

            // 2. Charred Timber Logs Crossed in Center
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(0.14f, 0.14f, 1.10f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.22f, 0f), new Vector3(1.10f, 0.14f, 0.14f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.26f, 0f), new Vector3(0.78f, 0.13f, 0.78f), ToonColor.CastIron);

            // 3. Glowing Fire & Ember Cones
            b.AddCone(new Vector3(0f, 0.25f, 0f), 0.48f, 0.42f, ToonColor.Terracotta, 6);
            b.AddCone(new Vector3(0f, 0.35f, 0f), 0.30f, 0.38f, ToonColor.SunlightGold, 6);

            // 4. Two Sitting Tree Stumps around the Hearth
            b.AddCylinder(new Vector3(-1.30f, 0.22f,  0.10f), 0.34f, 0.44f, ToonColor.HoneyWood, 6);
            b.AddCylinder(new Vector3( 1.30f, 0.22f, -0.20f), 0.34f, 0.44f, ToonColor.HoneyWood, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_Campfire.obj"));
        }

        // ==========================================
        // 33. AUTUMN PICNIC TABLE (BÀN GHẾ GỖ DÃ NGOẠI MÙA THU)
        // ==========================================
        public static void BuildAutumnPicnicTable()
        {
            var b = new ToonObjBuilder("Toon_Autumn_PicnicTable");

            // 1. Heavy Timber Table Top
            b.AddBox(new Vector3(0f, 0.82f, 0f), new Vector3(1.25f, 0.10f, 2.30f), ToonColor.WarmWood);

            // 2. Sturdy A-Frame Leg Supports
            b.AddBox(new Vector3(0f, 0.38f, -0.80f), new Vector3(0.95f, 0.76f, 0.14f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.38f,  0.80f), new Vector3(0.95f, 0.76f, 0.14f), ToonColor.DarkTimber);

            // 3. Attached Flanking Benches
            b.AddBox(new Vector3(-0.85f, 0.44f, 0f), new Vector3(0.38f, 0.08f, 2.30f), ToonColor.HoneyWood);
            b.AddBox(new Vector3( 0.85f, 0.44f, 0f), new Vector3(0.38f, 0.08f, 2.30f), ToonColor.HoneyWood);

            // Bench Stretcher Beams
            b.AddBox(new Vector3(0f, 0.38f, -0.80f), new Vector3(1.90f, 0.08f, 0.10f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.38f,  0.80f), new Vector3(1.90f, 0.08f, 0.10f), ToonColor.DarkTimber);

            // 4. Picnic Props on Table (Apple Basket & Stone Jug)
            b.AddBox(new Vector3(0.20f, 0.96f, -0.35f), new Vector3(0.35f, 0.20f, 0.35f), ToonColor.HoneyWood);
            b.AddOvoid(new Vector3(0.20f, 1.10f, -0.35f), new Vector3(0.12f, 0.12f, 0.12f), ToonColor.Terracotta, 5, 3);
            b.AddCylinder(new Vector3(-0.25f, 1.00f, 0.40f), 0.12f, 0.28f, ToonColor.MediumStone, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_PicnicTable.obj"));
        }

        // ==========================================
        // 34. AUTUMN ROWBOAT (THUYỀN GỖ CÂU CÁ MÙA THU)
        // ==========================================
        public static void BuildAutumnRowboat()
        {
            var b = new ToonObjBuilder("Toon_Autumn_Rowboat");

            // 1. Keel & Hull Bottom
            b.AddBox(new Vector3(0f, 0.10f, 0f), new Vector3(0.85f, 0.12f, 2.80f), ToonColor.DarkTimber);

            // 2. Flanking Curved Planks (Port & Starboard)
            b.AddTaperedBox(new Vector3(-0.54f, 0.38f, 0f), new Vector3(0.12f, 0.45f, 2.90f), new Vector3(0.12f, 0.45f, 3.20f), ToonColor.WarmWood);
            b.AddTaperedBox(new Vector3( 0.54f, 0.38f, 0f), new Vector3(0.12f, 0.45f, 2.90f), new Vector3(0.12f, 0.45f, 3.20f), ToonColor.WarmWood);

            // 3. Stern Transom (Back Wall)
            b.AddBox(new Vector3(0f, 0.38f, -1.48f), new Vector3(1.10f, 0.45f, 0.12f), ToonColor.DarkTimber);

            // 4. Pointed Bow Stem (Front Wedge)
            b.AddWedgeRoof(new Vector3(0f, 0.38f, 1.62f), new Vector3(1.05f, 0.45f, 0.50f), ToonColor.WarmWood);

            // 5. Wooden Thwart Seats (2 Benches)
            b.AddBox(new Vector3(0f, 0.36f, -0.45f), new Vector3(1.02f, 0.08f, 0.35f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(0f, 0.36f,  0.55f), new Vector3(0.92f, 0.08f, 0.35f), ToonColor.HoneyWood);

            // 6. Two Wooden Oars resting across gunwales
            b.AddBox(new Vector3(-0.75f, 0.54f, 0.20f), new Vector3(0.08f, 0.08f, 1.60f), ToonColor.HoneyWood);
            b.AddBox(new Vector3( 0.75f, 0.54f, -0.10f), new Vector3(0.08f, 0.08f, 1.60f), ToonColor.HoneyWood);

            // 7. Coiled Mooring Rope at Bow
            b.AddCylinder(new Vector3(0f, 0.42f, 1.35f), 0.22f, 0.08f, ToonColor.SunlightGold, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_Rowboat.obj"));
        }

        // ==========================================
        // 35. AUTUMN MUSHROOM CLUSTER (CỤM NẤM RỪNG MÙA THU)
        // ==========================================
        public static void BuildAutumnMushroomCluster()
        {
            var b = new ToonObjBuilder("Toon_Autumn_MushroomCluster");

            // 1. Mossy Earth Base
            b.AddOvoid(new Vector3(0f, 0.06f, 0f), new Vector3(0.55f, 0.12f, 0.55f), ToonColor.DeepMoss, 6, 2);

            // 2. Large Red Toadstool Mushroom
            b.AddCylinder(new Vector3(0f, 0.24f, 0f), 0.08f, 0.36f, ToonColor.PlasterWhite, 6);
            b.AddOvoid(new Vector3(0f, 0.46f, 0f), new Vector3(0.26f, 0.18f, 0.26f), ToonColor.Terracotta, 6, 3);
            b.AddOvoid(new Vector3(0f, 0.54f, 0f), new Vector3(0.12f, 0.06f, 0.12f), ToonColor.PlasterWhite, 5, 2);

            // 3. Medium Golden Chanterelle Mushroom
            b.AddCylinder(new Vector3(0.24f, 0.18f, 0.16f), 0.06f, 0.26f, ToonColor.PlasterWhite, 6);
            b.AddCone(new Vector3(0.24f, 0.34f, 0.16f), 0.20f, 0.16f, ToonColor.SunlightGold, 6);

            // 4. Baby Button Mushroom
            b.AddCylinder(new Vector3(-0.20f, 0.12f, -0.14f), 0.05f, 0.18f, ToonColor.PlasterWhite, 6);
            b.AddOvoid(new Vector3(-0.20f, 0.22f, -0.14f), new Vector3(0.14f, 0.12f, 0.14f), ToonColor.Terracotta, 5, 3);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_MushroomCluster.obj"));
        }

        // ==========================================
        // 36. AUTUMN LANTERN POST (ĐÈN LỒNG GỖ ẤM ÁP MÙA THU)
        // ==========================================
        public static void BuildAutumnLanternPost()
        {
            var b = new ToonObjBuilder("Toon_Autumn_LanternPost");

            // 1. Stone Foundation Footing
            b.AddBox(new Vector3(0f, 0.16f, 0f), new Vector3(0.46f, 0.32f, 0.46f), ToonColor.MediumStone);

            // 2. Main Timber Post
            b.AddBox(new Vector3(0f, 1.45f, 0f), new Vector3(0.20f, 2.30f, 0.20f), ToonColor.DarkTimber);

            // 3. Iron Arm Bracket
            b.AddBox(new Vector3(0f, 2.45f, 0.32f), new Vector3(0.08f, 0.08f, 0.64f), ToonColor.CastIron);

            // 4. Black Iron Lantern Body
            b.AddCone(new Vector3(0f, 2.36f, 0.58f), 0.22f, 0.16f, ToonColor.CastIron, 4);
            b.AddBox(new Vector3(0f, 2.18f, 0.58f), new Vector3(0.22f, 0.24f, 0.22f), ToonColor.SunlightGold);
            b.AddBox(new Vector3(0f, 2.02f, 0.58f), new Vector3(0.24f, 0.06f, 0.24f), ToonColor.CastIron);

            b.SaveObj(Path.Combine(OutDir, "Toon_Autumn_LanternPost.obj"));
        }

        // ==========================================
        // 37. ARCTIC SNOWMAN (NGƯỜI TUYẾT BẮC CỰC)
        // ==========================================
        public static void BuildArcticSnowman()
        {
            var b = new ToonObjBuilder("Toon_Arctic_Snowman");

            // 1. Three Snow Spheres (Base, Torso, Head)
            b.AddOvoid(new Vector3(0f, 0.55f, 0f), new Vector3(0.68f, 0.55f, 0.68f), ToonColor.PlasterWhite, 8, 4);
            b.AddOvoid(new Vector3(0f, 1.36f, 0f), new Vector3(0.50f, 0.42f, 0.50f), ToonColor.PlasterWhite, 8, 4);
            b.AddOvoid(new Vector3(0f, 2.02f, 0f), new Vector3(0.36f, 0.32f, 0.36f), ToonColor.PlasterWhite, 8, 4);

            // 2. Coal Pebbles (Eyes & Smile)
            b.AddOvoid(new Vector3(-0.12f, 2.10f, 0.32f), new Vector3(0.04f, 0.04f, 0.04f), ToonColor.CastIron, 4, 2);
            b.AddOvoid(new Vector3( 0.12f, 2.10f, 0.32f), new Vector3(0.04f, 0.04f, 0.04f), ToonColor.CastIron, 4, 2);
            b.AddOvoid(new Vector3(-0.10f, 1.94f, 0.32f), new Vector3(0.03f, 0.03f, 0.03f), ToonColor.CastIron, 4, 2);
            b.AddOvoid(new Vector3( 0.00f, 1.92f, 0.34f), new Vector3(0.03f, 0.03f, 0.03f), ToonColor.CastIron, 4, 2);
            b.AddOvoid(new Vector3( 0.10f, 1.94f, 0.32f), new Vector3(0.03f, 0.03f, 0.03f), ToonColor.CastIron, 4, 2);

            // 3. Carrot Nose pointing forward
            b.AddCone(new Vector3(0f, 2.02f, 0.48f), 0.06f, 0.30f, ToonColor.Terracotta, 5);

            // 4. Red Knitted Winter Scarf
            b.AddTaperedCylinder(new Vector3(0f, 1.70f, 0f), 0.42f, 0.42f, 0.16f, ToonColor.Terracotta, 8);
            b.AddBox(new Vector3(0.18f, 1.45f, 0.38f), new Vector3(0.14f, 0.38f, 0.06f), ToonColor.Terracotta);

            // 5. Charcoal Top Hat with Red Ribbon
            b.AddCylinder(new Vector3(0f, 2.30f, 0f), 0.44f, 0.06f, ToonColor.CastIron, 8);
            b.AddCylinder(new Vector3(0f, 2.54f, 0f), 0.28f, 0.44f, ToonColor.DarkTimber, 8);
            b.AddCylinder(new Vector3(0f, 2.36f, 0f), 0.30f, 0.08f, ToonColor.Terracotta, 8);

            // 6. Tree Branch Arms
            b.AddBox(new Vector3(-0.70f, 1.45f, 0f), new Vector3(0.55f, 0.07f, 0.07f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.70f, 1.55f, 0f), new Vector3(0.55f, 0.07f, 0.07f), ToonColor.DarkTimber);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_Snowman.obj"));
        }

        // ==========================================
        // 38. ARCTIC BONFIRE (LỬA TRẠI SƯỞI ẤM BẮC CỰC CÓ NỒI HẦM)
        // ==========================================
        public static void BuildArcticBonfire()
        {
            var b = new ToonObjBuilder("Toon_Arctic_Bonfire");

            // 1. Melted Snow Dark Basin / Stone Ring
            b.AddCylinder(new Vector3(0f, 0.06f, 0f), 1.15f, 0.10f, ToonColor.MediumStone, 8);

            // 2. Iron Tripod Poles meeting at apex
            b.AddBox(new Vector3(-0.45f, 0.95f, -0.26f), new Vector3(0.08f, 1.85f, 0.08f), ToonColor.CastIron);
            b.AddBox(new Vector3( 0.45f, 0.95f, -0.26f), new Vector3(0.08f, 1.85f, 0.08f), ToonColor.CastIron);
            b.AddBox(new Vector3( 0.00f, 0.95f,  0.52f), new Vector3(0.08f, 1.85f, 0.08f), ToonColor.CastIron);

            // 3. Hanging Iron Cauldron / Stew Pot
            b.AddOvoid(new Vector3(0f, 0.95f, 0f), new Vector3(0.36f, 0.28f, 0.36f), ToonColor.CastIron, 6, 3);
            b.AddCylinder(new Vector3(0f, 1.12f, 0f), 0.38f, 0.06f, ToonColor.CastIron, 6);
            b.AddBox(new Vector3(0f, 1.48f, 0f), new Vector3(0.04f, 0.65f, 0.04f), ToonColor.CastIron);

            // 4. Roaring Fire beneath Pot
            b.AddBox(new Vector3(0f, 0.16f, 0f), new Vector3(0.14f, 0.14f, 1.10f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(1.10f, 0.14f, 0.14f), ToonColor.DarkTimber);
            b.AddCone(new Vector3(0f, 0.32f, 0f), 0.50f, 0.50f, ToonColor.Terracotta, 6);
            b.AddCone(new Vector3(0f, 0.44f, 0f), 0.32f, 0.42f, ToonColor.SunlightGold, 6);

            // 5. Snow Camp Sitting Logs
            b.AddBox(new Vector3(-1.45f, 0.22f, 0f), new Vector3(0.40f, 0.36f, 1.20f), ToonColor.DarkTimber);
            b.AddBox(new Vector3(-1.45f, 0.42f, 0f), new Vector3(0.42f, 0.06f, 1.22f), ToonColor.PlasterWhite);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_Bonfire.obj"));
        }

        // ==========================================
        // 39. ARCTIC EXPEDITION SLED (XE TRƯỢT TUYẾT THÁM HIỂM)
        // ==========================================
        public static void BuildArcticSled()
        {
            var b = new ToonObjBuilder("Toon_Arctic_Sled");

            // 1. Curved Wooden Runners
            b.AddBox(new Vector3(-0.45f, 0.08f, -0.10f), new Vector3(0.08f, 0.08f, 2.00f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.45f, 0.08f, -0.10f), new Vector3(0.08f, 0.08f, 2.00f), ToonColor.DarkTimber);
            // Front upturned curved tips
            b.AddBox(new Vector3(-0.45f, 0.24f, 1.05f), new Vector3(0.08f, 0.28f, 0.22f), ToonColor.DarkTimber);
            b.AddBox(new Vector3( 0.45f, 0.24f, 1.05f), new Vector3(0.08f, 0.28f, 0.22f), ToonColor.DarkTimber);

            // 2. Cargo Deck Platform
            b.AddBox(new Vector3(0f, 0.28f, -0.10f), new Vector3(0.88f, 0.08f, 1.80f), ToonColor.HoneyWood);

            // 3. Driver Push Handle at Back
            b.AddBox(new Vector3(0f, 0.72f, -1.02f), new Vector3(0.88f, 0.85f, 0.08f), ToonColor.DarkTimber);

            // 4. Cargo Packed on Sled:
            // Expedition Crate 1
            b.AddBox(new Vector3(0f, 0.54f, -0.45f), new Vector3(0.68f, 0.44f, 0.65f), ToonColor.WarmWood);
            b.AddBox(new Vector3(0f, 0.78f, -0.45f), new Vector3(0.70f, 0.06f, 0.67f), ToonColor.PlasterWhite);

            // Expedition Supply Bedroll / Tarp
            b.AddCylinder(new Vector3(0f, 0.52f, 0.35f), 0.24f, 0.72f, ToonColor.IceBlue, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_Sled.obj"));
        }

        // ==========================================
        // 40. ARCTIC SUPPLY CRATES & FUEL DRUMS (KHO HÀNG & THÙNG NHIÊN LIỆU)
        // ==========================================
        public static void BuildArcticSupplyCrates()
        {
            var b = new ToonObjBuilder("Toon_Arctic_SupplyCrates");

            // 1. Two Base Wooden Shipping Crates
            b.AddBox(new Vector3(-0.45f, 0.36f, 0f), new Vector3(0.72f, 0.72f, 0.72f), ToonColor.WarmWood);
            b.AddBox(new Vector3( 0.45f, 0.36f, -0.10f), new Vector3(0.68f, 0.72f, 0.68f), ToonColor.DarkTimber);

            // 2. Top Stacked Crate
            b.AddBox(new Vector3(-0.20f, 0.98f, -0.05f), new Vector3(0.60f, 0.55f, 0.60f), ToonColor.WarmWood);
            // Thick Snow Blanket on Top
            b.AddBox(new Vector3(-0.20f, 1.28f, -0.05f), new Vector3(0.64f, 0.08f, 0.64f), ToonColor.PlasterWhite);

            // 3. Red Fuel Drum with Snow Cap
            b.AddCylinder(new Vector3(0.85f, 0.45f, 0.48f), 0.32f, 0.90f, ToonColor.Terracotta, 8);
            b.AddCylinder(new Vector3(0.85f, 0.92f, 0.48f), 0.33f, 0.06f, ToonColor.PlasterWhite, 8);

            // 4. Blue Frost Drum
            b.AddCylinder(new Vector3(0.24f, 0.42f, 0.65f), 0.30f, 0.84f, ToonColor.IceBlue, 8);
            b.AddCylinder(new Vector3(0.24f, 0.86f, 0.65f), 0.31f, 0.06f, ToonColor.PlasterWhite, 8);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_SupplyCrates.obj"));
        }

        // ==========================================
        // 41. ARCTIC ICE SPIRE (CỘT PHA LÊ BĂNG NGẦM DƯỚI NƯỚC)
        // ==========================================
        public static void BuildArcticIceSpire()
        {
            var b = new ToonObjBuilder("Toon_Arctic_IceSpire");

            // 1. Submerged Ice Crystal Base
            b.AddCylinder(new Vector3(0f, 0.10f, 0f), 1.25f, 0.20f, ToonColor.IceBlue, 8);

            // 2. Main Glacial Ice Pillar (Towering Crystalline Spike)
            b.AddTaperedCylinder(new Vector3(0f, 1.10f, 0f), 0.70f, 0.45f, 2.00f, ToonColor.IceBlue, 6);
            b.AddCone(new Vector3(0f, 2.50f, 0f), 0.45f, 0.90f, ToonColor.IceBlue, 6);
            b.AddCone(new Vector3(0f, 2.85f, 0f), 0.22f, 0.35f, ToonColor.PlasterWhite, 6);

            // 3. Left Secondary Shard (Tilted Sharp Crystal)
            b.AddTaperedCylinder(new Vector3(-0.62f, 0.75f, 0.20f), 0.42f, 0.25f, 1.40f, ToonColor.IceBlue, 5);
            b.AddCone(new Vector3(-0.62f, 1.72f, 0.20f), 0.25f, 0.68f, ToonColor.IceBlue, 5);

            // 4. Right Secondary Shard
            b.AddTaperedCylinder(new Vector3( 0.55f, 0.58f, -0.25f), 0.38f, 0.22f, 1.10f, ToonColor.IceBlue, 5);
            b.AddCone(new Vector3( 0.55f, 1.38f, -0.25f), 0.22f, 0.60f, ToonColor.IceBlue, 5);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_IceSpire.obj"));
        }

        // ==========================================
        // 42. ARCTIC DIRECTIONAL SIGNPOST (BIỂN CHỈ DẪN BẮC CỰC)
        // ==========================================
        public static void BuildArcticSignpost()
        {
            var b = new ToonObjBuilder("Toon_Arctic_Signpost");

            // 1. Ground Snow Mound
            b.AddOvoid(new Vector3(0f, 0.16f, 0f), new Vector3(0.50f, 0.20f, 0.50f), ToonColor.PlasterWhite, 6, 3);

            // 2. Wooden Post
            b.AddCylinder(new Vector3(0f, 1.15f, 0f), 0.10f, 2.10f, ToonColor.DarkTimber, 6);

            // 3. Arrow Sign 1 (Pointing Left)
            b.AddBox(new Vector3(-0.35f, 1.85f, 0f), new Vector3(0.70f, 0.18f, 0.06f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(-0.35f, 1.96f, 0f), new Vector3(0.72f, 0.05f, 0.08f), ToonColor.PlasterWhite);

            // 4. Arrow Sign 2 (Pointing Right)
            b.AddBox(new Vector3(0.35f, 1.55f, 0f), new Vector3(0.70f, 0.18f, 0.06f), ToonColor.HoneyWood);
            b.AddBox(new Vector3(0.35f, 1.66f, 0f), new Vector3(0.72f, 0.05f, 0.08f), ToonColor.PlasterWhite);

            // 5. Snow Cap Apex
            b.AddCone(new Vector3(0f, 2.25f, 0f), 0.16f, 0.16f, ToonColor.PlasterWhite, 6);

            b.SaveObj(Path.Combine(OutDir, "Toon_Arctic_Signpost.obj"));
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

            var springMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Spring.mat");
            var summerMat = AssetDatabase.LoadAssetAtPath<Material>(SummerMatPath);
            var fallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Fall.mat");
            var winterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Winter.mat");
            var desertMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Desert.mat");
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>(GlassMatPath);
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);

            CreateStandardBuildingPrefab("Toon_Harbor_IntakeStorage", summerMat);
            CreateMarketDisplayCounterPrefab(summerMat, glassMat);
            CreateAquariumLuxuryTankPrefab(summerMat, glassMat, waterMat);
            CreateLighthouseBeaconPrefab(summerMat);
            CreateCargoTrawlerShipPrefab(summerMat);
            CreateShipPrefab("Toon_Ship_Level1_Trawler", summerMat, Vector3.zero, Vector3.zero);
            CreateShipPrefab("Toon_Ship_Level2_Cruiser", summerMat, Vector3.zero, Vector3.zero);
            CreateShipPrefab("Toon_Ship_Level3_SteamTrawler", summerMat, new Vector3(0.96f, 0.45f, -0.6f), new Vector3(0f, 90f, 15f), 0.5f);
            CreateShipPrefab("Toon_Ship_Level4_Icebreaker", winterMat ?? summerMat, new Vector3(1.14f, 0.65f, -0.6f), new Vector3(0f, 90f, 15f), 0.65f);
            CreateShipPrefab("Toon_Ship_Level5_RoyalFlagship", summerMat, new Vector3(1.29f, 0.75f, -0.6f), new Vector3(0f, 90f, 15f), 0.75f);
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
            CreateStandardBuildingPrefab("Toon_Tropical_PalmTree", summerMat);
            CreateStandardBuildingPrefab("Toon_Tropical_TikiBungalow", summerMat);
            CreateStandardBuildingPrefab("Toon_Harbor_PlazaMonument", summerMat);
            CreateStandardBuildingPrefab("Toon_Autumn_TimberLodge", fallMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Arctic_SnowCabin", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Desert_OasisPavilion", desertMat ?? summerMat);

            // Level 3 & Level 4 Signature Natural & Atmospheric Models
            BuildAutumnMapleTree();
            BuildArcticFrostedSpruce();
            BuildGlacialIceberg();
            BuildFloatingIceFloe();
            BuildArcticIgloo();
            BuildAutumnHarvestBarrel();
            BuildAutumnPumpkin();

            // Level 3 Rich Biome Additions
            BuildAutumnHarvestCart();
            BuildAutumnCampfire();
            BuildAutumnPicnicTable();
            BuildAutumnRowboat();
            BuildAutumnMushroomCluster();
            BuildAutumnLanternPost();

            // Level 4 Rich Biome Additions
            BuildArcticSnowman();
            BuildArcticBonfire();
            BuildArcticSled();
            BuildArcticSupplyCrates();
            BuildArcticIceSpire();
            BuildArcticSignpost();

            CreateStandardBuildingPrefab("Toon_Autumn_MapleTree", fallMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Arctic_FrostedSpruce", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Glacial_Iceberg", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Floating_IceFloe", winterMat ?? summerMat);
            CreateArcticIglooPrefab(winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Autumn_HarvestBarrel", fallMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Autumn_Pumpkin", fallMat ?? summerMat);

            // Level 3 Prefabs
            CreateStandardBuildingPrefab("Toon_Autumn_HarvestCart", fallMat ?? summerMat);
            CreateCampfirePrefab("Toon_Autumn_Campfire", fallMat ?? summerMat, new Color(1.0f, 0.58f, 0.18f), 2.2f, 6.0f);
            CreateStandardBuildingPrefab("Toon_Autumn_PicnicTable", fallMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Autumn_Rowboat", fallMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Autumn_MushroomCluster", fallMat ?? summerMat);
            CreateLanternPostPrefab("Toon_Autumn_LanternPost", fallMat ?? summerMat, new Color(1.0f, 0.68f, 0.22f), 2.0f, 5.5f);

            // Level 4 Prefabs
            CreateStandardBuildingPrefab("Toon_Arctic_Snowman", winterMat ?? summerMat);
            CreateCampfirePrefab("Toon_Arctic_Bonfire", winterMat ?? summerMat, new Color(1.0f, 0.62f, 0.20f), 2.8f, 7.5f);
            CreateStandardBuildingPrefab("Toon_Arctic_Sled", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Arctic_SupplyCrates", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Arctic_IceSpire", winterMat ?? summerMat);
            CreateStandardBuildingPrefab("Toon_Arctic_Signpost", winterMat ?? summerMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ToonBuildingGenerator] All Toon Building Prefabs generated successfully in " + PrefabDir);
        }

        private static void CreateCampfirePrefab(string name, Material mat, Color lightColor, float intensity, float range)
        {
            var root = CreatePrefabRootWithModel(name, mat);
            var lightObj = new GameObject("Campfire_PointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightColor;
            l.range = range;
            l.intensity = intensity;

            string path = $"{PrefabDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateLanternPostPrefab(string name, Material mat, Color lightColor, float intensity, float range)
        {
            var root = CreatePrefabRootWithModel(name, mat);
            var lightObj = new GameObject("Lantern_PointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.20f, 0.58f);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightColor;
            l.range = range;
            l.intensity = intensity;

            string path = $"{PrefabDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateArcticIglooPrefab(Material mat)
        {
            var root = CreatePrefabRootWithModel("Toon_Arctic_Igloo", mat);
            var lightObj = new GameObject("Campfire_PointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.45f, -0.6f);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.65f, 0.25f);
            l.range = 6.5f;
            l.intensity = 2.4f;

            string path = $"{PrefabDir}/Toon_Arctic_Igloo.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
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

            // 5 Panoramic Glass Panes (Front, Back, Left, Right, Top)
            if (glassMat != null)
            {
                // Front
                var gFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gFront.name = "Glass_Front";
                gFront.transform.SetParent(root.transform, false);
                gFront.transform.localPosition = new Vector3(0f, 1.49f, -1.29f);
                gFront.transform.localScale = new Vector3(3.30f, 1.70f, 0.02f);
                var mrFront = gFront.GetComponent<MeshRenderer>();
                mrFront.sharedMaterial = glassMat;
                mrFront.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrFront.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(gFront.GetComponent<Collider>());

                // Back
                var gBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gBack.name = "Glass_Back";
                gBack.transform.SetParent(root.transform, false);
                gBack.transform.localPosition = new Vector3(0f, 1.49f, 1.29f);
                gBack.transform.localScale = new Vector3(3.30f, 1.70f, 0.02f);
                var mrBack = gBack.GetComponent<MeshRenderer>();
                mrBack.sharedMaterial = glassMat;
                mrBack.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrBack.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(gBack.GetComponent<Collider>());

                // Left
                var gLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gLeft.name = "Glass_Left";
                gLeft.transform.SetParent(root.transform, false);
                gLeft.transform.localPosition = new Vector3(-1.64f, 1.49f, 0f);
                gLeft.transform.localScale = new Vector3(0.02f, 1.70f, 2.60f);
                var mrLeft = gLeft.GetComponent<MeshRenderer>();
                mrLeft.sharedMaterial = glassMat;
                mrLeft.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrLeft.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(gLeft.GetComponent<Collider>());

                // Right
                var gRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gRight.name = "Glass_Right";
                gRight.transform.SetParent(root.transform, false);
                gRight.transform.localPosition = new Vector3(1.64f, 1.49f, 0f);
                gRight.transform.localScale = new Vector3(0.02f, 1.70f, 2.60f);
                var mrRight = gRight.GetComponent<MeshRenderer>();
                mrRight.sharedMaterial = glassMat;
                mrRight.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrRight.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(gRight.GetComponent<Collider>());

                // Top (Transparent glass lid)
                var gTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gTop.name = "Glass_Top";
                gTop.transform.SetParent(root.transform, false);
                gTop.transform.localPosition = new Vector3(0f, 2.34f, 0f);
                gTop.transform.localScale = new Vector3(3.30f, 0.02f, 2.60f);
                var mrTop = gTop.GetComponent<MeshRenderer>();
                mrTop.sharedMaterial = glassMat;
                mrTop.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrTop.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(gTop.GetComponent<Collider>());
            }

            // Water Volume
            if (waterMat != null)
            {
                var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
                water.name = "Water_Volume";
                water.transform.SetParent(root.transform, false);
                water.transform.localPosition = new Vector3(0f, 1.48f, 0f);
                water.transform.localScale = new Vector3(3.26f, 1.62f, 2.56f);
                var mrWater = water.GetComponent<MeshRenderer>();
                mrWater.sharedMaterial = waterMat;
                mrWater.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrWater.receiveShadows = false;
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

        private static void CreateShipPrefab(string modelName, Material mat, Vector3 ladderLocalPos, Vector3 ladderRot, float ladderScale = 0.9f)
        {
            var root = CreatePrefabRootWithModel(modelName, mat);

            if (ladderLocalPos != Vector3.zero)
            {
                var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Prefabs/Props/Ladder.prefab");
                if (ladderPrefab != null)
                {
                    var ladder = UnityEngine.Object.Instantiate(ladderPrefab, root.transform);
                    ladder.name = "Boarding_Ladder";
                    ladder.transform.localPosition = ladderLocalPos;
                    ladder.transform.localRotation = Quaternion.Euler(ladderRot);
                    ladder.transform.localScale = Vector3.one * ladderScale;
                }
            }

            string path = $"{PrefabDir}/{modelName}.prefab";
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
