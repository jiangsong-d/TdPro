using System;
using System.Collections;
using UnityEngine;
using TowerDefense.Proto;

/// <summary>
/// 账号服务管理器
/// 基于HttpClient封装账号服务器的业务逻辑
/// </summary>
public class AccountServiceManager :GameSingleton<AccountServiceManager>
{
    private HttpClient _httpClient;
    private string _currentToken;
    private string _currentUsername;
    private string _serverUrl;

    // 事件回调
    public event Action<bool, string> OnConnectionTest;  // 连接测试回调
    public event Action<bool, string> OnLoginResult; // 登录结果回调
    public event Action<bool, string> OnRegisterResult;// 注册结果回调
    public event Action<bool, string> OnServerListLoaded;// 服务器列表加载回调
    public event Action<bool> OnLocalAccountCheck;  // 本地账号检查回调(是否有账号)

    /// <summary>
    /// 初始化账号服务
    /// </summary>
    public void Init(string serverUrl, bool autoTest = true)
    {
        _serverUrl = serverUrl;
        _httpClient = new HttpClient(serverUrl);
        LogUtlis.Info($"[账号服务] 初始化完成: {serverUrl}");
        
        // 自动测试连接
        if (autoTest)
        {
            TestConnection();
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 检查是否初始化
    /// </summary>
    private bool CheckInitialized(string operation)
    {
        if (_httpClient == null)
        {
            LogUtlis.Error($"[账号服务] {operation}失败：未初始化");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 解析登录/注册响应，提取token
    /// </summary>
    private bool TryParseTokenResponse(string response, string username, out string token, out string extractedUsername)
    {
        token = null;
        extractedUsername = username;

        try
        {
            // 尝试方式1: 纯token字符串
            if (!response.Contains("{") && !response.Contains("["))
            {
                token = response.Trim().Trim('"');
                return !string.IsNullOrEmpty(token);
            }

            // 尝试方式2: 嵌套JSON
            var json = JsonUtility.FromJson<LoginResponseJson>(response);
            if (json?.code == 0 && json.data != null && !string.IsNullOrEmpty(json.data.token))
            {
                token = json.data.token;
                extractedUsername = json.data.username ?? username;
                return true;
            }
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"[账号服务] 解析响应失败: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 处理登录成功逻辑
    /// </summary>
    private void HandleLoginSuccess(string token, string username, string password)
    {
        _currentToken = token;
        _currentUsername = username;
        _httpClient.SetToken(_currentToken);
        SaveToken(_currentToken);
        SaveAccount(username, password);
        LogUtlis.Info($"[账号服务] 登录成功: {_currentUsername}");
    }

    #endregion

    /// <summary>
    /// 测试与账号服务器的连接
    /// </summary>
    public void TestConnection()
    {
        if (!CheckInitialized("连接测试"))
        {
            OnConnectionTest?.Invoke(false, "客户端未初始化");
            return;
        }
        
        GameManager.Instance.StartCoroutine(_httpClient.GetJson(
            "/health", null,
            (success, response, error) => OnConnectionTest?.Invoke(success, success ? "连接成功" : error)
        ));
    }

    /// <summary>
    /// 账号注册
    /// </summary>
    public void Register(string username, string password)
    {
        if (!CheckInitialized("注册"))
        {
            OnRegisterResult?.Invoke(false, "客户端未初始化");
            return;
        }

        GameManager.Instance.StartCoroutine(_httpClient.PostJson(
            "/api/register",
            $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}",
            (success, response, error) =>
            {
                if (!success)
                {
                    LogUtlis.Error($"[账号服务] 注册失败: {error}");
                    OnRegisterResult?.Invoke(false, error);
                    return;
                }

                LogUtlis.Info($"[账号服务] 注册成功: {username}");
                
                // 尝试自动登录
                if (TryParseTokenResponse(response, username, out string token, out string user))
                {
                    HandleLoginSuccess(token, user, password);
                }

                OnRegisterResult?.Invoke(true, "注册成功");
            }
        ));
    }

    /// <summary>
    /// 账号登录
    /// </summary>
    public void Login(string username, string password)
    {
        if (!CheckInitialized("登录"))
        {
            OnLoginResult?.Invoke(false, "客户端未初始化");
            return;
        }

        GameManager.Instance.StartCoroutine(_httpClient.PostJson(
            "/api/login",
            $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}",
            (success, response, error) =>
            {
                if (!success)
                {
                    LogUtlis.Error($"[账号服务] 登录失败: {error}");
                    OnLoginResult?.Invoke(false, error);
                    return;
                }

                if (TryParseTokenResponse(response, username, out string token, out string user))
                {
                    HandleLoginSuccess(token, user, password);
                    OnLoginResult?.Invoke(true, token);
                }
                else
                {
                    LogUtlis.Error($"[账号服务] 登录失败：无法解析token");
                    OnLoginResult?.Invoke(false, "无效的响应");
                }
            }
        ));
    }

    /// <summary>
    /// 使用保存的Token自动登录
    /// </summary>
    public void AutoLogin()
    {
        string savedToken = LoadToken();
        if (string.IsNullOrEmpty(savedToken))
        {
            LogUtlis.Warn("[账号服务] 没有保存的Token，无法自动登录");
            OnLoginResult?.Invoke(false, "无保存的Token");
            return;
        }

        _currentToken = savedToken;
        _httpClient.SetToken(_currentToken);
        
        // 验证Token是否有效
        ValidateToken((isValid) =>
        {
            if (isValid)
            {
                LogUtlis.Info("[账号服务] Token有效，自动登录成功");
                OnLoginResult?.Invoke(true, _currentToken);
            }
            else
            {
                LogUtlis.Warn("[账号服务] Token已失效，需要重新登录");
                ClearToken();
                OnLoginResult?.Invoke(false, "Token已失效");
            }
        });
    }

    /// <summary>
    /// 验证Token是否有效
    /// </summary>
    private void ValidateToken(Action<bool> callback)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_currentToken))
        {
            callback?.Invoke(false);
            return;
        }

        // 通过请求服务器列表来验证Token（或者可以用专门的验证接口）
        GameManager.Instance.StartCoroutine(_httpClient.GetJson(
            "/api/servers",
            null,
            (success, response, error) =>
            {
                callback?.Invoke(success);
            }
        ));
    }

    /// <summary>
    /// 获取游戏服务器列表
    /// </summary>
    public void GetServerList()
    {
        if (!CheckInitialized("获取服务器列表") || string.IsNullOrEmpty(_currentToken))
        {
            OnServerListLoaded?.Invoke(false, "未登录或未初始化");
            return;
        }

        GameManager.Instance.StartCoroutine(_httpClient.GetJson(
            "/api/servers", null,
            (success, response, error) =>
            {
                if (!success)
                {
                    LogUtlis.Error($"[账号服务] 获取服务器列表失败: {error}");
                    OnServerListLoaded?.Invoke(false, error);
                    return;
                }

                try
                {
                    var json = JsonUtility.FromJson<LoginResponseJson>(response);
                    if (json?.code == 0 && json.data != null)
                    {
                        LogUtlis.Info("[账号服务] 服务器列表获取成功");
                        OnServerListLoaded?.Invoke(true, response);
                    }
                    else
                    {
                        string errorMsg = json?.message ?? "未知错误";
                        LogUtlis.Error($"[账号服务] 服务器错误: {errorMsg}");
                        OnServerListLoaded?.Invoke(false, errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    LogUtlis.Error($"[账号服务] 解析失败: {ex.Message}");
                    OnServerListLoaded?.Invoke(false, "解析失败");
                }
            }
        ));
    }

    /// <summary>
    /// 登出
    /// </summary>
    public void Logout()
    {
        ClearToken();
        ClearLocalAccount();  // 同时清除本地账号
        _currentUsername = null;
        LogUtlis.Info("[账号服务] 登出成功");
    }

    /// <summary>
    /// 连接到游戏服务器
    /// </summary>
    public void ConnectToGameServer(string gameServerUrl, Action<bool> callback)
    {
        if (string.IsNullOrEmpty(_currentToken))
        {
            LogUtlis.Error("[账号服务] 未登录，无法连接游戏服");
            callback?.Invoke(false);
            return;
        }

        // 使用Token连接游戏服务器
        NetworkManager.Instance.Init(ServerType.Game, gameServerUrl, _currentToken);
        
        LogUtlis.Info($"[账号服务] 正在连接游戏服: {gameServerUrl}");
        callback?.Invoke(true);
    }

    /// <summary>
    /// 获取当前Token
    /// </summary>
    public string GetCurrentToken()
    {
        return _currentToken;
    }

    /// <summary>
    /// 获取当前用户名
    /// </summary>
    public string GetCurrentUsername()
    {
        return _currentUsername;
    }

    /// <summary>
    /// 是否已登录
    /// </summary>
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(_currentToken);
    }

    #region 本地存储

    private const string TOKEN_KEY = "account_token";
    private const string USERNAME_KEY = "account_username";
    private const string PASSWORD_KEY = "account_password";

    private void SaveToken(string token)
    {
        PlayerPrefs.SetString(TOKEN_KEY, token);
        PlayerPrefs.Save();
        LogUtlis.Info("[账号服务] Token已保存");
    }

    private string LoadToken()
    {
        return PlayerPrefs.GetString(TOKEN_KEY, "");
    }

    /// <summary>
    /// 保存账号密码到本地
    /// </summary>
    private void SaveAccount(string username, string password)
    {
        PlayerPrefs.SetString(USERNAME_KEY, username);
        PlayerPrefs.SetString(PASSWORD_KEY, password);
        PlayerPrefs.Save();
        LogUtlis.Info($"[账号服务] 账号已保存: {username}");
    }

    /// <summary>
    /// 加载本地保存的账号
    /// </summary>
    private (string username, string password) LoadAccount()
    {
        string username = PlayerPrefs.GetString(USERNAME_KEY, "");
        string password = PlayerPrefs.GetString(PASSWORD_KEY, "");
        return (username, password);
    }

    /// <summary>
    /// 检查本地是否有保存的账号
    /// </summary>
    public bool HasLocalAccount()
    {
        var (username, password) = LoadAccount();
        return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
    }

    /// <summary>
    /// 检查本地账号并触发回调
    /// </summary>
    public void CheckLocalAccount()
    {
        var (username, password) = LoadAccount();
        bool hasAccount = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        
        if (hasAccount)
        {
            LogUtlis.Info($"[账号服务] 发现本地账号: {username}");
            OnLocalAccountCheck?.Invoke(true);
        }
        else
        {
            LogUtlis.Info("[账号服务] 没有本地账号");
            OnLocalAccountCheck?.Invoke(false);
        }
    }

    /// <summary>
    /// 使用本地保存的账号自动登录
    /// </summary>
    public void LoginWithLocalAccount()
    {
        var (username, password) = LoadAccount();
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            LogUtlis.Warn("[账号服务] 没有本地账号，无法自动登录");
            OnLoginResult?.Invoke(false, "没有保存的账号");
            return;
        }
        
        LogUtlis.Info($"[账号服务] 使用本地账号登录: {username}");
        Login(username, password);
    }

    private void ClearToken()
    {
        _currentToken = null;
        _httpClient?.ClearToken();
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.Save();
        LogUtlis.Info("[账号服务] Token已清除");
    }

    /// <summary>
    /// 清除本地保存的账号密码
    /// </summary>
    public void ClearLocalAccount()
    {
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.DeleteKey(PASSWORD_KEY);
        PlayerPrefs.Save();
        LogUtlis.Info("[账号服务] 本地账号已清除");
    }

    #endregion

    private void OnDestroy()
    {
       
    }
}

/// <summary>
/// 登录响应JSON格式
/// </summary>
[Serializable]
public class LoginResponseJson
{
    public int code;              // 0表示成功
    public LoginData data;        // 嵌套的数据对象
    public string message;        // "success"
}

/// <summary>
/// 登录数据JSON格式
/// </summary>
[Serializable]
public class LoginData
{
    public string token;
    public string player_id;
    public string username;
    public string message;
    public long expire_time;      // token过期时间
}

/// <summary>
/// 服务器信息JSON格式
/// </summary>
[Serializable]
public class GameServerInfoJson
{
    public int server_id;
    public string server_name;
    public string host;
    public int port;
    public string status;
    public int online_num;
    public int max_player;
    public bool recommend;
    public bool is_new;
}
