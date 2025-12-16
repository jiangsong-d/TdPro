using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 窗口显隐标志位
/// </summary>
public class ViewActiveFlag
{
    public const int None = 0;        // 无显隐标志位
    public const int LogicalFlag = 1 << 0;    // 逻辑显隐标志位(有值标识隐藏)
    public const int FullScreenStrategyFlag = 1 << 1;// 全屏策略显隐标志位(有值标识隐藏)
}
