using Cinemachine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
public enum SceneType
{
    MainCity,  // 主城场景
    Battle,     // 关卡战斗场景
}
/// <summary>
/// 场景基类
/// </summary>
public class BaseScene
{
    public string SceneName = "";
    public SceneConfig SceneConfig;
    public Transform ParentNode;
    public Action<BaseScene> OnOpenCompletedCB = null;

    public bool IsLoaded = false; // 是否加载完成
    public bool IsOpened = false; // 场景是否打开
    public bool IsVisible = false; // 场景是否可见
    public GameObject SceneInstance = null; // 场景实体资源对象
    public Transform SceneTransform = null; // 场景实体资源对象节点
    public object para; // 初始化场景时传入的参数

    /// <summary>
    /// 常用节点初始化
    /// </summary>
    public Dictionary<string, GameObject> Obj = new();

    public UIGoTable goTable;
    public BaseScene()
    {
        IsLoaded = false;
        IsOpened = false;
        IsVisible = false;
    }

    // 是否可用(动态加载在未加载完成前被使用可能需要判定)
    public bool IsAvalible()
    {
        return IsOpened == true && IsLoaded == true;
    }

    // 是否可见(必须通过此接口判定场景显隐)
    public bool IsActive()
    {
        return IsVisible;
    }

    // 初始化场景(子类请勿使用)
    public void Init(Transform parentnode, Action<BaseScene> onopencompletedcb, object para)
    {
        ParentNode = parentnode;
        OnOpenCompletedCB = onopencompletedcb;
        this.para = para;
        IsLoaded = false;
        IsOpened = true;
        LoadRes().Forget();
    }

    // 资源加载
    private async UniTaskVoid LoadRes()
    {
        GameObject go = await LoadManager.Instance.LoadPrefabAsync(SceneConfig.ResPath, ParentNode);
        go.name = SceneName;
        GetAutoGoTable(go.transform);
        
        NodeInit();
        OnLoadResCompleted(go);
    }

    // 资源加载完成回调
    public void OnLoadResCompleted(GameObject go)
    {
        if (!IsOpened)
        {
            LogUtlis.Warn($"场景已关闭，直接销毁加载完成资源对象:{go.name}!");
            GameObject.Destroy(go);
            return;
        }
        ///绑定虚拟像机
        CinemachineVirtualCamera _virtualCamera = Obj["obj_vm_Camera"].transform.GetComponent<CinemachineVirtualCamera>();
        CameraManager.Instance.SwitchTo(_virtualCamera);
        SceneInstance = go;
        SceneTransform = SceneInstance.transform;
        GetBindComponents(go);
        Awake();
        OnCreate();
        IsLoaded = true;
       
        if (OnOpenCompletedCB != null)
        {
            OnOpenCompletedCB(this);
            OnOpenCompletedCB = null;
        }
        OnRefresh();
    }

    // 获取自动生成的节点表
    public void GetAutoGoTable(Transform node)
    {
        goTable = node.GetComponent<UIGoTable>();
    }

    // 节点初始化
    public void NodeInit()
    {
        UINodeInfo[] uiNodeArray = goTable.uiNodeArray;
        for (int i = 0; i < uiNodeArray.Length; i++)
        {
            UINodeInfo uINodeInfo = uiNodeArray[i];
            string name = uINodeInfo.KeyName.Replace("@_", "");
            name = name.Replace("$_", "");
            Obj[name] = uINodeInfo.gameObject;
        }
    }

    // 模拟Unity Component的 Awake
    public virtual void Awake()
    {
    }

    public virtual void Update(float dt)
    {

    }
    public virtual void FixedUpdate(float dt)
    {

    }
    // 绑定节点数据
    public virtual void GetBindComponents(GameObject go)
    {
    }

    // 场景显示(子类重写自定义每个参数)
    public virtual void OnCreate()
    {
        LogUtlis.Info($"场景名:{SceneName} OnCreate");
    }

    // 场景二次打开(子类重写自定义每个参数)
    public virtual void OnRefresh()
    {
    }

    // 可用
    public void OnBaseEnable()
    {
        LogUtlis.Info($"场景名:{SceneName} OnEnable");
        OnEnable();
    }

    // 可用，子类扩展实现
    public virtual void OnEnable()
    {
    }

    // 不可用
    public void OnBaseDisable()
    {
        LogUtlis.Info($"场景名:{SceneName} OnDisable");
        OnDisable();
    }

    // 不可用，子类扩展实现
    public virtual void OnDisable()
    {
    }

#if UNITY_EDITOR
    // 场景编辑器下绘制Gizmos
    public virtual void OnDrawGizmos()
    {

    }
#endif

    // 场景清理销毁
    public void Destroy()
    {
        IsOpened = false;

        if (IsLoaded == true)
        {
            OnBaseDisable();
            OnDestroy();
            RemoveAllListener();

            GameObject.Destroy(SceneInstance);
        }
        else
        {
            LogUtlis.Error($"出现未加载完成又被关闭的情况!{SceneName}");
        }

        SceneName = "";
        SceneConfig = null;
        ParentNode = null;
        OnOpenCompletedCB = null;
        IsLoaded = false;
        IsOpened = false;
        SceneInstance = null;
        SceneTransform = null;
    }

    // 场景清理(子类重写实现自定义清理流程)
    public virtual void OnDestroy()
    {

    }

    // 关闭自身(子类重写实现自定义关闭流程)
    public void Close()
    {
        SceneManager.Instance.CloseScene(SceneName);
    }
    // 为GameObject获得或添加组件(场景子组件统一添加入口)

    #region 全局事件监听部分

    private Dictionary<int, EngineEventManager.EventCallback> EventIDList = new Dictionary<int, EngineEventManager.EventCallback>();

    // 添加事件（在Awake中添加才生效）
    public void AddEvent(int id, EngineEventManager.EventCallback eventCallback)
    {
        EngineEventManager.Instance.AddEventListener(id, eventCallback);
        EventIDList.Add(id, eventCallback);
    }

    // 获取注册监听的事件ID列表
    public Dictionary<int, EngineEventManager.EventCallback> GetEventIDList()
    {
        return EventIDList;
    }

    // 事件处理
    public virtual void EventHandle(int id)
    {
    }

    // 移除所有监听
    public void RemoveAllListener()
    {
        foreach (KeyValuePair<int, EngineEventManager.EventCallback> item in EventIDList)
        {
            EngineEventManager.Instance.RemoveEventListener(item.Key, item.Value);
        }
        EventIDList.Clear();
    }

    #endregion


}