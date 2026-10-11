using UnityEngine;
using UnityEditor;
using IdleFishing.World;

namespace IdleFishing.Editor
{
    [CustomEditor(typeof(OceanWaterFlowController))]
    public class OceanWaterFlowControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            OceanWaterFlowController controller = (OceanWaterFlowController)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "🌊 TOON STYLIZED WATER & CONTOUR FOAM CONTROLLER\n" +
                "• Chỉnh sửa trực tiếp các thông số bên dưới để thấy hiệu ứng cập nhật theo thời gian thực (Real-time Preview).\n" +
                "• Đường viền bọt ôm sát hình khối 3D (Scene Depth contour), không dùng hình tròn nhân tạo.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Save to Material Asset", GUILayout.Height(28)))
            {
                controller.ApplySettingsToMaterial();
            }
            if (GUILayout.Button("📥 Load from Material Asset", GUILayout.Height(28)))
            {
                controller.ReadSettingsFromMaterial();
                EditorUtility.SetDirty(controller);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("🔄 Rebuild Subdivided Water Mesh", GUILayout.Height(24)))
            {
                controller.RebuildWaterMesh();
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                controller.SyncShaderParameters();
                EditorUtility.SetDirty(controller);
            }
        }
    }
}
