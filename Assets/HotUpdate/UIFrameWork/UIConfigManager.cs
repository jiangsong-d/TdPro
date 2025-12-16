using System.Collections.Generic;

// 窗口配置管理类
public class UIConfigManager
{
    public static Dictionary<string, UIConfig> UIConfig = new Dictionary<string, UIConfig>()
    {
        ///ConstUI
        { "LoadingView", new UIConfig("LoadingView", "Prefabs/Loading/LoadingView", EUIType.ConstUI, "", 15000, true, false, false, false,"")},

        //NormalUI
        { "LoginView", new UIConfig("LoginView", "Prefabs/Login/LoginView", EUIType.NormalUI, "", 0, true, false, false, false,"")},
        { "MainView", new UIConfig("MainView", "Prefabs/MainView/MainView", EUIType.NormalUI, "", 0, true, false, false, false,"")},
       
    };

    public static Dictionary<string, ModulePath> ModulePathsDic = new Dictionary<string, ModulePath>()
    {
        //["ActiveModel"] = new ModulePath("ActiveModel", "UIPage/MainPage/ActiveModel"),
    };

    public static Dictionary<string, SceneConfig> SConfig = new()
    {
        { "MainCityScene", new SceneConfig("MainCityScene", "Prefabs/MainCity/MainCityScene", SceneType.MainCity) },
    };
}

// 窗口配置类
public class UIConfig
{
    public string Name; // 预制体名字，即View名字
    public string ResPath; // 预制体资源路径
    public EUIType UIType; // 页面层级类型
    public string RelativeViewName; // 关联的页面名
    public int ConstOrder; // 页面层级
    public bool IsFullScreen = true;
    public bool IsScenceObst = false;
    public bool IsShowChat = false;
    public bool IsShowPower = false;
    public string PropRes;

    public UIConfig(string Name, string ResPath, EUIType UIType, string RelativeViewName, int ConstOrder,
        bool IsFullScreen, bool IsScenceObst, bool IsShowChat, bool IsShowPower, string PropRes)
    {
        this.Name = Name;
        this.ResPath = ResPath;
        this.UIType = UIType;
        this.RelativeViewName = RelativeViewName;
        this.ConstOrder = ConstOrder;
        this.IsFullScreen = IsFullScreen;
        this.IsScenceObst = IsScenceObst;
        this.IsShowChat = IsShowChat;
        this.IsShowPower = IsShowPower;
        this.PropRes = PropRes;
    }
}

public class SceneConfig
{
    public string Name;
    public string ResPath; // 预制体资源路径
    public SceneType SceneType;

    public SceneConfig(string name, string ResPath, SceneType sceneType)
    {
        this.Name = name;
        this.ResPath = ResPath;
        this.SceneType = sceneType;
    }
}

public class ModulePath
{
    public ModulePath(string _name, string _path)
    {
        moduleName = _name;
        modulePath = _path;
    }

    public string moduleName;
    public string modulePath;
}

// UI层级枚举
// NormalUI number @窗口(e.g. Canvas自增长层)
// ConstUI number @常驻窗口(e.g. Canvas手动控制层)
public enum EUIType
{
    NormalUI,
    ConstUI,
}