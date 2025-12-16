using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YooAsset;
using UnityEngine.Networking;
using HybridCLR;
using System.IO;

public class HybridManager : MonoSingleton<HybridManager>
{
    // 存储已加载的程序集
    private Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

    // 配置常量
    private const string HOT_UPDATE_DLL_PATH_PREFIX = "Assets/HotCodeDll/";
    private const string MAIN_GAME_TYPE = "GameManager";
    private const string MAIN_GAME_METHOD = "GameStart";
    
    // AOT元数据目录名称
    private const string AOT_METADATA_DIR = "Il2CppMetadata";

    /// <summary>
    /// 异步加载热更新DLL
    /// </summary>
    public IEnumerator LoadHotUpdateAssembly(string dllName)
    {
        // 如果已经加载过，直接返回
        if (loadedAssemblies.ContainsKey(dllName))
        {
            LogUtlis.Info($"程序集已加载: {dllName}");
            yield break;
        }

        string dllPath = $"{HOT_UPDATE_DLL_PATH_PREFIX}{dllName}.dll";
        LogUtlis.Info($"开始加载热更新程序集: {dllName}, 路径: {dllPath}");

#if UNITY_EDITOR
        // 编辑器模式：直接从当前域中获取程序集
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == dllName);

        if (assembly != null)
        {
            loadedAssemblies[dllName] = assembly;
            LogUtlis.Info($"编辑器模式加载程序集成功: {dllName}");
        }
        else
        {
            LogUtlis.Error($"编辑器模式找不到程序集: {dllName}");
            // 打印所有已加载的程序集供调试
            LogUtlis.Info("当前已加载的程序集:");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                LogUtlis.Info($"  - {asm.GetName().Name}");
            }
        }
        yield return null;
#else
        // 非编辑器模式：使用YooAsset加载资源
        var package = YooAssets.GetPackage(YooManager.Instance.packageName);
        LogUtlis.Info($"获取YooAsset包: {YooManager.Instance.packageName}");
        
        var handle = package.LoadAssetAsync<TextAsset>(dllPath);
        yield return handle;

        LogUtlis.Info($"YooAsset加载状态: {handle.Status}, 错误: {handle.LastError}");
        
        if (handle.Status == EOperationStatus.Succeed)
        {
            TextAsset dllAsset = handle.GetAssetObject<TextAsset>();
            if (dllAsset != null)
            {
                LogUtlis.Info($"资源加载成功, 字节长度: {dllAsset.bytes.Length}");
                
                // 补充AOT元数据
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(
                    dllAsset.bytes,  
                    HomologousImageMode.SuperSet);
                
                LogUtlis.Info($"AOT元数据补充完成, 状态: {err}");
                
                // 加载程序集
                Assembly hotUpdateAss = Assembly.Load(dllAsset.bytes);
                
                if (hotUpdateAss != null)
                {
                    loadedAssemblies[dllName] = hotUpdateAss;
                    LogUtlis.Info($"热更新DLL加载成功: {dllName}, 程序集版本: {hotUpdateAss.FullName}");
                }
                else
                {
                    LogUtlis.Error($"程序集加载失败: {dllName}");
                }
            }
            else
            {
                LogUtlis.Error($"资源加载成功但AssetObject为空: {dllPath}");
            }
        }
        else
        {
            LogUtlis.Error($"热更新DLL资源加载失败: {dllPath}, 错误: {handle.LastError}");
        }

        // 释放资源句柄
        handle.Release();
        LogUtlis.Info($"释放资源句柄: {dllName}");
