using System;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;
using static LoopListViewHelp;

public class LoopGridViewListener : GameSingleton<LoopGridViewListener>
{
    public Dictionary<int, LoopGridViewHelp.OnRefreshAction> onRefreshEvents =
        new Dictionary<int, LoopGridViewHelp.OnRefreshAction>();

    public void AddListener(LoopGridView loopGridView, LoopGridViewHelp.OnRefreshAction getItemEvent)
    {
        if (!onRefreshEvents.ContainsKey(loopGridView.GetInstanceID()))
            onRefreshEvents.Add(loopGridView.GetInstanceID(), getItemEvent);
    }

    /// <summary>
    /// 选择
    /// </summary>
    public Dictionary<int, LoopGridViewHelp.OnRefreshAction> SelectCallBack =
        new Dictionary<int, LoopGridViewHelp.OnRefreshAction>();

    /// <summary>
    /// 停止
    /// </summary>
    public Dictionary<int, Action> onEndDragEvens = new Dictionary<int, Action>();
}

public static class LoopGridViewHelp
{
    public delegate Transform OnRefreshAction(int comId, int index);

    // item刷新时
    public static OnRefreshAction onRefreshEvent = (int comId, int Index) =>
    {
        OnRefreshAction action = LoopGridViewListener.Instance.onRefreshEvents[comId];
        if (action != null)
        {
            return action(comId, Index);
        }

        return null;
    };

    // 拖拽结束
    public static OnRefreshAction onEndDragEvent = null;

    // 销毁时
    public static OnDestroyAction onDestroyEvent = null;

    static LoopGridViewHelp()
    {
        LoopGridView.onViewDestroyEvent += (loopGridView) =>
        {
            if (onDestroyEvent != null)
            {
                onDestroyEvent(loopGridView.GetInstanceID());
            }
        };
    }

    public static void Release()
    {
        onRefreshEvent = null;
        onEndDragEvent = null;
        onDestroyEvent = null;
    }

    // 初始化视图
    public static void InitGridView(LoopGridView loopGridView, int count)
    {
        var objId = loopGridView.GetInstanceID();
        loopGridView.ResetGridView();
        loopGridView.InitGridView(count, (LoopGridView gridView, int itemIndex, int row, int column) =>
        {
            if (itemIndex < 0 || itemIndex >= gridView.ItemTotalCount)
            {
            }

            if (onRefreshEvent != null)
            {
                var r = onRefreshEvent(objId, itemIndex);
                if (r == null)
                {
                    return null;
                }

                return r.GetComponent<LoopGridViewItem>();
            }

            return null;
        });
    }

    /// <summary>
    /// 一般初始化，常用
    /// </summary>
    /// <param name="loopGridView"></param>
    /// <param name="count"></param>
    /// <param name="refreshItemEvent"></param>
    /// <param name="isReInit">再次调用初始化时，需要设置为true，会重新初始化</param>
    public static void InitGirdViewDefault(LoopGridView loopGridView, int count,
        Action<Transform, int> refreshItemEvent, bool isReInit = false)
    {
        LoopGridViewListener.Instance.AddListener(loopGridView, (int comId, int index) =>
        {
            Transform trans = GetGridViewItem(loopGridView, index);
            if (trans == null)
            {
                trans = NewGridViewItem(loopGridView, "");
            }

            refreshItemEvent(trans, index);
            return trans;
        });
        InitGridView(loopGridView, count);
        if (isReInit)
        {
            for (int i = 0; i < count; i++)
            {
                if (GetGridViewItem(loopGridView, i) != null)
                    refreshItemEvent(GetGridViewItem(loopGridView, i), i);
            }
        }
    }

    public static Transform NewGridViewItem(LoopGridView arg1, string name)
    {
        var item = arg1.NewListViewItem(name);
        return item.transform;
    }

