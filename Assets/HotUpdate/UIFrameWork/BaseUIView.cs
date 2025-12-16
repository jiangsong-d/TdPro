using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI窗口基类
/// </summary>
public class BaseUIView
{
    // UI窗口流程介绍:
    // 1. __init()                         -- 构造函数调用
    // 2. _Init()                          -- 初始化
    // 3. LoadRes()                        -- 加载资源
    // 4. OnLoadResCompleted()             -- 资源加载完成
    // 5. UpdateOrder()                    -- 更新Order
    // 6. InitCommonComponent()            -- 初始化通用组件
    // 7. Awake()                          -- Awake(子类重写实现)
    // 8. OnCreate()                       -- 窗口显示(子类重写实现)
    // 9. OnRefresh()                       -- 窗口二次打开(子类重写实现)
    // 10. OnEnable()                       -- 组件可用(子类重写实现)
    // 11. OnDisable()                     -- 组件不可用(子类重写实现)
    // 12. Destroy()                       -- 窗口清理销毁
    // 13. ClearRootViewComponent()        -- 清除根组件
    // 14. OnDestroy()                     -- 自定义清理(子类重写实现)
    // 15. RemoveAllListener()             -- 移除所有事件监听

    // UI窗口组件使用介绍:
    // 1. 静态绑定子组件接口(BaseUIView:GetOrAddComponent())
    // 3. 子组件移除销毁接口(BaseUIView:RemoveComponentInstance() or BaseViewComponent:Close())

    AdditionalCanvasShaderChannels TexCoord1 = AdditionalCanvasShaderChannels.TexCoord1;
    // local UIModule = LogUtilities.Module.UIModule
    // local UpdateUIGameObjectOrder = CS.UIUtils.UpdateUIGameObjectOrder
    // local CSCameraManager = CS.CameraManager.Get()
    // local GetAutoGoTable = GetAutoGoTable
    // local AnimationUtils = AnimationUtils
    // local handler = handler
    // local AppSetting = AppSetting
    // local IsNil = IsNil
    // local typeof = typeof
    // local GraphicRaycaster = GraphicRaycaster
    // local Canvas = Canvas
    // local Bool2Num = Bool2Num
    // local Num2Bool = Num2Bool
    // local table_pack = table.pack
    // local table_unpack = table.unpack
    // local string_format = string.format
    // local GameObject = GameObject

    public string ViewName = "";
    public UIConfig ViewConfig;
    public Transform ParentNode;
    public int ViewOrder;
    public Action<BaseUIView> OnOpencompletedCB = null;

    public Action OpenCompletedCall = null;

    //self.Datas = nil
    public bool IsLoaded = false; // 是否加载完成
    public bool IsOpened = false; // 窗口是否打开
    public bool IsVisible = false; // 窗口是否可见
    public int HideFlag; // 隐藏标志位
    public GameObject ViewInstance = null; // 窗口实体资源对象
    public Transform ViewTransform = null; // 窗口实体资源对象节点
    public Canvas ViewCanvas = null; // 窗口Canvas组件
    public BaseViewComponent ViewRootComponent; // 窗口根组件(用于支持嵌套子组件增删)
    public bool HaveCloseAni = false; // 是否有关闭动画
    public Dictionary<string, int> CachePrefabPaths = new Dictionary<string, int>(); // 缓存的预制体信息
    public int UIQueueId; // 窗口的队列id
    public Action _closeCallback; // 界面关闭回调
    public object para; //初始化界面时传入的参数
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

    public BaseUIView()
    {
        IsLoaded = false;
        IsOpened = false;
        IsVisible = false;
        HideFlag = ViewActiveFlag.None;
        //self.CSFunCallList = { }
        HaveCloseAni = false;
        UIQueueId = 0;
    }


    // 是否可用(动态加载在未加载完成前被使用可能需要判定)
    public bool IsAvalible()
    {
        return IsOpened == true && IsLoaded == true;
    }


    //是否可见(必须通过此接口判定窗口显隐)
    public bool IsActive()
    {
        return HideFlag == ViewActiveFlag.None;
    }

