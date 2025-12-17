# Proto 协议集成说明

## 概述

本项目已成功集成 Protocol Buffers (protobuf) 协议，实现了客户端（Unity C#）和服务端（Go）之间的高效二进制通信。

## 目录结构

### 服务端（Go）
```
TdSever/
├── proto/                    # 生成的 Go proto 代码
│   ├── common.pb.go         # 通用类型（Vector3, NetworkPacket等）
│   ├── connection.pb.go     # 连接相关（登录、心跳等）
│   ├── room.pb.go           # 房间相关
│   ├── battle.pb.go         # 战斗相关
│   └── sync.pb.go           # 同步相关
└── network/
    ├── session.go           # 会话管理
    ├── proto_handler.go     # Proto 消息处理器
    ├── broadcaster.go       # 消息广播器
    └── websocket.go         # WebSocket 服务器
```

### 客户端（Unity C#）
```
Assets/HotUpdate/
├── Protoc/                  # 生成的 C# proto 代码
│   ├── Common.cs           # 通用类型
│   ├── Connection.cs       # 连接相关
│   ├── Room.cs             # 房间相关
│   ├── Battle.cs           # 战斗相关
│   └── Sync.cs             # 同步相关
├── NetWork/
│   ├── NetworkManager.cs   # 网络管理器
│   ├── WebSocketClient.cs  # WebSocket 客户端
│   └── ProtoExample.cs     # 使用示例
└── Common/
    └── Cmd.cs              # 命令枚举
```

## 协议定义

### Proto 文件位置
```
ConfigTool/Proto/proto/
├── common.proto            # 通用类型定义
├── connection.proto        # 连接相关消息
├── room.proto              # 房间相关消息
├── battle.proto            # 战斗相关消息
└── sync.proto              # 同步相关消息
```

### 消息类型（MessageType）

#### 连接相关 (1000-1099)
- `MSG_HEARTBEAT (1000)` - 心跳
- `MSG_LOGIN (1001)` - 登录
- `MSG_LOGOUT (1002)` - 登出

#### 房间相关 (2000-2099)
- `MSG_CREATE_ROOM (2001)` - 创建房间
- `MSG_JOIN_ROOM (2002)` - 加入房间
- `MSG_LEAVE_ROOM (2003)` - 离开房间
- `MSG_ROOM_INFO (2004)` - 房间信息
- `MSG_START_GAME (2005)` - 开始游戏

#### 战斗相关 (3000-3099)
- `MSG_PLACE_TOWER (3001)` - 放置防御塔
- `MSG_UPGRADE_TOWER (3002)` - 升级防御塔
- `MSG_SELL_TOWER (3003)` - 出售防御塔
- `MSG_WAVE_START (3004)` - 波次开始
- `MSG_WAVE_COMPLETE (3005)` - 波次完成
- `MSG_GAME_OVER (3006)` - 游戏结束

#### 同步相关 (4000-4099)
- `MSG_SYNC_STATE (4001)` - 状态同步
- `MSG_SYNC_ENEMY (4002)` - 敌人同步
- `MSG_SYNC_TOWER (4003)` - 防御塔同步
- `MSG_SYNC_DAMAGE (4004)` - 伤害同步

#### 错误消息 (9999)
- `MSG_ERROR (9999)` - 错误响应

## 使用方法

### 服务端（Go）

#### 1. 发送消息
```go
// 发送登录响应
resp := &pb.LoginResponse{
    Success: true,
    PlayerId: "player123",
    PlayerName: "TestPlayer",
    PlayerInfo: &pb.PlayerBaseInfo{
        PlayerId: "player123",
        PlayerName: "TestPlayer",
        Level: 1,
        Coin: 1000,
    },
}
session.SendProtoMessage(pb.MessageType_MSG_LOGIN, resp)
```

#### 2. 处理消息
```go
// 在 proto_handler.go 中实现消息处理
func (s *Session) handleProtoLogin(payload []byte) {
    var req pb.LoginRequest
    if err := proto.Unmarshal(payload, &req); err != nil {
        s.SendProtoError(pb.ErrorCode_ERROR_INVALID_PARAM, "登录数据解析失败")
        return
    }
    
    // 处理登录逻辑...
}
```

### 客户端（Unity C#）

#### 1. 初始化
```csharp
// 初始化网络管理器
NetworkManager.Instance.Init("ws://localhost:8081/ws", "token");

// 注册消息处理器
NetworkManager.Instance.AddNetEvent(
    Cmd.Login, 
    OnLoginResponse, 
    typeof(LoginResponse)
);
```

