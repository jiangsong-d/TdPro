using System.Collections;
using UpdetaFramework;
using YooAsset;
/// <summary>
/// 开始下载文件
/// </summary>
public class DownloadPackageFiles : IStateNode
{
    private StateMachine Machine;

    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }
    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("Updating...");
        Launcher.Instance.StartCoroutine(BeginDownload());
    }
    public void OnUpdate()
    {
    }
    public void OnExit()
    {
    }

    private IEnumerator BeginDownload()
    {
        var downloader = YooManager.Instance.operation;
        var package = YooAssets.GetPackage(YooManager.Instance.packageName);
        downloader.DownloadErrorCallback = PatchEventDefine.WebFileDownloadFailed.SendEventMessage;
        downloader.DownloadUpdateCallback = PatchEventDefine.DownloadUpdate.SendEventMessage;
        downloader.BeginDownload();
        yield return downloader;

        // 检测下载结果
        if (downloader.Status != EOperationStatus.Succeed)
            yield break;


        LogUtlis.Info("下载资源文件完成！");
        //判断是否下载成功
        //HybridManager.Instance.LoadUpdateAsset();

        Machine.ChangeState<DownloadPackageOver>();
    }
}
