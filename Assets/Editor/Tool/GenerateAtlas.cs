#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;
using System.IO;

public class AutoSetAtlasContent : UnityEditor.Editor
{
    private static string _atlasPath = "";
    private static string _pngPath = "";

    [MenuItem("Assets/以文件夹名生成【图集】", false, 201)]
    static void AutoSetAtlasContentsByPathName()
    {
        // 获取当前选中的文件夹路径
        _pngPath = GetCurrentAssetDirectory();
        if (string.IsNullOrEmpty(_pngPath) || !AssetDatabase.IsValidFolder(_pngPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个有效的文件夹！", "确定");
            return;
        }

        // 确保集中管理图集的目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Res"))
            AssetDatabase.CreateFolder("Assets", "Res");
        if (!AssetDatabase.IsValidFolder("Assets/Res/AtlasSprite"))
            AssetDatabase.CreateFolder("Assets/Res", "AtlasSprite");

        // 在集中目录创建图集
        string folderName = Path.GetFileNameWithoutExtension(_pngPath);
        _atlasPath = $"Assets/Res/AtlasSprite/{folderName}.spriteatlas";

        CreateSpriteAtlas(_pngPath, _atlasPath);
    }

    /// <summary>
    /// 创建SpriteAtlas并添加文件夹
    /// </summary>
    static void CreateSpriteAtlas(string folderPath, string atlasPath)
    {
        // 加载或创建SpriteAtlas资产
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            // 基本打包设置
            SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
            {
                blockOffset = 1,
                enableRotation = false, // 通常禁用防止精灵意外旋转[2](@ref)
                enableTightPacking = false, // 通常禁用避免精灵显示问题[2](@ref)
                padding = 2, // 精灵之间的间隔
            };
            atlas.SetPackingSettings(packSetting);

            // 纹理设置
            SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
            {
                readable = false, // 通常禁用以减少内存占用，除非需要通过脚本访问像素数据[2](@ref)
                generateMipMaps = false, // UI精灵通常不需要MipMaps
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            atlas.SetTextureSettings(textureSetting);

            // 平台特定设置
            TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
            {
                maxTextureSize = 2048,
                format = TextureImporterFormat.Automatic,
                crunchedCompression = true,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
            };
            atlas.SetPlatformSettings(platformSetting);

            // 创建资产
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        // 清除现有打包对象（可选，根据需求决定是否保留）
        // 注意：这会清除已手动添加的其他对象
        // atlas.Remove(atlas.GetPackables());

        // 加载文件夹对象
        Object folderObj = AssetDatabase.LoadAssetAtPath(folderPath, typeof(Object));
        if (folderObj == null)
        {
            Debug.LogError("无法加载文件夹: " + folderPath);
            return;
        }

        // 添加文件夹到图集
        // Album方式的核心：直接添加文件夹，Unity会自动包含文件夹内的所有精灵[4](@ref)
        atlas.Add(new Object[] { folderObj });

        // 保存资产
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"已创建/更新图集: {atlasPath}，包含文件夹: {folderPath}");
    }

    public static string GetCurrentAssetDirectory()
    {
        foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (System.IO.Directory.Exists(path))
                return path;
            else if (System.IO.File.Exists(path))
                return System.IO.Path.GetDirectoryName(path);
        }
        return "Assets";
    }
}
#endif