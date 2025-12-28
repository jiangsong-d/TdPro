using System;
using System.Collections.Generic;
using Google.Protobuf;
using TowerDefense.Proto;

/// <summary>
/// 服务器类型枚举
/// </summary>
public enum ServerType
{
    /// <summary>游戏主服务器</summary>
    Game = 0,
    /// <summary>聊天服务器</summary>
    Chat = 1,
    /// <summary>战斗服务器</summary>
    Battle = 2,
    /// <summary>账号服务器</summary>
    Account = 3
}

/// <summary>
/// 服务器连接信息
/// </summary>
public class ServerConnection
{
    public ServerType ServerType { get; set; }
    public WebSocketClient Client { get; set; }
    public Dictionary<Cmd, Action<int, Google.Protobuf.IMessage>> Handlers { get; set; }
    public Dictionary<Cmd, Type> ResponseMap { get; set; }
    public bool IsConnected { get; set; }
    public string ServerUrl { get; set; }

    public ServerConnection(ServerType serverType)
    {
        ServerType = serverType;
        Client = new WebSocketClient();
        Handlers = new Dictionary<Cmd, Action<int, Google.Protobuf.IMessage>>();
        ResponseMap = new Dictionary<Cmd, Type>();
        IsConnected = false;
    }
}

/// <summary>
/// 多连接网络管理器
/// </summary>
public class NetworkManager : GameSingleton<NetworkManager>
{
    // 所有服务器连接
    private Dictionary<ServerType, ServerConnection> _connections = new Dictionary<ServerType, ServerConnection>();
    
    // 默认服务器类型（向后兼容）
    private ServerType _defaultServer = ServerType.Game;

    /// <summary>
    /// 初始化指定服务器连接
    /// </summary>
    public void Init(ServerType serverType, string serverUrl, string token)
    {
        if (!_connections.ContainsKey(serverType))
        {
            _connections[serverType] = new ServerConnection(serverType);
        }

        var connection = _connections[serverType];
        
        // 断开旧连接
        connection.Client?.Disconnect();
        connection.Client?.StopNetWordkThread();
        connection.Client = new WebSocketClient();
        connection.ServerUrl = serverUrl;

        // 建立新连接
        connection.Client.Connect(serverUrl, token);
        connection.IsConnected = true;
        
        LogUtlis.Info($"初始化 {serverType} 服务器连接: {serverUrl}");
    }

    /// <summary>
    /// 初始化默认游戏服务器（向后兼容）
    /// </summary>
    public void Init(string serverUrl, string token)
    {
        Init(_defaultServer, serverUrl, token);
    }

    /// <summary>
    /// 设置默认服务器
    /// </summary>
    public void SetDefaultServer(ServerType serverType)
    {
        _defaultServer = serverType;
    }

    /// <summary>
    /// 获取服务器连接
    /// </summary>
    private ServerConnection GetConnection(ServerType serverType)
    {
        if (!_connections.ContainsKey(serverType))
        {
            LogUtlis.Warn($"服务器 {serverType} 未初始化，自动创建连接对象");
            _connections[serverType] = new ServerConnection(serverType);
        }
        return _connections[serverType];
    }

    /// <summary>
    /// 设置客户端令牌
    /// </summary>
    public void SetClientToken(ServerType serverType, string token)
    {
        var connection = GetConnection(serverType);
        connection.Client?.SetToken(token);
    }