#### 2. 发送消息
```csharp
// 发送登录请求
var request = new LoginRequest
{
    PlayerId = "player123",
    PlayerName = "TestPlayer",
    Token = "auth_token",
    Platform = "PC"
};

NetworkManager.Instance.SendMsg(Cmd.Login, request);
```

#### 3. 处理消息
```csharp
private void OnLoginResponse(int code, IMessage message)
{
    if (code != 0)
    {
        LogUtlis.Error($"登录失败，错误码: {code}");
        return;
    }

    var response = message as LoginResponse;
    if (response != null && response.Success)
    {
        LogUtlis.Info($"登录成功！玩家: {response.PlayerName}");
        LogUtlis.Info($"金币: {response.PlayerInfo.Coin}");
    }
}
```

#### 4. Update 循环
```csharp
void Update()
{
    // 处理接收到的消息
    NetworkManager.Instance.NetHandleRecovers();
}
```

## 代码生成

### 生成 C# 代码（Unity）
```batch
cd E:\Tdpro\ConfigTool\Proto
gen_proto.bat
```

### 生成 Go 代码（服务端）
```batch
cd E:\Tdpro\ConfigTool\Proto
gen_go.bat
```

## 错误码（ErrorCode）

### 通用错误 (1000-1999)
- `ERROR_NONE (0)` - 无错误
- `ERROR_UNKNOWN (1000)` - 未知错误
- `ERROR_INVALID_PARAM (1001)` - 无效参数
- `ERROR_PERMISSION_DENIED (1002)` - 权限拒绝
- `ERROR_NOT_FOUND (1003)` - 未找到
- `ERROR_ALREADY_EXISTS (1004)` - 已存在
- `ERROR_TIMEOUT (1005)` - 超时

### 连接错误 (2000-2099)
- `ERROR_NOT_LOGIN (2000)` - 未登录
- `ERROR_LOGIN_FAILED (2001)` - 登录失败
- `ERROR_TOKEN_INVALID (2002)` - Token无效
- `ERROR_ALREADY_LOGIN (2003)` - 已登录

### 房间错误 (3000-3099)
- `ERROR_ROOM_FULL (3000)` - 房间已满
- `ERROR_ROOM_NOT_FOUND (3001)` - 房间不存在
- `ERROR_NOT_IN_ROOM (3002)` - 不在房间中
- `ERROR_NOT_HOST (3003)` - 不是房主
- `ERROR_ROOM_ALREADY_STARTED (3004)` - 房间已开始

### 战斗错误 (4000-4099)
- `ERROR_NOT_ENOUGH_GOLD (4000)` - 金币不足
- `ERROR_INVALID_POSITION (4001)` - 无效位置
- `ERROR_TOWER_NOT_FOUND (4002)` - 防御塔不存在
- `ERROR_GAME_NOT_STARTED (4003)` - 游戏未开始
- `ERROR_GAME_OVER (4004)` - 游戏已结束

## 网络包结构

所有消息都使用 `NetworkPacket` 进行封装：

```protobuf
message NetworkPacket {
  int32 cmd = 1;          // 命令ID（对应 Cmd 枚举）
  int32 code = 2;         // 错误码（0表示成功）
  bytes payload = 3;      // 序列化后的消息内容
  int64 timestamp = 4;    // 时间戳
  string message_id = 5;  // 消息ID（用于追踪）
}
```

## 完整示例

参考 `ProtoExample.cs` 查看完整的使用示例，包括：
- 登录流程
- 创建/加入房间
- 放置防御塔
- 消息处理

## 注意事项

1. **消息类型匹配**：确保客户端的 `Cmd` 枚举值与服务端的 `MessageType` 保持一致
2. **Proto 更新**：修改 proto 文件后，需要重新运行代码生成脚本
3. **错误处理**：始终检查响应的 `code` 字段，0 表示成功
4. **二进制传输**：WebSocket 使用 Binary 模式传输 protobuf 数据
5. **命名空间**：C# 生成的代码在 `TowerDefense.Proto` 命名空间下

## 编译和运行

### 服务端
```bash
cd E:\Tdpro\TdSever
go build -o gameserver.exe
./gameserver.exe
```

### 客户端
在 Unity 编辑器中运行项目，或构建后运行。

## 依赖项

### 服务端（Go）
- google.golang.org/protobuf v1.36.11
- github.com/gorilla/websocket v1.5.3
- github.com/google/uuid v1.6.0

### 客户端（Unity）
- Google.Protobuf (NuGet)
- WebSocketSharp
