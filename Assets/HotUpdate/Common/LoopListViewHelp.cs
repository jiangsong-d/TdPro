using System;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;

/// <summary>
/// looplist 容器 管理每个list的自己的事件
/// </summary>
public class LoopListViewListener : GameSingleton<LoopListViewListener>
{
    public Dictionary<int, LoopListViewHelp.OnRefreshAction> onRefreshEvents =
        new Dictionary<int, LoopListViewHelp.OnRefreshAction>();

    /// <summary>
    /// 选择
    /// </summary>
    public Dictionary<int, LoopListViewHelp.OnRefreshAction> SelectCallBack =
        new Dictionary<int, LoopListViewHelp.OnRefreshAction>();

    /// <summary>
    /// 停止
    /// </summary>
    public Dictionary<int, Action> onEndDragEvens = new Dictionary<int, Action>();

    public void AddListener(LoopListView loopListView, LoopListViewHelp.OnRefreshAction getItemEvent)
    {
        if (!onRefreshEvents.ContainsKey(loopListView.GetInstanceID()))
            onRefreshEvents.Add(loopListView.GetInstanceID(), getItemEvent);
    }

    public void AddSelectLister(LoopListView loopListView, LoopListViewHelp.OnRefreshAction callback)
    {
        SelectCallBack.Add(loopListView.GetInstanceID(), callback);
    }

    /// <summary>
    /// 注册停止事件
    /// </summary>
    /// <param name="loopListView"></param>
    /// <param name="callback"></param>
    public void RegisterEndDragListener(LoopListView loopListView, Action callback)
    {
        onEndDragEvens.Add(loopListView.GetInstanceID(), callback);
        LoopListViewHelp.RegisterEndDragEvent(loopListView, loopListView.GetInstanceID());
    }

    public void RemoveListener(LoopListView loopListView)
    {
        onRefreshEvents.Remove(loopListView.GetInstanceID());
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        onRefreshEvents.Clear();
        SelectCallBack.Clear();
        onEndDragEvens.Clear();
    }

    /// <summary>
    /// 移除停止事件
    /// </summary>
    /// <param name="loopListView"></param>
    public void UnRegisterEndDragListener(LoopListView loopListView)
    {
        onEndDragEvens.Remove(loopListView.GetInstanceID());
        LoopListViewHelp.UnRegisterEndDragEvent(loopListView);
    }

    public void RegisterToTopAndBottomListener(LoopListView loopListView, Action<bool> action)
    {
        RegisterEndDragListener(loopListView, () =>
        {
            if (loopListView.EndDragDelta < 0)
            {
                if (loopListView.ItemViewFirstIndex == 0)
                {
                    if (action != null)
                    {
                        action(true);
                    }
                }
            }
            else if (loopListView.EndDragDelta > 0)
            {
                if (loopListView.ItemViewLastIndex == loopListView.ItemTotalCount - 1)
                {
                    if (action != null)
                    {
                        action(false);
                    }
                }
            }
        });
    }
}


public static class LoopListViewHelp
{
    public delegate Transform OnRefreshAction(int comId, int index);

    public delegate void OnDestroyAction(int comId);

    /// <summary>
    /// 刷新
    /// </summary>
    public static OnRefreshAction onRefreshEvent = (int comId, int Index) =>
    {
        OnRefreshAction action = LoopListViewListener.Instance.onRefreshEvents[comId];
        if (action != null)
        {
            return action(comId, Index);
        }

        return null;
    };

    /// <summary>
    /// 删除
    /// </summary>
    public static OnDestroyAction onDestroyEvent = (int comId) =>
    {
        LoopListViewListener.Instance.onRefreshEvents[comId] = null;
        LoopListViewListener.Instance.onEndDragEvens[comId] = null;
        LoopListViewListener.Instance.SelectCallBack[comId] = null;
    };

