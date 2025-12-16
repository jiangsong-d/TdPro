using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Audio;
using YooAsset;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using System.IO;
using System.Text;

/// <summary>
/// 纹理类型
/// </summary>
public enum TextureType
{
    PNE,
    JPG,
}

/// <summary>
/// 资源加载管理器
/// 同步加载资源 异步加载资源
/// 4. 加载图集Sprite   LoadSpriteAtlasSprite LoadSpriteAtlasSpriteAsync
/// 5. 加载 单图 不走图集 LoadSpriteSingle  LoadSpriteSingleAsync
/// 6. 加载Texture LoadTexture  LoadTextureAsync
/// 7. 加载音频 LoadAudio LoadAudioAsync
/// 8. 加载Spine动画 LoadSpineAnimation LoadSpineAnimationAsync
/// 9. 加载特效 LoadEffect LoadEffectAsync
/// 10.加载预制体 LoadPrefab LoadPrefabAsync
/// </summary>
public class LoadManager : MonoSingleton<LoadManager>
{
    public const string RootDirPath = "Assets/Res/";
    private ResourcePackage resourcePack = null;


    /// <summary>
    /// 特效缓存池
    /// </summary>
    private CachePool<string, GameObject> _effectPool = new CachePool<string, GameObject>(20);
    /// <summary>
    /// 图集缓存
    /// </summary>
    private CachePool<string, SpriteAtlas> _saPool = new CachePool<string, SpriteAtlas>(10);
    /// <summary>
    /// 音效缓存
    /// </summary>
    private CachePool<string, AudioClip> _acPool = new CachePool<string, AudioClip>(20);
    // /// <summary>
    // /// Spine动画缓存
    // /// </summary>
    // private CachePool<string, SkeletonDataAsset> _skPool = new CachePool<string, SkeletonDataAsset>(20);

    public void Init()
    {

        if (resourcePack == null)
        {
            // 直接从 YooAssets 获取默认包裹
            resourcePack = YooAssets.GetPackage(YooManager.Instance.packageName);
            if (resourcePack == null)
            {
                LogUtlis.Error($"资源包裹加载失败！包名: {YooManager.Instance.packageName}");
                LogUtlis.Error("请检查资源包是否已初始化完成");
            }
            else
            {
                LogUtlis.Info($"LoadManager 初始化成功，资源包: {YooManager.Instance.packageName}");
            }
        }

    }

    #region 通用加载方法

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public T SyncLoadAsset<T>(string assetPath) where T : Object
    {
        string path = RootDirPath + assetPath;
        try
        {
            var handle = resourcePack.LoadAssetSync<T>(path);
            var asset = handle.AssetObject as T;
            handle.Release();
            return asset;
        }
        catch (Exception e)
        {
            LogUtlis.Error($"加载资源失败：{path}, 错误：{e.Message}");
            return default;
        }
    }
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object
    {
        string path = RootDirPath + assetPath;
        try
        {
             if (!resourcePack.CheckLocationValid(path))
            {
                LogUtlis.Error($"加载资源失败：{path}, 错误：资源无效");
                return default;
            }
            var handler = resourcePack.LoadAssetAsync<T>(path);
            await handler.ToUniTask();
            var asset = (T)handler.AssetObject;
            handler.Release();
            return asset;
        }
        catch (Exception e)
        {
            LogUtlis.Error($"加载资源失败：{path}, 错误：{e.Message}");
            return default;
        }
    }
    private string GetAssetName(string assetPath)
    {
        return Path.GetFileName(assetPath);
    }

    #endregion

    //------------------------ 同步加载接口 ------------------------
    #region 同步加载
    public GameObject LoadPrefab(string assetPath, Transform parentRoot = null)
    {
        assetPath = assetPath + ".prefab";
        var go = SyncLoadAsset<GameObject>(assetPath);
        var prefab = Object.Instantiate(go, parentRoot);
        if (prefab == null)
        {
            LogUtlis.Error(string.Format("ERR:prefab{0}路径未加载成功", assetPath));
            return null;
        }
        return prefab;
    }

