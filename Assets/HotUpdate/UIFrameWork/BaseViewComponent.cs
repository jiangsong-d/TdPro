using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 窗口组件基类(类似原来的Component但脱离Mono且和View强绑定)
/// </summary>
public class BaseViewComponent
{

    // 窗口组件绑定流程介绍
    // 1. __init()                 -- 构造函数调用
    // 2. UpdateOrder()            -- 更新Order
    // 3. Awake()                  -- Awake(子类重写实现)
    // 4. OnCreate()               -- 组件显示(子类重写实现)
    // 5. OnEnable()               -- 组件可用(子类重写实现)
    // 6. OnDisable()              -- 组件不可用(子类重写实现)
    // 7. Destroy()                -- 组件清理或销毁
    // 8. OnDestroy()              -- 组件自定义清理(子类重写实现)
    // 9. ClearAllComponents()     -- 清理嵌套组件
    // 10. RemoveAllListener()     -- 移除所有事件监听

    // 和原来Component的区别介绍:
    // 1. 脱离Mono,纯逻辑驱动
    // 2. 强耦合View,通过View和嵌套BaseComponent来驱动管理
    // 3. 支持手动移除组件绑定接口

    // 注意事项:
    // 1. 组件移除和清理(清除组件绑定以及触发组件逻辑清理)都不会主动销毁实体绑定对象,如果实体对象不在窗口上需要自行清理(比如在OnDestroy里)
    // 2. 如果组件绑定对象节点位置变化造成显示Order问题，请主动调用UpdateOrder方法更新Order
    //-----------------------------------------------------

    public bool IsValide;// 是否是有效组件
    public bool IsShow;// 是否可见
    public BaseViewComponent Owner;// 所属组件(支持组件套组件，窗口的第一个组件(哪怕是继承)的Owner是BaseViewComponent,第二个组件开始就是上一个组件)
    public BaseUIView OwnerView;// 所属窗口
    public Transform transform;// 物体对应的transform
    public GameObject gameObject;// 物体对应的gameObject
    private Dictionary<int, EngineEventManager.EventCallback> EventIDList = new Dictionary<int, EngineEventManager.EventCallback>();// 全局事件监听回调Map(Key为事件名,Value为对应监听回调列表)
    public Dictionary<GameObject, Dictionary<Type, BaseViewComponent>> Components = new Dictionary<GameObject, Dictionary<Type, BaseViewComponent>>();// 子组件缓存映射Map(Key为GameObject，Value为类名和绑定的组件实例对象)
    public UIGoTable uIGoTable;

    /// <summary>
    /// 常用节点初始化
    /// </summary>
    public Dictionary<string, TMPTextPro> Tmp = new();

    public Dictionary<string, ButtonPro> Btn = new();

    public Dictionary<string, Image> Img = new();

    public Dictionary<string, RawImage> RawImg = new();

    public Dictionary<string, GameObject> Obj = new();

    public Dictionary<string, Toggle> Toggles = new();

    public BaseViewComponent()
    {

    }

    // BaseViewComponent构造函数
    //@param owner BaseViewComponent @所属组件
    //@param ownerview BaseUIView @所属窗口
    //@param gameobject UnityEngine.GameObject @绑定的GameObject
    //-@param...any @参数
    public void SetBaseViewComponent(BaseViewComponent owner, BaseUIView ownerview, GameObject gameobject)
    {
        IsValide = true;
        IsShow = false;
        Owner = owner;
        OwnerView = ownerview;
        transform = gameobject.transform;
        gameObject = gameobject;
        Components.Clear();
        EventIDList.Clear();
        GetAutoGoTableOrNil(gameObject.transform);
        if (Owner != null)
        {
            NodeInit();
        }
        //self.go_table = GetAutoGoTableOrNil(self.gameObject, handler(self, self.OnClickBtn), handler(self, self.OnClickToggle), handler(self, self.OnClickTmp))
        UpdateOrder();
        //GetBindComponents(gameobject);
        Awake();
        //EventManager: GetInstance() :Register(self)
        OnCreate();
        OnEnable();
    }

