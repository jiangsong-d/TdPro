using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 引擎事件管理器
/// 功能：提供全局事件的注册、触发、移除机制
/// 使用示例：
/// // 注册事件
/// EngineEventManager.Instance.AddEventListener(EventID.LOGIN_SUCCESS, OnLoginSuccess);
/// 
/// // 触发事件
/// EngineEventManager.Instance.DispatchEvent(new EngineEvent(EventID.LOGIN_SUCCESS, playerData));
/// 
/// // 移除事件
/// EngineEventManager.Instance.RemoveEventListener(EventID.LOGIN_SUCCESS, OnLoginSuccess);
/// </summary>
public class EngineEventManager
{
    /// <summary>
    /// 事件回调委托定义
    /// </summary>
    /// <param name="engineEvent">事件对象，包含事件类型和参数</param>
    public delegate void EventCallback(EngineEvent engineEvent);

    /// <summary>
    /// 事件处理器字典：Key=事件ID, Value=事件处理器
    /// 使用Dictionary替代Hashtable，避免装箱拆箱操作，提升性能
    /// </summary>
    private Dictionary<int, EngineEventHandler> eventHashtable = null;

    /// <summary>
    /// 事件回调列表字典：Key=事件ID, Value=该事件的所有回调列表
    /// 用于追踪所有注册的回调，便于批量清理
    /// </summary>
    private Dictionary<int, List<EventCallback>> eventCallbackHashtable = null;

    /// <summary>
    /// 私有构造函数，确保单例模式
    /// </summary>
    private EngineEventManager()
    {
        eventHashtable = new Dictionary<int, EngineEventHandler>();
        eventCallbackHashtable = new Dictionary<int, List<EventCallback>>();
    }

    /// <summary>
    /// 单例实例
    /// </summary>
    private static EngineEventManager _instance = null;

    /// <summary>
    /// 获取事件管理器单例
    /// </summary>
    public static EngineEventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EngineEventManager();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="engineEvent">要触发的事件对象</param>
    /// <returns>true=事件已触发, false=事件未注册或无监听者</returns>
    public bool DispatchEvent(EngineEvent engineEvent)
    {
        // 使用TryGetValue在一次查找中同时判断存在和获取值
        if (eventHashtable.TryGetValue(engineEvent.eventType, out EngineEventHandler handler))
        {
            return handler.DispatchEvent(engineEvent);
        }
        return false;
    }

    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="eventID">事件ID，通常使用EventID类中定义的静态常量</param>
    /// <param name="eventCallback">事件回调函数</param>
    public void AddEventListener(int eventID, EventCallback eventCallback)
    {
        // 如果该事件ID还未注册，创建新的事件处理器
        if (!eventHashtable.ContainsKey(eventID))
        {
            eventHashtable.Add(eventID, new EngineEventHandler());
        }

        // 先移除再添加，防止重复注册
        // C#的事件+=操作符不会自动去重，需要手动处理
        eventHashtable[eventID].eventHander -= eventCallback;
        eventHashtable[eventID].eventHander += eventCallback;

        // 在回调追踪表中记录，用于批量清理
        if (!eventCallbackHashtable.ContainsKey(eventID))
        {
            eventCallbackHashtable.Add(eventID, new List<EventCallback>());
        }
        eventCallbackHashtable[eventID].Add(eventCallback);
    }

    /// <summary>
    /// 移除事件监听
    /// 重要：务必在对象销毁时移除监听，否则会导致内存泄漏
    /// </summary>
    /// <param name="eventID">事件ID</param>
    /// <param name="eventCallback">要移除的回调函数（必须与添加时的引用一致）</param>
    public void RemoveEventListener(int eventID, EventCallback eventCallback)
    {
        // 使用TryGetValue提升性能
        if (eventHashtable.TryGetValue(eventID, out EngineEventHandler handler))
        {
            // 从C#事件中移除回调
            handler.eventHander -= eventCallback;

            // 如果该事件已无任何监听者，从字典中移除，释放内存
            if (!handler.HasEvent)
            {
                eventHashtable.Remove(eventID);
            }
        }
    }

    /// <summary>
    /// 清除所有事件监听
    /// 使用场景：通常在退出游戏、切换场景等情况下调用
    /// </summary>
    public void ClearEventListener()
    {
        LogUtlis.Info("ClearEventListener");

        // 步骤1：先复制所有事件ID，避免遍历时修改集合导致异常
        List<int> keys = new List<int>(eventCallbackHashtable.Keys);

        // 步骤2：遍历每个事件ID
        foreach (int eventID in keys)
        {
            if (eventCallbackHashtable.TryGetValue(eventID, out List<EventCallback> callbacks))
            {
                // 步骤3：复制回调列表，避免在RemoveEventListener中修改正在遍历的列表
                EventCallback[] callbackArray = callbacks.ToArray();

                // 步骤4：逐个移除回调
                foreach (EventCallback callback in callbackArray)
                {
                    RemoveEventListener(eventID, callback);
                }
            }
        }

        // 步骤5：清空所有字典
        eventCallbackHashtable.Clear();
        eventHashtable.Clear();
    }

    /// <summary>
    /// 内部事件处理器类
    /// 功能：封装C#事件机制，管理单个事件ID的所有监听者
    /// </summary>
    class EngineEventHandler
    {
        /// <summary>
        /// C#事件：存储该事件的所有回调函数
        /// 使用event关键字确保外部只能+=和-=，不能直接赋值或调用
        /// </summary>
        public event EventCallback eventHander;

        /// <summary>
        /// 分发事件到所有监听者
        /// </summary>
        /// <param name="engineEvent">事件对象</param>
        /// <returns>true=事件已触发, false=无监听者</returns>
        public bool DispatchEvent(EngineEvent engineEvent)
        {
            // 检查是否有监听者
            if (eventHander != null)
            {
                // 依次调用所有注册的回调函数
                // 注意：如果某个回调抛异常，后续回调仍会执行
                eventHander(engineEvent);
                return true;
            }
            else
            {
                // 无监听者时输出日志，便于调试
                LogUtlis.Info("no " + engineEvent.eventType + " event");
                return false;
            }
        }

        /// <summary>
        /// 检查是否有事件监听者
        /// </summary>
        public bool HasEvent
        {
            get { return eventHander != null; }
        }
    }
}