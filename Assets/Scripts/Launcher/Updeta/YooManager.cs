using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

/// <summary>
/// 资源文件查询服务类
/// </summary>
/// <summary>
/// 远端资源地址查询服务类
/// </summary>
public class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }

    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}

public class YooManager : MonoSingleton<YooManager>
{
    // 新增路径配置
    public string HotUpdateDllPath => "HotCodeDll";
    public string MainDllName => "HotScripts";
    
    // 获取完整DLL路径
    public string GetDllPath(string dllName)
    {
        return $"{HotUpdateDllPath}/{dllName}.bytes";
    }
    private ResourcePackage _defaultPackage;

    public string packageName;

    public string packageVersion = "1.0.0";

    public ResourceDownloaderOperation operation;

    public void Initialized()
    {
        YooAssets.Initialize();
        _defaultPackage = YooAssets.TryGetPackage(packageName);
        if (_defaultPackage == null)
        {
            _defaultPackage = YooAssets.CreatePackage(packageName);
        }
        YooAssets.SetDefaultPackage(_defaultPackage);
        SpriteAtlasManager.atlasRequested += RequestAtlas;
    }
        /// <summary>
    /// YooAssets图集请求回调  2.3.8 问题
    /// </summary>
    /// <param name="atlasName"></param>
    /// <param name="callback"></param>
    private void RequestAtlas(string atlasName, Action<SpriteAtlas> callback)
    {
        var package = YooManager.Instance.GetResourcePackage();
        var path = "Assets/Res/AtlasSprite/"+atlasName+".spriteatlas";
        var loadHandle = package.LoadAssetSync<SpriteAtlas>(path);
        callback.Invoke(loadHandle.AssetObject as SpriteAtlas);

        if (loadHandle.Status == EOperationStatus.Succeed)
        {
            callback.Invoke(loadHandle.AssetObject as SpriteAtlas);
            LogUtlis.Info($"✅ 成功加载图集: {atlasName}");
        }
        else
        {
            Debug.LogError($"❌ 图集加载失败: {atlasName} | 错误: {loadHandle.LastError}");
            callback.Invoke(null);
        }
        loadHandle.Release();
    }
    public void SetPackageName(string pName)
    {
        packageName = pName;
    }
    public ResourcePackage GetResourcePackage()
    {
        return _defaultPackage;
    }
    // public List<string> GetAOTMetaAssemblyFiles()
    // {
    //     return AOTMetaAssemblyFiles;
    // }
    public string GetHostServerURL()
    {
        return "http://pandagameres.yyyyp.com:25381/gameupdate/tk_test/";
    }
}
