using System;
using System.Threading;
using WebSocketSharp;
using Google.Protobuf;
using TowerDefense.Proto;
using System.Collections.Concurrent;
/// <summary>
/// 客户端队列数据包结构
/// </summary>
public class QueuePack
{
    // 重试次数 
    public int tryNum;
    public NetworkPacket packet;
}

public class ResQueuePack
{
    public Cmd cmd;
    public int code = 0;
    // 响应消息 code == 0 有效
    public ByteString responseMessage;
}

/// <summary>
/// 消息代码枚举
/// </summary>
public enum MessageCode
{
    Heartbeat = 1
}

public class WebSocketClient
{
    #region 配置常量
    // 重连配置
    private const int ReconnectInterval = 3000;     // 重连尝试间隔（毫秒）
    private const int MaxReconnectAttempts = 3;     // 最大重连次数

    // 心跳配置
    private const int HeartbeatInterval = 60000;    // 心跳发送间隔（毫秒）
    private const int HeartbeatTimeout = 10000;     // 心跳响应超时时间

    // 网络配置
    private const int SendRetryLimit = 3;           // 单条消息发送重试次数
    #endregion

    #region 内部状态
    // WebSocket核心组件
    private WebSocket _ws;
    private Thread _networkThread;
    private readonly object _stateLock = new object(); // 状态同步锁

    // 连接参数缓存
    private string _cachedUrl;                      // 服务器地址缓存
    private string _cachedToken;                    // 身份令牌缓存

    // 重连控制
    private int _reconnectAttempts;                 // 当前重连尝试次数
    private bool _isUserDisconnect;                // 是否用户主动断开

    // 心跳控制
    private DateTime _lastHeartbeatSendTime;        // 最后心跳发送时间
    private DateTime _lastMessageReceiveTime;       // 最后消息接收时间

    private SynchronizationContext _mainThreadContext;

    // 双工队列
    private readonly ConcurrentQueue<QueuePack> _sendQueue = new ConcurrentQueue<QueuePack>();
    private readonly ConcurrentQueue<ResQueuePack> _receiveQueue = new ConcurrentQueue<ResQueuePack>();
    #endregion

    #region 公共接口
    /// <summary>
    /// 初始化WebSocket连接
    /// </summary>
    /// <param name="url">websocket服务器地址</param>
    /// <param name="token">身份验证令牌</param>
    public void Connect(string url, string token)
    {
        lock (_stateLock)
        {
            // 清理旧连接
            if (_ws != null)
            {
                _ws.Close();
                _ws = null;
            }

            // 检测协议变化
            bool protocolChanged = _cachedUrl?.StartsWith("wss://") != url.StartsWith("wss://");

            // 协议变化时强制重置
            if (protocolChanged && _ws != null)
            {
                _ws.Close();
                _ws = null;
            }
            // 缓存连接参数
            _cachedUrl = url;
            _cachedToken = token;

            // 重置状态
            _reconnectAttempts = 0;
            _isUserDisconnect = false;

            // 初始化WebSocket实例
            _ws = new WebSocket(url);
            _ws.OnOpen += OnWebSocketOpen;
            _ws.OnMessage += OnWebSocketMessage;
            _ws.OnClose += OnWebSocketClose;
            _ws.OnError += OnWebSocketError;
            _ws.SetCredentials("Bearer", token, true);

            // 启动网络线程
            StartNetworkThread();

            LogUtlis.Info($"开始连接服务器，剩余重连次数: {MaxReconnectAttempts - _reconnectAttempts}");
        }
    }

    public void SetToken(string token)
    {
        lock (_stateLock)
        {
            _cachedToken = token;
        }
    }
    /// <summary>
    /// 主动断开连接
    /// </summary>
    public void Disconnect()
    {

        lock (_stateLock)
        {
            _isUserDisconnect = true;
            _reconnectAttempts = MaxReconnectAttempts;

            if (_ws != null && _ws.IsAlive)
            {
                // 使用正常关闭状态码(1000)
                _ws.Close(1000, "User initiated closure");
            }

            LogUtlis.Info("连接已安全关闭");
        }
    }
    public void StopNetWordkThread()
    {
        _networkThread?.Abort();
    }

