using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T mInstance = null;
    private static GameObject bootObject = null; // 静态Boot对象引用
    private static bool isApplicationQuitting = false; // 应用退出标记

    public static T Instance
    {
        get
        {
            // 防止在应用退出或OnDestroy中创建新实例
            if (isApplicationQuitting)
            {
                return null;
            }

            if (mInstance == null)
            {
                mInstance = GameObject.FindObjectOfType(typeof(T)) as T;
                if (mInstance == null)
                {
                    // 创建单例实例
                    GameObject go = new GameObject(typeof(T).Name);
                    mInstance = go.AddComponent<T>();
                    
                    // 创建或获取Boot父对象（只创建一次）
                    if (bootObject == null)
                    {
                        bootObject = GameObject.Find("Boot");
                        if (bootObject == null)
                        {
                            bootObject = new GameObject("Boot");
                            DontDestroyOnLoad(bootObject); // 立即设置DontDestroyOnLoad
                        }
                    }
                    
                    // 挂载到Boot下
                    go.transform.SetParent(bootObject.transform, false);
                }
            }

            return mInstance;
        }
    }

    //如果遇到报错：Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)
    //请在OnDestory调用单例的地方使用这个方法判断一下
    /// <summary>
    /// 单例是否存在
    /// </summary>
    /// <returns></returns>
    public static bool IsInstance()
    {
        return mInstance != null;
    }

    /// <summary>
    /// 没有任何实现的函数，用于保证MonoSingleton在使用前已创建
    /// </summary>
    public virtual void Startup()
    {

    }
   
    protected virtual void Awake()
    {
        if (mInstance == null)
        {
            mInstance = this as T;
            // 不需要重复DontDestroyOnLoad，因为已经在Boot下了
            Init();
        }
        else if (mInstance != this)
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    protected virtual void Init()
    {

    }

    /// <summary>
    /// 延迟一帧销毁
    /// <para>需要修改时机请重写DelayCo</para>
    /// </summary>
    public void DelayDestroySelf()
    {
        StartCoroutine(CoDestroySelf());
    }

    private IEnumerator CoDestroySelf()
    {
        yield return DelayCo();
        Dispose();
        MonoSingleton<T>.mInstance = null;
        UnityEngine.Object.DestroyImmediate(gameObject);
    }

    /// <summary>
    /// 销毁前延迟流程
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator DelayCo()
    {
        yield return null;
    }

    public void DestroySelf()
    {
        Dispose();
        MonoSingleton<T>.mInstance = null;
        UnityEngine.Object.DestroyImmediate(gameObject);
    }

    public virtual void Dispose()
    {

    }
}

