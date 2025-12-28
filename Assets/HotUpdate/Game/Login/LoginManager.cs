using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginManager : GameSingleton<LoginManager>
{
    private bool isInitialized = false;
    
    // 本地存储的键名
    private const string LAST_SERVER_ID_KEY = "last_selected_server_id";
    private const string LAST_SERVER_NAME_KEY = "last_selected_server_name";
    private const string LAST_SERVER_URL_KEY = "last_selected_server_url";
    public override void Init()
    {
        if (isInitialized)
        {
            LogUtlis.Warn("[LoginManager] 已初始化，跳过重复初始化");
            return;
        }
        
        base.Init();
        
        // 初始化账号服务
        AccountServiceManager.Instance.Init(Launcher.Instance.httpIp);
        isInitialized = true;
        LogUtlis.Info("[LoginManager] 初始化完成");
        
        // 检查本地账号并自动登录
        // CheckAndAutoLogin();
    }

    /// <summary>
    /// 检查本地账号并自动登录
    /// </summary>
    public void CheckAndAutoLogin()
    {
        if (AccountServiceManager.Instance.HasLocalAccount)
        {
            LogUtlis.Info("[LoginManager] 发现本地账号，自动登录");
            AccountServiceManager.Instance.LoginWithLocalAccount((success, msg, servers) =>
            {
                if (success)
                {
                    LogUtlis.Info($"[LoginManager] 自动登录成功，服务器数量: {servers?.Count ?? 0}");
                    // 直接使用服务器返回的完整URL
                    LoginGameServer(servers[0].url);
                }
                else
                {
                    LogUtlis.Error($"[LoginManager] 自动登录失败: {msg}");
                    // 登录失败，可能需要重新注册或输入账号
                }
            });
        }
        else
        {
            LogUtlis.Info("[LoginManager] 没有本地账号，需要注册或登录");
        }
    }
    /// <summary>
    /// 连接游戏服务器
    /// </summary>
    /// <param name="url"></param>
    public void LoginGameServer(string url)
    {
        NetworkManager.Instance.Init(ServerType.Game,url,AccountServiceManager.Instance.CurrentToken);
    }   

    /// <summary>
    /// 注册账号（成功后自动登录并获取服务器列表）
    /// </summary>
    public void Register(string username, string password, Action<bool, string> callback = null)
    {
        AccountServiceManager.Instance.Register(username, password, (success, msg, servers) =>
        {
            if (success)
            {
                LogUtlis.Info($"[LoginManager] 注册成功，服务器数量: {servers?.Count ?? 0}");
            }
            callback?.Invoke(success, msg);
        });
    }

    /// <summary>
    /// 登录账号
    /// </summary>
    public void Login(string username, string password, Action<bool, string> callback = null)
    {
        AccountServiceManager.Instance.Login(username, password, (success, msg, servers) =>
        {
            if (success)
            {
                LogUtlis.Info($"[LoginManager] 登录成功，服务器数量: {servers?.Count ?? 0}");
            }
            callback?.Invoke(success, msg);
        });
    }


    


    public override void OnDestroy()
    {
        isInitialized = false;
    }
    
    /// <summary>
    /// 打开服务器列表界面
    /// </summary>
    private void OpenServerListView(GameServerInfoJson lastSelectedServer)
    {
        LogUtlis.Info($"[LoginManager] 打开服务器列表界面，上次选择的服务器: {lastSelectedServer?.server_name ?? "无"}");
        // TODO: 根据你的项目实际情况实现
    }
    
    #region 上次选择服务器的本地存储
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        isInitialized = false;
        LogUtlis.Info("[LoginManager] 清理完成");
    }
    
    /// <summary>
    /// 保存上次选择的服务器
    /// </summary>
    public void SaveLastSelectedServer(GameServerInfoJson server)
    {
        if (server == null)
        {
            LogUtlis.Warn("[LoginManager] 尝试保存空的服务器信息");
            return;
        }
        
        PlayerPrefs.SetInt(LAST_SERVER_ID_KEY, server.server_id);
        PlayerPrefs.SetString(LAST_SERVER_NAME_KEY, server.server_name);
        PlayerPrefs.SetString(LAST_SERVER_URL_KEY, server.url);
        PlayerPrefs.Save();
        
        LogUtlis.Info($"[LoginManager] 保存上次选择的服务器: {server.server_name} (ID:{server.server_id})");
    }
    
    /// <summary>
    /// 加载上次选择的服务器
    /// </summary>
    public GameServerInfoJson LoadLastSelectedServer()
    {
        if (!HasLastSelectedServer())
        {
            return null;
        }
        
        var server = new GameServerInfoJson
        {
            server_id = PlayerPrefs.GetInt(LAST_SERVER_ID_KEY, 0),
            server_name = PlayerPrefs.GetString(LAST_SERVER_NAME_KEY, ""),
            url = PlayerPrefs.GetString(LAST_SERVER_URL_KEY, "")
        };
        
        return server;
    }
    
    /// <summary>
    /// 检查是否有上次选择的服务器
    /// </summary>
    public bool HasLastSelectedServer()
    {
        return PlayerPrefs.HasKey(LAST_SERVER_ID_KEY) && 
               PlayerPrefs.HasKey(LAST_SERVER_NAME_KEY);
    }
    
    /// <summary>
    /// 清除上次选择的服务器
    /// </summary>
    public void ClearLastSelectedServer()
    {
        PlayerPrefs.DeleteKey(LAST_SERVER_ID_KEY);
        PlayerPrefs.DeleteKey(LAST_SERVER_NAME_KEY);
        PlayerPrefs.DeleteKey(LAST_SERVER_URL_KEY);
        PlayerPrefs.Save();
        
        LogUtlis.Info("[LoginManager] 清除上次选择的服务器");
    }
    
    #endregion
    
    public void ConnectLoginServer()
    {
        
    }
}

/// <summary>
/// 服务器列表包装类（用于JSON解析）
/// </summary>
[Serializable]
public class ServerListWrapper
{
    public GameServerInfoJson[] servers;
}
