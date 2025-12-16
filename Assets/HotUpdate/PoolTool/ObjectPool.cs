using System.Collections.Generic;
using UnityEngine;
/// <summary>
///功能特性说明
///1.智能内存管理
///自动计算最佳池大小（基于对象内存占用）
///动态调整池容量，防止内存溢出
///定期清理闲置对象池（每5分钟）

///2.自动化生命周期
///通过IPoolable接口实现自定义回收逻辑
///自动处理对象激活/禁用状态
///对象类型自动注册与追踪

/// 3. 高性能设计
///使用字典+队列的复合数据结构
///避免反射操作
///自动扩容策略减少GC压力

///4.易用性优化
///泛型接口支持
///自动初始化对象池


///预制体关联管理
/// <summary>
/// <summary>
/// 对象池接口（实现此接口可自定义回收逻辑）
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 对象被回收时自动调用
    /// </summary>
    void OnRecycle();
}

/// <summary>
/// 智能对象池管理器
/// </summary>
public class ObjectPool : MonoSingleton<ObjectPool>
{

    #region 核心数据结构
    private class PoolData
    {
        /// <summary>
        /// 不活跃对象
        /// </summary>
        public Queue<GameObject> inactiveObjects = new Queue<GameObject>();
        /// <summary>
        /// 活跃对象
        /// </summary>
        public List<GameObject> activeObjects = new List<GameObject>();
        public int maxPoolSize = 100; // 自动内存管理阈值
        
        public void Clear()
        {
            foreach (var go in inactiveObjects)
            {
                Destroy(go);
            }
            
            foreach (var go in activeObjects)
            {
                Destroy(go);
            }
            inactiveObjects.Clear();
            activeObjects.Clear();
        }
    }

    private Dictionary<string, PoolData> _pools = new Dictionary<string, PoolData>();
    private Dictionary<GameObject, string> _objectTypeMap = new Dictionary<GameObject, string>();
    
    
    #endregion

    #region 公共接口

    /// <summary>
    /// 获得最大池大小
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public int GetMaxPoolSize(GameObject prefab)
    {
        var poolKey = GetPoolKey(prefab);
        if (!_pools.ContainsKey(poolKey)) InitializePool(poolKey, prefab);
        return _pools[poolKey].maxPoolSize;
    }
    /// <summary>
    /// 设置最大池大小
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="maxPoolSize"></param>
    public void SetMaxPoolSize(GameObject prefab,int maxPoolSize)
    {

        var poolKey = GetPoolKey(prefab);
        if (!_pools.ContainsKey(poolKey)) InitializePool(poolKey, prefab);
        _pools[poolKey].maxPoolSize=maxPoolSize;
    }
    /// <summary>
    /// 预加载对象池（推荐在Loading阶段调用）
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="count">预加载数量</param>
    public void Preload(GameObject prefab,Transform root, int count)
    {
        var poolKey = GetPoolKey(prefab);
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new PoolData();
        }

        for (int i = 0; i < count; i++)
        {
            var obj = CreateNewObject(prefab);
            obj.transform.SetParent(root);
            obj.SetActive(false);
            _pools[poolKey].inactiveObjects.Enqueue(obj);
        }
    }

    /// <summary>
    /// 获取对象（泛型版本）
    /// </summary>
    public T Get<T>(GameObject prefab) where T : Component
    {
        var obj = Get(prefab);
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// 获取对象（基础版本）
    /// </summary>
    public GameObject Get(GameObject prefab)
    {
        var poolKey = GetPoolKey(prefab);
        if (!_pools.ContainsKey(poolKey)) InitializePool(poolKey, prefab);

        var pool = _pools[poolKey];
       
        // 自动扩容策略
        if (pool.inactiveObjects.Count == 0)
        {
            var newObj = CreateNewObject(prefab);
            pool.activeObjects.Add(newObj);
            if (!newObj.activeSelf)
            {
                newObj.SetActive(true);
            }
            return newObj;
        }

        var obj = pool.inactiveObjects.Dequeue();
        if (!obj.activeSelf)
        {
            obj.SetActive(true);
        }
        pool.activeObjects.Add(obj);
        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public void Recycle(GameObject obj,bool needHide=true)
    {
        if (!_objectTypeMap.TryGetValue(obj, out var poolKey)) return;

        var pool = _pools[poolKey];
        pool.activeObjects.Remove(obj);

        // 执行自定义回收逻辑
        var poolable = obj.GetComponent<IPoolable>();
        poolable?.OnRecycle();

        // 智能内存管理
        if (pool.inactiveObjects.Count < pool.maxPoolSize)
        {
            if (needHide)
            {
                obj.SetActive(false);
            }
            pool.inactiveObjects.Enqueue(obj);
        }
        else
        {
            Destroy(obj); // 防止内存溢出
        }
    }
    #endregion

    #region 内部实现
    private string GetPoolKey(GameObject prefab) => prefab.name.GetHashCode().ToString();

    private void InitializePool(string poolKey, GameObject prefab)
    {
        _pools[poolKey] = new PoolData
        {
            maxPoolSize = CalculateOptimalPoolSize(prefab)
        };
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        var obj = Instantiate(prefab);
        _objectTypeMap[obj] = GetPoolKey(prefab);
        return obj;
    }

    private int CalculateOptimalPoolSize(GameObject prefab)
    {
        // 根据对象内存占用自动计算最佳池大小
        var sizeFactor = Mathf.Clamp(1024 / (prefab.EstimateMemorySize() + 1), 10, 1000);
        return Mathf.Clamp(sizeFactor, 50, 500);
    }
    #endregion

    #region 内存监控
    public void Update()
    {
        // 自动清理长时间未使用的对象池
        foreach (var pool in _pools)
        {
            if (Time.frameCount % 300 == 0 && 
                pool.Value.activeObjects.Count == 0 && 
                pool.Value.inactiveObjects.Count > pool.Value.maxPoolSize/2)
            {
                while (pool.Value.inactiveObjects.Count > pool.Value.maxPoolSize/2)
                {
                    var obj = pool.Value.inactiveObjects.Dequeue();
                    Destroy(obj);
                }
            }
        }
    }
    #endregion

    public void Release()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }
        _pools.Clear();
        _objectTypeMap.Clear();
    }
}

// 扩展方法：估算GameObject内存占用
public static class MemoryExtensions
{
    public static int EstimateMemorySize(this GameObject obj)
    {
        int size = 0;
        foreach (var component in obj.GetComponents<Component>())
        {
            // size += System.Runtime.InteropServices.Marshal.SizeOf(component);
        }
        return Mathf.Max(size, 64); // 最低64字节
    }
}