    public static void RefreshGridViewItemAll(LoopGridView loopGridView, int itemTotalCount, int resetPos)
    {
        if (itemTotalCount < 0 || loopGridView.ItemTotalCount == itemTotalCount)
        {
            if (resetPos != 0)
            {
                loopGridView.MovePanelToItemByRowColumn(0, 0);
            }
            else
            {
                loopGridView.RefreshAllShownItem();
            }
        }
        else
        {
            loopGridView.SetGridItemCount(itemTotalCount, resetPos != 0);
        }
    }


    /// <summary>
    /// 刷新显示的单位
    /// </summary>
    /// <param name="loopGridView"></param>
    public static void RefreshShowItems(LoopGridView loopGridView)
    {
        var firstItem = loopGridView.ItemViewFirstIndex;
        var lastItem= loopGridView.ItemViewLastIndex;
        // 获得所有打开的对象
        for (int i = firstItem; i <= lastItem; i++)
        {
            onRefreshEvent(loopGridView.GetInstanceID(), i);
        }
    }
    public static Transform GetGridViewItem(LoopGridView LoopGridView, int itemIndex)
    {
        var item = LoopGridView.GetShownItemByItemIndex(itemIndex);
        if (item == null)
            return null;
        return item.transform;
    }

    /// <summary>
    /// 播放动画(简单的写了个动画如果需要组动画需要扩展)
    /// </summary>
    /// <param name="loopGridView"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="time"></param>
    /// <param name="interval"></param>
    public static void PlayAnim(LoopGridView loopGridView, float x, float y, float time, float interval)
    {
        loopGridView.PlayInitAnim(new Vector2(x, y), time, interval);
    }

    // 构建一个视图item
    public static Transform NewViewItem(LoopGridView gridView, string name)
    {
        var item = gridView.NewListViewItem(name);
        return item.transform;
    }

    // 获取指定索引下的视图item
    public static Transform GetItemByItemIndex(LoopGridView view, int itemIndex)
    {
        var itemview = view.GetShownItemByItemIndex(itemIndex);
        if (itemview == null)
        {
            return null;
        }

        return itemview.transform;
    }

    // 设置item长度
    public static void SetItemCount(LoopGridView gridView, int itemCount, int iresetPos)
    {
        var resetPos = iresetPos != 0;
        gridView.SetGridItemCount(itemCount, resetPos);
    }

    // 注册拖拽结束事件
    public static void RegisterEndDragEvent(LoopGridView view, int objId)
    {
        view.mOnEndDragAction = (data) => { onEndDragEvent(objId, 0); };
    }

    // 取消拖拽结束事件
    public static void UnRegisterEndDragEvent(LoopGridView view)
    {
        view.mOnEndDragAction = null;
    }

    /// <summary>
    /// 超宽屏适配方案
    /// </summary>
    /// <param name="loopGridView"></param>
    /// <exception cref="NotImplementedException"></exception>
    public static void UltraWideScreenAdapter(LoopGridView loopGridView)
    {
        // 用本地静态方法替代 ScreenUtil.IsUltraWideScreen()
        bool wideScreen = IsUltraWideScreen();
        if (!wideScreen) return;
        int horizontalPadding = loopGridView.Padding.left + loopGridView.Padding.right;
        float rectWidth = loopGridView.ViewPortWidth - horizontalPadding;
        int columnCount = Mathf.FloorToInt(rectWidth / loopGridView.ItemSizeWithPadding.x);
        if (columnCount < 1) columnCount = 1;
        loopGridView.SetGridFixedGroupCount(loopGridView.GridFixedType, columnCount);
        float paddingWidth = (rectWidth - loopGridView.ItemSize.x * columnCount) / (columnCount > 1 ? (columnCount - 1) : 1);
        loopGridView.SetItemPadding(new Vector2(paddingWidth, loopGridView.ItemPadding.y));
    }

    // 本地静态方法实现超宽屏判断，避免依赖 ScreenUtil 类
    private static bool IsUltraWideScreen()
    {
        float aspect = (float)Screen.width / Screen.height;
        return aspect > 2.0f; // 你可以根据实际需求调整阈值
    }
}