using System.Collections;
using System.IO;
using UnityEngine;
using UpdetaFramework;
using YooAsset;
/// <summary>
/// 初始化yoo系统
/// </summary>
public class InitializePackage : IStateNode
{
    private StateMachine Machine;
    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }

    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("InitYooResouce");
        // PatchEventDefine.ShowTipEvent.SendEventMessage(1);
        Launcher.Instance.StartCoroutine(InitPackage());
        // 创建资源包裹类

        

    }
    public void OnUpdate()
    {

    }
    public void OnExit()
    {
    }
    private IEnumerator InitPackage()
    {
        string packageName = YooManager.Instance.packageName;
        // 资源包裹类
        var package = YooManager.Instance.GetResourcePackage();
        var playMode = Launcher.Instance.ePlayMode;
        if(package==null)
            package = YooAssets.CreatePackage(packageName);

        Debug.Log($"[调试] package: {package}");

        InitializationOperation initializationOperation = null;

        yield return null;

        switch (playMode)
        {
            case EPlayMode.EditorSimulateMode:
                initializationOperation = InitEditorMode();
                break;
            case EPlayMode.HostPlayMode:
                initializationOperation = InitHostMode();
                break;
            case EPlayMode.OfflinePlayMode:
                initializationOperation = InitOfflineMode();
                break;
        }

        yield return initializationOperation;

        if (initializationOperation == null)
        {
            PatchEventDefine.InitializeFailed.SendEventMessage();
            yield break;
        }
        // 如果初始化失败弹出提示界面
        if (initializationOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning($"{initializationOperation.Error}");
            PatchEventDefine.InitializeFailed.SendEventMessage();

        }
        else
        {
            switch (playMode)
            {
                case EPlayMode.HostPlayMode:
                    Machine.ChangeState<RequestPackageVersion>();
                    break;
                case EPlayMode.EditorSimulateMode:
                    Machine.ChangeState<RequestPackageVersion>();
                    break;
                case EPlayMode.OfflinePlayMode:
                    Machine.ChangeState<RequestPackageVersion>();
                    break;
            }
        }


        // 在线模式
        InitializationOperation InitHostMode()
        {
            //联机运行模式
            string defaultHostServer = YooManager.Instance.GetHostServerURL();
            string fallbackHostServer = YooManager.Instance.GetHostServerURL();
            Debug.Log(defaultHostServer);
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            initParameters.CacheFileSystemParameters = cacheFileSystemParams;
            return package.InitializeAsync(initParameters);
        }
        InitializationOperation InitEditorMode()
        {
            var buildResult = EditorSimulateModeHelper.SimulateBuild(YooManager.Instance.packageName);
            var packageRoot = buildResult.PackageRootDirectory;
            var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var initParameters = new EditorSimulateModeParameters();
            initParameters.EditorFileSystemParameters = editorFileSystemParams;
            return package.InitializeAsync(initParameters);
        }
        
        InitializationOperation InitOfflineMode()
        {
            var initParameters = new OfflinePlayModeParameters();
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            return package.InitializeAsync(initParameters);
        }

    }
}
