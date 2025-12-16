using System.Collections;
using UnityEngine;
using UpdetaFramework;
using YooAsset;

public class UpdatePackageManifest : IStateNode
{
    private StateMachine Machine;

    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }
    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("");
        Launcher.Instance.StartCoroutine(UpdateManifest());
    }
    public void OnUpdate()
    {
    }
    public void OnExit()
    {
    }

    private IEnumerator UpdateManifest()
    {
        var packageName = YooManager.Instance.packageName;
        var packageVersion = YooManager.Instance.packageVersion;
        var package = YooAssets.GetPackage(packageName);
        var operation = package.UpdatePackageManifestAsync(packageVersion);
        yield return operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            LogUtlis.Warn(operation.Error);
            PatchEventDefine.PackageManifestUpdateFailed.SendEventMessage();
            yield break;
        }
        else
        {
            Machine.ChangeState<DownloaderResPackage>();
        }
    }
}