#endif
    }

    /// <summary>
    /// 获取已加载的程序集
    /// </summary>
    public Assembly GetAssembly(string dllName)
    {
        if (loadedAssemblies.TryGetValue(dllName, out Assembly assembly))
        {
            LogUtlis.Info($"获取已加载程序集: {dllName}, 状态: {assembly != null}");
            return assembly;
        }
        
        LogUtlis.Error($"未找到程序集: {dllName}");
        return null;
    }

    /// <summary>
    /// 初始化游戏管理器
    /// </summary>
    public IEnumerator InitGameManager()
    {
        LogUtlis.Info("初始化游戏管理器开始");
        
        // 热更新程序集
        string mainDllName = "HotUpdate";
        LogUtlis.Info($"加载主程序集: {mainDllName}");

        // 确保主程序集已加载
        if (!loadedAssemblies.ContainsKey(mainDllName))
        {
            LogUtlis.Error($"热更新程序集 {mainDllName} 未加载，开始加载...");
            yield return LoadHotUpdateAssembly(mainDllName);
        }

        // 获取主程序集
        Assembly mainAssembly = GetAssembly(mainDllName);
        if (mainAssembly == null)
        {
            LogUtlis.Error($"主程序集加载失败: {mainDllName}");
            yield break;
        }
        
        LogUtlis.Info($"主程序集加载成功，开始查找类型: {MAIN_GAME_TYPE}");
        
        // 获取GameManager类型
        Type type = mainAssembly.GetType(MAIN_GAME_TYPE);
        if (type == null)
        {
            LogUtlis.Error($"类型未找到: {MAIN_GAME_TYPE}");
            yield break;
        }
        LogUtlis.Info($"找到游戏管理器类型: {MAIN_GAME_TYPE}");

        // 获取并调用启动方法
        MethodInfo method = type.GetMethod(MAIN_GAME_METHOD, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            LogUtlis.Error($"方法未找到: {MAIN_GAME_METHOD}");
            yield break;
        }
        LogUtlis.Info($"找到启动方法: {MAIN_GAME_METHOD}");

        LogUtlis.Info($"调用游戏启动方法: {MAIN_GAME_METHOD}");
        
        // 调用方法（假设返回IEnumerator，用协程执行）
        if (method.ReturnType == typeof(IEnumerator))
        {
            IEnumerator invokeCoroutine = (IEnumerator)method.Invoke(null, null);
            yield return StartCoroutine(invokeCoroutine);
            LogUtlis.Info($"游戏启动方法执行完成");
        }
        else
        {
            method.Invoke(null, null);
            LogUtlis.Info($"游戏启动方法执行完成");
        }
        
        LogUtlis.Info("初始化游戏管理器完成");
    }

    /// <summary>
    /// 加载所有热更新DLL（可选）
    /// </summary>
    public IEnumerator LoadAllHotUpdateAssemblies()
    {
        LogUtlis.Info("开始加载所有热更新DLL");
        
        string[] dllNames = {
            "HotUpdate",
            // 可以添加其他需要加载的DLL
        };

        foreach (string dllName in dllNames)
        {
            if (!loadedAssemblies.ContainsKey(dllName))
            {
                LogUtlis.Info($"开始加载热更新DLL: {dllName}");
                yield return LoadHotUpdateAssembly(dllName);
            }
            else
            {
                LogUtlis.Info($"跳过已加载的DLL: {dllName}");
            }
        }
        
        LogUtlis.Info("所有热更新DLL加载完成");
    }
    
 /// <summary>
    /// 补充AOT元数据
    /// </summary>
    public IEnumerator LoadAOTMetadata()
    {
        LogUtlis.Info("========== 开始加载AOT元数据 ==========");
        LogUtlis.Info($"StreamingAssets路径: {Application.streamingAssetsPath}");
        LogUtlis.Info($"当前平台: {Application.platform}");

        var fileNames = GetAotFileNames();
        LogUtlis.Info($"需要加载的AOT DLL数量: {fileNames.Count}");

        int successCount = 0;
        int failCount = 0;
        
        foreach (var fileName in fileNames)
        {   
            LogUtlis.Info($"--- 开始加载: {fileName} ---");
            yield return LoadSingleAotDll(fileName);
            
            if (lastAotLoadSuccess)
            {
                successCount++;
                LogUtlis.Info($"+++ {fileName} 加载成功 +++");
            }
            else
            {
                failCount++;
                LogUtlis.Error($"!!! {fileName} 加载失败 !!!");
            }
        }

        LogUtlis.Info($"AOT元数据加载完成: 成功 {successCount} 个, 失败 {failCount} 个");
        LogUtlis.Info("========== AOT元数据加载结束 ==========");
    }

    private bool lastAotLoadSuccess = false;

   /// <summary>
    /// 加载单个AOT DLL
    /// </summary>
    private IEnumerator LoadSingleAotDll(string fileName)
    {
        lastAotLoadSuccess = false;
        byte[] dllBytes = null;
        string filePath = "";
        UnityWebRequest request = null;
        
        try
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            filePath = Path.Combine(Application.streamingAssetsPath, AOT_METADATA_DIR, fileName);
            LogUtlis.Info($"PC模式加载路径: {filePath}");

            if (!File.Exists(filePath))
            {
                LogUtlis.Error($"文件不存在: {filePath}");
                yield break;
            }

            dllBytes = File.ReadAllBytes(filePath);
            LogUtlis.Info($"成功读取文件, 大小: {dllBytes?.Length ?? 0} 字节");

#elif UNITY_WEBGL
            filePath = $"{Application.streamingAssetsPath}/{AOT_METADATA_DIR}/{fileName}";
            LogUtlis.Info($"WebGL加载URL: {filePath}");
            
            request = UnityWebRequest.Get(filePath);
#else
            filePath = GetMobileAotPath(fileName);
            LogUtlis.Info($"移动平台加载路径: {filePath}");
            
            request = UnityWebRequest.Get(filePath);
            
    #if UNITY_ANDROID
            request.timeout = 15;
    #endif
#endif
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"加载准备阶段异常: {ex.Message}");
            LogUtlis.Error($"异常堆栈: {ex.StackTrace}");
            lastAotLoadSuccess = false;
            yield break;
        }
        
        if (request != null)
        {
            try
            {
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogUtlis.Error($"加载失败: {request.error}");
                    lastAotLoadSuccess = false;
                    yield break;
                }
                
                dllBytes = request.downloadHandler.data;
                LogUtlis.Info($"成功加载, 大小: {dllBytes?.Length ?? 0} 字节");
            }
            finally
            {
                request.Dispose();
            }
        }
        
        // 数据处理统一在此
        if (dllBytes == null || dllBytes.Length == 0)
        {
            LogUtlis.Error($"文件内容为空: {fileName}");
            lastAotLoadSuccess = false;
            yield break;
        }
        
        LogUtlis.Info($"文件内容有效, 大小: {dllBytes.Length} 字节");

        // 加载AOT元数据
        try
        {
            var result = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(
                dllBytes,
                HomologousImageMode.SuperSet);

            LogUtlis.Info($"AOT元数据加载完成, 状态: {result}");
            lastAotLoadSuccess = true;
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"AOT元数据加载异常: {ex.Message}");
            LogUtlis.Error($"异常堆栈: {ex.StackTrace}");
            lastAotLoadSuccess = false;
        }
    }

    private string GetMobileAotPath(string fileName)
    {
        #if UNITY_ANDROID
        return $"{Application.streamingAssetsPath}/{AOT_METADATA_DIR}/{fileName}";
        #else
        return Path.Combine(Application.streamingAssetsPath, AOT_METADATA_DIR, fileName);
        #endif
    }

    private static List<string> GetAotFileNames()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        return new List<string>
        {
            "DOTween.dll",
            "GameFrameX.Network.Runtime.dll",
            "GameFrameX.Runtime.dll",
            "Luban.Runtime.dll",
            "Newtonsoft.Json.dll",
            "ProtoBuffer.Runtime.dll",
            "System.Core.dll",
            "System.dll",
            "UniTask.dll",
            "UnityEngine.AndroidJNIModule.dll",
            "UnityEngine.CoreModule.dll",
            "mscorlib.dll",
            "spine-csharp.dll",
        };
#endif
#if UNITY_IOS
        return new List<string>
        {
            "DOTween.dll",
            "Google.Protobuf.dll",
            "Luban.Runtime.dll",
            "Newtonsoft.Json.dll",
            "System.Core.dll",
            "System.dll",
            "UniTask.dll",
            "Unity.Collections.dll",
            "UnityEngine.CoreModule.dll",
            "mscorlib.dll",
            "spine-unity.dll",
        };
#endif
    }

    /// <summary>
    /// 调试方法：打印所有已加载程序集
    /// </summary>
    public void PrintLoadedAssemblies()
    {
        LogUtlis.Info("=== 已加载程序集列表 ===");
        foreach (var kv in loadedAssemblies)
        {
            LogUtlis.Info($"- {kv.Key}: {kv.Value?.FullName}");
        }
        LogUtlis.Info($"共加载 {loadedAssemblies.Count} 个程序集");
    }
}