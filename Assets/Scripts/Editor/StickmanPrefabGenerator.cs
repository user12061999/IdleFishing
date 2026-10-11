using System.IO;
using UnityEngine;
using UnityEditor;
using IdleFishing.Characters;

namespace IdleFishing.Editor
{
    public static class StickmanPrefabGenerator
    {
        private const string OutputDir = "Assets/Prefabs/Characters";
        private const string MatDir = "Assets/EKstudio/HyperCasual Character Stickman/Material";
        public const float CharacterScale = 1.45f;

        [MenuItem("IdleFishing/Generate Stickman Character Prefabs")]
        public static void GenerateAllPrefabs()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            CreateFishermanPrefab();
            CreateWranglerPrefab();
            CreateTransporterPrefab();
            CreateCustomerShopperPrefab();
            CreateCustomerVIPPrefab();
            CreateCustomerTouristPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StickmanPrefabGenerator] Successfully generated all 6 Stickman Character Prefabs at 1.45x scale!");
        }

        private static GameObject InstantiateStickmanBase(string sourcePrefabPath, string name, float scale = CharacterScale)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            if (src == null)
            {
                Debug.LogError("Source stickman prefab not found: " + sourcePrefabPath);
                return null;
            }

            var root = new GameObject(name);
            var visual = Object.Instantiate(src, root.transform);
            visual.name = "Model_Stickman";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;

            // Turn off default Animator so procedural kinematics can animate cleanly
            var anim = visual.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;

            return root;
        }

        private static void CreateFishermanPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_1.prefab",
                "Stickman_Fisherman",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.SittingFisherman);
            cartoonVis.Initialize(true);

            // 1. Yellow Bucket Hat on Head
            var headSocket = cartoonVis.HeadSocket;
            if (headSocket != null)
            {
                var hatRoot = new GameObject("Fisherman_BucketHat");
                hatRoot.transform.SetParent(headSocket, false);
                hatRoot.transform.localPosition = new Vector3(0f, 0.18f, 0f);

                var yellowMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Yellow.mat"));

                // Hat Brim
                var brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                brim.name = "Hat_Brim";
                brim.transform.SetParent(hatRoot.transform, false);
                brim.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                brim.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
                if (yellowMat != null) brim.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;
                Object.DestroyImmediate(brim.GetComponent<Collider>());

                // Hat Crown
                var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crown.name = "Hat_Crown";
                crown.transform.SetParent(hatRoot.transform, false);
                crown.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                crown.transform.localScale = new Vector3(0.35f, 0.065f, 0.35f);
                if (yellowMat != null) crown.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;
                Object.DestroyImmediate(crown.GetComponent<Collider>());
            }

            // 2. Cartoon Fishing Rod on Right Hand (palm-pivoted so rod rotation stays locked in hand)
            var rHandSocket = cartoonVis.RightHandSocket;
            if (rHandSocket != null)
            {
                var rod = new GameObject("FishingRod");
                rod.transform.SetParent(rHandSocket, false);
                rod.transform.localPosition = Vector3.zero;
                rod.transform.localRotation = Quaternion.Euler(48f, 0f, 0f);
                rod.transform.localScale = Vector3.one;

                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Rod_Shaft";
                shaft.transform.SetParent(rod.transform, false);
                shaft.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                shaft.transform.localRotation = Quaternion.identity;
                shaft.transform.localScale = new Vector3(0.05f, 0.85f, 0.05f);

                var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_ToyPierWood.mat");
                if (woodMat == null) woodMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Brown.mat"));
                if (woodMat != null) shaft.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(shaft.GetComponent<Collider>());

                // Rod Tip
                var tip = new GameObject("RodTip");
                tip.transform.SetParent(shaft.transform, false);
                tip.transform.localPosition = new Vector3(0, 1.0f, 0);

                // Rod Reel
                var reel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reel.name = "Reel";
                reel.transform.SetParent(shaft.transform, false);
                reel.transform.localPosition = new Vector3(0.07f, -0.4f, 0);
                reel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                reel.transform.localScale = new Vector3(1.6f, 0.22f, 1.6f);
                var goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_GoldTrim.mat");
                if (goldMat != null) reel.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
                Object.DestroyImmediate(reel.GetComponent<Collider>());

                // Grip
                var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                grip.name = "Grip";
                grip.transform.SetParent(shaft.transform, false);
                grip.transform.localPosition = new Vector3(0, -0.65f, 0);
                grip.transform.localScale = new Vector3(1.15f, 0.35f, 1.15f);
                var blackMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Black.mat"));
                if (blackMat != null) grip.GetComponent<MeshRenderer>().sharedMaterial = blackMat;
                Object.DestroyImmediate(grip.GetComponent<Collider>());
            }

            // Collider
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Fisherman.prefab"));
        }

        private static void CreateWranglerPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_4.prefab",
                "Stickman_Wrangler",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.FishWrangler);
            cartoonVis.Initialize(true);

            // 1. Red Martial Arts Headband
            var headSocket = cartoonVis.HeadSocket;
            if (headSocket != null)
            {
                var redMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Red.mat"));
                var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = "Headband";
                band.transform.SetParent(headSocket, false);
                band.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                band.transform.localScale = new Vector3(0.36f, 0.035f, 0.36f);
                if (redMat != null) band.GetComponent<MeshRenderer>().sharedMaterial = redMat;
                Object.DestroyImmediate(band.GetComponent<Collider>());
            }

            // 2. Bat Weapon in Right Hand Socket (palm-pivoted)
            var rHandSocket = cartoonVis.RightHandSocket;
            if (rHandSocket != null)
            {
                var bat = new GameObject("Wrangler_Bat");
                bat.transform.SetParent(rHandSocket, false);
                bat.transform.localPosition = Vector3.zero;
                bat.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                bat.transform.localScale = Vector3.one;

                var batShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                batShaft.name = "Bat_Shaft";
                batShaft.transform.SetParent(bat.transform, false);
                batShaft.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                batShaft.transform.localRotation = Quaternion.identity;
                batShaft.transform.localScale = new Vector3(0.09f, 0.45f, 0.09f);

                var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_ToyPierWood.mat");
                if (woodMat == null) woodMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Brown.mat"));
                if (woodMat != null) batShaft.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(batShaft.GetComponent<Collider>());

                var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knob.name = "Knob";
                knob.transform.SetParent(batShaft.transform, false);
                knob.transform.localPosition = new Vector3(0, -1.05f, 0);
                knob.transform.localScale = new Vector3(1.5f, 0.4f, 1.5f);
                var goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_GoldTrim.mat");
                if (goldMat != null) knob.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
                Object.DestroyImmediate(knob.GetComponent<Collider>());
            }

            // 3. Carry Point at Back
            var carryPoint = new GameObject("CarryPoint_Back");
            var backSocket = cartoonVis.BackCarrySocket;
            if (backSocket != null)
            {
                carryPoint.transform.SetParent(backSocket, false);
                carryPoint.transform.localPosition = Vector3.zero;
            }
            else
            {
                carryPoint.transform.SetParent(root.transform, false);
                carryPoint.transform.localPosition = new Vector3(0f, 1.1f, -0.45f);
            }

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Wrangler.prefab"));
        }

        private static void CreateTransporterPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_4.prefab",
                "Stickman_Transporter",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.LogisticsTransporter);
            cartoonVis.Initialize(true);

            // Orange Work Cap on Head
            var headSocket = cartoonVis.HeadSocket;
            if (headSocket != null)
            {
                var orangeMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Orange.mat"));
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Transporter_Cap";
                cap.transform.SetParent(headSocket, false);
                cap.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                cap.transform.localScale = new Vector3(0.35f, 0.05f, 0.35f);
                if (orangeMat != null) cap.GetComponent<MeshRenderer>().sharedMaterial = orangeMat;
                Object.DestroyImmediate(cap.GetComponent<Collider>());

                var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Cap_Visor";
                visor.transform.SetParent(cap.transform, false);
                visor.transform.localPosition = new Vector3(0f, -0.2f, 0.85f);
                visor.transform.localScale = new Vector3(0.85f, 0.25f, 0.65f);
                if (orangeMat != null) visor.GetComponent<MeshRenderer>().sharedMaterial = orangeMat;
                Object.DestroyImmediate(visor.GetComponent<Collider>());
            }

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Transporter.prefab"));
        }

        private static void CreateCustomerShopperPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_5.prefab",
                "Stickman_Customer_Casual",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.CustomerOrTourist);
            cartoonVis.Initialize(true);

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Customer_Casual.prefab"));
        }

        private static void CreateCustomerVIPPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_2.prefab",
                "Stickman_Customer_VIP",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.CustomerOrTourist);
            cartoonVis.Initialize(true);

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Customer_VIP.prefab"));
        }

        private static void CreateCustomerTouristPrefab()
        {
            var root = InstantiateStickmanBase(
                "Assets/EKstudio/HyperCasual Character Stickman/Prefabs/Stickman_3.prefab",
                "Stickman_Customer_Tourist",
                CharacterScale
            );
            if (root == null) return;

            var visual = root.transform.Find("Model_Stickman");
            var cartoonVis = visual.gameObject.AddComponent<StickmanCartoonVisual>();
            cartoonVis.SetupRole(StickmanRole.CustomerOrTourist);
            cartoonVis.Initialize(true);

            // Flower / Hairclip in hair
            var headSocket = cartoonVis.HeadSocket;
            if (headSocket != null)
            {
                var redMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MatDir, "M_Red.mat"));
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "Tourist_FlowerClip";
                flower.transform.SetParent(headSocket, false);
                flower.transform.localPosition = new Vector3(0.18f, 0.16f, 0f);
                flower.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
                if (redMat != null) flower.GetComponent<MeshRenderer>().sharedMaterial = redMat;
                Object.DestroyImmediate(flower.GetComponent<Collider>());
            }

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.52f;
            col.height = 2.45f;
            col.center = new Vector3(0, 1.22f, 0);
            col.isTrigger = true;

            cartoonVis.ResetToBindPose();
            SaveAndDestroy(root, Path.Combine(OutputDir, "Stickman_Customer_Tourist.prefab"));
        }

        private static void SaveAndDestroy(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[StickmanPrefabGenerator] Created: " + path);
        }
    }
}