    public void GetAutoGoTableOrNil(Transform node)
    {
        uIGoTable = node.GetComponent<UIGoTable>();
        if (uIGoTable == null)
        {
        }
        else
        {
            uIGoTable.AddClickBtnEvent((ButtonPro g) => { OnClickBtn(g); }, (Toggle g, bool isOn) => { OnClickToggle(g, isOn); },
           (ButtonPro g) => { OnClickBtnUp(g); }, (ButtonPro g) => { OnClickBtnDown(g); });
        }
    }
    public virtual void OnClickBtnUp(ButtonPro btn)
    {
    }

    public virtual void OnClickBtnDown(ButtonPro btn)
    {
    }
    public void NodeInit()
    {
        if (uIGoTable == null || uIGoTable.uiNodeArray == null) return;
        
        UINodeInfo[] uiNodeArray = uIGoTable.uiNodeArray;
        for (int i = 0; i < uiNodeArray.Length; i++)
        {
            UINodeInfo uINodeInfo = uiNodeArray[i];
            if (uINodeInfo?.gameObject == null) continue;

            // 一次性处理字符串替换，减少GC分配
            string name = uINodeInfo.KeyName.Replace("@_", string.Empty).Replace("$_", string.Empty);
            string keyNameLower = uINodeInfo.KeyName.ToLower(); // 缓存ToLower结果
            
            Obj[name] = uINodeInfo.gameObject;
            
            // 优化：使用IndexOf代替Contains，避免重复ToLower
            if (keyNameLower.Contains("btn"))
            {
                ButtonPro button = uINodeInfo.gameObject.GetComponent<ButtonPro>();
                if (button != null) Btn[name] = button;
            }
            else if (keyNameLower.Contains("image"))
            {
                Image ig = uINodeInfo.gameObject.GetComponent<Image>();
                if (ig != null) Img[name] = ig;
            }
            else if (keyNameLower.Contains("rawimage"))
            {
                RawImage rg = uINodeInfo.gameObject.GetComponent<RawImage>();
                if (rg != null) RawImg[name] = rg;
            }
            else if (keyNameLower.Contains("toggle"))
            {
                Toggle toggle = uINodeInfo.gameObject.GetComponent<Toggle>();
                if (toggle != null) Toggles[name] = toggle;
            }
            else if (keyNameLower.Contains("tmp"))
            {
                TMPTextPro text = uINodeInfo.gameObject.GetComponent<TMPTextPro>();
                if (text != null) Tmp[name] = text;
            }
        }
    }

    //是否可用(动态加载在未加载完成前被使用可能需要判定)
    //@return boolean @是否可用
    public bool IsAvalible()
    {
        return IsValide;
    }

    //是否可见
    //@return boolean @是否可见
    public bool IsActive()
    {
        return IsShow == true;
    }

    //设置可见
    //@param active boolean @设置可见
    public void SetActive(bool active)
    {
        if (IsAvalible())
        {
            gameObject.SetActive(active);
            IsShow = active;
            if (active)
            {
                OnEnable();
                //EventManager: GetInstance() :Register(self)
            }
            else
            {
                OnDisable();
                //EventManager: GetInstance() :Unregister(self)
            }
        }
        else
        {
            LogUtlis.Error("组件:对象已无效，不应该进入这里!");
        }
    }

    //更新Order
    public void UpdateOrder()
    {
        //UpdateUIGameObjectOrder(gameObject, UIData.PerWindowOrder, false);
    }

    // 绑定节点数据
    public virtual void GetBindComponents(GameObject go)
    {
    }

    //模拟Unity Component的 Awake
    public virtual void Awake()
    { }

    //初始化
    public virtual void OnCreate()
    { }

    //可用
    public virtual void OnEnable()
    { }

    //不可用
    public virtual void OnDisable()
    { }

