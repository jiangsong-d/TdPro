public class EventID
{
    #region 登录

    public static int NetError = EventNumberUtil.GetEventID();

    public static int WebSocketConnected = EventNumberUtil.GetEventID();

    #endregion
    
    #region 塔防网络事件
    
    // 网络相关
    public static int NETWORK_CONNECTED = EventNumberUtil.GetEventID();
    public static int NETWORK_DISCONNECTED = EventNumberUtil.GetEventID();
    public static int LOGIN_SUCCESS = EventNumberUtil.GetEventID();
    
    // 房间相关
    public static int ROOM_CREATED = EventNumberUtil.GetEventID();
    public static int ROOM_JOINED = EventNumberUtil.GetEventID();
    public static int ROOM_INFO_UPDATED = EventNumberUtil.GetEventID();
    public static int ROOM_LEFT = EventNumberUtil.GetEventID();
    
    // 游戏流程
    public static int GAME_START = EventNumberUtil.GetEventID();
    public static int GAME_OVER = EventNumberUtil.GetEventID();
    public static int GAME_PAUSE = EventNumberUtil.GetEventID();
    public static int GAME_RESUME = EventNumberUtil.GetEventID();
    
    // 波次相关
    public static int WAVE_START = EventNumberUtil.GetEventID();
    public static int WAVE_COMPLETE = EventNumberUtil.GetEventID();
    
    // 防御塔相关
    public static int TOWER_PLACED = EventNumberUtil.GetEventID();
    public static int TOWER_UPGRADED = EventNumberUtil.GetEventID();
    public static int TOWER_SOLD = EventNumberUtil.GetEventID();
    
    // 战斗相关
    public static int STATE_SYNCED = EventNumberUtil.GetEventID();
    public static int DAMAGE_DEALT = EventNumberUtil.GetEventID();
    public static int ENEMY_SPAWNED = EventNumberUtil.GetEventID();
    public static int ENEMY_KILLED = EventNumberUtil.GetEventID();
    public static int ENEMY_REACHED_END = EventNumberUtil.GetEventID();
    
    #endregion
}

public static class EventNumberUtil
{
    static EventNumberUtil()
    {
    }

    private static int ID_Message_Start = 100000000;

    // 获取event id
    public static int GetEventID()
    {
        ID_Message_Start++;
        return ID_Message_Start;
    }
}