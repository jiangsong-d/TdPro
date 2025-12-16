# Unity 塔防游戏 Protobuf 网络集成指南

## 📦 依赖安装

### 1. 安装 NativeWebSocket

在 Unity Package Manager 中添加：
```
https://github.com/endel/NativeWebSocket.git#upm
```

### 2. 安装 Google.Protobuf

**方法A: UPM方式（推荐）**
```json
在 Packages/manifest.json 中添加:
{
  "dependencies": {
    "com.google.protobuf": "3.21.12"
  }
}
```

**方法B: NuGet包方式**
1. 下载 `Google.Protobuf.dll` from NuGet
2. 放到 `Assets/Plugins/` 目录

### 3. 生成 Protobuf 代码

**Windows:**
```bash
cd E:\tafang\ConfigTool\Proto
gen_csharp.bat
```

**Linux/Mac:**
```bash
cd E:/tafang/ConfigTool/Proto
chmod +x gen_csharp.sh
./gen_csharp.sh
```

生成的代码会输出到: `Assets/HotUpdate/Network/Proto/TowerDefense.cs`

## 🚀 快速开始

### 1. 在登录场景初始化网络

```csharp
public class LoginScene : BaseScene
{
    public override void OnCreate()
    {
        // 连接服务器
        TDNetworkManager.Instance.Connect("ws://localhost:8080/ws");
    }
}
```

### 2. 创建房间

```csharp
public void OnCreateRoomClick()
{
    TDNetworkManager.Instance.CreateRoom("我的房间", 4, 1);
}
```

### 3. 监听事件

```csharp
private void Start()
{
    // 监听游戏开始
    EngineEventManager.Instance.AddEventListener(
        EventID.GAME_START, 
        OnGameStart
    );
}

private void OnGameStart(EngineEvent evt)
{
    var gameData = evt.GetParam<GameInitData>("game_data");
    LogUtlis.Info($"游戏开始！金币: {gameData.Gold}");
}
```

### 4. 放置防御塔

```csharp
public void PlaceTower(Vector3 position)
{
    int towerType = 1; // 箭塔
    TDNetworkManager.Instance.PlaceTower(towerType, position);
}
```

## 📋 完整示例

查看 `TDNetworkExample.cs` 获取完整的使用示例。

## 🎯 API 参考

### 连接管理

```csharp
// 连接服务器
TDNetworkManager.Instance.Connect("ws://localhost:8080/ws");

// 断开连接
TDNetworkManager.Instance.Disconnect();

// 检查连接状态
bool isConnected = TDNetworkManager.Instance.IsConnected;
```

### 房间操作

```csharp
// 创建房间
TDNetworkManager.Instance.CreateRoom("房间名", 4, 1);

// 加入房间
TDNetworkManager.Instance.JoinRoom("room_id_123");

// 离开房间
TDNetworkManager.Instance.LeaveRoom();

// 开始游戏
TDNetworkManager.Instance.StartGame();
```

### 游戏操作

```csharp
// 放置防御塔
TDNetworkManager.Instance.PlaceTower(towerType, position);

// 升级防御塔
TDNetworkManager.Instance.UpgradeTower("tower_id");

// 出售防御塔
TDNetworkManager.Instance.SellTower("tower_id");
```

### 获取状态

```csharp
// 当前玩家信息
string playerId = TDNetworkManager.Instance.PlayerID;
string playerName = TDNetworkManager.Instance.PlayerName;

// 游戏状态
int gold = TDNetworkManager.Instance.Gold;
int life = TDNetworkManager.Instance.Life;
int waveNum = TDNetworkManager.Instance.WaveNum;
```

## 📡 事件系统

所有网络事件都通过 `EngineEventManager` 分发：

| 事件ID | 说明 | 参数 |
|--------|------|------|
| NETWORK_CONNECTED | 连接成功 | - |
| NETWORK_DISCONNECTED | 连接断开 | - |
| LOGIN_SUCCESS | 登录成功 | - |
| ROOM_CREATED | 房间创建成功 | room_id |
| ROOM_JOINED | 加入房间成功 | room_id, players |
| ROOM_INFO_UPDATED | 房间信息更新 | players, status |
| GAME_START | 游戏开始 | game_data |
| WAVE_START | 波次开始 | wave_num |
| WAVE_COMPLETE | 波次完成 | wave_num, reward |
| GAME_OVER | 游戏结束 | is_victory, score |
| TOWER_PLACED | 塔放置成功 | tower_id, gold |
| TOWER_UPGRADED | 塔升级成功 | tower_id, level, gold |
| TOWER_SOLD | 塔出售成功 | tower_id, gold |
| STATE_SYNCED | 状态同步 | enemies, towers |
| DAMAGE_DEALT | 伤害事件 | tower_id, enemy_id, damage |

