using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace TowerDefense.Network
{
    /// <summary>
    /// 账号服务器通信管理器 - HTTP REST API
    /// </summary>
    public class AccountService : MonoSingleton<AccountService>
    {
        [Header("账号服务器配置")]
        public string accountServerUrl = "http://localhost:8080";
        
        private string token = "";
        private string playerId = "";
        private string username = "";
        
        #region 账号登录
        
        /// <summary>
        /// 登录账号服务器
        /// </summary>
        public void Login(string username, string password, Action<bool, string, LoginData> callback)
        {
            StartCoroutine(LoginCoroutine(username, password, callback));
        }
        
        private IEnumerator LoginCoroutine(string username, string password, Action<bool, string, LoginData> callback)
        {
            string url = $"{accountServerUrl}/api/login";
            
            // 构造请求数据
            var requestData = new
            {
                username = username,
                password = password
            };
            
            string jsonData = JsonConvert.SerializeObject(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    LogUtlis.Info($"登录响应: {responseText}");
                    
                    try
                    {
                        var response = JsonConvert.DeserializeObject<ApiResponse<LoginData>>(responseText);
                        
                        if (response.code == 0 && response.data != null)
                        {
                            // 保存登录信息
                            this.token = response.data.token;
                            this.playerId = response.data.player_id;
                            this.username = response.data.username;
                            
                            LogUtlis.Info($"登录成功: {this.username} (ID: {this.playerId})");
                            callback?.Invoke(true, "登录成功", response.data);
                        }
                        else
                        {
                            LogUtlis.Error($"登录失败: {response.message}");
                            callback?.Invoke(false, response.message, null);
                        }
                    }
                    catch (Exception e)
                    {
                        LogUtlis.Error($"解析登录响应失败: {e.Message}");
                        callback?.Invoke(false, "解析响应失败", null);
                    }
                }
                else
                {
                    string error = $"登录请求失败: {request.error}";
                    LogUtlis.Error(error);
                    callback?.Invoke(false, error, null);
                }
            }
        }
        
        #endregion
        
        #region 获取区服列表
        
        /// <summary>
        /// 获取游戏区服列表
        /// </summary>
        public void GetServerList(Action<bool, string, List<GameServerInfo>> callback)
        {
            StartCoroutine(GetServerListCoroutine(callback));
        }
        
        private IEnumerator GetServerListCoroutine(Action<bool, string, List<GameServerInfo>> callback)
        {
            if (string.IsNullOrEmpty(token))
            {
                callback?.Invoke(false, "请先登录", null);
                yield break;
            }
            
            string url = $"{accountServerUrl}/api/servers?token={token}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", token);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    LogUtlis.Info($"区服列表响应: {responseText}");
                    
                    try
                    {
                        var response = JsonConvert.DeserializeObject<ApiResponse<ServerListData>>(responseText);
                        
                        if (response.code == 0 && response.data != null)
                        {
                            LogUtlis.Info($"获取到 {response.data.servers.Count} 个区服");
                            callback?.Invoke(true, "获取成功", response.data.servers);
                        }
                        else
                        {
                            LogUtlis.Error($"获取区服列表失败: {response.message}");
                            callback?.Invoke(false, response.message, null);
                        }
                    }
                    catch (Exception e)
                    {
                        LogUtlis.Error($"解析区服列表失败: {e.Message}");
                        callback?.Invoke(false, "解析响应失败", null);
                    }
                }
                else
                {
                    string error = $"获取区服列表失败: {request.error}";
                    LogUtlis.Error(error);
                    callback?.Invoke(false, error, null);
                }
            }
        }
        
        #endregion
        
        #region 注册（可选）
        
        /// <summary>
        /// 注册新账号
        /// </summary>
        public void Register(string username, string password, Action<bool, string> callback)
        {
            StartCoroutine(RegisterCoroutine(username, password, callback));
        }
        
        private IEnumerator RegisterCoroutine(string username, string password, Action<bool, string> callback)
        {
            string url = $"{accountServerUrl}/api/register";
            
            var requestData = new
            {
                username = username,
                password = password
            };
            
            string jsonData = JsonConvert.SerializeObject(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    
                    try
                    {
                        var response = JsonConvert.DeserializeObject<ApiResponse<RegisterData>>(responseText);
                        
                        if (response.code == 0)
                        {
                            LogUtlis.Info($"注册成功");
                            callback?.Invoke(true, "注册成功");
                        }
                        else
                        {
                            LogUtlis.Error($"注册失败: {response.message}");
                            callback?.Invoke(false, response.message);
                        }
                    }
                    catch (Exception e)
                    {
                        LogUtlis.Error($"解析注册响应失败: {e.Message}");
                        callback?.Invoke(false, "解析响应失败");
                    }
                }
                else
                {
                    string error = $"注册请求失败: {request.error}";
                    LogUtlis.Error(error);
                    callback?.Invoke(false, error);
                }
            }
        }
        
        #endregion
        
        #region 属性访问
        
        public string Token => token;
        public string PlayerId => playerId;
        public string Username => username;
        public bool IsLoggedIn => !string.IsNullOrEmpty(token);
        
        /// <summary>
        /// 清除登录状态
        /// </summary>
        public void Logout()
        {
            token = "";
            playerId = "";
            username = "";
            LogUtlis.Info("已登出账号");
        }
        
        #endregion
    }
    
    #region 数据结构
    
    [Serializable]
    public class ApiResponse<T>
    {
        public int code;
        public string message;
        public T data;
    }
    
    [Serializable]
    public class LoginData
    {
        public string token;
        public string player_id;
        public string username;
        public long expire_time;
        public string message;
    }
    
    [Serializable]
    public class ServerListData
    {
        public string player_id;
        public List<GameServerInfo> servers;
    }
    
    [Serializable]
    public class GameServerInfo
    {
        public int server_id;
        public string server_name;
        public string host;
        public int port;
        public string status;       // online, maintain, full
        public int online_num;
        public int max_player;
        public bool recommend;
        public bool is_new;
        
        /// <summary>
        /// 获取WebSocket连接地址
        /// </summary>
        public string GetWebSocketUrl()
        {
            return $"ws://{host}:{port}/ws";
        }
        
        /// <summary>
        /// 是否可以连接
        /// </summary>
        public bool CanConnect()
        {
            return status == "online" && online_num < max_player;
        }
        
        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        public string GetStatusText()
        {
            return status switch
            {
                "online" => "正常",
                "maintain" => "维护中",
                "full" => "爆满",
                _ => "未知"
            };
        }
    }
    
    [Serializable]
    public class RegisterData
    {
        public string player_id;
        public string player_name;
        public string message;
    }
    
    #endregion
}