    public GameObject LoadPrefab(string assetPath, Transform parentRoot, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        assetPath = assetPath + ".prefab";
        var go = SyncLoadAsset<GameObject>(assetPath);
        var prefab = Object.Instantiate(go, position, Quaternion.Euler(rotation), parentRoot);
        if (prefab == null)
        {
            LogUtlis.Error(string.Format("ERR:prefab{0}路径未加载成功", assetPath));
            return null;
        }
        prefab.transform.localScale = scale;
        return prefab;
    }
    /// <summary>
    /// 加载图集Sprite
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public Sprite LoadSpriteAtlasSprite(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        // 解析 atlas 路径与 sprite 名称，支持短写 "Common/icon" 或完整写法 "Atlas/Common.spriteatlas/icon"
        if (!ResolveAtlasAndSprite(assetName, out string atlasPath, out string spriteName))
        {
            LogUtlis.Error($"ERR: 无法解析图集路径或精灵名: {assetName}path: {atlasPath}, sprite: {spriteName}");
            return null;
        }

        var atlas = _saPool.Get(atlasPath);
        if (atlas == null)
        {
            atlas = SyncLoadAsset<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                LogUtlis.Error(string.Format("ERR:SpriteAtlas {0} 路径未加载成功", atlasPath));
                return null;
            }
            _saPool.Add(atlasPath, atlas);
        }
        Sprite sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
        {
            LogUtlis.Error(string.Format("ERR:{0} 不存在 {1} 图片", atlasPath, spriteName));
            return null;
        }
        return sprite;
    }
    /// <summary>
    /// 加载单图不走图集
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="packageName"></param>
    /// <returns></returns>
    public Sprite LoadSpriteSingle(string assetName)
    {
        Sprite sprite = SyncLoadAsset<Sprite>(assetName);
        if (sprite == null)
        {
            LogUtlis.Error(string.Format("ERR: 不存在{0}图片", assetName));
            return null;
        }
        return sprite;

    }

    // 实现与LoadPrefab类似，替换泛型参数
    public Texture2D LoadTexture(string assetName, TextureType textureType)
    {
        string path = null;
       
        if (textureType == TextureType.PNE)
        {
             path = "Texture/" + assetName + ".png";
        }
        else
        {
             path = "Texture/" + assetName + ".jpg";
        }
        Texture2D texture = SyncLoadAsset<Texture2D>(path);
        if (texture == null)
        {
            LogUtlis.Error(string.Format("ERR: 不存在{0}纹理", assetName));
            return null;
        }
        return texture;
    }

    public AudioClip LoadAudio(string assetName)
    {
        AudioClip audio = _acPool.Get(assetName);
        if (audio != null)
        {
            return audio;
        }
        audio = SyncLoadAsset<AudioClip>(assetName);
        if (audio == null)
        {
            LogUtlis.Error(string.Format("ERR: 不存在{0}音频", assetName));
            return null;
        }
        _acPool.Add(assetName, audio);
        return audio;
    }
    // public SkeletonDataAsset LoadSpineAnimation(string assetName)
    // {
    //     SkeletonDataAsset spine = _skPool.Get(assetName);
    //     if (spine != null)
    //     {
    //         return spine;
    //     }

    //     spine = SyncLoadAsset<SkeletonDataAsset>(assetName);
    //     if (spine == null)
    //     {
    //         LogUtlis.Error(string.Format("ERR: 不存在{0}Spine动画", assetName));
    //         return null;
    //     }
    //     _skPool.Add(assetName, spine);
    //     return spine;
    // }
    /// <summary>
    /// 加载特效
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public GameObject LoadEffect(string assetName)
    {
        var go = _effectPool.Get(assetName);
        if (go != null)
        {
            var prefab = Object.Instantiate(go);
            prefab.name = GetAssetName(assetName); ;
            return prefab;
        }
        else
        {
            go = SyncLoadAsset<GameObject>(assetName);
            _effectPool.Add(assetName, go);
            var prefab = Object.Instantiate(go);
            prefab.name = GetAssetName(assetName); ;
            return prefab;
        }
    }
    #endregion


    //------------------------ 异步加载接口 ------------------------
    #region 异步加载
    /// <summary>
    /// 异步加载预制体
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public async UniTask<GameObject> LoadPrefabAsync(string assetName)
    {
        assetName = assetName.Replace("\\", "/");
        assetName = assetName + ".prefab";
        var go = await LoadAssetAsync<GameObject>(assetName);
        if (go == null)
        {
            return null;
        }
        var prefab = Object.Instantiate(go);
        return prefab;
    }
    /// <summary>
    /// 异步加载预制体
    /// </summary>
    /// <param name="assetPath"></param>
    /// <param name="parentRoot"></param>
    /// <returns></returns>
    public async UniTask<GameObject> LoadPrefabAsync(string assetPath, Transform parentRoot = null)
    {
        assetPath = assetPath + ".prefab";

        var go = await LoadAssetAsync<GameObject>(assetPath);
        if (go == null)
        {
            return null;
        }
        else
        {
            var prefab = Object.Instantiate(go, parentRoot);
            return prefab;
        }
    }
    /// <summary>
    /// 异步加载预制体
    /// </summary>
    /// <param name="assetPath"></param>
    /// <param name="parentRoot"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public async UniTask<GameObject> LoadPrefabAsync(string assetPath, Transform parentRoot, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        assetPath = assetPath + ".prefab";
        var go = await LoadAssetAsync<GameObject>(assetPath);
        if (go == null)
        {
            return null;
        }
        var prefab = Object.Instantiate(go, position, Quaternion.Euler(rotation), parentRoot);
        prefab.transform.localScale = scale;
        return prefab;
    }
    /// <summary>
    /// 异步加载图集Sprite
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public async UniTask<Sprite> LoadSpriteAtlasSpriteAsync(string assetName)
    {
        if (!ResolveAtlasAndSprite(assetName, out string atlasPath, out string spriteName))
        {
            LogUtlis.Error($"ERR: 无法解析图集路径或精灵名: {assetName}");
            return null;
        }

        var atlas = _saPool.Get(atlasPath);
        if (atlas == null)
        {
            atlas = await LoadAssetAsync<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                LogUtlis.Error(string.Format("ERR:SpriteAtlas {0} 路径未加载成功", atlasPath));
                return null;
            }
            _saPool.Add(atlasPath, atlas);
        }
        Sprite sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
        {
            LogUtlis.Error(string.Format("ERR:{0} 不存在 {1} 图片", atlasPath, spriteName));
            return null;
        }
        return sprite;
    }
    /// <summary>
    /// 异步加载 单图 不走图集
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public async UniTask<Sprite> LoadSpriteAsync(string assetName)
    {
        var path = "Sprite/" + assetName + ".png";
        var sprite = await LoadAssetAsync<Sprite>(assetName);
        if (sprite == null)
        {
            LogUtlis.Error(string.Format("ERR: 不存在{0}图片", assetName));
            return null;
        }
        return sprite;
    }
    /// <summary>
    /// 异步加载Texture
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="packageName"></param>
    /// <returns></returns>
    public async UniTask<Texture2D> LoadTextureAsync(string assetName, TextureType textureType)
    {
        string path = null;
       
        if (textureType == TextureType.PNE)
        {
             path = "Texture/" + assetName + ".png";
        }
        else
        {
             path = "Texture/" + assetName + ".jpg";
        }
        Texture2D texture = await LoadAssetAsync<Texture2D>(path);
        if (texture == null)
        {
            LogUtlis.Error(string.Format("ERR: 不存在{0}纹理", assetName));
            return null;
        }
        return texture;
    }
    /// <summary>
    /// 异步加载特效
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="packageName"></param>
    /// <returns></returns>
    public async UniTask<GameObject> LoadEffectAsync(string assetName)
    {
        var go = _effectPool.Get(assetName);
        if (go != null)
        {
            var prefab = Object.Instantiate(go);
            prefab.name = GetAssetName(assetName);
            return prefab;
        }
        else
        {
            go = await LoadAssetAsync<GameObject>(assetName);
            _effectPool.Add(assetName, go);
            var prefab = Object.Instantiate(go);
            prefab.name = GetAssetName(assetName);
            return prefab;
        }
    }
    /// <summary>
    /// 异步加载Spine动画
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="packageName"></param>
    /// <returns></returns>
    // public async UniTask<SkeletonDataAsset> LoadSpineAnimationAsync(string assetName, string packageName = null)
    // {
    //     SkeletonDataAsset spine = _skPool.Get(assetName);
    //     if (spine != null)
    //     {
    //         return spine;
    //     }
    //     spine = await LoadAssetAsync<SkeletonDataAsset>(assetName);
    //     if (spine == null)
    //     {
    //         LogUtlis.Error(string.Format("ERR: 不存在{0}Spine动画", assetName));
    //         return null;
    //     }
    //     _skPool.Add(assetName, spine);
    //     return spine;
    // }
    // #endregion

    // //------------------------ 资源管理 ------------------------
    // #region 资源释放
    // public void ReleaseAsset(string assetName)
    // {
    //     if (_assetHandles.TryGetValue(assetName, out AssetHandle handle))
    //     {
    //         handle.Release();
    //         _assetHandles.Remove(assetName);
    //     }
    // }

    public void ReleaseAllCachePool()
    {
        _effectPool.Clear();
        _saPool.Clear();
        _acPool.Clear();
        // _skPool.Clear();
    }

    public void OnDestroy()
    {
        StopAllCoroutines();
        ReleaseAllCachePool();
    }
    #endregion

    /// <summary>
    /// 解析传入的 assetName，支持短写与多种命名习惯。
    /// 返回 atlasPath（相对于 Assets/Res/ 的路径部分，例如 "Atlas/Common.spriteatlas"）和 spriteName（例如 "icon"）。
    /// 支持输入示例：
    /// - "Atlas/Common.spriteatlas/icon"
    /// - "Common/icon" (会解析为 "Atlas/Common.spriteatlas" + "icon")
    /// - "CommonAtlas.spriteatlas/icon"
    /// - "Atlas/CommonAtlas.spriteatlas/icon"
    /// </summary>
    private bool ResolveAtlasAndSprite(string assetName, out string atlasPath, out string spriteName)
    {
        atlasPath = null;
        spriteName = null;
        if (string.IsNullOrEmpty(assetName)) return false;

        // 标准拆分：用 '/' 分割最后一段为 sprite 名称
        string[] parts = assetName.Replace("\\", "/").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        // 最后一段认为是 sprite 名称
        spriteName = parts[parts.Length - 1];

        // 如果只有一段（没有 '/'），那默认图集名和 sprite 同名，此时我们没有 atlas 信息，返回 false
        if (parts.Length == 1)
        {
            return false;
        }

        // atlas 部分是前面的所有段拼接
        string atlasPart = string.Join("/", parts, 0, parts.Length - 1);

        // 规范化 atlasPart：如果已经以 "Atlas/" 开头，去掉前导斜杠
        if (atlasPart.StartsWith("/")) atlasPart = atlasPart.Substring(1);

        // 若 atlasPart 直接包含 ".spriteatlas"，则认为用户传入了完整文件名
        if (atlasPart.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase))
        {
            // 支持用户传入 "Atlas/Common.spriteatlas" 或 "Common.spriteatlas"
            if (atlasPart.StartsWith("AtlasSprite/"))
                atlasPath = atlasPart;
            else
                atlasPath = "AtlasSprite/" + atlasPart;
            return true;
        }

        // 如果 atlasPart 形如 "Atlas/Common" 或 "Common"，需要补上后缀
        string candidate = atlasPart;
        if (!candidate.StartsWith("AtlasSprite/"))
            candidate = "AtlasSprite/" + candidate;

        // 首先尝试 {candidate}.spriteatlas
        atlasPath = candidate + ".spriteatlas";
        return true;
    }
}