## 🎨 UI集成示例

### 房间UI

```csharp
public class RoomUI : BaseUIView
{
    private void OnEnable()
    {
        EngineEventManager.Instance.AddEventListener(
            EventID.ROOM_INFO_UPDATED, 
            OnRoomInfoUpdated
        );
    }
    
    private void OnRoomInfoUpdated(EngineEvent evt)
    {
        var players = evt.GetParam<List<PlayerInfo>>("players");
        UpdatePlayerList(players);
    }
    
    public void OnStartButtonClick()
    {
        TDNetworkManager.Instance.StartGame();
    }
}
```

### 战斗UI

```csharp
public class BattleUI : BaseUIView
{
    public Text goldText;
    public Text lifeText;
    public Text waveText;
    
    private void OnEnable()
    {
        EngineEventManager.Instance.AddEventListener(
            EventID.STATE_SYNCED, 
            UpdateUI
        );
    }
    
    private void UpdateUI(EngineEvent evt)
    {
        goldText.text = $"金币: {TDNetworkManager.Instance.Gold}";
        lifeText.text = $"生命: {TDNetworkManager.Instance.Life}";
        waveText.text = $"波次: {TDNetworkManager.Instance.WaveNum}";
    }
}
```

## 🔧 配置

### 修改服务器地址

```csharp
// 在代码中
TDNetworkManager.Instance.Connect("ws://your-server.com:8080/ws");

// 或者创建配置文件
public class GameConfig
{
    public static string ServerUrl = "ws://localhost:8080/ws";
}
```

### 心跳间隔

心跳自动每30秒发送一次，无需手动调用。

## 🐛 调试技巧

### 1. 查看网络日志

所有网络消息都会通过 `LogUtlis` 输出，查看 Unity Console。

### 2. 测试连接

```csharp
private void Start()
{
    // 测试连接
    StartCoroutine(TestConnection());
}

private IEnumerator TestConnection()
{
    TDNetworkManager.Instance.Connect();
    
    yield return new WaitForSeconds(2f);
    
    if (TDNetworkManager.Instance.IsConnected)
    {
        Debug.Log("✅ 连接成功");
    }
    else
    {
        Debug.LogError("❌ 连接失败");
    }
}
```

### 3. Protobuf 调试

```csharp
// 打印消息内容
private void OnLoginResponse(LoginResponse msg)
{
    Debug.Log($"登录响应: {msg}");
}
```

## ⚠️ 注意事项

1. **WebGL平台**: WebSocket在WebGL上行为不同，需要使用 `#if !UNITY_WEBGL`

2. **线程安全**: 所有Unity API调用必须在主线程，网络回调已处理

3. **内存管理**: Protobuf消息会自动GC，无需手动释放

4. **热更新**: 网络管理器放在HotUpdate中，支持热更新

5. **断线重连**: 建议实现自动重连机制（当前未实现）

## 🚀 性能优化

### 1. 消息池

对于高频消息，可以使用对象池：

```csharp
private Queue<Vector3> positionPool = new Queue<Vector3>();

private Vector3 GetPosition()
{
    if (positionPool.Count > 0)
        return positionPool.Dequeue();
    return new Vector3();
}
```

### 2. 批量更新

状态同步在Update中批量处理，避免逐个更新。

### 3. 插值平滑

对于位置同步，使用插值：

```csharp
private void UpdateEnemyPosition(EnemyState state)
{
    Vector3 targetPos = new Vector3(state.Position.X, state.Position.Y, state.Position.Z);
    enemy.transform.position = Vector3.Lerp(
        enemy.transform.position, 
        targetPos, 
        Time.deltaTime * 5f
    );
}
```

## 📚 参考资源

- [Protobuf C# 文档](https://developers.google.com/protocol-buffers/docs/csharptutorial)
- [NativeWebSocket](https://github.com/endel/NativeWebSocket)
- [Unity 网络最佳实践](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity6.html)

---

**更多帮助**: 查看 `TDNetworkExample.cs` 完整示例代码
