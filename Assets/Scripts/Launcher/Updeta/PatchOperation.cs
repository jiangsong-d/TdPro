using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpdetaFramework;
using YooAsset;
/// <summary>
/// 补丁更新操作类
/// </summary>
public class PatchOperation : GameAsyncOperation
{
    private enum ESteps
    {
        None,
        Update,
        Done,
    }
    private readonly EventGroup eventGroup = new EventGroup();
    /// <summary>
    /// 状态机
    /// </summary>
    private readonly StateMachine UpMachine;
    private readonly string packageName;
    private ESteps Steps = ESteps.None;

    public PatchOperation()
    {
        eventGroup.AddListener<UserEventDefine.UserTryInitialize>(OnHandleEventMessage);
        eventGroup.AddListener<UserEventDefine.UserBeginDownloadWebFiles>(OnHandleEventMessage);
        eventGroup.AddListener<UserEventDefine.UserTryRequestPackageVersion>(OnHandleEventMessage);
        eventGroup.AddListener<UserEventDefine.UserTryUpdatePackageManifest>(OnHandleEventMessage);
        eventGroup.AddListener<UserEventDefine.UserTryDownloadWebFiles>(OnHandleEventMessage);
        UpMachine = new StateMachine(this);
        //初始化yooAseet包体
        UpMachine.AddNode<InitializePackage>();
        //检查版本更新
        UpMachine.AddNode<RequestPackageVersion>();
        //更新包体清单
        UpMachine.AddNode<UpdatePackageManifest>();
        //创建下载器
        UpMachine.AddNode<DownloaderResPackage>();
        //下载资源文件
        UpMachine.AddNode<DownloadPackageFiles>();
        //下载资源文件完成
        UpMachine.AddNode<DownloadPackageOver>();
        //清理缓存
        UpMachine.AddNode<ClearCacheBundle>();
        ///补充AOT元数据
        // UpMachine.AddNode<SupplementAOTAssemblies>();
        //更新流程完成
        UpMachine.AddNode<UpdetaComplete>();

    }

    protected override void OnStart()
    {
        Steps = ESteps.Update;
        UpMachine.Run<InitializePackage>();
    }
    protected override void OnUpdate()
    {
        if (Steps == ESteps.None || Steps == ESteps.Done)
            return;

        if (Steps == ESteps.Update)
        {
            UpMachine.Update();
        }
    }
    protected override void OnAbort()
    {
    }
    public void SetFinish()
    {
        Steps = ESteps.Done;
        eventGroup.RemoveAllListener();
        Status = EOperationStatus.Succeed;
        LogUtlis.Info($"Package {packageName} patch done !");
    }
    private void OnHandleEventMessage(IEventMessage message)
    {
        LogUtlis.Info("PatchOperation" + message);
        if (message is UserEventDefine.UserTryInitialize)
        {
            UpMachine.ChangeState<InitializePackage>();
        }
        else if (message is UserEventDefine.UserBeginDownloadWebFiles)
        {
            UpMachine.ChangeState<DownloadPackageFiles>();
        }
        else if (message is UserEventDefine.UserTryRequestPackageVersion)
        {
            UpMachine.ChangeState<RequestPackageVersion>();
        }
        else if (message is UserEventDefine.UserTryUpdatePackageManifest)
        {
            UpMachine.ChangeState<UpdatePackageManifest>();
        }
        else if (message is UserEventDefine.UserTryDownloadWebFiles)
        {
            UpMachine.ChangeState<DownloaderResPackage>();
        }
        else
        {
            throw new System.NotImplementedException($"{message.GetType()}");
        }
    }
}
