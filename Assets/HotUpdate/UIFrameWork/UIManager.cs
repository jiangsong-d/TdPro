using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI窗口管理类
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    public Camera UICamera; // UI摄像机
    public Transform SceneUIRoot;// SceneUIRoot根节点
    public Transform NormalUIRoot;// NormalUI根节点
    public Transform ConstUIRoot;// NormalUI根节点
    public Dictionary<EUIType, Transform> ViewRootMap = new Dictionary<EUIType, Transform>();// 层级节点映射Map(Key为层级值，Value为对应层级节点)
    public Dictionary<EUIType, Dictionary<string, BaseUIView>> LayerAllOpenedViewMap = new Dictionary<EUIType, Dictionary<string, BaseUIView>>();// UI Layer对应所有已打开的窗口集合(Key为UI Layer值,Value为该Layer已打开窗口映射Map--为了获取Layer最高Order时避免不必要的窗口遍历)
    public List<BaseUIView> AllOpenedViewList = new List<BaseUIView>();// 当前所有已打开的窗口列表(用列表的原因是为了确保和打开顺序一致)
    public BaseUIView NextTopFullScreenView = null;// 下一个Order最高的全屏(全屏策略有用)
    public int BlurCount = 0;   // 快速计算已经模糊掉的UI数量
    public Dictionary<string, int> BlurViewMap = new Dictionary<string, int>();// 模糊掉的UI映射
    public Dictionary<string, bool> ObstructionTab = new Dictionary<string, bool>();// 已注册的场景绘制阻挠列表

    // 定义主窗口类型(支持动态注入)
    private Dictionary<string, bool> MainWinType = new Dictionary<string, bool>()
    {
        { "MainView", true }
    };    
    
    /// <summary>
    /// 是否保留打开窗口时的状态(回退到窗口时是否保留打开状态)
    /// </summary>
    private Dictionary<string, bool> KeepOpenWhenBackToWindow = new Dictionary<string, bool>()
    {
        { "LoadingView", true }
    };

    public void Init()
    {
        ViewRootMap.Clear();
        LayerAllOpenedViewMap.Clear();

        LayerAllOpenedViewMap.Add(EUIType.NormalUI, new Dictionary<string, BaseUIView>());
        LayerAllOpenedViewMap.Add(EUIType.ConstUI, new Dictionary<string, BaseUIView>());
        AllOpenedViewList.Clear();
        ObstructionTab.Clear();
        InitUIRoot();
    }

    // 初始化UI根节点
    public void InitUIRoot()
    {
        UICamera = UIModel.Inst.UICamera;
        SceneUIRoot = UIModel.Inst.SceneUIRoot;
        NormalUIRoot = UIModel.Inst.NormalUIRoot;
        ConstUIRoot = UIModel.Inst.ConstUIRoot;

        ViewRootMap[EUIType.NormalUI] = UIModel.Inst.NormalUIRoot;
        ViewRootMap[EUIType.ConstUI] = UIModel.Inst.ConstUIRoot;
    }

    /// <summary>
    /// 打开指定窗口
    /// </summary>
    /// <typeparam name="T">窗口脚本</typeparam>
    /// <param name="viewName">窗口名</param>
    /// <param name="para">窗口传参</param>
    /// <returns></returns>
    public T OpenWindow<T>(string viewName, object para = null) where T : BaseUIView, new()
    {
        T viewValue = null;
        if (GetOpenedWindow(viewName) != null)
        {
            //打开已经打开的界面直接回退
            viewValue = BackToWindow<T>(viewName, para);
            return viewValue;
        }
        if (!IsWindowOpened(viewName))
        {
            //GameUtil.UsedTimeStartMark("打开窗口"..viewName)

            // Note:
            // 1.相对Order窗口参考的窗口必须打开的情况下才允许打开当前窗口
            UIConfig uiconfig = UIConfigManager.UIConfig[viewName];
            //LogUtlis.InfoError($"找不到窗口:{viewName}信息!");
            if (uiconfig.RelativeViewName != "")
            {
                if (IsWindowOpened(uiconfig.RelativeViewName))
                {
                    LogUtlis.Error($"窗口名:{viewName}参考窗口:{uiconfig.RelativeViewName}未打开,不允许打开!");
                    return null;
                }
            }

            //EventManager: Broadcast(EventID.UIMask, true)--打开遮罩

            // 每一次都New一个对象来支持异步加载等概念
            // BaseUIView
            viewValue = new T();
            Transform uiviewparentnode = GetLayerTransform(uiconfig.UIType);
            // 获取窗口Order必须在添加到LayerAllOpenedViewMap之前,因为里面会访问
            int vieworder = GetUIViewWindowOrder(uiconfig);

            viewValue.ViewConfig = uiconfig;
            viewValue.ViewName = uiconfig.Name;
            viewValue.ViewOrder = vieworder;
            // UI数据维护
            AllOpenedViewList.Add(viewValue);
            if (LayerAllOpenedViewMap[uiconfig.UIType].ContainsKey(uiconfig.Name))
            {
                LayerAllOpenedViewMap[uiconfig.UIType][uiconfig.Name] = viewValue;
            }
            else
            {
                LayerAllOpenedViewMap[uiconfig.UIType].Add(uiconfig.Name, viewValue);
            }
            LogUtlis.Info($"UIType:{uiconfig.UIType}打开窗口:{uiconfig.Name}");
            LogUtlis.Info($"UIType:{uiconfig.UIType}当前已打开窗口数量:{LayerAllOpenedViewMap[uiconfig.UIType].Count}");
            // 处理窗口逻辑
            DoFullScreenStrategy(viewValue, true);
            viewValue.Init(uiviewparentnode, (view) =>
            {
                OnWindowOpenCompleted(view);
            }, para);
            //EventManager: Broadcast(EventID.OnViewOpened, uiconfig.Name)
            return viewValue;
        }
        else
        {
            LogUtlis.Error($"窗口名:{viewName}已打开,无法重复打开!");
            return viewValue;
        }
    }

    // 窗口完全打开回调(OnShow之后)
    //-@param viewInstance BaseUIView @打开的窗口
    public void OnWindowOpenCompleted(BaseUIView viewInstance)
    {
        UpdateViewActiveState();
        
        // 自动处理InputManager的地图拖拽开关
        // 过滤ConstUI和MainView，其他NormalUI打开时禁用地图拖拽
        if (InputManager.IsInstance() && 
            viewInstance.ViewConfig.UIType != EUIType.ConstUI && 
            viewInstance.ViewName != "MainView")
        {
            InputManager.Instance.SetEnableMapDrag(false);
            InputManager.Instance.SetEnablePinchZoom(false);
        }
        
        //EventManager: Broadcast(EventID.UIMask, false)--解除遮罩
        //EventManager:Broadcast(EventID.OnViewCompletedOpened, viewInstance)--这里放在最后处理

        //GameUtil.UsedTimeEndLog("打开窗口"..viewInstance.ViewName)
    }

    // 关闭指定窗口
    //-@return boolean @是否关闭成功
    public bool CloseWindow(string viewName)
    {
        if (IsWindowOpened(viewName))
        {
            // UIConfig_Item
            if (!UIConfigManager.UIConfig.ContainsKey(viewName))
            {
                LogUtlis.Warn($"UIConfig不存在窗口:{viewName}，跳过关闭");
                return false;
            }
            
            UIConfig uiconfig = UIConfigManager.UIConfig[viewName];
            if (uiconfig == null || !LayerAllOpenedViewMap.ContainsKey(uiconfig.UIType))
            {
                LogUtlis.Warn($"窗口配置或Layer不存在:{viewName}，跳过关闭");
                return false;
            }
            
            if (!LayerAllOpenedViewMap[uiconfig.UIType].ContainsKey(viewName))
            {
                return false;
            }
            
            BaseUIView viewinstance = LayerAllOpenedViewMap[uiconfig.UIType][viewName];
            if (viewinstance == null)
            {
                LogUtlis.Warn($"窗口实例为null:{viewName}，跳过关闭");
                LayerAllOpenedViewMap[uiconfig.UIType].Remove(viewName);
                return false;
            }
            
            AllOpenedViewList.Remove(viewinstance);
            LayerAllOpenedViewMap[viewinstance.ViewConfig.UIType].Remove(viewName);
            LogUtlis.Info($"关闭窗口:{viewName}");
            LogUtlis.Info($"UIType:{viewinstance.ViewConfig.UIType}当前已打开窗口数量:{LayerAllOpenedViewMap[viewinstance.ViewConfig.UIType].Count}");
            LogUtlis.Info($"当前已打开窗口数量:{AllOpenedViewList.Count}");
            // 自动处理InputManager的地图拖拽开关（在销毁前处理）
            // 过滤ConstUI和MainView，其他NormalUI关闭时恢复地图拖拽
            if (InputManager.IsInstance() && 
                viewinstance.ViewConfig != null &&
                viewinstance.ViewConfig.UIType != EUIType.ConstUI && 
                viewName != "MainView")
            {
                // 检查是否还有其他NormalUI窗口打开（除了MainView和当前关闭的窗口）
                bool hasOtherNormalUI = false;
                foreach (var view in AllOpenedViewList)
                {
                    if (view != null && 
                        view.ViewConfig != null &&
                        view.ViewConfig.UIType == EUIType.NormalUI && 
                        view.ViewName != "MainView")
                    {
                        hasOtherNormalUI = true;
                        break;
                    }
                }
                
                // 如果没有其他NormalUI窗口，恢复地图拖拽
                if (!hasOtherNormalUI)
                {
                    InputManager.Instance.SetEnableMapDrag(true);
                    InputManager.Instance.SetEnablePinchZoom(true);
                }
            }
            
            // 直接销毁窗口，不需要更新其他窗口的显示状态
            viewinstance.Destroy();
            
            //EventManager: Broadcast(EventID.OnViewClosed, viewName)
            return true;
        }
        else
        {
            LogUtlis.Info($"窗口名:{viewName}未打开,无法关闭!");
            return false;
        }
    }

    //关闭所有窗口（退出游戏时才调用,这个接口会干掉所有窗口,包括Const窗口）
    public void CloseAllWindow()
    {
        if (AllOpenedViewList == null || AllOpenedViewList.Count == 0)
        {
            return;
        }
        
        // 复制列表避免遍历时修改集合
        List<BaseUIView> viewsToClose = new List<BaseUIView>(AllOpenedViewList);
        for (int i = viewsToClose.Count - 1; i >= 0; i--)
        {
            if (viewsToClose[i] != null)
            {
                CloseWindow(viewsToClose[i].ViewName);
            }
        }
    }

    // 关闭所有窗口,(保留Const窗口与MainView)
    public void CloseAllWindowByConst()
    {
        string name = UIConfigManager.UIConfig["MainPage"].Name;    //ConfigManager.UIConfig.MainView.Name
        for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
        {
            if (UIConfigManager.UIConfig[AllOpenedViewList[i].ViewName].UIType != EUIType.ConstUI)
            {
                if (AllOpenedViewList[i].ViewName != name)
                {
                    CloseWindow(AllOpenedViewList[i].ViewName);
                }
            }
        }
    }

    // 场景使用打开场景绑定界面
    //@param viewName string 窗口名称
    public T OpenSceneWindow<T>(string viewName, Action callback = null) where T : BaseUIView, new()
    {
        T viewpara = new T();
        if (!MainWinType[viewName])
        {
            MainWinType[viewName] = true;
        }
        CloseAllWindowByConst();
        if (GetOpenedWindow(viewName) != null)
        {
            callback?.Invoke();
            return viewpara;
        }
        T view = OpenWindow<T>(viewName);
        view.OnOpenCompleted(() =>
        {
            callback?.Invoke();
        });
        return viewpara;
    }

    // 主窗口是否打开并显示中
    public bool IsMainViewsShow()
    {
        foreach (var item in MainWinType)
        {
            if (IsWindowOpenedAndShow(item.Key))
            {
                return true;
            }
        }
        return false;
    }

    //是否是主窗口
    public bool IsMainView(string viewName)
    {
        return MainWinType[viewName];
    }

    // 获取当前打开的主界面(可能不是激活的！)
    public BaseUIView GetCurrentMainView()
    {
        foreach (var item in MainWinType)
        {
            BaseUIView view = GetOpenedWindow(item.Key);
            if (view != null)
            {
                return view;
            }
        }
        return null;
    }


    // 是否有主界面外的其他界面
    //-@return boolean
    public bool HavePanelExceptMainView()
    {
        for (int i = 0; i < AllOpenedViewList.Count; i++)
        {
            if (!MainWinType[AllOpenedViewList[i].ViewName])
            {
                if (UIConfigManager.UIConfig[AllOpenedViewList[i].ViewName].UIType != EUIType.ConstUI)
                {
                    return true;
                }
            }
        }
        return false;
    }

    //关闭除了主窗口以外的所有窗口
    //@param keepConst boolean 是否保留Const界面(不传默认保留)
    public void CloseAllExceptMainWindow(bool keepConst = true)
    {
        for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
        {
            if (!MainWinType.ContainsKey(AllOpenedViewList[i].ViewName))
            {
                if (keepConst)
                {
                    if (UIConfigManager.UIConfig[AllOpenedViewList[i].ViewName].UIType != EUIType.ConstUI)
                    {
                        CloseWindow(AllOpenedViewList[i].ViewName);
                    }
                }
                else
                {
                    CloseWindow(AllOpenedViewList[i].ViewName);
                }
            }
        }
    }

    // 关闭除了主窗口以及特殊窗口以外的所有窗口
    // @param windows table<string, boolean>[]  <界面名称,true>  e.p.{[界面名称1] = true,[界面名称2] = true}
    public void CloseAllExceptMainAndSpecialWindow(Dictionary<string, bool> windows)
    {
        for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
        {
            if (!MainWinType[AllOpenedViewList[i].ViewName] && !windows[AllOpenedViewList[i].ViewName])
            {
                CloseWindow(AllOpenedViewList[i].ViewName);
            }
        }
    }

    // 回退到指定界面
    // @param viewName string @窗口名
    public T BackToWindow<T>(string viewName, object para = null) where T : BaseUIView, new()
    {
        for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
        {
            if (AllOpenedViewList[i].ViewName == viewName)
            {
                return AllOpenedViewList[i] as T;
            }

            // 回退的时候 不关闭主UI，和BattleLoadPanel
            if (!MainWinType.ContainsKey(AllOpenedViewList[i].ViewName) &&
                !KeepOpenWhenBackToWindow.ContainsKey(AllOpenedViewList[i].ViewName))
            {
                CloseWindow(AllOpenedViewList[i].ViewName);
            }
        }
        return OpenWindow<T>(viewName, para);
    }


    // 回退到主界面
    public void BackToMainView()
    {
        //BackToWindow<MainPage>("MainPage");
    }

    //指定窗口是否打开
    //-@param viewName string @窗口名
    //-@return boolean @指定窗口是否打开
    public bool IsWindowOpened(string viewName)
    {
        return GetOpenedWindow(viewName) != null;
    }

    //-指定窗口是否打开并显示中
    //---@param viewName string @窗口名
    //---@return boolean @指定窗口是否显示中
    public bool IsWindowOpenedAndShow(string viewName)
    {
        BaseUIView view = GetOpenedWindow(viewName);
        return view != null && view.IsActive();
    }

    //---获取指定已打开窗口
    //---@param viewName string @窗口名
    //---@return BaseUIView @已打开的对应窗口
    public BaseUIView GetOpenedWindow(string viewName)
    {
        if (!UIConfigManager.UIConfig.ContainsKey(viewName))
        {
            LogUtlis.Error($"GetOpenedWindow不存在:{viewName}");
            return null;
        }
        EUIType uilayer = UIConfigManager.UIConfig[viewName].UIType;
        if (!LayerAllOpenedViewMap.ContainsKey(uilayer))
        {
            return null;
        }
        if (!LayerAllOpenedViewMap[uilayer].ContainsKey(viewName))
        {
            return null;
        }
        return LayerAllOpenedViewMap[uilayer][viewName];
    }

    //--- 获取指定UILayer的挂载节点
    //--@private
    //---@param uiLayer EUIType @UI层级
    //---@return UnityEngine.Transform @指定UILayer对应的挂载节点
    public Transform GetLayerTransform(EUIType uiLayer)
    {
        return ViewRootMap[uiLayer];
    }

    // 获取指定UI层级已打开窗口Order最高的值(不含固定Order窗口, 没有有效窗口则返回0)
    // @param uiLayer number @UI层级
    // @return number @获取指定UI层级Order最高的值(不含固定Order窗口, 没有有效窗口则返回0)
    public int GetUILayerHighestWindowOrder(EUIType uiLayer)
    {
        int highestorder = 0;
        if (!LayerAllOpenedViewMap.ContainsKey(uiLayer))
        {
            return highestorder;
        }
        // viewinstance BaseUIView
        foreach (var item in LayerAllOpenedViewMap[uiLayer])
        {
            BaseUIView viewinstance = item.Value;
            if (!viewinstance.IsUsingConstOrder())
            {
                if (viewinstance.ViewOrder > highestorder)
                {
                    highestorder = viewinstance.ViewOrder;
                }
            }
        }
        return highestorder;
    }


    //获取指定窗口信息的窗口的Order
    //---@param viewConfig UIConfig_Item @窗口配置信息
    //---@return number @获取指定窗口信息的窗口的Order
    public int GetUIViewWindowOrder(UIConfig viewConfig)
    {
        int windowcanvasorder = 0;
        if (viewConfig.UIType == EUIType.ConstUI)
        {
            // 常驻窗口Order读取指定的Order配置
            windowcanvasorder = viewConfig.ConstOrder;
            if (windowcanvasorder <= 0)
            {
                LogUtlis.Error($"常驻窗口ConstOrder不能为0，viewName:{viewConfig.Name}");
            }
        }
        else if (viewConfig.UIType == EUIType.NormalUI)
        {
            // 普通窗口Order有两种情况：
            // 未指定相对Order窗口规则如下:
            // 窗口Order = 当前Layer最高Order窗口(不含固定Order窗口)(没有窗口则为Layer起始Order) + 单窗口之前相差的SortingOrder
            // 指定相对Order窗口规则如下:
            // 窗口Order = 相对窗口Order + 相对偏移Order设置
            if (viewConfig.RelativeViewName == "")
            {
                int uilayerhighestorder = GetUILayerHighestWindowOrder(viewConfig.UIType);
                if (uilayerhighestorder != 0)
                {
                    windowcanvasorder = uilayerhighestorder + UIData.PerWindowOrder;
                }
                else
                {
                    windowcanvasorder = UIData.LayerStartOrder;
                }
            }
            else
            {
                BaseUIView relativewindow = GetOpenedWindow(viewConfig.RelativeViewName);
                // 设置了相对Order的直接按照设定的相对值取值
                windowcanvasorder = relativewindow.ViewOrder + viewConfig.ConstOrder;
            }
        }
        LogUtlis.Info($"UIType:{viewConfig.UIType}获取Window Order:{windowcanvasorder}");
        return windowcanvasorder;
    }


    //处理全屏窗口策略
    //@param viewInstance BaseUIView @对应窗口实体对象
    //@param isOpen boolean @是否是打开(反之关闭)
    public void DoFullScreenStrategy(BaseUIView viewInstance, bool isOpen = false)
    {
        // 原本打算采用改Layer的方式来做全屏窗口优化，但粒子不受根节点Layer修改影响,又不想通过GetComponentsInChidren的方式来设置Layer
        // 所以修改成SetActive的方式来做全屏优化(SetActive支持标志位来表示显隐原因来支持不同的设置显隐缘由)
        if (viewInstance.ViewConfig.IsFullScreen)
        {
            // 策略如下:
            // 1. 如果是打开全屏窗口就找到当前Order最高的全屏作为Active状态设置参考对象
            // 2. 如果是关闭全屏窗口就找到排除当前关闭窗口以外的Order最高的全屏作为Active状态设置参考对象
            //-@type BaseUIView
            BaseUIView nexttopmostfullscreenwindow = null;
            for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
            {
                BaseUIView openedview = AllOpenedViewList[i];
                if ((isOpen || (!isOpen && viewInstance != openedview)) && openedview.ViewConfig.IsFullScreen)
                {
                    if (nexttopmostfullscreenwindow == null || (nexttopmostfullscreenwindow != null && nexttopmostfullscreenwindow.ViewOrder < openedview.ViewOrder))
                    {
                        nexttopmostfullscreenwindow = openedview;
                    }
                }
            }

            NextTopFullScreenView = nexttopmostfullscreenwindow;
            if (NextTopFullScreenView != null)
            {
                LogUtlis.Info($"下一个有效全屏窗口:{NextTopFullScreenView.ViewName}");
            }
            else
            {
                LogUtlis.Info("无下一个有效全屏窗口!");
            }

            for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
            {
                //-@type BaseUIView
                BaseUIView uiview = AllOpenedViewList[i];
                if (NextTopFullScreenView != null)
                {
                    if (uiview.ViewOrder < NextTopFullScreenView.ViewOrder)
                    {
                        uiview.UpdateActiveFlag(false, ViewActiveFlag.FullScreenStrategyFlag);
                        //LogUtlis.Info($"全屏窗口:{viewInstance.ViewName} 操作:{isOpen} 设置已打开窗口:{uiview.ViewName}的隐藏标志位!");
                    }
                    else
                    {
                        uiview.UpdateActiveFlag(true, ViewActiveFlag.FullScreenStrategyFlag);
                       // LogUtlis.Info($"全屏窗口:{viewInstance.ViewName} 操作:{isOpen} 设置已打开窗口:{uiview.ViewName}的隐藏标志位!");
                    }
                }
                else
                {
                    //没有有效全屏窗口说明是关闭全屏窗口后找不到任何全屏窗口了
                    uiview.UpdateActiveFlag(true, ViewActiveFlag.FullScreenStrategyFlag);
                    //LogUtlis.Info($"全屏窗口:{viewInstance.ViewName} 操作:{isOpen} 设置已打开窗口:{uiview.ViewName}的隐藏标志位!");
                }
            }
        }
        else
        {
            // 策略如下:
            // 1. 如果打开的是非全屏窗口需要根据当前下一个最高全屏窗口决定新开窗口的Active状态,确保新开窗口处于正确的Active状态
            if (NextTopFullScreenView != null && isOpen)
            {
                if (NextTopFullScreenView.ViewOrder > viewInstance.ViewOrder)
                {
                    viewInstance.UpdateActiveFlag(false, ViewActiveFlag.FullScreenStrategyFlag);
                    //LogUtlis.Info($"非全屏窗口:{viewInstance.ViewName} 操作:{isOpen} 低于下一个全屏窗口:{NextTopFullScreenView.ViewName} Order 设置已打开窗口:{viewInstance.ViewName}的隐藏标志位!");
                }
            }
        }
    }

    // 更新显示状态
    public void UpdateViewActiveState()
    {
        for (int i = AllOpenedViewList.Count - 1; i >= 0; i--)
        {
            BaseUIView uiview = AllOpenedViewList[i];
            if (uiview.IsAvalible())
            {
                uiview.UpdateActiveState(null);
            }
        }
    }

    // 是否为最顶层界面(不区分全屏 非全屏界面)
    // ---@param viewName string 判断是否为顶层界面的界面名
    public bool IsTopView(string viewName)
    {
        int viewCount = AllOpenedViewList.Count;
        if (viewCount > 0)
        {
            return AllOpenedViewList[viewCount].ViewName == viewName;
        }
        else
        {
            return false;
        }
    }

    // 获取最顶层界面名(不区分全屏 非全屏界面)
    //---@return string 最顶层界面名
    public string GetTopViewName()
    {
        int viewCount = AllOpenedViewList.Count;
        if (viewCount > 0)
        {
            return AllOpenedViewList[viewCount - 1].ViewName;
        }
        else
        {
            return "";
        }
    }

    // 返回当前所有打开的界面队列界面名
    public List<string> GetAllOpenedViewNameList()
    {
        List<string> t = new List<string>();
        for (int i = 0; i < AllOpenedViewList.Count; i++)
        {
            t.Add(AllOpenedViewList[i].ViewName);
        }
        return t;
    }


    #region 场景绘制阻挠

    // 注册场景绘制阻挠器
    //---@param viewName string 窗口名称
    public void RegisterObst(string viewName)
    {
        ObstructionTab[viewName] = true;
        CheckObst();
    }

    // 取消注册场景绘制阻挠器
    //---@param viewName string 窗口名称
    public void UnRegisterObst(string viewName)
    {
        if (ObstructionTab[viewName])
        {
            ObstructionTab.Remove(viewName);
        }
        CheckObst();
    }

    // 场景绘制阻挠器逻辑处理
    //---@return boolean 是否有场景阻挠
    public void CheckObst()
    {
        if (IsObst())
        {
            //GameUtil.HideWeather();
        }
        else
        {
            //GameUtil.RecoverWeather();
        }
    }

    // 是否有场景阻挠
    // ---@return boolean 是否有场景阻挠
    public bool IsObst()
    {
        return ObstructionTab.Count > 0;
    }
    #endregion

    // 析构函数
    public void Dispose()
    {
        CloseAllWindow();
        UICamera = null;
        SceneUIRoot = null;
        NormalUIRoot = null;
        ViewRootMap.Clear();
        LayerAllOpenedViewMap.Clear();
        AllOpenedViewList.Clear();
        NextTopFullScreenView = null;
        ObstructionTab.Clear();
        BlurViewMap.Clear();
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
