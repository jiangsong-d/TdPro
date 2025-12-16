using UpdetaFramework;
using YooAsset;

public class PatchEventDefine
{
    /// <summary>
    /// 补丁包初始化失败
    /// </summary>
    public class InitializeFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new InitializeFailed();
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 补丁流程步骤改变
    /// </summary>
    public class PatchStepsChange : IEventMessage
    {
        public string Tips;

        public static void SendEventMessage(string tips)
        {
            var msg = new PatchStepsChange();
            msg.Tips = tips;
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 发现更新文件
    /// </summary>
    public class FoundUpdateFiles : IEventMessage
    {
        public int TotalCount;
        public long TotalSizeBytes;

        public static void SendEventMessage(int totalCount, long totalSizeBytes)
        {
            var msg = new FoundUpdateFiles();
            msg.TotalCount = totalCount;
            msg.TotalSizeBytes = totalSizeBytes;
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 下载进度更新
    /// </summary>
    public class DownloadUpdate : IEventMessage
    {
        public int TotalDownloadCount;
        public int CurrentDownloadCount;
        public long TotalDownloadSizeBytes;
        public long CurrentDownloadSizeBytes;

        public static void SendEventMessage(DownloadUpdateData downloadUpdateData)
        {
            var msg = new DownloadUpdate();
            msg.TotalDownloadCount = downloadUpdateData.TotalDownloadCount;
            msg.CurrentDownloadCount = downloadUpdateData.CurrentDownloadCount;
            msg.TotalDownloadSizeBytes = downloadUpdateData.TotalDownloadBytes;
            msg.CurrentDownloadSizeBytes = downloadUpdateData.CurrentDownloadBytes;
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 资源版本请求失败
    /// </summary>
    public class PackageVersionRequestFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new PackageVersionRequestFailed();
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 资源清单更新失败
    /// </summary>
    public class PackageManifestUpdateFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new PackageManifestUpdateFailed();
            UpdetaEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 网络文件下载失败
    /// </summary>
    public class WebFileDownloadFailed : IEventMessage
    {
        public string FileName;
        public string Error;

        public static void SendEventMessage(DownloadErrorData downloadErrorData)
        {

            var msg = new WebFileDownloadFailed();
            msg.FileName = downloadErrorData.FileName;
            msg.Error = downloadErrorData.ErrorInfo;
            UpdetaEvent.SendMessage(msg);
        }
    }
    /// <summary>
    /// 网络文件下载失败
    /// </summary>
    public class ShowTipEvent : IEventMessage
    {
        public int Id;
        public static void SendEventMessage(int id)
        {

            var msg = new ShowTipEvent();
            msg.Id = id;
            UpdetaEvent.SendMessage(msg);
        }
    }
}