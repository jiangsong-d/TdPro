using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpdetaFramework;
using YooAsset;

/// <summary>
/// 请求版本信息
/// </summary>
public class RequestPackageVersion : IStateNode
{
    private StateMachine Machine;
    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }

    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("RequestVersionInfo");
        Launcher.Instance.StartCoroutine(UpdatePackageVersion());
    }
    public void OnUpdate()
    {

    }
    public void OnExit()
    {
    }

    /// <summary>
    /// 更新资源版本信息
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdatePackageVersion()
    {
        string packageName = YooManager.Instance.packageName;
        var package = YooAssets.GetPackage(packageName);
        var operation = package.RequestPackageVersionAsync();
        yield return operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            LogUtlis.Error(operation.Error);
            PatchEventDefine.PackageVersionRequestFailed.SendEventMessage();
        }
        else
        {
            LogUtlis.Info($"Request package version : {operation.PackageVersion}");
            YooManager.Instance.packageVersion = operation.PackageVersion;
            Machine.ChangeState<UpdatePackageManifest>();
        }
    }
}
