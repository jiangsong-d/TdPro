using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using System.IO;

public class SpriteAtlasTool : ScriptableObject
{
    /// <summary>
    /// 精灵所在路径
    /// </summary>
    public const string AtlasPath = "/Res/AtlasSprite/";
    /// <summary>
    /// 图集精灵所在路径
    /// </summary>
    public static readonly string AtlasFullPath = Application.dataPath + AtlasPath;
    /// <summary>
    /// 精灵所在路径
    /// </summary>
    public const string AtlasSpritePath = "/Res/Sprite/";
    /// <summary>
    /// 图集精灵所在全路径
    /// </summary>
    public static readonly string AtlasSpriteFullPath = Application.dataPath + AtlasSpritePath;

    private const string Platform_iOS = "iPhone";
    private const string Platform_Android = "Android";


    /// <summary>
    /// 创建SpriteAtlas
    /// </summary>
    /// <param name="fileName">文件名</param>
    public static void CreateSpriteAtlas(string fileName)
    {
        SpriteAtlas atlas = new SpriteAtlas();
        // 设置参数 可根据项目具体情况进行设置
        SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 2,
        };
        atlas.SetPackingSettings(packSetting);

        SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear,
        };
        atlas.SetTextureSettings(textureSetting);

        TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
        {
            maxTextureSize = 2048,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Compressed,
        };
        atlas.SetPlatformSettings(platformSetting);
        //安卓
        var androidSetting = atlas.GetPlatformSettings(Platform_Android);
        if (null == androidSetting)
        {
            androidSetting = new TextureImporterPlatformSettings();
            androidSetting.name = Platform_Android;
        }
        androidSetting.overridden = true;
        androidSetting.textureCompression = TextureImporterCompression.Compressed;
        androidSetting.format = TextureImporterFormat.ASTC_6x6;
        atlas.SetPlatformSettings(androidSetting);
        //IOS
        var iosSetting = atlas.GetPlatformSettings(Platform_iOS);
        if (null == iosSetting)
        {
            iosSetting = new TextureImporterPlatformSettings();
            iosSetting.name = Platform_iOS;
        }
        iosSetting.overridden = true;
        iosSetting.textureCompression = TextureImporterCompression.Compressed;
        iosSetting.format = TextureImporterFormat.ASTC_6x6;
        atlas.SetPlatformSettings(iosSetting);

        string _atlasPath = $"Assets/Res/AtlasSprite/{fileName}.spriteatlas";

        AssetDatabase.CreateAsset(atlas, _atlasPath);

        //添加文件夹
        string _atlasSpritePath = EditorUtils.FullPathToAssetPath(AtlasSpriteFullPath + fileName);
        Object obj = AssetDatabase.LoadAssetAtPath(_atlasSpritePath, typeof(Object));
        atlas.Add(new[] { obj });

        SpriteAtlasUtility.PackAtlases(new SpriteAtlas[1] { atlas }, EditorUserBuildSettings.activeBuildTarget);
    }
}