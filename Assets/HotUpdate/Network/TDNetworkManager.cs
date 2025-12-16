using UnityEngine;
using System;
using System.Collections.Generic;
using NativeWebSocket;
using TowerDefense.Proto;
using Google.Protobuf;

/// <summary>
/// 塔防游戏网络管理器 (Protobuf版本)
/// 负责与服务器的 WebSocket + Protobuf 通信
/// </summary>
public class TDNetworkManager : MonoSingleton<TDNetworkManager>
{
    private WebSocket websocket;
    private string serverUrl = "ws://localhost:8080/ws";
    
    // 消息处理回调
    private Dictionary<MessageType, Action<IMessage>> messageHandlers;
    
    // 连接状态
    public bool IsConnected { get; private set; }
    
    // 当前玩家信息
    public string PlayerID { get; private set; }
    public string PlayerName { get; private set; }
    public string CurrentRoomID { get; private set; }
    
    // 游戏状态
    public int Gold { get; private set; }
    public int Life { get; private set; }
    public int WaveNum { get; private set; }
    
    protected override void Init()
    {
        RegisterMessageHandlers();
    }
    
    /// <summary>
    /// 连接服务器
    /// </summary>
    public async void Connect(string url = null)
    {
        if (!string.IsNullOrEmpty(url))
            serverUrl = url;
            
        websocket = new WebSocket(serverUrl);
        
        websocket.OnOpen += () =>
        {
            IsConnected = true;
            LogUtlis.Info("✅ 服务器连接成功");
            OnConnected();
        };
        
        websocket.OnMessage += (bytes) =>
        {
            HandleMessage(bytes);
        };
        
        websocket.OnError += (e) =>
        {
            LogUtlis.Error($"❌ WebSocket 错误: {e}");
        };
        
        websocket.OnClose += (e) =>
        {
            IsConnected = false;
            LogUtlis.Warn($"⚠️ 连接关闭: {e}");
            OnDisconnected();
        };
        
        try
        {
            await websocket.Connect();
        }
        catch (Exception e)
        {
            LogUtlis.Error($"连接失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public async void Disconnect()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }
    
    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        #endif
    }
    
    private void OnDestroy()
    {
        Disconnect();
    }
    
    /// <summary>
    /// 注册消息处理器
    /// </summary>
    private void RegisterMessageHandlers()
    {
        messageHandlers = new Dictionary<MessageType, Action<IMessage>>
        {
            { MessageType.MsgHeartbeat, msg => OnHeartbeatResponse((HeartbeatResponse)msg) },
            { MessageType.MsgLogin, msg => OnLoginResponse((LoginResponse)msg) },
            { MessageType.MsgCreateRoom, msg => OnCreateRoomResponse((CreateRoomResponse)msg) },
            { MessageType.MsgJoinRoom, msg => OnJoinRoomResponse((JoinRoomResponse)msg) },
            { MessageType.MsgRoomInfo, msg => OnRoomInfo((RoomInfoBroadcast)msg) },
            { MessageType.MsgStartGame, msg => OnStartGame((StartGameResponse)msg) },
            { MessageType.MsgPlaceTower, msg => OnPlaceTowerResponse((PlaceTowerResponse)msg) },
            { MessageType.MsgUpgradeTower, msg => OnUpgradeTowerResponse((UpgradeTowerResponse)msg) },
            { MessageType.MsgSellTower, msg => OnSellTowerResponse((SellTowerResponse)msg) },
            { MessageType.MsgWaveStart, msg => OnWaveStart((WaveStartBroadcast)msg) },
            { MessageType.MsgWaveComplete, msg => OnWaveComplete((WaveCompleteBroadcast)msg) },
            { MessageType.MsgGameOver, msg => OnGameOver((GameOverBroadcast)msg) },
            { MessageType.MsgSyncState, msg => OnSyncState((SyncStateBroadcast)msg) },
            { MessageType.MsgSyncDamage, msg => OnSyncDamage((SyncDamageBroadcast)msg) },
            { MessageType.MsgError, msg => OnError((ErrorResponse)msg) }
        };
    }
    
    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private void HandleMessage(byte[] data)
    {
        try
        {
            // 解析外层消息包装
            GameMessage gameMsg = GameMessage.Parser.ParseFrom(data);
            
            // 根据类型解析具体消息
            IMessage message = ParseMessageByType(gameMsg.Type, gameMsg.Payload);
            
            if (message != null && messageHandlers.ContainsKey(gameMsg.Type))
            {
                messageHandlers[gameMsg.Type]?.Invoke(message);
            }
            else
            {
                LogUtlis.Warn($"未处理的消息类型: {gameMsg.Type}");
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"消息解析失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 根据类型解析消息
    /// </summary>
    private IMessage ParseMessageByType(MessageType type, ByteString payload)
    {
        try
        {
            switch (type)
            {
                case MessageType.MsgHeartbeat:
                    return HeartbeatResponse.Parser.ParseFrom(payload);
                case MessageType.MsgLogin:
                    return LoginResponse.Parser.ParseFrom(payload);
                case MessageType.MsgCreateRoom:
                    return CreateRoomResponse.Parser.ParseFrom(payload);
                case MessageType.MsgJoinRoom:
                    return JoinRoomResponse.Parser.ParseFrom(payload);
                case MessageType.MsgRoomInfo:
                    return RoomInfoBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgStartGame:
                    return StartGameResponse.Parser.ParseFrom(payload);
                case MessageType.MsgPlaceTower:
                    return PlaceTowerResponse.Parser.ParseFrom(payload);
                case MessageType.MsgUpgradeTower:
                    return UpgradeTowerResponse.Parser.ParseFrom(payload);
                case MessageType.MsgSellTower:
                    return SellTowerResponse.Parser.ParseFrom(payload);
                case MessageType.MsgWaveStart:
                    return WaveStartBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgWaveComplete:
                    return WaveCompleteBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgGameOver:
                    return GameOverBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgSyncState:
                    return SyncStateBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgSyncDamage:
                    return SyncDamageBroadcast.Parser.ParseFrom(payload);
                case MessageType.MsgError:
                    return ErrorResponse.Parser.ParseFrom(payload);
                default:
                    return null;
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"解析消息失败 [{type}]: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    private async void SendMessage(MessageType msgType, IMessage message)
    {
        if (!IsConnected)
        {
            LogUtlis.Error("未连接到服务器");
            return;
        }
        
        try
        {
            // 包装消息
            GameMessage gameMsg = new GameMessage
            {
                Type = msgType,
                Payload = message.ToByteString()
            };
            
            // 序列化
            byte[] data = gameMsg.ToByteArray();
            
            // 发送
            await websocket.Send(data);
        }
        catch (Exception e)
        {
            LogUtlis.Error($"发送消息失败: {e.Message}");
        }
    }
    
    // ==================== API 方法 ====================
    
    /// <summary>
    /// 登录
    /// </summary>
    public void Login(string playerId, string playerName)
    {
        PlayerID = playerId;
        PlayerName = playerName;
        
        LoginRequest req = new LoginRequest
        {
            PlayerId = playerId,
            PlayerName = playerName,
            Token = "test_token"
        };
        
        SendMessage(MessageType.MsgLogin, req);
    }
    
    /// <summary>
    /// 创建房间
    /// </summary>
    public void CreateRoom(string roomName, int maxPlayer, int levelId)
    {
        CreateRoomRequest req = new CreateRoomRequest
        {
            RoomName = roomName,
            MaxPlayer = maxPlayer,
            LevelId = levelId
        };
        
        SendMessage(MessageType.MsgCreateRoom, req);
    }
    
    /// <summary>
    /// 加入房间
    /// </summary>
    public void JoinRoom(string roomId)
    {
        JoinRoomRequest req = new JoinRoomRequest
        {
            RoomId = roomId
        };
        
        SendMessage(MessageType.MsgJoinRoom, req);
    }
    
    /// <summary>
    /// 离开房间
    /// </summary>
    public void LeaveRoom()
    {
        // 发送空消息即可
        SendMessage(MessageType.MsgLeaveRoom, new LoginRequest());
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        StartGameRequest req = new StartGameRequest
        {
            RoomId = CurrentRoomID
        };
        
        SendMessage(MessageType.MsgStartGame, req);
    }
    
    /// <summary>
    /// 放置防御塔
    /// </summary>
    public void PlaceTower(int towerType, UnityEngine.Vector3 position)
    {
        PlaceTowerRequest req = new PlaceTowerRequest
        {
            TowerType = towerType,
            Position = new TowerDefense.Proto.Vector3
            {
                X = position.x,
                Y = position.y,
                Z = position.z
            }
        };
        
        SendMessage(MessageType.MsgPlaceTower, req);
    }
    
    /// <summary>
    /// 升级防御塔
    /// </summary>
    public void UpgradeTower(string towerId)
    {
        UpgradeTowerRequest req = new UpgradeTowerRequest
        {
            TowerId = towerId
        };
        
        SendMessage(MessageType.MsgUpgradeTower, req);
    }
    
    /// <summary>
    /// 出售防御塔
    /// </summary>
    public void SellTower(string towerId)
    {
        SellTowerRequest req = new SellTowerRequest
        {
            TowerId = towerId
        };
        
        SendMessage(MessageType.MsgSellTower, req);
    }
    
    /// <summary>
    /// 发送心跳
    /// </summary>
    public void SendHeartbeat()
    {
        HeartbeatRequest req = new HeartbeatRequest
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        SendMessage(MessageType.MsgHeartbeat, req);
    }
    
    // ==================== 消息处理回调 ====================
    
    private void OnConnected()
    {
        // 自动登录
        string playerId = SystemInfo.deviceUniqueIdentifier;
        string playerName = "玩家" + UnityEngine.Random.Range(1000, 9999);
        Login(playerId, playerName);
        
        // 启动心跳
        InvokeRepeating(nameof(SendHeartbeat), 30f, 30f);
    }
    
    private void OnDisconnected()
    {
        CancelInvoke(nameof(SendHeartbeat));
        EngineEventManager.Instance.SendEvent(EventID.NETWORK_DISCONNECTED);
    }
    
    private void OnHeartbeatResponse(HeartbeatResponse msg)
    {
        // 心跳响应，更新延迟等
    }
    
    private void OnLoginResponse(LoginResponse msg)
    {
        if (msg.Success)
        {
            LogUtlis.Info($"✅ 登录成功: {msg.PlayerId}");
            EngineEventManager.Instance.SendEvent(EventID.LOGIN_SUCCESS);
        }
        else
        {
            LogUtlis.Error($"❌ 登录失败: {msg.Message}");
        }
    }
    
    private void OnCreateRoomResponse(CreateRoomResponse msg)
    {
        if (msg.Success)
        {
            CurrentRoomID = msg.RoomId;
            LogUtlis.Info($"✅ 房间创建成功: {msg.RoomId}");
            
            // 发送事件，携带房间ID
            EngineEvent evt = new EngineEvent(EventID.ROOM_CREATED);
            evt.AddParam("room_id", msg.RoomId);
            EngineEventManager.Instance.SendEvent(evt);
        }
        else
        {
            LogUtlis.Error($"❌ 创建房间失败: {msg.Message}");
        }
    }
    
    private void OnJoinRoomResponse(JoinRoomResponse msg)
    {
        if (msg.Success)
        {
            CurrentRoomID = msg.RoomId;
            LogUtlis.Info($"✅ 加入房间成功: {msg.RoomId}, 玩家数: {msg.Players.Count}");
            
            EngineEvent evt = new EngineEvent(EventID.ROOM_JOINED);
            evt.AddParam("room_id", msg.RoomId);
            evt.AddParam("players", msg.Players);
            EngineEventManager.Instance.SendEvent(evt);
        }
        else
        {
            LogUtlis.Error($"❌ 加入房间失败: {msg.Message}");
        }
    }
    
    private void OnRoomInfo(RoomInfoBroadcast msg)
    {
        LogUtlis.Info($"📢 房间信息更新，玩家数: {msg.Players.Count}");
        
        EngineEvent evt = new EngineEvent(EventID.ROOM_INFO_UPDATED);
        evt.AddParam("players", msg.Players);
        evt.AddParam("status", msg.Status);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnStartGame(StartGameResponse msg)
    {
        if (msg.Success)
        {
            Gold = msg.GameData.Gold;
            Life = msg.GameData.Life;
            
            LogUtlis.Info($"🎮 游戏开始！金币: {Gold}, 生命: {Life}");
            
            EngineEvent evt = new EngineEvent(EventID.GAME_START);
            evt.AddParam("game_data", msg.GameData);
            EngineEventManager.Instance.SendEvent(evt);
            
            // 切换到战斗场景
            LoadingManager.Instance.SwitchScene(LoadSceneType.Battle);
        }
    }
    
    private void OnPlaceTowerResponse(PlaceTowerResponse msg)
    {
        if (msg.Success)
        {
            Gold = msg.Gold;
            LogUtlis.Info($"✅ 防御塔放置成功，ID: {msg.TowerId}, 剩余金币: {Gold}");
            
            EngineEvent evt = new EngineEvent(EventID.TOWER_PLACED);
            evt.AddParam("tower_id", msg.TowerId);
            evt.AddParam("gold", Gold);
            EngineEventManager.Instance.SendEvent(evt);
        }
        else
        {
            LogUtlis.Warn($"⚠️ 放置失败: {msg.Message}");
        }
    }
    
    private void OnUpgradeTowerResponse(UpgradeTowerResponse msg)
    {
        if (msg.Success)
        {
            Gold = msg.Gold;
            LogUtlis.Info($"⬆️ 防御塔升级成功，等级: {msg.Level}, 剩余金币: {Gold}");
            
            EngineEvent evt = new EngineEvent(EventID.TOWER_UPGRADED);
            evt.AddParam("tower_id", msg.TowerId);
            evt.AddParam("level", msg.Level);
            evt.AddParam("gold", Gold);
            EngineEventManager.Instance.SendEvent(evt);
        }
    }
    
    private void OnSellTowerResponse(SellTowerResponse msg)
    {
        if (msg.Success)
        {
            Gold = msg.Gold;
            LogUtlis.Info($"💰 防御塔出售成功，获得金币，总金币: {Gold}");
            
            EngineEvent evt = new EngineEvent(EventID.TOWER_SOLD);
            evt.AddParam("tower_id", msg.TowerId);
            evt.AddParam("gold", Gold);
            EngineEventManager.Instance.SendEvent(evt);
        }
    }
    
    private void OnWaveStart(WaveStartBroadcast msg)
    {
        WaveNum = msg.WaveNum;
        LogUtlis.Info($"⚔️ 第 {msg.WaveNum} 波开始！");
        
        EngineEvent evt = new EngineEvent(EventID.WAVE_START);
        evt.AddParam("wave_num", msg.WaveNum);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnWaveComplete(WaveCompleteBroadcast msg)
    {
        LogUtlis.Info($"✅ 第 {msg.WaveNum} 波完成！奖励: {msg.Reward}");
        
        EngineEvent evt = new EngineEvent(EventID.WAVE_COMPLETE);
        evt.AddParam("wave_num", msg.WaveNum);
        evt.AddParam("reward", msg.Reward);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnGameOver(GameOverBroadcast msg)
    {
        string result = msg.IsVictory ? "🎉 胜利" : "💀 失败";
        LogUtlis.Info($"{result}！得分: {msg.Score}, 击杀: {msg.KillCount}");
        
        EngineEvent evt = new EngineEvent(EventID.GAME_OVER);
        evt.AddParam("is_victory", msg.IsVictory);
        evt.AddParam("score", msg.Score);
        evt.AddParam("kill_count", msg.KillCount);
        evt.AddParam("total_waves", msg.TotalWaves);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnSyncState(SyncStateBroadcast msg)
    {
        // 同步游戏状态
        Gold = msg.Gold;
        Life = msg.Life;
        WaveNum = msg.WaveNum;
        
        // 发送事件，让战斗管理器处理
        EngineEvent evt = new EngineEvent(EventID.STATE_SYNCED);
        evt.AddParam("enemies", msg.Enemies);
        evt.AddParam("towers", msg.Towers);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnSyncDamage(SyncDamageBroadcast msg)
    {
        // 播放伤害特效
        string critText = msg.IsCrit ? " [暴击]" : "";
        LogUtlis.Info($"💥 伤害: {msg.Damage}{critText}");
        
        EngineEvent evt = new EngineEvent(EventID.DAMAGE_DEALT);
        evt.AddParam("tower_id", msg.TowerId);
        evt.AddParam("enemy_id", msg.EnemyId);
        evt.AddParam("damage", msg.Damage);
        evt.AddParam("is_crit", msg.IsCrit);
        evt.AddParam("is_kill", msg.IsKill);
        EngineEventManager.Instance.SendEvent(evt);
    }
    
    private void OnError(ErrorResponse msg)
    {
        LogUtlis.Error($"❌ 服务器错误 [{msg.Code}]: {msg.Message}");
        
        // 显示错误提示
        // TODO: 调用UI提示框
    }
}