    public static OnRefreshAction onEndDragEvent = (int comId, int Index) =>
    {
        Action action = LoopListViewListener.Instance.onEndDragEvens[comId];
        if (action != null)
        {
            action();
        }

        return null;
    };

    public static OnRefreshAction OnCenterDragEvent = (int comId, int Index) =>
    {
        OnRefreshAction action = LoopListViewListener.Instance.SelectCallBack[comId];
        if (action != null)
        {
            action(comId, Index);
        }

        return null;
    };

    static LoopListViewHelp()
    {
        LoopListView.onLoopListViewDestroyEvent += (loopListView) =>
        {
            if (onDestroyEvent != null)
            {
                onDestroyEvent(loopListView.GetInstanceID());
            }
        };
    }

    /// <summary>
    /// 重置helper，清理监听器，便于再次调用InitGridView
    /// </summary>
    public static void Release()
    {
        onRefreshEvent = null;
        onDestroyEvent = null;
        OnCenterDragEvent = null;
        onEndDragEvent = null;
    }

    /// <summary>
    /// 无限循环滚动选择的列表
    /// </summary>
    /// <param name="loopListView"></param>
    /// <param name="count"></param>
    public static void InitListView2(LoopListView loopListView, int count)
    {
        var objId = loopListView.GetInstanceID();
        loopListView.mOnSnapNearestChanged = (view, item) =>
        {
            int index = view.GetIndexInShownItemList(item);
            if (index < 0)
            {
                return;
            }

            if (OnCenterDragEvent != null)
            {
                OnCenterDragEvent(objId, index);
            }
        };
        loopListView.InitListView(-1, (view, index) =>
        {
            if (onRefreshEvent != null)
            {
                int firstItemVal = 1;
                int val = 0;
                if (index >= 0)
                {
                    val = index % count;
                }
                else
                {
                    val = count + ((index + 1) % count) - 1;
                }

                val = val + firstItemVal;
                var r = onRefreshEvent(objId, val);
                if (r == null)
                {
                    return null;
                }

                r.name = val.ToString(); //这里有问题是用名字当索引后面有空再优化吧
                return r.GetComponent<LoopListViewItem>();
            }

            return null;
        });
    }

    //暂时不提供外部使用
    private static void InitListView(LoopListView loopListView, int count, int initJumpIndex)
    {
        var objId = loopListView.GetInstanceID();
        loopListView.ResetGridView();
        loopListView.mOnSnapNearestChanged = (view, item) =>
        {
            int index = view.GetIndexInShownItemList(item);
            if (index < 0)
            {
                return;
            }

            if (OnCenterDragEvent != null)
            {
                OnCenterDragEvent(objId, item.ItemIndex);
            }
        };
        loopListView.InitListView(count, (LoopListView arg1, int index) =>
        {
            if (index < 0 || index >= arg1.ItemTotalCount)
            {
                return null;
            }

            if (onRefreshEvent != null)
            {
                var r = onRefreshEvent(objId, index);
                if (r == null)
                {
                    return null;
                }

                return r.GetComponent<LoopListViewItem>();
            }

            return null;
        }, null, initJumpIndex);
    }

    /// <summary>
    /// 一般初始化，常用
    /// </summary>
    /// <param name="loopListView"></param>
    /// <param name="count"></param>
    /// <param name="refreshItemEvent"></param>
    /// <param name="initJumpIndex"></param>
    /// <param name="isReInit">再次调用初始化时，需要设置为true，会重新初始化</param>
    public static void InitListViewDefault(LoopListView loopListView, int count,
        Action<Transform, int> refreshItemEvent, int initJumpIndex = -1, bool isReInit = true)
    {
        LoopListViewListener.Instance.AddListener(loopListView, (int comId, int index) =>
        {
            Transform trans = GetListViewItem(loopListView, index);
            if (trans == null)
            {
                trans = NewListViewItem(loopListView, "");
            }

            refreshItemEvent(trans, index);
            return trans;
        });

        InitListView(loopListView, count, initJumpIndex);
        if (initJumpIndex != -1)
        {
            loopListView.MovePanelToItemIndex(initJumpIndex, 0);
        }

        if (isReInit)
        {
            for (int i = 0; i < count; i++)
            {
                if (GetListViewItem(loopListView, i) != null)
                    refreshItemEvent(GetListViewItem(loopListView, i), i);
            }
        }
    }