    /// <summary>
    /// 设置默认服务器令牌（向后兼容）
    /// </summary>
    public void SetClientToken(string token)
    {
        SetClientToken(_defaultServer, token);
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    public void AddNetEvent(ServerType serverType, Cmd cmd, Action<int, Google.Protobuf.IMessage> handler, Type type)
    {
        var connection = GetConnection(serverType);
        
        if (!connection.Handlers.ContainsKey(cmd))
            connection.Handlers.Add(cmd, (id, pack) => handler(id, pack));
        if (!connection.ResponseMap.ContainsKey(cmd))
            connection.ResponseMap.Add(cmd, type);
    }

    /// <summary>
    /// 注册默认服务器消息处理器（向后兼容）
    /// </summary>
    public void AddNetEvent(Cmd cmd, Action<int, Google.Protobuf.IMessage> handler, Type type)
    {
        AddNetEvent(_defaultServer, cmd, handler, type);
    }

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    public void RemoveNetEvent(ServerType serverType, Cmd cmd, Action<int, Google.Protobuf.IMessage> handler)
    {
        var connection = GetConnection(serverType);
        
        if (connection.Handlers.ContainsKey(cmd))
            connection.Handlers.Remove(cmd);
        if (connection.ResponseMap.ContainsKey(cmd))
            connection.ResponseMap.Remove(cmd);
    }

    /// <summary>
    /// 移除默认服务器消息处理器（向后兼容）
    /// </summary>
    public void RemoveNetEvent(Cmd cmd, Action<int, Google.Protobuf.IMessage> handler)
    {
        RemoveNetEvent(_defaultServer, cmd, handler);
    }

    /// <summary>
    /// 获取响应类型
    /// </summary>
    private Type GetResponseType(ServerType serverType, Cmd cmd)
    {
        var connection = GetConnection(serverType);
        
        Type type;
        if (connection.ResponseMap.TryGetValue(cmd, out type))
        {
            return type;
        }
        return null;
    }

    /// <summary>
    /// 发送协议消息到指定服务器
    /// </summary>
    public void SendMsg(ServerType serverType, Cmd cmd, Google.Protobuf.IMessage message)
    {
        var connection = GetConnection(serverType);
        
        if (!connection.IsConnected || connection.Client == null)
        {
            LogUtlis.Error($"服务器 {serverType} 未连接，无法发送消息");
            return;
        }

        connection.Client.Send(cmd, message);
        LogUtlis.NetSendLog($"[{serverType}] 发送:{cmd}, Msg: {message}");
    }

    /// <summary>
    /// 发送消息到默认服务器（向后兼容）
    /// </summary>
    public void SendMsg(Cmd cmd, Google.Protobuf.IMessage message)
    {
        SendMsg(_defaultServer, cmd, message);
    }

    /// <summary>
    /// 处理所有服务器的接收消息
    /// </summary>
    public void NetHandleRecovers()
    {
        foreach (var kvp in _connections)
        {
            var serverType = kvp.Key;
            var connection = kvp.Value;
            
            if (connection.Client == null) continue;

            // 处理该服务器的接收队列
            while (connection.Client.TryGetResponse(out var res))
            {
                if (connection.Handlers.TryGetValue(res.cmd, out var handler))
                {
                    var type = GetResponseType(serverType, res.cmd);
                    if (type != null && res.responseMessage != null)
                    {
                        try
                        {
                            var message = Activator.CreateInstance(type) as Google.Protobuf.IMessage;
                            var msg = message.Descriptor.Parser.ParseFrom(res.responseMessage);
                            LogUtlis.NetReceiveLog($"[{serverType}] 接收:{res.cmd}, Msg: {msg}, 错误码: {res.code}");
                            
                            if (res.code != 0)
                            {
                                // GameUnitls.OpenErrorTipView(res.code, null);
                            }
                            
                            handler?.Invoke(res.code, msg);
                        }
                        catch (Exception ex)
                        {
                            LogUtlis.Error($"[{serverType}] 处理消息失败: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 断开指定服务器连接
    /// </summary>
    public void Disconnect(ServerType serverType)
    {
        if (_connections.ContainsKey(serverType))
        {
            var connection = _connections[serverType];
            connection.Client?.Disconnect();
            connection.Client?.StopNetWordkThread();
            connection.IsConnected = false;
            LogUtlis.Info($"断开 {serverType} 服务器连接");
        }
    }

    /// <summary>
    /// 断开所有服务器连接
    /// </summary>
    public void DisconnectAll()
    {
        foreach (var kvp in _connections)
        {
            kvp.Value.Client?.Disconnect();
            kvp.Value.Client?.StopNetWordkThread();
            kvp.Value.IsConnected = false;
        }
        LogUtlis.Info("断开所有服务器连接");
    }

    /// <summary>
    /// 检查服务器是否已连接
    /// </summary>
    public bool IsConnected(ServerType serverType)
    {
        if (_connections.ContainsKey(serverType))
        {
            return _connections[serverType].IsConnected;
        }
        return false;
    }

    /// <summary>
    /// 获取所有已连接的服务器
    /// </summary>
    public List<ServerType> GetConnectedServers()
    {
        var result = new List<ServerType>();
        foreach (var kvp in _connections)
        {
            if (kvp.Value.IsConnected)
            {
                result.Add(kvp.Key);
            }
        }
        return result;
    }

    /// <summary>
    /// 应用退出时清理
    /// </summary>
    public void OnApplicationQuit()
    {
        DisconnectAll();
    }
}