

public interface ILoadingStep
{
    /// <summary>
    /// 是否执行步骤完成
    /// </summary>
    bool IsComplete { get; set; }

    /// <summary>
    /// 执行步骤
    /// </summary>
    /// <returns></returns>
    void Execute();

    /// <summary>
    /// 执行步骤完成
    /// </summary>
    void OnComplete();

    /// <summary>
    /// 步骤进度
    /// </summary>
    float Progress { get; set; }
    /// <summary>
    /// 启动模块名称
    /// </summary>
    string ModelName { get; set; }
}
