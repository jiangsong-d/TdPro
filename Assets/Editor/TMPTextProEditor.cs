#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro.EditorUtilities;

namespace Framework.Language.Editor
{
    /// <summary>
    /// TMPTextPro自定义Inspector
    /// 提供更友好的编辑器界面
    /// </summary>
    [CustomEditor(typeof(TMPTextPro))]
    [CanEditMultipleObjects]
    public class TMPTextProEditor : TMP_EditorPanelUI
    {
        private SerializedProperty m_SourceTypeProp;
        private SerializedProperty m_LanguageKeyProp;
        private SerializedProperty m_AutoUpdateProp;
        
        private SerializedProperty m_UseCustomOutlineProp;
        private SerializedProperty m_CustomOutlineColorProp;
        private SerializedProperty m_CustomOutlineWidthProp;
        
        private SerializedProperty m_UseCustomGradientProp;
        private SerializedProperty m_GradientTopColorProp;
        private SerializedProperty m_GradientBottomColorProp;
        
        private SerializedProperty m_UseCustomShadowProp;
        private SerializedProperty m_CustomShadowColorProp;
        private SerializedProperty m_CustomShadowOffsetProp;

        private bool m_ShowLanguageSettings = true;
        private bool m_ShowCustomEffects = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // 获取自定义属性
            m_SourceTypeProp = serializedObject.FindProperty("m_SourceType");
            m_LanguageKeyProp = serializedObject.FindProperty("m_LanguageKey");
            m_AutoUpdateProp = serializedObject.FindProperty("m_AutoUpdate");
            
            m_UseCustomOutlineProp = serializedObject.FindProperty("m_UseCustomOutline");
            m_CustomOutlineColorProp = serializedObject.FindProperty("m_CustomOutlineColor");
            m_CustomOutlineWidthProp = serializedObject.FindProperty("m_CustomOutlineWidth");
            
            m_UseCustomGradientProp = serializedObject.FindProperty("m_UseCustomGradient");
            m_GradientTopColorProp = serializedObject.FindProperty("m_GradientTopColor");
            m_GradientBottomColorProp = serializedObject.FindProperty("m_GradientBottomColor");
            
            m_UseCustomShadowProp = serializedObject.FindProperty("m_UseCustomShadow");
            m_CustomShadowColorProp = serializedObject.FindProperty("m_CustomShadowColor");
            m_CustomShadowOffsetProp = serializedObject.FindProperty("m_CustomShadowOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制增强功能区域
            DrawEnhancedSettings();
            
            // 立即应用自定义设置的修改，但不重新Update避免刷新
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(10);
            
            // 绘制原有的TextMeshPro属性
            base.OnInspectorGUI();
        }

        private void DrawEnhancedSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("增强功能设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 多语言设置
            m_ShowLanguageSettings = EditorGUILayout.Foldout(m_ShowLanguageSettings, "多语言设置", true);
            if (m_ShowLanguageSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SourceTypeProp, new GUIContent("语言源类型"));
                EditorGUILayout.PropertyField(m_LanguageKeyProp, new GUIContent("语言Key"));
                
                // 检测到Key或SourceType变化时，立即预览文本
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    PreviewLanguageText();
                }
                
                EditorGUILayout.PropertyField(m_AutoUpdateProp, new GUIContent("自动更新"));
                
                // 显示预览文本
                if (!string.IsNullOrWhiteSpace(m_LanguageKeyProp.stringValue) && 
                    m_SourceTypeProp.enumValueIndex != 0) // 不是None
                {
                    string previewText = GetPreviewText();
                    if (!string.IsNullOrEmpty(previewText))
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField("预览文本:", EditorStyles.miniLabel);
                        EditorGUILayout.HelpBox(previewText, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.HelpBox($"未找到Key: {m_LanguageKeyProp.stringValue}", MessageType.Warning);
                    }
                }
                
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("刷新语言文本"))
                    {
                        TMPTextPro textPro = target as TMPTextPro;
                        textPro?.RefreshLanguageText();
                    }
                }
                else
                {
                    // 编辑器模式下提供预览按钮
                    if (GUILayout.Button("应用预览文本"))
                    {
                        PreviewLanguageText();
                    }
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // 自定义效果设置
            m_ShowCustomEffects = EditorGUILayout.Foldout(m_ShowCustomEffects, "自定义效果", true);
            if (m_ShowCustomEffects)
            {
                EditorGUI.indentLevel++;
                
                // 使用 BeginChangeCheck 只在实际修改时触发更新
                EditorGUI.BeginChangeCheck();
                
                // 描边
                EditorGUILayout.PropertyField(m_UseCustomOutlineProp, new GUIContent("使用自定义描边"));
                if (m_UseCustomOutlineProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_CustomOutlineColorProp, new GUIContent("描边颜色"));
                    EditorGUILayout.PropertyField(m_CustomOutlineWidthProp, new GUIContent("描边宽度"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(3);
                
                // 渐变
                EditorGUILayout.PropertyField(m_UseCustomGradientProp, new GUIContent("使用自定义渐变"));
                if (m_UseCustomGradientProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_GradientTopColorProp, new GUIContent("顶部颜色"));
                    EditorGUILayout.PropertyField(m_GradientBottomColorProp, new GUIContent("底部颜色"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(3);
                
                // 阴影
                EditorGUILayout.PropertyField(m_UseCustomShadowProp, new GUIContent("使用自定义阴影"));
                if (m_UseCustomShadowProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_CustomShadowColorProp, new GUIContent("阴影颜色"));
                    EditorGUILayout.PropertyField(m_CustomShadowOffsetProp, new GUIContent("阴影偏移"));
                    EditorGUI.indentLevel--;
                }
                
                // 如果修改了任何参数，手动应用效果实现实时预览
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    
                    TMPTextPro textPro = target as TMPTextPro;
                    if (textPro != null)
                    {
                        textPro.EditorApplyCustomEffects();
                        EditorUtility.SetDirty(textPro);
                    }
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 获取预览文本
        /// </summary>
        private string GetPreviewText()
        {
            if (string.IsNullOrWhiteSpace(m_LanguageKeyProp.stringValue))
                return "";

            string key = m_LanguageKeyProp.stringValue.Trim();
            LanguageSourceType sourceType = (LanguageSourceType)m_SourceTypeProp.enumValueIndex;

            try
            {
                switch (sourceType)
                {
                    case LanguageSourceType.Language:
                        return LanguageManager.GetLangVal(key);
                    case LanguageSourceType.LanguagePack:
                        return LanguageManager.GetLangPackVal(key);
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 预览语言文本（编辑器模式）
        /// </summary>
        private void PreviewLanguageText()
        {
            TMPTextPro textPro = target as TMPTextPro;
            if (textPro == null) return;

            string previewText = GetPreviewText();
            if (!string.IsNullOrEmpty(previewText))
            {
                // 替换换行符
                previewText = previewText.Replace("\\n", "\n");
                
                // 在编辑器中直接更新text属性
                SerializedProperty textProp = serializedObject.FindProperty("m_text");
                if (textProp != null)
                {
                    textProp.stringValue = previewText;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(textPro);
                }
            }
        }

    }
}
#endif
