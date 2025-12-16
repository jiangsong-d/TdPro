using UnityEngine;
using UnityEditor;

public class UIUtils
{
    #region UI
    /// <summary>
    /// Unity默认UI Shader
    /// </summary>
    public static readonly string UnityUIDefaultShaderName = "UI/Default";

    /// <summary>
    /// 默认图片材质球路径
    /// </summary>
    private const string ImageDefaultMatPath = "";

    private const string ImageDefaultWorldMatPath = "";

    /// <summary>
    /// 默认图片材质球
    /// </summary>
    public static readonly Material ImageDefaultMat;

    public static readonly Material ImageDefaultWorldMat;
    #endregion

    #region Font
    /// <summary>
    /// 默认字体材质球路径
    /// </summary>
    private const string FontDefaultMatPath = "";

    private const string FontDefaultWorldMatPath = "";

    /// <summary>
    /// 默认字体材质球
    /// </summary>
    public static readonly Material FontDefaultMat;

    public static readonly Material FontDefaultWorldMat;
    #endregion

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static UIUtils()
    {
#if UNITY_EDITOR
        ImageDefaultMat = AssetDatabase.LoadAssetAtPath<Material>(ImageDefaultMatPath);
        ImageDefaultWorldMat = AssetDatabase.LoadAssetAtPath<Material>(ImageDefaultWorldMatPath);
        FontDefaultMat = AssetDatabase.LoadAssetAtPath<Material>(FontDefaultMatPath);
        FontDefaultWorldMat = AssetDatabase.LoadAssetAtPath<Material>(FontDefaultWorldMatPath);
#endif
        
    }
}