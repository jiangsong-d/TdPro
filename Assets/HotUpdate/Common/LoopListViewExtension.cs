using SuperScrollView;
using System;
using UnityEngine;

/// <summary>
/// LoopListView 的扩展方法（不修改原始代码）
/// </summary>
public static class LoopListViewExtension
{
    /// <summary>
    /// 初始化列表并强制指定父节点（外部封装方法）
    /// </summary>
    public static void InitWithParent(
        this LoopListView loopListView,int count,
        Action<Transform, int> refreshItemEvent,Transform parent,
        int initJumpIndex = -1, bool isReInit = true)
    {
        // 临时存储原始回调
        var originalCallback = LoopListViewHelp.onRefreshEvent;

        // 替换为新的回调（强制设置父节点）
        LoopListViewHelp.onRefreshEvent = (int comId, int index) =>
        {
            // 调用原始逻辑
            Transform item = originalCallback?.Invoke(comId, index);

            // 如果是新生成的项，设置父节点
            if (item != null && parent != null)
            {
                item.SetParent(parent, false);
                item.localPosition = Vector3.zero;
            }
            return item;
        };

        // 调用原始初始化方法
        LoopListViewHelp.InitListViewDefault(
            loopListView,
            count,
            refreshItemEvent,
            initJumpIndex,
            isReInit
        );

        // 恢复原始回调
        LoopListViewHelp.onRefreshEvent = originalCallback;
    }
}