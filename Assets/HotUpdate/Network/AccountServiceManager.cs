using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 账号服务管理器 - 负责账号注册、登录和获取服务器列表
/// </summary>
public class AccountServiceManager : GameSingleton<AccountServiceManager>
{
    private HttpClient _httpClient;
    private string _currentToken;      // 当前登录的token（内存中，不持久化）
    private string _currentUsername;

    #region
    private const string USERNAME_KEY = "account_username";
    private const string PASSWORD_KEY = "account_password";
    #endregion

    #region 属性
    public string CurrentToken => _currentToken;
    public string CurrentUsername => _currentUsername;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_currentToken);
    public bool HasLocalAccount => !string.IsNullOrEmpty(PlayerPrefs.GetString(USERNAME_KEY)) 
                                 && !string.IsNullOrEmpty(PlayerPrefs.GetString(PASSWORD_KEY));
    #endregion

    /// <summary>
    /// 初始化账号服务
    /// </summary>
    public void Init(string serverUrl)
    {
        _httpClient = new HttpClient(serverUrl);
        LogUtlis.Info($"[账号服务] 初始化: {serverUrl}");
    }

    #region 公开API

    /// <summary>
    /// 注册账号（成功后自动登录并返回服务器列表）
    /// </summary>
    public void Register(string username, string password, Action<bool, string, List<GameServerInfoJson>> callback = null)
    {
        var json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        Request("/api/register", json, HttpMethod.Post, (success, response, error) =>
        {
            if (!success)
            {
                LogUtlis.Error($"[账号服务] 注册失败: {error}");
                callback?.Invoke(false, error, null);
                return;
            }

            LogUtlis.Info($"[账号服务] 注册成功: {username}");
            
            // 注册成功后尝试提取token和服务器列表
            if (TryParseLoginResponse(response, username, out var token, out var user, out var servers))
            {
                SaveLoginState(token, user, password);
                callback?.Invoke(true, "注册成功", servers);
            }
            else
            {
                // 服务端未返回完整数据，手动登录
                Login(username, password, callback);
            }
        });
    }

    /// <summary>
    /// 登录账号（成功后自动返回服务器列表）
    /// </summary>
    public void Login(string username, string password, Action<bool, string, List<GameServerInfoJson>> callback = null)
    {
        var json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        Request("/api/login", json, HttpMethod.Post, (success, response, error) =>
        {
            if (!success)
            {
                LogUtlis.Error($"[账号服务] 登录失败: {error}");
                callback?.Invoke(false, error, null);
                return;
            }
            
            if (TryParseLoginResponse(response, username, out var token, out var user, out var servers))
            {
                SaveLoginState(token, user, password);
                LogUtlis.Info($"[账号服务] 登录成功: {username}, 服务器数量: {servers?.Count ?? 0}");
                callback?.Invoke(true, token, servers);
            }
            else
            {
                LogUtlis.Error("[账号服务] 登录失败：无法解析响应");
                callback?.Invoke(false, "无效的响应", null);
            }
        });
    }

    /// <summary>
    /// 使用本地账号自动登录
    /// </summary>
    public void LoginWithLocalAccount(Action<bool, string, List<GameServerInfoJson>> callback = null)
    {
        if (!HasLocalAccount)
        {
            LogUtlis.Warn("[账号服务] 没有本地账号");
            callback?.Invoke(false, "没有保存的账号", null);
            return;
        }
        
        var username = PlayerPrefs.GetString(USERNAME_KEY);
        var password = PlayerPrefs.GetString(PASSWORD_KEY);
        LogUtlis.Info($"[账号服务] 使用本地账号登录: {username}");
        Login(username, password, callback);
    }

    /// <summary>
    /// 获取服务器列表（无需登录）
    /// </summary>
    public void GetServerList(Action<bool, List<GameServerInfoJson>> callback)
    {
        Request("/api/servers", null, HttpMethod.Get, (success, response, error) =>
        {
            if (!success)
            {
                LogUtlis.Error($"[账号服务] 获取服务器列表失败: {error}");
                callback?.Invoke(false, null);
                return;
            }

            try
            {
                var json = JsonUtility.FromJson<ServerListResponseJson>(response);
                if (json?.code == 0 && json.data != null)
                {
                    LogUtlis.Info($"[账号服务] 获取服务器列表成功，共{json.data.servers?.Count ?? 0}个服务器");
                    callback?.Invoke(true, json.data.servers);
                }
                else
                {
                    LogUtlis.Error($"[账号服务] 服务器错误: {json?.message ?? "未知错误"}");
                    callback?.Invoke(false, null);
                }
            }
            catch (Exception ex)
            {
                LogUtlis.Error($"[账号服务] 解析失败: {ex.Message}");
                callback?.Invoke(false, null);
            }
        });
    }

    /// <summary>
    /// 登出
    /// </summary>
    public void Logout()
    {
        ClearLoginState();
        LogUtlis.Info("[账号服务] 登出成功");
    }

    #endregion

    #region 私有方法

    private enum HttpMethod { Get, Post }

    private void Request(string endpoint, string body, HttpMethod method, Action<bool, string, string> callback)
    {
        if (_httpClient == null)
        {
            LogUtlis.Error("[账号服务] 未初始化");
            callback?.Invoke(false, null, "客户端未初始化");
            return;
        }
        
        var coroutine = method == HttpMethod.Get 
            ? _httpClient.GetJson(endpoint, null, callback)
            : _httpClient.PostJson(endpoint, body, callback);
        GameManager.Instance.StartCoroutine(coroutine);
    }

    private bool TryParseLoginResponse(string response, string username, out string token, out string extractedUsername, out List<GameServerInfoJson> servers)
    {
        token = null;
        extractedUsername = username;
        servers = null;

        try
        {
            var json = JsonUtility.FromJson<LoginResponseJson>(response);
            if (json?.code == 0 && json.data != null && !string.IsNullOrEmpty(json.data.token))
            {
                token = json.data.token;
                extractedUsername = json.data.username ?? username;
                servers = json.data.servers;
                return true;
            }
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"[账号服务] 解析响应失败: {ex.Message}");
        }
        return false;
    }
    
    private void SaveLoginState(string token, string username, string password)
    {
        // token 只保存在内存中，不持久化（每次登录获取新token）
        _currentToken = token;
        _currentUsername = username;
        _httpClient.SetToken(token);
        
        // 只保存账号密码到本地，用于下次自动登录
        PlayerPrefs.SetString(USERNAME_KEY, username);
        PlayerPrefs.SetString(PASSWORD_KEY, password);
        PlayerPrefs.Save();
    }

    private void ClearLoginState()
    {
        _currentToken = null;
        _currentUsername = null;
        _httpClient?.ClearToken();
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.DeleteKey(PASSWORD_KEY);
        PlayerPrefs.Save();
    }

    #endregion
}

#region JSON数据结构

[Serializable]
public class LoginResponseJson
{
    public int code;
    public LoginData data;
    public string message;
}

[Serializable]
public class LoginData
{
    public string token;
    public string username;
    public long expire_time;
    public List<GameServerInfoJson> servers;  // 登录成功直接返回服务器列表
}

[Serializable]
public class ServerListResponseJson
{
    public int code;
    public ServerListData data;
    public string message;
}

[Serializable]
public class ServerListData
{
    public List<GameServerInfoJson> servers;
}

[Serializable]
public class GameServerInfoJson
{
    public int server_id;
    public string server_name;
    public string url;          // 完整的WebSocket连接地址
    public string status;
    public int online_num;
    public int max_player;
    public bool recommend;
    public bool is_new;
}

#endregion