    public static Transform NewListViewItem(LoopListView arg1, string name)
    {
        var item = arg1.NewListViewItem(name);
        return item.transform;
    }

    public static void RefreshListViewItemAll(LoopListView loopListView, int itemTotalCount, int resetPos)
    {
        if (itemTotalCount < 0 || loopListView.ItemTotalCount == itemTotalCount)
        {
            if (resetPos != 0)
            {
                loopListView.MovePanelToItemIndex(0, 0);
            }
            else
            {
                loopListView.RefreshAllShownItem();
            }
        }
        else
        {
            //这里面可能存在bug, 设置长度不一定会调用到刷新, ?需要测试
            loopListView.SetListItemCount(itemTotalCount, resetPos != 0 ? 0 : -1);
            //loopListView.ResetListView
        }
    }
    /// <summary>
    /// 刷新显示单位
    /// </summary>
    /// <param name="loopListView"></param>
    public static void RefreshShowItems(LoopListView loopListView)
    {
        if (loopListView.ItemViewFirstIndex == -1)
        {
            return;
        }
        for (int i = loopListView.ItemViewFirstIndex; i < loopListView.ShownItemCount; i++)
        {
            onRefreshEvent(loopListView.GetInstanceID(), i);
        }
    }
    public static Transform GetListViewItem(LoopListView loopListView, int itemIndex)
    {
        var item = loopListView.GetShownItemByItemIndex(itemIndex);
        if (item == null)
            return null;
        return item.transform;
    }

    public static Transform GetShowListViewItem(LoopListView loopListView, int showIndex)
    {
        var item = loopListView.GetShownItemByIndex(showIndex);
        if (item == null)
            return null;
        return item.transform;
    }

    public static void SetViewItemSize(LoopListView loopListView, int itemIndex, int Size)
    {
        var item = loopListView.GetShownItemByItemIndex(itemIndex);
        if (item == null)
            return;
        RectTransform rt = item.GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Size);
        //rt.sizeDelta = new Vector2(rt.sizeDelta.x, Size);
    }

    public static void OnItemSizeChanged(LoopListView loopListView, int itemIndex)
    {
        loopListView.OnItemSizeChanged(itemIndex);
    }

    public static void SetViewItemWidth(Transform trans, float itemWidth)
    {
        LoopListViewItem listViewItem = trans.GetComponent<LoopListViewItem>();
        listViewItem.SetItemWidth(itemWidth);
    }

    public static void SetViewItemHeight(Transform trans, float itemHeight)
    {
        LoopListViewItem listViewItem = trans.GetComponent<LoopListViewItem>();
        listViewItem.SetItemHeight(itemHeight);
    }

    public static int GetItemIndex(Transform trans)
    {
        LoopListViewItem listViewItem = trans.GetComponent<LoopListViewItem>();
        return listViewItem.ItemIndex;
    }

    public static void RegisterEndDragEvent(LoopListView loopListView, int objId)
    {
        loopListView.mOnEndDragAction = () => { onEndDragEvent(objId, 0); };
    }

    public static void UnRegisterEndDragEvent(LoopListView loopListView)
    {
        loopListView.mOnEndDragAction = null;
    }


    public static float GetDistanceWithViewPortSnapCenter(Transform trans)
    {
        LoopListViewItem listViewItem = trans.GetComponent<LoopListViewItem>();
        return listViewItem.DistanceWithViewPortSnapCenter;
    }
}