    //关闭组件(带销毁预制体过程)
    public void Close()
    {
        if (Owner != null)
        {
            Owner.DestoryComponentGameObj(gameObject);
        }
        else
        {
            LogUtlis.Error("按照嵌套使用方式,只有根节点不存在数据拥有者");
        }
    }

    //子类重写实现自定义清理[virtual]
    public virtual void OnDestroy()
    { }

    //清除所有子组件绑定
    public void ClearAllComponents()
    {
        //---@param classinstancemap table<string, BaseViewComponent>
        foreach (var item in Components)
        {
            foreach (var com in item.Value)
            {
                LogUtlis.Info($"清理窗口组件:实例对象:{com.Key}绑定窗口组件:!");
                com.Value.Destroy();
            }
            item.Value.Clear();
        }
        Components.Clear();
    }

    // 逻辑销毁(窗口关闭时和RemoveComponent时调用)
    public void Destroy()
    {
        if (IsValide)
        {
            OnDisable();
            OnDestroy();
            RemoveAllListener();
            ClearAllComponents();
            
            // 清理UIGoTable事件绑定
            if (uIGoTable != null)
            {
                uIGoTable = null;
            }
            
            // 清理字典，防止内存泄漏
            Tmp?.Clear();
            Btn?.Clear();
            Img?.Clear();
            RawImg?.Clear();
            Obj?.Clear();
            Toggles?.Clear();
            
            Owner = null;
            OwnerView = null;
            transform = null;
            gameObject = null;
            Components.Clear();
            IsValide = false;
        }
        else
        {
            LogUtlis.Info("组件类:%s对象已经被销毁,不应该再次进入,请检查代码!");
        }
    }

    // 点击Button回调
    public virtual void OnClickBtn(Button btn)
    { }

    //点击Toggle回调
    //---@param toggle UnityEngine.UI.Toggle Toggle
    //---@param isOn boolean 是否选中
    public virtual void OnClickToggle(Toggle toggle, bool isOn)
    { }

    //获取所属窗口
    //---@return BaseUIView @所属窗口
    public BaseUIView GetOwnerView()
    {
        return OwnerView;
    }

    #region 组件添加获取移除接口开始

    //为GameObject获得或添加组件(嵌套子组件统一添加入口，不允许直接New)
    //@param bindtarget UnityEngine.GameObject 绑定的GameObject
    //@param class BaseViewComponent  组件类
    //---@return BaseViewComponent @组件实例化类
    public T GetOrAddComponent<T>(BaseViewComponent viewComponent, BaseUIView baseUI, GameObject bindtarget) where T : BaseViewComponent
    {
        return GetOrAddComponent(viewComponent, baseUI, bindtarget, typeof(T)) as T;
    }

    public BaseViewComponent GetOrAddComponent(BaseViewComponent viewComponent, BaseUIView baseUI, GameObject bindtarget, Type componentType)
    {
        // behaviour 记录数据是否存在
        BaseViewComponent cl = GetComponent(bindtarget, componentType);
        // 检测组件是否已经添加过
        if (cl == null)
        {
            cl = Activator.CreateInstance(componentType) as BaseViewComponent;
            cl.SetBaseViewComponent(viewComponent, baseUI, bindtarget);
            // 添加缓存到本地集合中
            if (Components.ContainsKey(bindtarget))
            {
                if (Components[bindtarget].ContainsKey(componentType))
                {
                    Components[bindtarget][componentType] = cl;
                }
                else
                {
                    Components[bindtarget].Add(componentType, cl);
                }
            }
            else
            {
                Components.Add(bindtarget, new Dictionary<Type, BaseViewComponent>()
                {
                    { componentType, cl }
                });
            }
        }
        return cl;
    }

