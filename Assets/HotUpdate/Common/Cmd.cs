/// <summary>
/// 消息类型枚举 - 对应 Proto 中的 MessageType
/// </summary>
public enum Cmd
{
    None = 0,

    // ========== 连接相关 1000-1099 ==========
    /// <summary>心跳消息</summary>
    Heartbeat = 1000,
    /// <summary>登录</summary>
    Login = 1001,
    /// <summary>登出</summary>
    Logout = 1002,

    // ========== 房间相关 2000-2099 ==========
    /// <summary>创建房间</summary>
    CreateRoom = 2001,
    /// <summary>加入房间</summary>
    JoinRoom = 2002,
    /// <summary>离开房间</summary>
    LeaveRoom = 2003,
    /// <summary>房间信息</summary>
    RoomInfo = 2004,
    /// <summary>开始游戏</summary>
    StartGame = 2005,
    /// <summary>准备/取消准备</summary>
    Ready = 2006,
    /// <summary>房间列表</summary>
    RoomList = 2007,

    // ========== 战斗相关 3000-3099 ==========
    /// <summary>放置防御塔</summary>
    PlaceTower = 3001,
    /// <summary>升级防御塔</summary>
    UpgradeTower = 3002,
    /// <summary>出售防御塔</summary>
    SellTower = 3003,
    /// <summary>波次开始</summary>
    WaveStart = 3004,
    /// <summary>波次完成</summary>
    WaveComplete = 3005,
    /// <summary>游戏结束</summary>
    GameOver = 3006,
    /// <summary>使用技能</summary>
    UseSkill = 3007,
    /// <summary>暂停游戏</summary>
    PauseGame = 3008,

    // ========== 同步相关 4000-4099 ==========
    /// <summary>状态同步</summary>
    SyncState = 4001,
    /// <summary>敌人同步</summary>
    SyncEnemy = 4002,
    /// <summary>防御塔同步</summary>
    SyncTower = 4003,
    /// <summary>伤害同步</summary>
    SyncDamage = 4004,
    /// <summary>敌人生成通知</summary>
    EnemySpawn = 4005,
    /// <summary>敌人死亡通知</summary>
    EnemyDeath = 4006,
    /// <summary>防御塔攻击通知</summary>
    TowerAttack = 4007,
    /// <summary>Buff同步通知</summary>
    BuffSync = 4008,
    /// <summary>请求游戏状态</summary>
    RequestGameState = 4009,

    // ========== 错误消息 9999 ==========
    /// <summary>错误响应</summary>
    Error = 9999
}
