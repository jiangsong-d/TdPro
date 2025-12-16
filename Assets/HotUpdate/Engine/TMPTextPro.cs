using TMPro;
using UnityEngine;
using System;

/// <summary>
/// 语言来源类型
/// </summary>
public enum LanguageSourceType
{
    [InspectorName("无(不使用多语言)")]
    None = 0,
    
    [InspectorName("Language表(策划多语言表)")]
    Language = 1,
    
    [InspectorName("LanguagePack表(程序多语言表)")]
    LanguagePack = 2
}

/// <summary>
/// TextMeshPro
/// </summary>
public class TMPTextPro : TextMeshProUGUI
{
    #region 多语言配置

    [Header("=== 多语言设置 ===")]
    [SerializeField] private LanguageSourceType m_SourceType = LanguageSourceType.None;
    [SerializeField] private string m_LanguageKey = "";
    [SerializeField] private bool m_AutoUpdate = true;

    #endregion

        #region 字体效果快捷配置

        [Header("=== 字体效果快捷配置 ===")]
        [Header("描边设置")]
        [SerializeField] private bool m_UseCustomOutline = false;
        [SerializeField] private Color m_CustomOutlineColor = Color.black;
        [SerializeField] private float m_CustomOutlineWidth = 0.2f;

        [Header("渐变设置")]
        [SerializeField] private bool m_UseCustomGradient = false;
        [SerializeField] private Color m_GradientTopColor = Color.white;
        [SerializeField] private Color m_GradientBottomColor = Color.gray;

        [Header("阴影设置")]
        [SerializeField] private bool m_UseCustomShadow = false;
        [SerializeField] private Color m_CustomShadowColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Vector2 m_CustomShadowOffset = new Vector2(1, -1);

        #endregion

        #region 动态参数

        private object[] m_Parameters;
        private string m_CachedText;

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            ApplyCustomEffects();
        }

        protected override void Start()
        {
            base.Start();
            RefreshLanguageText();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_AutoUpdate)
            {
                LanguageManager.OnLanguageChanged += OnLanguageChanged;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_AutoUpdate)
            {
                LanguageManager.OnLanguageChanged -= OnLanguageChanged;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (Application.isPlaying)
            {
                RefreshLanguageText();
            }
            // 编辑器模式下的刷新由Editor控制，不在这里处理
        }
#endif

        #endregion

        #region 自定义效果应用

        /// <summary>
        /// 应用自定义效果
        /// </summary>
        private void ApplyCustomEffects()
        {
            // 描边：使用TMP内置属性，TMP会自动管理shader
            this.outlineWidth = m_UseCustomOutline ? m_CustomOutlineWidth : 0;
            if (m_UseCustomOutline)
            {
                this.outlineColor = m_CustomOutlineColor;
            }

            // 渐变：使用TMP内置属性
            this.enableVertexGradient = m_UseCustomGradient;
            if (m_UseCustomGradient)
            {
                this.colorGradient = new VertexGradient(
                    m_GradientTopColor,
                    m_GradientTopColor,
                    m_GradientBottomColor,
                    m_GradientBottomColor
                );
            }
            // 获取或创建材质实例
            Material mat = materialForRendering;
            if (mat != null)
            {
                if (m_UseCustomShadow)
                {
                    mat.EnableKeyword("UNDERLAY_ON");
                    mat.SetColor(ShaderUtilities.ID_UnderlayColor, m_CustomShadowColor);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, m_CustomShadowOffset.x);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, m_CustomShadowOffset.y);
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
                }
                else
                {
                    mat.DisableKeyword("UNDERLAY_ON");
                }
            }
            
            // 标记属性已更改，触发TMP更新
            havePropertiesChanged = true;
            SetVerticesDirty();
            SetMaterialDirty();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：应用自定义效果（公开方法供Editor调用）
        /// </summary>
        public void EditorApplyCustomEffects()
        {
            if (!Application.isPlaying)
            {
                ApplyCustomEffects();
            }
        }