    /// <summary>
    /// 发送协议消息
    /// </summary>
    /// <param name="cmd">协议模块指令</param>
    /// <param name="message">协议消息体</param>
    public void Send(Cmd cmd, Google.Protobuf.IMessage message)
    {
        var packet = new NetworkPacket
        {
            Cmd = (int)cmd,
            Code = 0,
            Payload = message.ToByteString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            MessageId = Guid.NewGuid().ToString()
        };
        
        var pack = new QueuePack
        {
            tryNum = SendRetryLimit,
            packet = packet
        };
        _sendQueue.Enqueue(pack);
    }
    #endregion

    #region 网络线程核心
    /// <summary>
    /// 启动网络监控线程
    /// </summary>
    private void StartNetworkThread()
    {
        if (_networkThread?.IsAlive == true) return;

        _mainThreadContext = SynchronizationContext.Current;
        _networkThread = new Thread(NetworkLoop)
        {
            IsBackground = true,
            Priority = System.Threading.ThreadPriority.AboveNormal
        };
        _networkThread.Start();
    }

    /// <summary>
    /// 网络监控主循环
    /// </summary>
    private void NetworkLoop()
    {
        try
        {
            // 建立连接
            _ws.Connect();

            // 主循环
            while (IsConnectionActive())
            {
                Thread.Sleep(10); // 防止CPU空转

                ProcessSendQueue();   // 处理发送队列
                CheckHeartbeat();    // 心跳检测
            }
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"网络线程异常: {ex.Message}");
        }
        finally
        {
            LogUtlis.Info("网络监控线程退出");
        }
    }

    /// <summary>
    /// 判断连接是否处于活动状态
    /// </summary>
    private bool IsConnectionActive()
    {
        lock (_stateLock)
        {
            return !_isUserDisconnect &&
                   _reconnectAttempts <= MaxReconnectAttempts &&
                   _ws != null;
        }
    }
    #endregion

    #region 发送队列处理
    /// <summary>
    /// 处理发送队列
    /// </summary>
    private void ProcessSendQueue()
    {
        while (_sendQueue.TryDequeue(out QueuePack pack))
        {
            if (TrySendMessage(pack))
            {
                LogUtlis.Info($"消息发送成功: {(Cmd)pack.packet.Cmd}");
            }
            else if (pack.tryNum > 0)
            {
                pack.tryNum--;
                _sendQueue.Enqueue(pack);
                LogUtlis.Warn($"消息重新入队，剩余尝试次数: {pack.tryNum}");
            }
            else
            {
                LogUtlis.Error($"消息最终发送失败: {(Cmd)pack.packet.Cmd}");
            }
        }
    }

    /// <summary>
    /// 尝试发送单条消息
    /// </summary>
    private bool TrySendMessage(QueuePack pack)
    {
        try
        {
            if (_ws?.ReadyState == WebSocketState.Open)
            {
                byte[] data = pack.packet.ToByteArray();
                _ws.Send(data);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"发送异常: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region 心跳机制
    /// <summary>
    /// 心跳检测
    /// </summary>
    private void CheckHeartbeat()
    {
        if (_ws.ReadyState != WebSocketState.Open) return;

        // 发送心跳
        if ((DateTime.Now - _lastHeartbeatSendTime).TotalMilliseconds > HeartbeatInterval)
        {
            SendHeartbeat();
            _lastHeartbeatSendTime = DateTime.Now;
        }

        // 检查超时
        if ((DateTime.Now - _lastMessageReceiveTime).TotalMilliseconds > HeartbeatInterval + HeartbeatTimeout)
        {
            LogUtlis.Warn("心跳响应超时，主动断开连接");
            _ws.Close(1006, "Heartbeat timeout");
        }
    }

    /// <summary>
    /// 发送心跳包
    /// </summary>
    private void SendHeartbeat()
    {
        var heartbeat = new HeartbeatRequest 
        { 
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        Send(Cmd.MsgHeartbeatReq, heartbeat);
    }
    #endregion

    #region WebSocket事件处理
    /// <summary>
    /// 连接建立成功回调
    /// </summary>
    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        lock (_stateLock)
        {
            _reconnectAttempts = 0; // 重置重连计数器
            _lastMessageReceiveTime = DateTime.Now;
            LogUtlis.Info("连接成功建立，握手完成");
        }
        
        // 事件分发必须在主线程执行
        _mainThreadContext?.Post(_ =>
        {
            EngineEventManager.Instance.DispatchEvent(new EngineEvent(EventID.WebSocketConnected));
        }, null);
    }

    /// <summary>
    /// 消息接收回调
    /// </summary>
    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        _lastMessageReceiveTime = DateTime.Now;

        try
        {
            var packet = NetworkPacket.Parser.ParseFrom(e.RawData);
            HandleReceivedMessage(packet);
        }
        catch (Exception ex)
        {
            LogUtlis.Error($"消息解析失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 连接关闭回调
    /// </summary>
    private void OnWebSocketClose(object sender, CloseEventArgs e)
    {
        LogUtlis.Warn($"连接关闭，状态码: {e.Code}，原因: {e.Reason}");

        // 异常断开且非用户主动操作时触发重连
        if (!_isUserDisconnect && IsAbnormalDisconnect(e.Code))
        {
            ScheduleReconnect();
        }
    }

    /// <summary>
    /// 错误处理回调
    /// </summary>
    private void OnWebSocketError(object sender, ErrorEventArgs e)
    {
        LogUtlis.Error($"网络错误: {e.Message}");
        // TODO: 需要实现 ReconnectManager 或移除此调用
        // ReconnectManager.Instance.OpenReconnectView();
    }
    #endregion

    #region 重连机制
    /// <summary>
    /// 调度重连
    /// </summary>
    private void ScheduleReconnect()
    {
        lock (_stateLock)
        {
            if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                LogUtlis.Error($"已达到最大重连次数（{MaxReconnectAttempts}），停止重连");
                return;
            }

            _reconnectAttempts++;
            int delay = CalculateReconnectDelay();

            LogUtlis.Info($"准备第 {_reconnectAttempts}/{MaxReconnectAttempts} 次重连，等待 {delay}ms...");
            Thread.Sleep(delay);

            Connect(_cachedUrl, _cachedToken);
        }
    }

    /// <summary>
    /// 计算重连延迟  
    /// </summary>
    private int CalculateReconnectDelay()
    {
        return (int)(ReconnectInterval * Math.Pow(1.5, _reconnectAttempts - 1));
    }

    /// <summary>
    /// 判断是否异常断开
    /// </summary>
    private bool IsAbnormalDisconnect(ushort code)
    {
        // 1000-1001 为正常关闭
        // 1002-1015 为协议错误/连接重置等异常情况
        return code > 1001;
    }
    #endregion

    #region 消息处理
    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private void HandleReceivedMessage(NetworkPacket packet)
    {
        // 心跳响应特殊处理
        if (packet.Cmd == (int)Cmd.MsgHeartbeatRsp)
        {
            var res = HeartbeatResponse.Parser.ParseFrom(packet.Payload);
            _mainThreadContext.Post(_ =>
            {
                TimeUtil.UpdateServerTimeNow(res.ServerTime, res.ServerTime);
            }, null);
            LogUtlis.Info($"心跳响应 - 服务器时间: {res.ServerTime}, 延迟: {res.Ping}ms");
            return;
        }

        // 普通消息入队
        var resPack = new ResQueuePack
        {
            code = packet.Code,
            cmd = (Cmd)packet.Cmd,
            responseMessage = packet.Payload
        };
        _receiveQueue.Enqueue(resPack);
    }

    /// <summary>
    /// 尝试获取响应消息
    /// </summary>
    public bool TryGetResponse(out ResQueuePack pack)
    {
        return _receiveQueue.TryDequeue(out pack);
    }
    #endregion
}



