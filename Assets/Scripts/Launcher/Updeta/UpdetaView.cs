using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UpdetaFramework;
/// <summary>
/// 更新界面
/// </summary>
public class UpdetaView : MonoBehaviour
{
    public Slider slider;
    public Transform des;
    public Transform progress;
    // Start is called before the first frame update

    public TextMeshProUGUI TipsText;
    public TextMeshProUGUI progressText;

    private readonly EventGroup eventGroup = new EventGroup();
    
    public void Awake()
    {
        TipsText = des.GetComponent<TextMeshProUGUI>();
        progressText = progress.GetComponent<TextMeshProUGUI>();
       
        eventGroup.AddListener<PatchEventDefine.InitializeFailed>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.PatchStepsChange>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.FoundUpdateFiles>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.DownloadUpdate>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.PackageVersionRequestFailed>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.PackageManifestUpdateFailed>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.WebFileDownloadFailed>(OnHandleEventMessage);
        eventGroup.AddListener<PatchEventDefine.ShowTipEvent>(OnShowTip);
        
        

    }

    void OnShowTip(IEventMessage message)
    {
        if (message is PatchEventDefine.ShowTipEvent e)
        {
            // confirmTipView.ShowConfirmTip(e.Id);
        }
       
    }
    /// <summary>
    /// 处理事件消息
    /// </summary>
    private void OnHandleEventMessage(IEventMessage message)
    {
        if (message is PatchEventDefine.InitializeFailed)
        {
            System.Action callback = () =>
            {
                UserEventDefine.UserTryInitialize.SendEventMessage();
            };
            ShowMessageBox($"Failed to initialize package !", callback);
            ShowConfirmTip(7);
        }
        else if (message is PatchEventDefine.PatchStepsChange)
        {
            var msg = message as PatchEventDefine.PatchStepsChange;
            TipsText.text = msg.Tips;
            LogUtlis.Info(msg.Tips);
        }
        else if (message is PatchEventDefine.FoundUpdateFiles)
        {
            var msg = message as PatchEventDefine.FoundUpdateFiles;
            //System.Action callback = () =>
            //{

            //};
            UserEventDefine.UserBeginDownloadWebFiles.SendEventMessage();
            float sizeMB = msg.TotalSizeBytes / 1048576f;
            sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
            string totalSizeMB = sizeMB.ToString("f1");
            //ShowMessageBox($"Found update patch files, Total count {msg.TotalCount} Total szie {totalSizeMB}MB", callback);
        }
        else if (message is PatchEventDefine.DownloadUpdate)
        {
            var msg = message as PatchEventDefine.DownloadUpdate;
            slider.value = (float)(msg.CurrentDownloadSizeBytes / 1048576f) / (msg.TotalDownloadSizeBytes / 1048576f);
            string currentSizeMB = (msg.CurrentDownloadSizeBytes / 1048576f).ToString("f1");
            string totalSizeMB = (msg.TotalDownloadSizeBytes / 1048576f).ToString("f1");
            //{msg.CurrentDownloadCount}/{msg.TotalDownloadCount}  文件数量
            progressText.text = $"{currentSizeMB}MB/{totalSizeMB}MB {Math.Round((float)msg.CurrentDownloadCount / msg.TotalDownloadCount,2) * 100}%";
        }
        else if (message is PatchEventDefine.PackageVersionRequestFailed)
        {
            System.Action callback = () =>
            {
                UserEventDefine.UserTryRequestPackageVersion.SendEventMessage();
            };
            // ShowMessageBox($"Failed to update static version, please check the network status.", callback);
            ShowConfirmTip(1);
        }
        else if (message is PatchEventDefine.PackageManifestUpdateFailed)
        {
            System.Action callback = () =>
            {
                UserEventDefine.UserTryUpdatePackageManifest.SendEventMessage();
            };
            // ShowMessageBox($"Failed to update patch manifest, please check the network status.", callback);
            ShowConfirmTip(1);
        }
        else if (message is PatchEventDefine.WebFileDownloadFailed)
        {
            var msg = message as PatchEventDefine.WebFileDownloadFailed;
            System.Action callback = () =>
            {
                UserEventDefine.UserTryDownloadWebFiles.SendEventMessage();
            };
            // ShowMessageBox($"Failed to download file : {msg.FileName}", callback);
            ShowConfirmTip(7);
        }
        else
        {
            throw new System.NotImplementedException($"{message.GetType()}");
        }
    }

    void ShowMessageBox(string content, Action callback)
    {
        // confirmTipView.ShowTip("", content, callback);
    }
    void ShowConfirmTip(int id)
    {
        // confirmTipView.ShowConfirmTip(id);
    }
    public void Start()
    {

    }

    public void OnDestroy()
    {

    }
}