#endif

        #endregion

        #region 多语言功能

        /// <summary>
        /// 语言切换回调
        /// </summary>
        private void OnLanguageChanged(LangType newLangType)
        {
            RefreshLanguageText();
        }

        /// <summary>
        /// 刷新语言文本
        /// </summary>
        public void RefreshLanguageText()
        {
            if (!Application.isPlaying) return;

            // 如果使用了多语言但没有填写Key，清除预设文本
            if (m_SourceType != LanguageSourceType.None && string.IsNullOrWhiteSpace(m_LanguageKey))
            {
                text = "";
                m_CachedText = "";
                return;
            }

            string localizedText = GetLocalizedText();

            if (!string.IsNullOrEmpty(localizedText))
            {
                // 替换换行符
                localizedText = localizedText.Replace("\\n", "\n");

                // 参数替换
                if (m_Parameters != null && m_Parameters.Length > 0)
                {
                    try
                    {
                        localizedText = string.Format(localizedText, m_Parameters);
                    }
                    catch (Exception e)
                    {
                        LogUtlis.Error($"文本参数替换失败: {e.Message}");
                    }
                }

                text = localizedText;
                m_CachedText = localizedText;
            }
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        private string GetLocalizedText()
        {
            if (string.IsNullOrEmpty(m_LanguageKey))
                return text;

            string key = m_LanguageKey.Trim();

            switch (m_SourceType)
            {
                case LanguageSourceType.Language:
                    return LanguageManager.GetLangVal(key);
                case LanguageSourceType.LanguagePack:
                    return LanguageManager.GetLangPackVal(key);
                case LanguageSourceType.None:
                default:
                    return text;
            }
        }

        #endregion

        #region 公共接口 - 多语言

        /// <summary>
        /// 设置语言Key
        /// </summary>
        public void SetLanguageKey(string key, LanguageSourceType sourceType = LanguageSourceType.Language)
        {
            m_LanguageKey = key;
            m_SourceType = sourceType;
            RefreshLanguageText();
        }

        /// <summary>
        /// 设置语言Key并带参数
        /// </summary>
        public void SetLanguageKey(string key, LanguageSourceType sourceType, params object[] parameters)
        {
            m_LanguageKey = key;
            m_SourceType = sourceType;
            m_Parameters = parameters;
            RefreshLanguageText();
        }

        /// <summary>
        /// 直接设置文本（不使用多语言）
        /// </summary>
        public void SetTextDirect(string newText)
        {
            m_SourceType = LanguageSourceType.None;
            text = newText;
        }

        #endregion

        #region 公共接口 - 样式设置

        /// <summary>
        /// 设置描边
        /// </summary>
        public void SetOutline(bool enable, Color color, float width = 0.2f)
        {
            m_UseCustomOutline = enable;
            m_CustomOutlineColor = color;
            m_CustomOutlineWidth = width;
            
            ApplyCustomEffects();
        }

        /// <summary>
        /// 设置渐变
        /// </summary>
        public void SetGradient(bool enable, Color topColor, Color bottomColor)
        {
            m_UseCustomGradient = enable;
            m_GradientTopColor = topColor;
            m_GradientBottomColor = bottomColor;

            ApplyCustomEffects();
        }

        /// <summary>
        /// 设置阴影
        /// </summary>
        public void SetShadow(bool enable, Color color, Vector2 offset)
        {
            m_UseCustomShadow = enable;
            m_CustomShadowColor = color;
            m_CustomShadowOffset = offset;

            ApplyCustomEffects();
        }

        #endregion

        #region 访问器

        /// <summary>
        /// 获取当前语言Key
        /// </summary>
        public string GetLanguageKey() => m_LanguageKey;

        /// <summary>
        /// 获取语言源类型
        /// </summary>
        public LanguageSourceType GetSourceType() => m_SourceType;

        /// <summary>
        /// 获取缓存的文本
        /// </summary>
        public string GetCachedText() => m_CachedText;

        #endregion
    }

