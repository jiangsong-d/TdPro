using UpdetaFramework;
using YooAsset;
/// <summary>
/// 更新流程完成
/// </summary>
public class ClearCacheBundle : IStateNode
{
    private StateMachine Machine;

    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }
    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("");
        var packageName = YooManager.Instance.packageName;
        var package = YooAssets.GetPackage(packageName);
        var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
        operation.Completed += Operation_Completed;
    }
    public void OnUpdate()
    {
    }
    public void OnExit()
    {
    }
    private void Operation_Completed(YooAsset.AsyncOperationBase obj)
    {
        Machine.ChangeState<UpdetaComplete>();
    }
}
