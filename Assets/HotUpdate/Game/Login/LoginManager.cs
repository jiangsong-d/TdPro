using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginManager : GameSingleton<LoginManager>
{
    public bool isConnectionAccountServer = false;
    private bool isInitialized = false;  // 防止重复初始化
    
    // 本地存储的键名
    private const string LAST_SERVER_ID_KEY = "last_selected_server_id";
    private const string LAST_SERVER_NAME_KEY = "last_selected_server_name";
    private const string LAST_SERVER_HOST_KEY = "last_selected_server_host";
    private const string LAST_SERVER_PORT_KEY = "last_selected_server_port";
    
    // 事件：服务器列表加载完成
    public event Action<List<GameServerInfoJson>> OnServerListLoaded;
    // 事件：上次服务器检查完成（是否有保存的服务器）
    public event Action<bool, GameServerInfoJson> OnLastServerCheck;
    
    public override void Init()
    {
        if (isInitialized)
        {
            LogUtlis.Warn("[LoginManager] 已初始化，跳过重复初始化");
            return;
        }
        
        base.Init();
        
        // 先取消订阅，防止重复订阅
        AccountServiceManager.Instance.OnConnectionTest -= OnConnectionTest;
        AccountServiceManager.Instance.OnLoginResult -= OnLoginResult;
        AccountServiceManager.Instance.OnServerListLoaded -= OnAccountServerListLoaded;
        
        // 重新订阅
        AccountServiceManager.Instance.OnConnectionTest += OnConnectionTest;
        AccountServiceManager.Instance.OnLoginResult += OnLoginResult;
        AccountServiceManager.Instance.OnServerListLoaded += OnAccountServerListLoaded;
        
        AccountServiceManager.Instance.Init(Launcher.Instance.httpIp, autoTest: true);
        isInitialized = true;
        LogUtlis.Info("[LoginManager] 初始化完成");
    }

    public void OnConnectionTest(bool success, string message)
    {
        if (success)
        {
           isConnectionAccountServer = true;
           LogUtlis.Info("[LoginManager] 账号服务器连接成功");
        }
        else
        {
            isConnectionAccountServer = false;
            LogUtlis.Error($"[LoginManager] 账号服务器连接失败: {message}");
        }
    }
    
    public void OnLoginResult(bool success, string message)
    {
        if (success)
        {
            LogUtlis.Info("[LoginManager] 登录成功，获取服务器列表");
            // 登录成功后，获取服务器列表
            AccountServiceManager.Instance.GetServerList();
        }
        else
        {
            LogUtlis.Error($"[LoginManager] 登录失败: {message}");
        }
    }

    
    /// <summary>
    /// 服务器列表加载回调
    /// </summary>
    private void OnAccountServerListLoaded(bool success, string serverListJson)
    {
        if (!success)
        {
            LogUtlis.Error($"[LoginManager] 服务器列表获取失败: {serverListJson}");
            return;
        }

        try
        {
            var responseJson = JsonUtility.FromJson<LoginResponseJson>(serverListJson);
            
            if (responseJson?.code != 0 || responseJson.data == null)
            {
                LogUtlis.Error($"[LoginManager] 服务器返回错误: {responseJson?.message ?? "未知错误"}");
                OpenServerListView(null);
                return;
            }

            // TODO: 根据实际data结构解析服务器列表
            LogUtlis.Info("[LoginManager] 服务器列表获取成功");
            OpenServerListView(null);
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"[LoginManager] 解析失败: {ex.Message}");
            OpenServerListView(null);
        }
    }
    
    /// <summary>
    /// 检查上次选择的服务器
    /// </summary>
    private void CheckLastSelectedServer(List<GameServerInfoJson> serverList)
    {
        if (HasLastSelectedServer())
        {
            var lastServer = LoadLastSelectedServer();
            LogUtlis.Info($"[LoginManager] 发现上次选择的服务器: {lastServer.server_name} (ID:{lastServer.server_id})");
            
            // 验证服务器是否仍然在列表中且可用
            var matchedServer = serverList.Find(s => s.server_id == lastServer.server_id);
            
            if (matchedServer != null && matchedServer.status == "online")
            {
                LogUtlis.Info($"[LoginManager] 上次服务器可用，打开服务器列表并自动选择");
                OnLastServerCheck?.Invoke(true, matchedServer);
                // 打开服务器列表，并传入上次选择的服务器
                OpenServerListView(matchedServer);
            }
            else
            {
                LogUtlis.Warn($"[LoginManager] 上次服务器不可用或已下线，打开服务器列表");
                OnLastServerCheck?.Invoke(false, null);
                // 服务器不可用，清除记录，打开服务器列表
                ClearLastSelectedServer();
                OpenServerListView(null);
            }
        }
        else
        {
            LogUtlis.Info("[LoginManager] 没有上次选择的服务器，打开服务器列表");
            OnLastServerCheck?.Invoke(false, null);
            // 没有保存的服务器，打开服务器列表
            OpenServerListView(null);
        }
    }

    public override void OnDestroy()
    {
        AccountServiceManager.Instance.OnConnectionTest -= OnConnectionTest;
        AccountServiceManager.Instance.OnLoginResult -= OnLoginResult;
        AccountServiceManager.Instance.OnServerListLoaded -= OnAccountServerListLoaded;
    }
    
    /// <summary>
    /// 打开服务器列表界面
    /// </summary>
    private void OpenServerListView(GameServerInfoJson lastSelectedServer)
    {
        // 这里根据你的项目实际情况实现界面打开逻辑
        
        // 方式1: 使用UIManager
        // UIManager.Instance.ShowView<ServerListView>();
        
        // 方式2: 发送事件让其他脚本处理
        // EventManager.Trigger("OpenServerList", lastSelectedServer);
        
        LogUtlis.Info($"[LoginManager] 打开服务器列表界面，上次选择的服务器: {lastSelectedServer?.server_name ?? "无"}");
        
        // TODO: 根据你的项目实际情况实现
    }
    
    #region 上次选择服务器的本地存储
    
    /// <summary>
    /// 保存上次选择的服务器
    /// </summary>
    /// <summary>
    /// 清理资源，取消事件订阅
    /// </summary>
    public void Cleanup()
    {
        if (AccountServiceManager.Instance != null)
        {
            AccountServiceManager.Instance.OnConnectionTest -= OnConnectionTest;
            AccountServiceManager.Instance.OnLoginResult -= OnLoginResult;
            AccountServiceManager.Instance.OnServerListLoaded -= OnAccountServerListLoaded;
        }
        isInitialized = false;
        LogUtlis.Info("[LoginManager] 清理完成");
    }
    
    public void SaveLastSelectedServer(GameServerInfoJson server)
    {
        if (server == null)
        {
            LogUtlis.Warn("[LoginManager] 尝试保存空的服务器信息");
            return;
        }
        
        PlayerPrefs.SetInt(LAST_SERVER_ID_KEY, server.server_id);
        PlayerPrefs.SetString(LAST_SERVER_NAME_KEY, server.server_name);
        PlayerPrefs.SetString(LAST_SERVER_HOST_KEY, server.host);
        PlayerPrefs.SetInt(LAST_SERVER_PORT_KEY, server.port);
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
            host = PlayerPrefs.GetString(LAST_SERVER_HOST_KEY, ""),
            port = PlayerPrefs.GetInt(LAST_SERVER_PORT_KEY, 0)
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
        PlayerPrefs.DeleteKey(LAST_SERVER_HOST_KEY);
        PlayerPrefs.DeleteKey(LAST_SERVER_PORT_KEY);
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
