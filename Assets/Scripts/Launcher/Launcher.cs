
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using YooAsset;


public enum LangType
{
    CN,
    EN,
    JP,
}
public enum SdkType
{
    None,
    CiLiSdkAgent,

}

public class Launcher : MonoSingleton<Launcher>
{
    [HeaderAttribute("======服务器地址设置======")]

    [Label("Ip地址")]
    public string httpIp = "";
    [HeaderAttribute("======Sdk设置======")]
    [Label("Sdk开启")]
    public SdkType sdkType = SdkType.None;

    [HeaderAttribute("======日志设置======")]
    [Label("日志开关")]
    public bool enableLog = true;
    [Label("日志等级")]
    public LogLevel logLevel = LogLevel.Info;

    [HeaderAttribute("======语言======")]
    [Label("打包语言设置")]
    public LangType LangType = LangType.CN;
    [HeaderAttribute("======Yoo======")]
    [Label("运行模式")]
    public EPlayMode ePlayMode = EPlayMode.EditorSimulateMode;
    [Label("Yoo资源包名")]
    public string packageName = "DefaultPackage";

    [HeaderAttribute("======刘海屏测试======")]
    [Label("是否是刘海屏")]
    public bool isLiuHaiPing = false;
    private GameObject UpdetaView;
    protected override void Init()
    {
        InitLog();

        if (IsEditor())
        {
            ePlayMode = EPlayMode.EditorSimulateMode;
        }
        else
        {
            ePlayMode = GetResourceMode();
        }
        
        LogUtlis.Info($"当前运行模式: {ePlayMode}");
        
        //  获取当前 URP Asset
        // var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        // if (urpAsset != null)
        // {
        //     // 启用 SRP Batcher
        //     urpAsset.useSRPBatcher = true;
        //     // 启用动态批处理（若需要）
        //     urpAsset.supportsDynamicBatching = true;
        // }

        // 4. 启动应用
        StartCoroutine(AppStartUp());
    }
    public static EPlayMode GetResourceMode()
    {
#if RESOURCE_OFFLINE
        return EPlayMode.OfflinePlayMode;
#elif RESOURCE_ASSETBUNDLE
            return EPlayMode.HostPlayMode;
#else
        return EPlayMode.EditorSimulateMode;
#endif
    }
    private IEnumerator AppStartUp()
    {
        // 设置为默认运行
        Application.runInBackground = true;
        LogUtlis.Info(this, "初始化环境");

        YooManager.Instance.SetPackageName(packageName);

        if (!IsEditor())
        {
            yield return HybridManager.Instance.LoadAOTMetadata();
        }

        yield return null;

        // 再实例化UIModel
        if (UIModel.Inst == null)
        {
            GameObject go = Resources.Load<GameObject>("Prefabs/UIModel/UIModel");
            GameObject.Instantiate(go).name = "UIModel";

            GameObject cameraGo = Resources.Load<GameObject>("Prefabs/UIModel/CameraManager");
            if (cameraGo != null)
            {
                GameObject cameraInstance = GameObject.Instantiate(cameraGo);
                cameraInstance.name = "CameraManager";
                
                // 将CameraManager放到DontDestroyOnLoad的Boot对象下
                GameObject bootObject = GameObject.Find("Boot");
                if (bootObject == null)
                {
                    bootObject = new GameObject("Boot");
                    DontDestroyOnLoad(bootObject);
                }
                cameraInstance.transform.SetParent(bootObject.transform, false);
            }
        }
        
        UIModel.Inst.Init();
        while (!UIModel.Inst.IsInitFinish)
        {
            yield return null;
        }
        // GameObject Camera = Resources.Load<GameObject>("Prefabs/UIModel/CameraManager");
        // GameObject.Instantiate(Camera);
        yield return null;
        
        YooManager.Instance.Initialized();
        yield return null;

        StartUpdate();
        yield return null;
        // 开始补丁更新流程
        var operation = new PatchOperation();
        YooAssets.StartOperation(operation);
        yield return operation;

        var gamePackage = YooAssets.GetPackage(packageName);
        YooAssets.SetDefaultPackage(gamePackage);

        yield return null;

        //StartGame();
    }


    public void StartUpdate()
    {
        UpdetaView = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UpdetaView"));
        UpdetaView.transform.localPosition = Vector3.zero;
    }

    private void InitLog()
    {
        // 设置日志等级
        Debuger.LogLevel = logLevel;
        
        // 设置日志开关
        if (IsEditor())
        {
            LogUtlis.EnableLog = enableLog; // 编辑器模式使用配置的值
        }
        else
        {
            LogUtlis.EnableLog = false; // 非编辑器模式默认关闭
        }
    }
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {

        LogUtlis.Info("运行热更代码");
        StartCoroutine(HybridManager.Instance.InitGameManager());
    }
    public void CloseUpdetaView()
    {
        if (UpdetaView != null)
        {
            GameObject.Destroy(UpdetaView);
        }
    }
    /// <summary>
    /// 是否编辑器模式
    /// </summary>
    public bool IsEditor()
    {
#if UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }
}
