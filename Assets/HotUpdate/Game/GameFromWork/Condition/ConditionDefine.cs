/// <summary>
/// 条件类型枚举
/// </summary>
public enum ConditionType
{
    None = 0,
    
    // === 玩家相关 ===
    PlayerLevel = 101,              // 玩家等级
    PlayerVipLevel = 102,           // VIP等级
    PlayerPower = 103,              // 玩家战力
    
    // === 资源相关 ===
    Gold = 201,                     // 金币数量
    Diamond = 202,                  // 钻石数量
    Food = 203,                     // 粮食数量
    Wood = 204,                     // 木材数量
    Stone = 205,                    // 石头数量
    Iron = 206,                     // 铁矿数量
    
    // === 建筑相关 ===
    BuildingLevel = 301,            // 建筑等级
    BuildingCount = 302,            // 建筑数量
    BuildingExists = 303,           // 建筑是否存在
    
    // === 军队相关 ===
    TroopCount = 401,               // 军队数量
    TroopLevel = 402,               // 兵种等级
    HeroLevel = 403,                // 英雄等级
    HeroCount = 404,                // 英雄数量
    
    // === 任务相关 ===
    TaskCompleted = 501,            // 任务是否完成
    TaskProgress = 502,             // 任务进度
    
    // === 成就相关 ===
    AchievementCompleted = 601,     // 成就是否完成
    
    // === 引导相关 ===
    GuideCompleted = 701,           // 引导是否完成
    
    // === 时间相关 ===
    TimeRange = 801,                // 时间范围
    DayOfWeek = 802,                // 星期几
    ServerOpenDays = 803,           // 开服天数
    
    // === 背包相关 ===
    ItemCount = 901,                // 物品数量
    ItemOwn = 902,                  // 是否拥有物品
    
    // === 战斗相关 ===
    BattleWinCount = 1001,          // 战斗胜利次数
    BattleStarCount = 1002,         // 关卡星级
    
    // === 社交相关 ===
    FriendCount = 1101,             // 好友数量
    GuildJoined = 1102,             // 是否加入公会
    GuildLevel = 1103,              // 公会等级
    
    // === 自定义 ===
    Custom = 9999,                  // 自定义条件（通过事件名判断）
}

/// <summary>
/// 条件比较类型
/// </summary>
public enum ConditionCompareType
{
    Equal = 0,              // 等于 ==
    NotEqual = 1,           // 不等于 !=
    Greater = 2,            // 大于 >
    GreaterOrEqual = 3,     // 大于等于 >=
    Less = 4,               // 小于 <
    LessOrEqual = 5,        // 小于等于 <=
}

/// <summary>
/// 条件组逻辑类型
/// </summary>
public enum ConditionGroupLogic
{
    And = 0,    // 所有条件都满足（与）
    Or = 1,     // 任意条件满足（或）
}
