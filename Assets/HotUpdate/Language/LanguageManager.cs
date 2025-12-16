using System;
using System.Collections.Generic;
using cfg;

public class LanguageManager : GameSingleton<LanguageManager>
{
    /// <summary>
    /// 语言切换事件
    /// </summary>
    public static event Action<LangType> OnLanguageChanged;

    private LangType LangType = Launcher.Instance.LangType;

    private static Dictionary<string, string> langVals = new Dictionary<string, string>();
    private static Dictionary<string, string> langPckeVals = new Dictionary<string, string>();

    public static bool Init = false;

    public void SetLanguage(LangType langType)
    {
        if (LangType != langType)
        {
            LangType = langType;
            InitConfig();
            // 触发语言切换事件
            OnLanguageChanged?.Invoke(langType);
        }
    }

    public LangType GetLanguage()
    {
        return LangType;
    }
    public void InitConfig()
    {
        langVals.Clear();
        langPckeVals.Clear();
        // LangType= Launcher.Instance.LangType;
        Tblanguage lang = ConfigManager.Instance.GetVOData<Tblanguage>("tblanguage");
        foreach (var item in lang.DataList)
        {
            if (LangType == LangType.CN)
            {
                langVals[item.Key] = item.CN;
            }
            else if (LangType == LangType.EN)
            {
                langVals[item.Key] = item.EN;
            }
        }
        ConfigManager.Instance.RemoveConfigData<Tblanguage>("tblanguage");
        TblanguagePack langpack = ConfigManager.Instance.GetVOData<TblanguagePack>("tblanguagepack");
        foreach (var item in langpack.DataList)
        {
            if (LangType == LangType.CN)
            {
                langPckeVals[item.Key] = item.CN;
            }
            else if (LangType == LangType.EN)
            {
                langPckeVals[item.Key] = item.EN;
            }
        }
        ConfigManager.Instance.RemoveConfigData<TblanguagePack>("tblanguage");
        Init = true;
    }

    /// <summary>
    /// 策划表多语言配置
    /// </summary>
    /// <param name="langKey"></param>
    /// <returns></returns>
    public static string GetLangVal(string langKey)
    {
        if (!Init)
        {
            LogUtlis.Error("Err:ConfigManager 未初始化~！");
            return "";
        }
        if (langVals.ContainsKey(langKey))
        {
            return langVals[langKey];
        }
        LogUtlis.Error(string.Format("Err：多语言key{0}不存在 请检查配置表", langKey));
        return "";
    }
    /// <summary>
    /// 程序多语言配置
    /// </summary>
    /// <param name="langKey"></param>
    /// <returns></returns>
    public static string GetLangPackVal(string langKey)
    {
        if (!Init)
        {
            LogUtlis.Error("Err:ConfigManager 未初始化~！");
            return "";
        }
        if (langPckeVals.ContainsKey(langKey))
        {
            return langPckeVals[langKey];
        }
        LogUtlis.Error(string.Format("Err：多语言key{0}不存在 请检查配置表", langKey));
        return "";
    }
    /// <summary>
    /// 获得多语言图片纹理加载路径
    /// </summary>
    /// <returns></returns>
    public static string GetLangTexturePath()
    {
        switch (Instance.LangType)
        {
            case LangType.CN:
                return "Texture/Language/CN";
            case LangType.EN:
                return "Texture/Language/EN";
            default:
                return "Texture/Language/CN";
        }
    }

    public override void OnDestroy()
    {
        langVals.Clear();
        langPckeVals.Clear();
        
        // 清理静态事件，防止内存泄漏
        OnLanguageChanged = null;
    }
}