    //GetComponent 获取GameObject中挂载的组件
    // - @param bindtarget UnityEngine.GameObject 绑定的GameObject
    //-@param class BaseViewComponent  组件类
    // - @return  BaseViewComponent 实例化类 或nil
    public BaseViewComponent GetComponent(GameObject bindtarget, Type componentType)
    {
        Dictionary<Type, BaseViewComponent> tager = Components.ContainsKey(bindtarget) ? Components[bindtarget] : null;
        if (tager != null && tager.ContainsKey(componentType))
        {
            return tager[componentType];
        }
        return null;
    }

    //-销毁GameObject,并移除其中挂载的所有组件
    //-@param bindtarget UnityEngine.GameObject @绑定的GameObject
    public bool DestoryComponentGameObj(GameObject bindtarget)
    {
        //-@type table<string, BaseViewComponent>
        Dictionary<Type, BaseViewComponent> tager = Components.ContainsKey(bindtarget) ? Components[bindtarget] : null;
        if (tager != null)
        {
            foreach (var item in tager)
            {
                // Debug.Log($"组件类:%s移除实体对象名:{bindtarget.name}绑定的组件脚本:%s!");
                item.Value.Destroy();
            }
            Components.Remove(bindtarget);
            GameObject.Destroy(bindtarget);
            return true;
        }
        else
        {
            LogUtlis.Info($"当前组件类:实例对象:{bindtarget.name}未绑定组件类，清除失败!");
            return false;
        }
    }

    // 移除GameObject中挂载的指定组件
    // @param bindtarget UnityEngine.GameObject @绑定的GameObject
    //-@param class BaseViewComponent @组件类
    //-@return boolean @移除是否成功
    public bool RemoveComponent(GameObject bindtarget, Type componentType)
    {
        Dictionary<Type, BaseViewComponent> tager = Components.ContainsKey(bindtarget) ? Components[bindtarget] : null;
        if (tager != null && tager.ContainsKey(componentType))
        {
            LogUtlis.Info($"组件类:{componentType.Name}移除实体对象名:{bindtarget.name}绑定的组件脚本:%s!");
            tager[componentType].Destroy();
            tager.Remove(componentType);
            return true;
        }
        else
        {
            LogUtlis.Error($"当前组件类:{componentType.Name}实例对象:{bindtarget.name}未绑定组件类:%s，清除失败!");
            return false;
        }
    }

    //-移除指定组件实例对象
    //-@param componentinstance BaseViewComponent @组件实例对象
    //-@return boolean @移除是否成功
    public bool RemoveComponentInstance(BaseViewComponent componentinstance, Type componentType)
    {
        //Guard.AssertException(IsClass(componentinstance, BaseViewComponent), "未继承BaseViewComponent")
        Dictionary<Type, BaseViewComponent> tager = Components.ContainsKey(componentinstance.gameObject) ? Components[componentinstance.gameObject] : null;
        if (tager != null && tager.ContainsKey(componentType))
        {
            LogUtlis.Info($"组件类:{componentType}移除实体对象名:{componentinstance.gameObject.name}绑定的组件脚本:%s实体!");
            tager[componentType].Destroy();
            tager.Remove(componentType);
            return true;
        }
        else
        {
            LogUtlis.Info($"当前组件类:{componentType}实例对象:{componentinstance.gameObject.name}未绑定组件类:%s，清除失败!");
            return false;
        }
    }

    #endregion

    #region 全局事件监听部分
    // 添加事件（在Awake中添加才生效）
    //---@param id EventID
    public void AddEvent(int id, EngineEventManager.EventCallback eventCallback)
    {
        EngineEventManager.Instance.AddEventListener(id, eventCallback);
        EventIDList.Add(id, eventCallback);
    }

    // 获取注册监听的事件ID列表
    //---@return table<EventID>
    public Dictionary<int, EngineEventManager.EventCallback> GetEventIDList()
    {
        return EventIDList;
    }

    //事件处理
    //---@param id EventID EventID
    public virtual void EventHandle(int id)
    {
        // Logger.Warning("[%s:EventHandle]__此函数需要再子类覆盖使用！", self: GetClassName())
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
