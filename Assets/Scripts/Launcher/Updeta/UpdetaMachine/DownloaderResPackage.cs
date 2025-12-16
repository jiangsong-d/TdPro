using UpdetaFramework;
using YooAsset;
/// <summary>
/// 创建资源下载器
/// </summary>
public class DownloaderResPackage : IStateNode
{
    private StateMachine Machine;
    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }

    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("");
        CreateDownloader();
    }
    public void OnUpdate()
    {

    }
    public void OnExit()
    {
    }
    void CreateDownloader()
    {
        var packageName = YooManager.Instance.packageName;
        var package = YooAssets.GetPackage(packageName);
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
        YooManager.Instance.operation = downloader;
        if (downloader.TotalDownloadCount == 0)
        {
            LogUtlis.Info("没有需要更新的资源!");
            Machine.ChangeState<UpdetaComplete>();
        }
        else
        {
            // 发现新更新文件后，挂起流程系统
            // 注意：开发者需要在下载前检测磁盘空间不足
            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;
            PatchEventDefine.FoundUpdateFiles.SendEventMessage(totalDownloadCount, totalDownloadBytes);
        }
    }
    // public IEnumerator Complete()
    // {
    //     yield return Launcher.Instance.StartCoroutine(HybridManager.Instance.LoadUpdateAsset());
    //     Machine.ChangeState<UpdetaComplete>();
    // }
}
