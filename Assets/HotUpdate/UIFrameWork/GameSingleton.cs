using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 游戏单例（线程安全）
/// </summary>
public class GameSingleton<T> where T : GameSingleton<T>, new()
{
    private static T instance = null;
    private static readonly object lockObj = new object(); // 线程锁对象

    private Dictionary<int, EngineEventManager.EventCallback> EventIDList;

    /// <summary> 返回单例（线程安全） </summary>
    public static T Instance
    {
        get
        {
            // 双重检查锁定模式（Double-Check Locking）
            if (instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new T();
                        instance.Init(); // 自动初始化
                    }
                }
            }
            return instance;
        }
    }
    
    /// <summary> 检查单例是否已创建 </summary>
    public static bool IsInstanceCreated => instance != null;
    /// <summary>
    /// 初始化（自动调用，子类可重写扩展初始化逻辑）
    /// </summary>
    public virtual void Init()
    {
        // 优化：延迟初始化Dictionary，避免不必要的内存分配
        EventIDList ??= new Dictionary<int, EngineEventManager.EventCallback>();
    }
    /// <summary>
    /// 开始启动
    /// </summary>
    public virtual void Startup()
    {
        /// 子类继承
    }
    #region 全局事件监听部分
    
    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="id">事件ID</param>
    /// <param name="eventCallback">回调函数</param>
    /// <returns>是否添加成功</returns>
    public bool AddEvent(int id, EngineEventManager.EventCallback eventCallback)
    {
        if (eventCallback == null)
        {
            UnityEngine.Debug.LogWarning($"[GameSingleton] 尝试添加null回调，事件ID: {id}");
            return false;
        }
        
        // 防止重复添加同一事件
        if (EventIDList.ContainsKey(id))
        {
            UnityEngine.Debug.LogWarning($"[GameSingleton] 事件ID {id} 已存在，将覆盖旧回调");
            RemoveEvent(id); // 先移除旧的
        }
        
        EngineEventManager.Instance.AddEventListener(id, eventCallback);
        EventIDList[id] = eventCallback;
        return true;
    }
    
    /// <summary>
    /// 移除单个事件监听
    /// </summary>
    public bool RemoveEvent(int id)
    {
        if (EventIDList.TryGetValue(id, out var callback))
        {
            EngineEventManager.Instance.RemoveEventListener(id, callback);
            EventIDList.Remove(id);
            return true;
        }
        return false;
    }

    // 获取注册监听的事件ID列表
    //---@return table<EventID>
    public Dictionary<int, EngineEventManager.EventCallback> GetEventIDList()
    {
        return EventIDList;
    }

    //事件处理
    //---@param id EventID EventID
    public virtual void EventHandle(EngineEvent engineEvent)
    {
        // Logger.Warning("[%s:EventHandle]__此函数需要再子类覆盖使用！", self: GetClassName())
    }

    /// <summary>
    /// 移除所有事件监听
    /// </summary>
    public void RemoveAllListener()
    {
        if (EventIDList != null && EventIDList.Count > 0)
        {
            foreach (KeyValuePair<int, EngineEventManager.EventCallback> item in EventIDList)
            {
                EngineEventManager.Instance.RemoveEventListener(item.Key, item.Value);
            }
            EventIDList.Clear();
        }
    }

    #endregion
    
    /// <summary>
    /// 销毁单例（子类可重写扩展清理逻辑）
    /// </summary>
    public virtual void OnDestroy()
    {
        RemoveAllListener();
        EventIDList = null;
    }
    
    /// <summary>
    /// 手动销毁单例实例（慎用，一般不需要手动调用）
    /// </summary>
    public static void DestroyInstance()
    {
        if (instance != null)
        {
            lock (lockObj)
            {
                if (instance != null)
                {
                    instance.OnDestroy();
                    instance = null;
                }
            }
        }
    }

}