    // 更新可见状态
    // active boolean @设置是否可见
    // flag ViewActiveFlag @设置显隐原因标志位
    // boolean, ViewActiveFlag 修正后的参数
    public (bool, int) UpdateActiveFlag(bool active = false, int flag = ViewActiveFlag.LogicalFlag)
    {
        if (active == false)
        {
            HideFlag = HideFlag | flag;
        }
        else
        {
            bool hasflag = flag > 0;
            if (hasflag)
            {
                int reverseflag = ~flag;
                HideFlag = HideFlag & reverseflag;
            }
        }

        return (active, flag);
    }


    public void NodeInit()
    {
        if (uIGoTable == null || uIGoTable.uiNodeArray == null) return;
        
        UINodeInfo[] uiNodeArray = uIGoTable.uiNodeArray;
        for (int i = 0; i < uiNodeArray.Length; i++)
        {
            UINodeInfo uINodeInfo = uiNodeArray[i];
            if (uINodeInfo?.gameObject == null) continue;

            // 优化：一次性处理字符串替换，减少GC分配
            string name = uINodeInfo.KeyName.Replace("@_", string.Empty).Replace("$_", string.Empty);
            string keyNameLower = uINodeInfo.KeyName.ToLower(); // 缓存ToLower结果
            
            Obj[name] = uINodeInfo.gameObject;
            
            if (keyNameLower.Contains("btn"))
            {
                ButtonPro button = uINodeInfo.gameObject.GetComponent<ButtonPro>();
                if (button != null)
                {
                    Btn[name] = button;
                }
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

    // 更新显隐
    public void UpdateActiveState(bool? isActive)
    {
        if (isActive == null)
        {
            isActive = IsActive();
        }

        bool lastActive = IsVisible;
        if (IsAvalible())
        {
            if (isActive.Value && !IsVisible)
            {
                // 优化：使用Canvas.enabled而不是localPosition移动
                if (ViewCanvas != null)
                {
                    ViewCanvas.enabled = true;
                }
                else
                {
                    ViewInstance.SetActive(true);
                }
                IsVisible = true;
                LogUtlis.Info($"更新显隐UpdateActiveState 显示成功：{ViewName}");
            }
            else if (isActive == false && IsVisible)
            {
                // 优化：使用Canvas.enabled而不是localPosition移动
                if (ViewCanvas != null)
                {
                    ViewCanvas.enabled = false;
                }
                else
                {
                    ViewInstance.SetActive(false);
                }
                IsVisible = false;
                LogUtlis.Info($"更新显隐UpdateActiveState 隐藏成功：{ViewName}");
            }
        }

        // 修复：正确处理状态变化时的回调
        if (lastActive != IsVisible)
        {
            if (IsVisible)
            {
                OnBaseEnable();
                //EventManager: GetInstance() :Broadcast(EventID.OnViewShow, self)
            }
            else
            {
                OnBaseDisable();
                //EventManager: GetInstance() :Broadcast(EventID.OnViewHide, self)
            }
        }
    }

    /// <summary>
    /// 设置可见
    /// </summary>
    /// <param name="active">设置是否可见</param>
    /// <param name="flag">设置显隐原因标志位</param>
    public void SetActive(bool active, int flag)
    {
        //更新前状态
        bool preActive = IsActive();
        var afValue = UpdateActiveFlag(active, flag);
        active = afValue.Item1;
        flag = afValue.Item2;
        // 最新状态
        bool isActive = IsActive();
        LogUtlis.Info($"窗口名:{ViewName} 显隐操作:{active} 标志位:{flag} 最终隐藏标志位:{HideFlag}!");
        if (preActive != isActive)
        {
            UpdateActiveState(isActive);
        }
    }

    // 是否采用固定Order
    public bool IsUsingConstOrder()
    {
        return IsOpened == true && ViewConfig.RelativeViewName != "";
    }


    //初始化窗口(子类请勿使用)
    //@private
    //@param parentnode UnityEngine.Transform @窗口挂载节点
    //@param onopencompletedcb fun(BaseUIView) @窗口完全打开回调(OnCreate之后)
    //@param...any @窗口自定义参数
    public void Init(Transform parentnode, Action<BaseUIView> onopencompletedcb, object para)
    {
        ParentNode = parentnode;
        OnOpencompletedCB = onopencompletedcb;
        this.para = para;
        //self.Datas = table_pack(...)
        IsLoaded = false;
        IsOpened = true;
        LoadRes().Forget();
    }

    //资源加载
    private async UniTaskVoid LoadRes()
    {
        GameObject go = await LoadManager.Instance.LoadPrefabAsync(ViewConfig.ResPath, ParentNode);
        go.name = ViewName;
        LogUtlis.Info(string.Format("预制体加载完成：{0}", ViewName));
        GetAutoGoTable(go.transform);
        NodeInit();
        OnLoadResCompleted(go);
    }


    //资源加载完成回调
    //@param go UnityEngine.GameObject @资源实体对象
    public void OnLoadResCompleted(GameObject go)
    {
        if (!IsOpened)
        {
            LogUtlis.Warn($"窗口已关闭，直接销毁加载完成资源对象:{go.name}!");
            // 资源加载回来后子组件已经无效了，直接删除实体对象
            GameObject.Destroy(go);
            return;
        }

        ViewInstance = go;
        ViewTransform = ViewInstance.transform;

        CreateViewRootComponent();
        //self.go_table = GetAutoGoTable(self.ViewInstance, handler(self, self.OnBaseClickBtn), handler(self, self.OnClickToggle), handler(self, self.OnClickTmp))
        GetBindComponents(go);
        UpdateOrder();
        
        Awake();
        if (IsActive())
        {
            //GameUtil.PlayOneShotAuido(GlobalDefine.eAudioSid.JieMianDakai)
        }

        //EventManager:GetInstance() :Register(self)
        OnCreate();
        IsLoaded = true;
        //OnOpencompletedCB、OpenCompletedCall必须在IsLoaded之后
        if (OnOpencompletedCB != null)
        {
            OnOpencompletedCB(this);
            OnOpencompletedCB = null;
        }

        if (OpenCompletedCall != null)
        {
            OpenCompletedCall();
            OpenCompletedCall = null;
        }

        OnRefresh();
    }

    public void GetAutoGoTable(Transform node)
    {
        uIGoTable = node.GetComponent<UIGoTable>();
        if (uIGoTable == null)
        {
            LogUtlis.Error(string.Format("{0} 未挂载 UIGoTable 组件", node.name));
        }
        else
        {
            uIGoTable.AddClickBtnEvent((ButtonPro g) => { OnClickBtn(g); },
                (Toggle g, bool isOn) => { OnClickToggle(g, isOn); },
                (ButtonPro g) => { OnClickBtnUp(g); }, (ButtonPro g) => { OnClickBtnDown(g); });
        }
    }


    //窗口被彻底打开并且执行了OnCreate
    public void OnOpenCompleted(Action call)
    {
        OpenCompletedCall = null;
        if (IsLoaded)
        {
            call?.Invoke();
        }
        else
        {
            OpenCompletedCall = call;
        }
    }

    //更新Order
    public void UpdateOrder()
    {
        Canvas canvas = ViewInstance.GetComponent<Canvas>();
        if (canvas == null)
        {
            LogUtlis.Info($"{ViewName} 自行添加Canvas组件");
            canvas = ViewInstance.AddComponent<Canvas>();
        }
        /*if AppSetting.IsEditor == true then
        local gr = self.ViewInstance:GetComponent(typeof(GraphicRaycaster))
            if IsNil(gr) then
                Logger.LogFatal("%s 自行添加 GraphicRaycaster", self.ViewName)
            end
    end*/

        //设置它的canvas深度
        canvas.overrideSorting = true;
        canvas.additionalShaderChannels = TexCoord1;
        canvas.sortingOrder = ViewOrder;
        LogUtlis.Info($"UIType:{ViewConfig.UIType}窗口名:{ViewName}设置Order:{ViewOrder}");
        UpdateUIGameObjectOrder(ViewInstance, UIData.PerWindowOrder, true);

        GraphicRaycaster gr = ViewInstance.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            gr = ViewInstance.AddComponent<GraphicRaycaster>();
        }

        gr.blockingMask = 1 << LayerMask.NameToLayer("UI") | 1 << LayerMask.NameToLayer("Default");
    }

    //模拟Unity Component的 Awake
    public virtual void Awake()
    {
    }

    // 绑定节点数据
    public virtual void GetBindComponents(GameObject go)
    {
    }

    //窗口显示(子类重写自定义每个参数)
    public virtual void OnCreate()
    {
    }

    //窗口二次打开(子类重写自定义每个参数)
    public virtual void OnRefresh()
    {
    }

    // 可用
    public void OnBaseEnable()
    {
        LogUtlis.Info($"窗口名:{ViewName} OnEnable");
        if (ViewConfig.IsScenceObst)
        {
            //CSCameraManager: RegisterObst(self.ViewName)--检查并注册场景阻挠器
            UIManager.Instance.RegisterObst(ViewName);
        }

        OnEnable();
        OnRefresh();
    }

    // 可用，子类扩展实现
    public virtual void OnEnable()
    {
    }


    // 不可用
    public void OnBaseDisable()
    {
        LogUtlis.Info($"窗口名:{ViewName} OnDisable");
        if (ViewConfig.IsScenceObst)
        {
            //CSCameraManager:UnRegisterObst(self.ViewName) --检查并取消注册场景阻挠器
            UIManager.Instance.UnRegisterObst(ViewName);
        }

        OnDisable();
    }

    // 不可用，子类扩展实现
    public virtual void OnDisable()
    {
    }

    // 清除根组件(递归清除嵌套组件绑定)
    public void ClearRootViewComponent()
    {
        if (ViewRootComponent != null)
        {
            ViewRootComponent.Destroy();
            ViewRootComponent = null;
        }
    }

    // 获取View中的数据
    public virtual object OnGetViewParameter(string paraName)
    {
        return null;
    }

    // 窗口清理销毁
    public void Destroy()
    {
        if (_closeCallback != null)
        {
            _closeCallback();
            _closeCallback = null;
        }

        IsOpened = false;
        ClearRootViewComponent();
        // 资源加载未完成的情况下不走清理流程(资源异步加载才有可能发生)
        if (IsLoaded == true)
        {
            OnBaseDisable();
            OnDestroy();
            //RemoveAllCSFunCall();
            RemoveAllListener();

            if (HaveCloseAni)
            {
                GameObject.Destroy(ViewInstance, 0.2f);
            }
            else
            {
                GameObject.Destroy(ViewInstance);
            }
            //ClearPoolPrefabs();
        }
        else
        {
            //EventManager: GetInstance() :Broadcast(EventID.UIMask, false)--特殊情况当界面还没打开服务器又开始推送切场景
            LogUtlis.Error($"出现未加载完成又被关闭的情况!{ViewName}");
        }

        if (uIGoTable != null)
        {
            uIGoTable.RemoveClickBtnEvent();
            uIGoTable = null;
        }
        
        // 清理字典，防止内存泄漏
        Tmp?.Clear();
        Btn?.Clear();
        Img?.Clear();
        RawImg?.Clear();
        Obj?.Clear();
        Toggles?.Clear();
        CachePrefabPaths?.Clear();
        
        ViewName = "";
        ViewConfig = null;
        ParentNode = null;
        ViewOrder = 0;
        OnOpencompletedCB = null;
        OpenCompletedCall = null;
        IsLoaded = false;
        IsOpened = false;
        ViewInstance = null;
        ViewTransform = null;
        ViewCanvas = null;
        ViewRootComponent = null;
        HaveCloseAni = false;
        para = null;
    }

    // 窗口清理(子类重写实现自定义清理流程)
    public virtual void OnDestroy()
    {
    }

    // 关闭自身(子类重写实现自定义关闭流程)
    public void Close()
    {
        // UIManager.Instance
        UIManager.Instance.CloseWindow(ViewName);
        if (UIQueueId > 0)
        {
            //UIQueueManager.StopUIQueueWithId(UIQueueId);
        }
    }

    // 点击Button回调
    public void OnBaseClickBtn(ButtonPro btn)
    {
        OnClickBtn(btn);

        if (IsOpened)
        {
            //if (btn == self.go_table.aorbtn_close || btn == self.go_table.aorbtn_anyClose)
            {
                Close();
            }
        }

        /*    if not IsNil(self.go_table) and not IsNil(self.go_table.aorbtn_help) and btn == self.go_table.aorbtn_help then
                    if tableIsNilOrEmpty(self.ViewConfig.HelpKey) then
                        Logger.Error("该界面未配置HelpKey")
                        return
                end*/

        //PopupManager.ShowHelp(self.ViewConfig.HelpKey[self.CurrentHelpKeyIndex])
    }


    // 点击Button回调,子类扩展使用
    public virtual void OnClickBtn(ButtonPro btn)
    {
        LogUtlis.Info($"BaseUIView.OnClickBtn: {ViewName}, 按钮: {btn?.name}");
    }

    public virtual void OnClickBtnUp(ButtonPro btn)
    {
    }

    public virtual void OnClickBtnDown(ButtonPro btn)
    {
    }

    // 点击Toggle回调
    public virtual void OnClickToggle(Toggle toggle, bool isOn)
    {
    }

    public void OnClickTmp()
    {
    }

    /// <summary>
    /// 更新UI实体对象Order
    /// </summary>
    /// <param name="go">实体对象</param>
    /// <param name="limitordervalue">Order范围限制</param>
    /// <param name="includeself">获取参考Canvas是否包含自身</param>
    public void UpdateUIGameObjectOrder(GameObject go, int limitordervalue, bool includeself)
    {
        Debug.Assert(go != null, "不允许传空实体对象,更新UI实体对象Order失败!");
        Canvas referencecanvas = null;
        if (includeself)
        {
            referencecanvas = go.GetComponent<Canvas>();
        }

        if (referencecanvas == null)
        {
            //Note:
            //GetComponentInParent 会包含自身，这里需要取父节点来获取
            Transform parent = go.transform.parent;
            if (parent == null)
            {
                parent = go.transform;
            }

            referencecanvas = parent.GetComponentInParent<Canvas>();
        }

        // Note:
        // 取余Order范围限制是为了确保不会因为多次计算累加导致Order值计算过大问题
        if (referencecanvas != null)
        {
            var gotransform = go.transform;
            var referencecanvasorder = referencecanvas.sortingOrder;
            foreach (ParticleSystem p in gotransform.GetComponentsInChildren<ParticleSystem>(true))
            {
                Renderer render = p.GetComponent<Renderer>();
                if (null != render)
                {
                    render.sortingOrder = render.sortingOrder % limitordervalue;
                    render.sortingOrder += referencecanvasorder;
                }
            }

            //UI特效也可能存在图片文字
            foreach (var render in gotransform.GetComponentsInChildren<SpriteRenderer>(true))
            {
                render.sortingOrder = render.sortingOrder % limitordervalue;
                render.sortingOrder += referencecanvasorder;
            }

            //也可能加了Canvas
            foreach (var item in gotransform.GetComponentsInChildren<Canvas>(true))
            {
                // 如果参考对象有自身，那么排除自身，避免自身Canvas Order计算错误
                if (includeself)
                {
                    if (item.gameObject == go)
                    {
                        continue;
                    }
                }

                item.sortingOrder = item.sortingOrder % limitordervalue;
                item.sortingOrder += referencecanvasorder;
            }

            //也可能加了MeshRenderer
            foreach (var item in gotransform.GetComponentsInChildren<MeshRenderer>(true))
            {
                item.sortingOrder = item.sortingOrder % limitordervalue;
                item.sortingOrder += (referencecanvasorder + 1); //不加1是看不到的(?)
            }
        }
        else
        {
            LogUtlis.Error($"对象Go:{go.name}找不到有效的参考Canvas,更新UI实体对象Order失败!");
        }
    }

    #region 全局事件监听部分

    private Dictionary<int, EngineEventManager.EventCallback> EventIDList =
        new Dictionary<int, EngineEventManager.EventCallback>();

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

    #region 预加载支持

    // 缓存指定数量预制体
    //---@param path string 路径
    public void CachePrefab(string path)
    {
        if (!CachePrefabPaths.ContainsKey(path))
        {
            CachePrefabPaths.Add(path, 1);
        }
        else
        {
            CachePrefabPaths[path] -= 1;
        }
        /*GameUtil.GetOneShotFrameTimer(1, self, function()
        AssetLoadManager.CachePrefabToPool(GlobalDefine.ePoolType.UIModel, path, 1)
    end)*/
    }

    #endregion

    #region 窗口组件相关部分

    //创建窗口根组件
    public void CreateViewRootComponent()
    {
        if (ViewRootComponent == null)
        {
            //-TODO:采用对象池
            // - @type UnityEngine.GameObject
            GameObject viewrootcomponentinstance = new GameObject($"{ViewName}_ViewRootComponent");
            viewrootcomponentinstance.transform.SetParent(ViewInstance.transform);
            ViewRootComponent = new BaseViewComponent();
            ViewRootComponent.SetBaseViewComponent(null, this, viewrootcomponentinstance);
        }
        else
        {
            LogUtlis.Info($"窗口名:{ViewName}根组件已经创建,请勿重复创建!");
        }
    }

    //为GameObject获得或添加组件(窗口子组件统一添加入口，除了ViewRootComponent不允许直接New)
    //---@param bindtarget UnityEngine.GameObject @绑定的GameObject
    //-@param class BaseViewComponent @lua组件类
    //-@return BaseViewComponent @组件实例化类
    public T GetOrAddComponent<T>(GameObject bindtarget) where T : BaseViewComponent
    {
        return GetOrAddComponent(bindtarget, typeof(T)) as T;
    }

    public BaseViewComponent GetOrAddComponent(GameObject bindtarget, Type componentType)
    {
        if (ViewRootComponent != null)
        {
            return ViewRootComponent.GetOrAddComponent(ViewRootComponent, this, bindtarget, componentType);
        }
        else
        {
            LogUtlis.Info($"窗口名:{ViewName}根组件已经被清理,无法进行组件绑定!");
            return null;
        }
    }

    //-获取GameObject中挂载的脚本组件
    //-@param bindtarget UnityEngine.GameObject @绑定的GameObject
    //-@param class BaseViewComponent  @lua组件类
    //-@return BaseViewComponent @实例化类 或nil
    public BaseViewComponent GetComponent(GameObject bindtarget, Type componentType)
    {
        if (ViewRootComponent != null)
        {
            return ViewRootComponent.GetComponent(bindtarget, componentType);
        }
        else
        {
            LogUtlis.Info($"窗口名:{ViewName}根组件已经被清理,无法进行组件获取!");
            return null;
        }
    }

    //-移除GameObject中挂载的指定lua组件
    //-@param bindtarget UnityEngine.GameObject @绑定的GameObject
    //-@param class BaseViewComponent  @lua组件类
    //-@return boolean @移除是否成功
    public bool RemoveComponent(GameObject bindtarget, Type componentType)
    {
        if (ViewRootComponent != null)
        {
            return ViewRootComponent.RemoveComponent(bindtarget, componentType);
        }
        else
        {
            LogUtlis.Info($"窗口名:{ViewName}根组件已经被清理,无法进行组件清理!");
            return false;
        }
    }

    //-移除指定组件实例对象
    //-@param componentinstance BaseViewComponent @lua组件实例对象
    //-@return boolean @移除是否成功
    public bool RemoveComponentInstance(BaseViewComponent componentinstance, Type componentType)
    {
        if (ViewRootComponent != null)
        {
            return ViewRootComponent.RemoveComponentInstance(componentinstance, componentType);
        }
        else
        {
            LogUtlis.Info($"窗口名:{ViewName}根组件已经被清理,无法进行组件实例对象清理!");
            return false;
        }
    }

    #endregion
}