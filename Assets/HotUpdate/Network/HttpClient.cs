using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Google.Protobuf;
using TowerDefense.Proto;

/// <summary>
/// HTTP账号服务客户端
/// 用于与账号服务器进行HTTP通信（注册、登录、验证等）
/// </summary>
public class HttpClient
{
    private string _baseUrl;
    private string _token;
    private Dictionary<string, string> _headers = new Dictionary<string, string>();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseUrl">账号服务器基础URL，如：http://127.0.0.1:8080</param>
    public HttpClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _headers["Content-Type"] = "application/x-protobuf";
    }

    /// <summary>
    /// 设置认证Token
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
        if (!string.IsNullOrEmpty(token))
        {
            _headers["Authorization"] = $"Bearer {token}";
        }
        else
        {
            _headers.Remove("Authorization");
        }
    }

    /// <summary>
    /// 发送POST请求（使用Protobuf）
    /// </summary>
    public IEnumerator PostProto<TRequest, TResponse>(string path, TRequest request, Action<bool, TResponse, string> callback)
        where TRequest : Google.Protobuf.IMessage
        where TResponse : Google.Protobuf.IMessage, new()
    {
        string url = $"{_baseUrl}{path}";
        
        // 序列化请求消息
        byte[] bodyData = request.ToByteArray();
        
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyData);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置请求头
            foreach (var header in _headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            LogUtlis.Info($"[HTTP账号服] POST {path}, 请求: {request}");
            
            // 发送请求
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // 解析响应
                    byte[] responseData = webRequest.downloadHandler.data;
                    TResponse response = new TResponse();
                    response = (TResponse)response.Descriptor.Parser.ParseFrom(responseData);
                    
                    LogUtlis.Info($"[HTTP账号服] 响应成功: {response}");
                    callback?.Invoke(true, response, null);
                }
                catch (Exception ex)
                {
                    string error = $"解析响应失败: {ex.Message}";
                    LogUtlis.Error($"[HTTP账号服] {error}");
                    callback?.Invoke(false, default(TResponse), error);
                }
            }
            else
            {
                string error = $"请求失败: {webRequest.error}, 状态码: {webRequest.responseCode}";
                LogUtlis.Error($"[HTTP账号服] {error}");
                callback?.Invoke(false, default(TResponse), error);
            }
        }
    }

    /// <summary>
    /// 发送GET请求（使用Protobuf响应）
    /// </summary>
    public IEnumerator GetProto<TResponse>(string path, Dictionary<string, string> queryParams, Action<bool, TResponse, string> callback)
        where TResponse : Google.Protobuf.IMessage, new()
    {
        // 构建URL
        string url = $"{_baseUrl}{path}";
        if (queryParams != null && queryParams.Count > 0)
        {
            url += "?";
            foreach (var param in queryParams)
            {
                url += $"{UnityWebRequest.EscapeURL(param.Key)}={UnityWebRequest.EscapeURL(param.Value)}&";
            }
            url = url.TrimEnd('&');
        }
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置请求头
            foreach (var header in _headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            LogUtlis.Info($"[HTTP账号服] GET {url}");
            
            // 发送请求
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // 解析响应
                    byte[] responseData = webRequest.downloadHandler.data;
                    TResponse response = new TResponse();
                    response = (TResponse)response.Descriptor.Parser.ParseFrom(responseData);
                    
                    LogUtlis.Info($"[HTTP账号服] 响应成功: {response}");
                    callback?.Invoke(true, response, null);
                }
                catch (Exception ex)
                {
                    string error = $"解析响应失败: {ex.Message}";
                    LogUtlis.Error($"[HTTP账号服] {error}");
                    callback?.Invoke(false, default(TResponse), error);
                }
            }
            else
            {
                string error = $"请求失败: {webRequest.error}, 状态码: {webRequest.responseCode}";
                LogUtlis.Error($"[HTTP账号服] {error}");
                callback?.Invoke(false, default(TResponse), error);
            }
        }
    }

    /// <summary>
    /// 发送POST请求（使用JSON）
    /// </summary>
    public IEnumerator PostJson(string path, string jsonData, Action<bool, string, string> callback)
    {
        string url = $"{_baseUrl}{path}";
        
        byte[] bodyData = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyData);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // JSON请求头
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            
            LogUtlis.Info($"[HTTP账号服] POST JSON {path}, 数据: {jsonData}");
            
            // 发送请求
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                LogUtlis.Info($"[HTTP账号服] JSON响应成功: {response}");
                callback?.Invoke(true, response, null);
            }
            else
            {
                string error = $"请求失败: {webRequest.error}, 状态码: {webRequest.responseCode}";
                LogUtlis.Error($"[HTTP账号服] {error}");
                callback?.Invoke(false, null, error);
            }
        }
    }

    /// <summary>
    /// 发送GET请求（JSON响应）
    /// </summary>
    public IEnumerator GetJson(string path, Dictionary<string, string> queryParams, Action<bool, string, string> callback)
    {
        // 构建URL
        string url = $"{_baseUrl}{path}";
        if (queryParams != null && queryParams.Count > 0)
        {
            url += "?";
            foreach (var param in queryParams)
            {
                url += $"{UnityWebRequest.EscapeURL(param.Key)}={UnityWebRequest.EscapeURL(param.Value)}&";
            }
            url = url.TrimEnd('&');
        }
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(_token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            
            LogUtlis.Info($"[HTTP账号服] GET JSON {url}");
            
            // 发送请求
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                LogUtlis.Info($"[HTTP账号服] JSON响应成功: {response}");
                callback?.Invoke(true, response, null);
            }
            else
            {
                string error = $"请求失败: {webRequest.error}, 状态码: {webRequest.responseCode}";
                LogUtlis.Error($"[HTTP账号服] {error}");
                callback?.Invoke(false, null, error);
            }
        }
    }

    /// <summary>
    /// 清除Token
    /// </summary>
    public void ClearToken()
    {
        SetToken(null);
    }
}
