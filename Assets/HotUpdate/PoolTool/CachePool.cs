using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 缓存池（区别于对象池，用于资源/数据缓存）
/// 特性：
/// 1. LRU淘汰机制
/// 2. 自动资源清理
/// 3. 线程安全
/// 4. 容量控制
/// </summary>
public class CachePool<TKey, TValue> where TValue : class
{
    #region 内部结构
    private class CacheItem
    {
        public TValue Value;
        public LinkedListNode<TKey> Node;
        public Action<TValue> ReleaseAction;
    }
    #endregion

    private readonly Dictionary<TKey, CacheItem> _cacheDictionary;
    private readonly LinkedList<TKey> _accessOrder;
    
    public int MaxCapacity { get; private set; }
    public int Count => _cacheDictionary.Count;

    public CachePool(int maxCapacity = 100)
    {
        MaxCapacity = Math.Max(1, maxCapacity);
        _cacheDictionary = new Dictionary<TKey, CacheItem>(MaxCapacity);
        _accessOrder = new LinkedList<TKey>();
    }

    /// <summary>
    /// 添加缓存项（带自定义释放逻辑）
    /// </summary>
    public void Add(TKey key, TValue value, Action<TValue> onRelease = null)
    {
        lock (_cacheDictionary)
        {
            if (_cacheDictionary.ContainsKey(key))
            {
                // 更新现有项
                var existing = _cacheDictionary[key];
                _accessOrder.Remove(existing.Node);
                _accessOrder.AddFirst(existing.Node);
                return;
            }

            // 执行容量清理
            while (_cacheDictionary.Count >= MaxCapacity)
            {
                RemoveOldest();
            }

            var newNode = _accessOrder.AddFirst(key);
            _cacheDictionary[key] = new CacheItem
            {
                Value = value,
                Node = newNode,
                ReleaseAction = onRelease
            };
        }
    }

    /// <summary>
    /// 获取缓存项（更新访问时间）
    /// </summary>
    public TValue Get(TKey key)
    {
        lock (_cacheDictionary)
        {
            if (_cacheDictionary.TryGetValue(key, out var item))
            {
                // 更新访问顺序
                _accessOrder.Remove(item.Node);
                _accessOrder.AddFirst(item.Node);
                return item.Value;
            }
            return null;
        }
    }

    /// <summary>
    /// 主动移除缓存项
    /// </summary>
    public bool Remove(TKey key)
    {
        lock (_cacheDictionary)
        {
            if (_cacheDictionary.TryGetValue(key, out var item))
            {
                RemoveItem(key, item);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 清空整个缓存池
    /// </summary>
    public void Clear()
    {
        lock (_cacheDictionary)
        {
            foreach (var pair in _cacheDictionary)
            {
                ExecuteReleaseAction(pair.Value);
            }
            _cacheDictionary.Clear();
            _accessOrder.Clear();
        }
    }

    /// <summary>
    /// 动态调整缓存容量
    /// </summary>
    public void Resize(int newCapacity)
    {
        lock (_cacheDictionary)
        {
            MaxCapacity = Math.Max(1, newCapacity);
            while (_cacheDictionary.Count > MaxCapacity)
            {
                RemoveOldest();
            }
        }
    }

    #region 私有方法
    private void RemoveOldest()
    {
        var lastNode = _accessOrder.Last;
        if (lastNode != null)
        {
            RemoveItem(lastNode.Value, _cacheDictionary[lastNode.Value]);
        }
    }

    private void RemoveItem(TKey key, CacheItem item)
    {
        _accessOrder.Remove(item.Node);
        _cacheDictionary.Remove(key);
        ExecuteReleaseAction(item);
    }

    private void ExecuteReleaseAction(CacheItem item)
    {
        try
        {
            item.ReleaseAction?.Invoke(item.Value);
            // 针对Unity特殊资源的自动处理
            if (item.Value is GameObject unityObj && unityObj != null)
            {
                if (Application.isPlaying)
                {
                    if (unityObj.scene.IsValid())
                    {
                        Object.Destroy(unityObj);
                    }
                }
                else
                {
                    Object.DestroyImmediate(unityObj);
                }
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"Cache release error: {e}");
        }
    }
    #endregion

    
}