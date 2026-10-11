Shader "IdleFishing/ToonStylizedWater"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Reef Color", Color) = (0.12, 0.78, 0.86, 0.70)
        _BaseColor ("Mid Ocean Color", Color) = (0.07, 0.52, 0.82, 0.85)
        _DeepColor ("Deep Ocean Color", Color) = (0.04, 0.28, 0.58, 0.95)
        _HorizonColor ("Far Horizon Tint", Color) = (0.16, 0.62, 0.90, 1.0)
        _DepthMaxDistance ("Depth Gradient Distance", Range(0.5, 10.0)) = 4.2

        [Header(Waves and Vertex Motion)]
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.4)) = 0.055
        _WaveFrequency ("Wave Frequency", Range(0.1, 4.0)) = 0.85
        _WaveSpeed ("Wave Speed", Range(0.1, 6.0)) = 1.65
        _SecondaryWaveScale ("Secondary Wave Scale", Range(0.1, 4.0)) = 1.45

        [Header(Water Flow and Current)]
        _FlowDirection ("Flow Direction (XZ)", Vector) = (0.94, 0.18, 0, 0)
        _FlowSpeed ("Flow Current Speed", Range(0.0, 3.0)) = 0.65
        _FlowStreakScale ("Flow Streak Scale", Range(0.05, 2.0)) = 0.45
        _FlowStreakIntensity ("Flow Streak Intensity", Range(0.0, 1.0)) = 0.48
        _FlowBandCenterZ ("Main Current Channel Z", Float) = 16.5
        _FlowBandWidth ("Main Current Channel Width", Float) = 7.5

        [Header(Toon Voronoi Caustics)]
        _CausticColor ("Caustic Highlight Color", Color) = (0.58, 0.96, 1.0, 0.32)
        _CausticScale ("Caustic Cell Scale", Range(0.1, 3.0)) = 0.65
        _CausticSpeed ("Caustic Animation Speed", Range(0.1, 3.0)) = 0.85
        _CausticThreshold ("Toon Cell Edge Threshold", Range(0.01, 0.25)) = 0.055
        _CausticSoftness ("Toon Cell Softness", Range(0.005, 0.15)) = 0.025

        [Header(Object Contour Foam)]
        _FoamColor ("Foam Color & Opacity", Color) = (0.97, 0.99, 1.0, 0.96)
        _ContourFoamThickness ("Contour Foam Width", Range(0.01, 1.0)) = 0.22
        _ContourInnerLine ("Contact Edge Width (Inner Line)", Range(0.005, 0.4)) = 0.075
        _ContourOuterLine ("Secondary Contour Offset (Outer Line)", Range(0.02, 0.6)) = 0.17
        _ContourOuterWidth ("Secondary Contour Line Width", Range(0.005, 0.2)) = 0.045
        _ContourNoiseScale ("Contour Organic Jitter Scale", Range(0.5, 10.0)) = 2.4
        _ContourNoiseStrength ("Contour Organic Jitter Strength", Range(0.0, 0.1)) = 0.028
        _ContourPulseSpeed ("Contour Wave Pulse Speed", Range(0.0, 8.0)) = 2.8
        _ContourCutoffDepth ("Contour Min Breaching Depth Y", Range(-3.0, 0.0)) = -0.75

        [Header(Harbor Shoreline Wave)]
        _ShorelineZ ("Harbor Shoreline Z", Float) = 10.15
        _ShoreFoamWidth ("Shoreline Foam Width", Range(0.1, 2.0)) = 0.65

        [Header(Sun Specular Sparkles)]
        _SpecularColor ("Specular Sparkle Color", Color) = (1.0, 0.98, 0.88, 1.0)
        _SpecularSize ("Toon Specular Cutoff", Range(0.8, 0.999)) = 0.972
        _SparkleIntensity ("Sparkle Intensity", Range(0.0, 2.0)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _HorizonColor;
                float _DepthMaxDistance;

                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _SecondaryWaveScale;

                float4 _FlowDirection;
                float _FlowSpeed;
                float _FlowStreakScale;
                float _FlowStreakIntensity;
                float _FlowBandCenterZ;
                float _FlowBandWidth;

                half4 _CausticColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticThreshold;
                float _CausticSoftness;

                half4 _FoamColor;
                float _ContourFoamThickness;
                float _ContourInnerLine;
                float _ContourOuterLine;
                float _ContourOuterWidth;
                float _ContourNoiseScale;
                float _ContourNoiseStrength;
                float _ContourPulseSpeed;
                float _ContourCutoffDepth;
                float _ShorelineZ;
                float _ShoreFoamWidth;

                half4 _SpecularColor;
                float _SpecularSize;
                float _SparkleIntensity;
            CBUFFER_END

            // Interactive Ripples (only when bobbers splash or player taps)
            int _RippleCount;
            float4 _RippleData[16];
            float4 _RippleParams[16];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                float  waveCrest  : TEXCOORD5;
            };

            // --- Procedural Noise Helpers ---
            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float2 VoronoiEdge(float2 uv, float timeOffset)
            {
                float2 n = floor(uv);
                float2 f = frac(uv);

                float f1 = 8.0;
                float f2 = 8.0;

                [unroll]
                for (int j = -1; j <= 1; j++)
                {
                    [unroll]
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 g = float2((float)i, (float)j);
                        float2 o = Hash22(n + g);
                        o = 0.5 + 0.42 * sin(timeOffset + 6.2831 * o);
                        float2 r = g - f + o;
                        float d = dot(r, r);

                        if (d < f1)
                        {
                            f2 = f1;
                            f1 = d;
                        }
                        else if (d < f2)
                        {
                            f2 = d;
                        }
                    }
                }
                f1 = sqrt(f1);
                f2 = sqrt(f2);
                return float2(f1, f2 - f1);
            }

            void EvaluateWaves(float2 worldXZ, float t, out float heightOffset, out float2 gradXZ, out float crestFactor)
            {
                float2 flowDir = normalize(_FlowDirection.xy + float2(0.0001, 0.0));
                float2 crossDir = float2(-flowDir.y, flowDir.x);

                float2 dir1 = normalize(flowDir * 0.75 + float2(0.0, -0.65));
                float phase1 = dot(worldXZ, dir1) * _WaveFrequency + t * _WaveSpeed;
                float w1 = sin(phase1);
                float dw1 = cos(phase1) * _WaveFrequency;

                float2 dir2 = normalize(crossDir * 0.65 + float2(0.25, 0.72));
                float freq2 = _WaveFrequency * _SecondaryWaveScale;
                float phase2 = dot(worldXZ, dir2) * freq2 - t * (_WaveSpeed * 1.18);
                float w2 = sin(phase2) * 0.65;
                float dw2 = cos(phase2) * freq2 * 0.65;

                float2 dir3 = normalize(flowDir - crossDir * 0.4);
                float freq3 = _WaveFrequency * 2.3;
                float phase3 = dot(worldXZ, dir3) * freq3 + t * (_WaveSpeed * 1.55);
                float w3 = cos(phase3) * 0.30;
                float dw3 = -sin(phase3) * freq3 * 0.30;

                float combined = (w1 + w2 + w3) / 1.95;
                heightOffset = combined * _WaveAmplitude;
                gradXZ = (dir1 * dw1 + dir2 * dw2 + dir3 * dw3) * (_WaveAmplitude / 1.95);
                crestFactor = saturate(combined * 0.5 + 0.5);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);

                float t = _Time.y;
                float waveH = 0.0;
                float2 waveGrad = float2(0.0, 0.0);
                float crest = 0.5;

                EvaluateWaves(posWS.xz, t, waveH, waveGrad, crest);

                float topMask = saturate(normWS.y * 0.8 + 0.2);
                posWS.y += waveH * topMask;

                float3 waveNormal = normalize(float3(-waveGrad.x * 2.2, 1.0, -waveGrad.y * 2.2));
                normWS = normalize(lerp(normWS, waveNormal, topMask));

                output.positionWS = posWS;
                output.normalWS = normWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.waveCrest = crest;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 worldXZ = input.positionWS.xz;
                float2 flowDir = normalize(_FlowDirection.xy + float2(0.0001, 0.0));
                float2 perpDir = float2(-flowDir.y, flowDir.x);

                // =========================================================
                // 1. EXACT 3D GEOMETRY RECONSTRUCTION FROM DEPTH
                // =========================================================
                float2 screenUV = input.screenPos.xy / max(0.0001, input.screenPos.w);
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEyeDepth = input.screenPos.w;
                float depthDiff = sceneEyeDepth - surfaceEyeDepth;

                // Reconstruct world position of the underwater geometry
                float3 sceneWorldPos = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);

                // True vertical water depth in world meters directly beneath this water pixel
                float verticalDepth = input.positionWS.y - sceneWorldPos.y;

                // Protect against background skybox / far plane
                bool isUnderwaterGeometry = (depthDiff > 0.0001) && (depthDiff < 35.0);

                // Only consider 3D objects that breach or sit near the surface (Y > _ContourCutoffDepth),
                // completely ignoring the deep seabed (Y <= -1.3m) so it NEVER creates foam!
                bool isBreaching3DObject = isUnderwaterGeometry && (sceneWorldPos.y > _ContourCutoffDepth);

                // Exact distance from 3D object's surface to the water surface
                float objectContourDist = isBreaching3DObject ? max(0.0, min(verticalDepth, depthDiff * 0.50)) : 999.0;

                // =========================================================
                // 2. WATER COLOR GRADIENT (Shallow Reef -> Mid Ocean -> Deep)
                // =========================================================
                // Base harbor depth gradient along Z (shoreline to open sea)
                float shoreDistZ = max(0.0, worldXZ.y - _ShorelineZ);
                float baseDepthFactor = smoothstep(0.0, _DepthMaxDistance * 2.8, shoreDistZ);

                // Also brighten water slightly near any 3D object (crystal-clear reef water)
                float objectProximity = isUnderwaterGeometry ? saturate(verticalDepth / 1.6) : 1.0;
                float combinedDepth = min(baseDepthFactor, objectProximity);

                half4 waterCol = lerp(_ShallowColor, _BaseColor, smoothstep(0.0, 0.48, combinedDepth));
                waterCol = lerp(waterCol, _DeepColor, smoothstep(0.42, 1.0, combinedDepth));

                // Wave crest highlight
                waterCol.rgb = lerp(waterCol.rgb, _ShallowColor.rgb * 1.15, (input.waveCrest - 0.5) * 0.22);

                // =========================================================
                // 3. DIRECTIONAL WATER FLOW CURRENT (Dòng nước chảy)
                // =========================================================
                float2 flowUV = float2(dot(worldXZ, flowDir), dot(worldXZ, perpDir));
                float meander = sin(flowUV.x * 0.45 - t * _FlowSpeed * 0.8) * 0.42;

                float2 streamCoord1 = float2(flowUV.x * 0.14 - t * _FlowSpeed * 0.95, (flowUV.y + meander) * 2.4);
                float2 streamCoord2 = float2(flowUV.x * 0.22 - t * _FlowSpeed * 1.35 + 11.7, (flowUV.y - meander * 0.6) * 3.4 + 5.3);

                float nStream1 = ValueNoise(streamCoord1);
                float nStream2 = ValueNoise(streamCoord2);

                float ribbon1 = smoothstep(0.72, 0.82, nStream1);
                float ribbon2 = smoothstep(0.75, 0.85, nStream2);
                float flowStreaks = saturate(ribbon1 * 1.1 + ribbon2 * 0.9);

                float channelMask = exp(-pow((worldXZ.y - _FlowBandCenterZ) / max(1.0, _FlowBandWidth), 2.0));
                half3 streamTint = lerp(_ShallowColor.rgb * 1.25, _FoamColor.rgb, 0.55);
                waterCol.rgb = lerp(waterCol.rgb, streamTint, saturate(flowStreaks * _FlowStreakIntensity * channelMask));

                // =========================================================
                // 4. TOON VORONOI CAUSTICS & SHIMMER
                // =========================================================
                float2 causticUV1 = worldXZ * _CausticScale - flowDir * (t * _FlowSpeed * 0.65);
                float2 causticUV2 = worldXZ * (_CausticScale * 1.35) - flowDir * (t * _FlowSpeed * 0.42) + float2(4.2, -3.1);

                float2 v1 = VoronoiEdge(causticUV1, t * _CausticSpeed);
                float2 v2 = VoronoiEdge(causticUV2, -t * _CausticSpeed * 0.75 + 2.0);

                float cellWeb1 = 1.0 - smoothstep(_CausticThreshold, _CausticThreshold + _CausticSoftness, v1.y);
                float cellWeb2 = 1.0 - smoothstep(_CausticThreshold * 0.85, _CausticThreshold * 0.85 + _CausticSoftness * 1.2, v2.y);

                half3 causticGlow = _CausticColor.rgb * saturate(cellWeb1 * 0.85 + cellWeb2 * 0.40) * _CausticColor.a;
                waterCol.rgb += causticGlow;

                // =========================================================
                // 5. TRUE 3D CONTOUR FOAM (ĐƯỜNG VIỀN KHỚP CHÍNH XÁC OBJECT 3D)
                // =========================================================
                // ZERO analytical circles! Foam is strictly driven by the physical 3D geometry intersection!
                float objectContourFoam = 0.0;
                if (isBreaching3DObject && objectContourDist < _ContourFoamThickness)
                {
                    // Subtle organic noise perturbation along the contour line
                    float contourJitter = (ValueNoise(worldXZ * _ContourNoiseScale + t * 0.35) - 0.5) * _ContourNoiseStrength;
                    float effectiveDist = max(0.0, objectContourDist + contourJitter);

                    // Line 1: Sharp inner contact rim right against the 3D polygon surface
                    float contactLine = 1.0 - smoothstep(0.0, _ContourInnerLine, effectiveDist);

                    // Line 2: Secondary thin contour line tracing the object's silhouette
                    float outerCenter = _ContourOuterLine;
                    float outerWidth = max(0.002, _ContourOuterWidth);
                    float outerLine = 1.0 - smoothstep(0.0, outerWidth, abs(effectiveDist - outerCenter));
                    // Gentle wave pulse on the secondary contour line
                    float pulse = sin(t * _ContourPulseSpeed - effectiveDist * 25.0) * 0.5 + 0.5;

                    objectContourFoam = saturate(contactLine * 1.15 + outerLine * pulse * 0.85);
                }

                // Harbor south shoreline foam wave (Z ≈ 10.15)
                float shoreWaveFoam = 0.0;
                if (shoreDistZ < _ShoreFoamWidth && shoreDistZ >= 0.0)
                {
                    float normShore = 1.0 - (shoreDistZ / _ShoreFoamWidth);
                    float shoreLine = smoothstep(0.78, 0.92, normShore);
                    float rollWave = sin(normShore * 8.0 - t * 3.5) * 0.5 + 0.5;
                    shoreWaveFoam = saturate(shoreLine + rollWave * normShore * 0.65);
                }

                float totalFoam = saturate(max(objectContourFoam, shoreWaveFoam));

                // =========================================================
                // 6. INTERACTIVE RIPPLES (Only for bobber splash/tap)
                // =========================================================
                float rippleFoam = 0.0;
                float2 rippleNormOffset = float2(0.0, 0.0);
                int rCount = clamp(_RippleCount, 0, 16);

                for (int ri = 0; ri < rCount; ri++)
                {
                    float4 rData = _RippleData[ri];
                    float4 rParam = _RippleParams[ri];
                    float age = rData.w;
                    if (age <= 0.0 || age >= 1.0) continue;

                    float2 delta = worldXZ - rData.xz;
                    float dist = length(delta);
                    float maxRad = max(0.25, rParam.x);
                    float currentRad = age * maxRad;

                    float packetWidth = lerp(0.22, 0.45, age);
                    float distFromFront = dist - currentRad;

                    if (abs(distFromFront) < packetWidth)
                    {
                        float env = 1.0 - saturate(abs(distFromFront) / packetWidth);
                        float fade = (1.0 - age) * (1.0 - age) * rParam.y;
                        float ringWave = cos(distFromFront * rParam.z * 1.5);
                        float toonRing = smoothstep(0.60, 0.85, ringWave) * env * fade;

                        rippleFoam = saturate(rippleFoam + toonRing);
                        float2 dir = dist > 0.001 ? (delta / dist) : float2(0.0, 1.0);
                        rippleNormOffset += dir * (-sin(distFromFront * rParam.z * 1.5) * env * fade * 0.35);
                    }
                }

                totalFoam = saturate(totalFoam + rippleFoam);

                // Blend crisp foam into water
                waterCol.rgb = lerp(waterCol.rgb, _FoamColor.rgb, totalFoam * _FoamColor.a);
                waterCol.a = saturate(lerp(waterCol.a, 0.98, totalFoam));

                // =========================================================
                // 7. TOON LIGHTING & SPECULAR SPARKLES
                // =========================================================
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS + float3(rippleNormOffset.x, 0.0, rippleNormOffset.y));
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 lightDirWS = normalize(mainLight.direction);

                float ndotl = dot(normalWS, lightDirWS) * 0.5 + 0.5;
                half3 lightTint = lerp(half3(0.65, 0.74, 0.88), mainLight.color, saturate(ndotl));
                waterCol.rgb *= lightTint;

                float3 facetNormal = normalize(normalWS + float3((v1.x - 0.5) * 0.30, 0.0, (v2.x - 0.5) * 0.30));
                float3 halfVec = normalize(lightDirWS + viewDirWS);
                float ndoth = saturate(dot(facetNormal, halfVec));
                float specStep = smoothstep(_SpecularSize, _SpecularSize + 0.010, ndoth);
                waterCol.rgb += specStep * _SpecularColor.rgb * mainLight.color * _SparkleIntensity * (1.0 - totalFoam);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.5);
                waterCol.rgb = lerp(waterCol.rgb, _HorizonColor.rgb * lightTint, fresnel * 0.20);

                waterCol.rgb = MixFog(waterCol.rgb, input.fogFactor);

                return waterCol;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
