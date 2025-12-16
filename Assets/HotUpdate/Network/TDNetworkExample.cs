using UnityEngine;
using TowerDefense.Proto;

/// <summary>
/// 塔防游戏示例 - 如何使用网络管理器
/// </summary>
public class TDNetworkExample : MonoBehaviour
{
    private void Start()
    {
        // 注册事件监听
        RegisterEvents();
        
        // 连接服务器
        TDNetworkManager.Instance.Connect("ws://localhost:8080/ws");
    }
    
    private void RegisterEvents()
    {
        // 登录成功
        EngineEventManager.Instance.AddEventListener(EventID.LOGIN_SUCCESS, OnLoginSuccess);
        
        // 房间创建
        EngineEventManager.Instance.AddEventListener(EventID.ROOM_CREATED, OnRoomCreated);
        
        // 游戏开始
        EngineEventManager.Instance.AddEventListener(EventID.GAME_START, OnGameStart);
        
        // 波次开始
        EngineEventManager.Instance.AddEventListener(EventID.WAVE_START, OnWaveStart);
        
        // 游戏结束
        EngineEventManager.Instance.AddEventListener(EventID.GAME_OVER, OnGameOver);
        
        // 状态同步
        EngineEventManager.Instance.AddEventListener(EventID.STATE_SYNCED, OnStateSynced);
    }
    
    private void OnLoginSuccess(EngineEvent evt)
    {
        LogUtlis.Info("登录成功，可以创建或加入房间");
        
        // 示例：自动创建房间
        TDNetworkManager.Instance.CreateRoom("测试房间", 4, 1);
    }
    
    private void OnRoomCreated(EngineEvent evt)
    {
        string roomId = evt.GetString("room_id");
        LogUtlis.Info($"房间创建成功: {roomId}");
        
        // 可以邀请其他玩家或者直接开始游戏
        // TDNetworkManager.Instance.StartGame();
    }
    
    private void OnGameStart(EngineEvent evt)
    {
        GameInitData gameData = evt.GetParam<GameInitData>("game_data");
        LogUtlis.Info($"游戏开始！初始金币: {gameData.Gold}, 生命: {gameData.Life}");
        
        // 初始化游戏界面
        // UpdateGoldUI(gameData.Gold);
        // UpdateLifeUI(gameData.Life);
    }
    
    private void OnWaveStart(EngineEvent evt)
    {
        int waveNum = evt.GetInt("wave_num");
        LogUtlis.Info($"第 {waveNum} 波开始！");
        
        // 显示波次提示
        // ShowWaveNotification(waveNum);
    }
    
    private void OnGameOver(EngineEvent evt)
    {
        bool isVictory = evt.GetBool("is_victory");
        int score = evt.GetInt("score");
        
        LogUtlis.Info($"游戏结束！{(isVictory ? "胜利" : "失败")}, 得分: {score}");
        
        // 显示结算界面
        // ShowGameOverPanel(isVictory, score);
    }
    
    private void OnStateSynced(EngineEvent evt)
    {
        var enemies = evt.GetParam<Google.Protobuf.Collections.RepeatedField<EnemyState>>("enemies");
        var towers = evt.GetParam<Google.Protobuf.Collections.RepeatedField<TowerState>>("towers");
        
        // 更新游戏对象
        UpdateEnemies(enemies);
        UpdateTowers(towers);
    }
    
    private void UpdateEnemies(Google.Protobuf.Collections.RepeatedField<EnemyState> enemies)
    {
        foreach (var enemy in enemies)
        {
            // 查找或创建敌人对象
            // GameObject enemyObj = GetOrCreateEnemy(enemy.EnemyId);
            
            // 更新位置
            Vector3 pos = new Vector3(enemy.Position.X, enemy.Position.Y, enemy.Position.Z);
            // enemyObj.transform.position = pos;
            
            // 更新血条
            // UpdateEnemyHP(enemyObj, enemy.Hp, enemy.MaxHp);
            
            LogUtlis.Info($"敌人 {enemy.EnemyId} 位置: {pos}, HP: {enemy.Hp}/{enemy.MaxHp}");
        }
    }
    
    private void UpdateTowers(Google.Protobuf.Collections.RepeatedField<TowerState> towers)
    {
        foreach (var tower in towers)
        {
            // 查找塔对象
            // GameObject towerObj = FindTower(tower.TowerId);
            
            // 更新目标指向
            if (!string.IsNullOrEmpty(tower.TargetId))
            {
                // SetTowerTarget(towerObj, tower.TargetId);
                LogUtlis.Info($"塔 {tower.TowerId} 正在攻击 {tower.TargetId}");
            }
        }
    }
    
    // ==================== UI 按钮回调示例 ====================
    
    /// <summary>
    /// 点击放置防御塔按钮
    /// </summary>
    public void OnPlaceTowerButton(int towerType)
    {
        // 获取放置位置（例如从点击位置）
        Vector3 position = GetTowerPlacePosition();
        
        TDNetworkManager.Instance.PlaceTower(towerType, position);
    }
    
    /// <summary>
    /// 点击升级防御塔按钮
    /// </summary>
    public void OnUpgradeTowerButton(string towerId)
    {
        TDNetworkManager.Instance.UpgradeTower(towerId);
    }
    
    /// <summary>
    /// 点击出售防御塔按钮
    /// </summary>
    public void OnSellTowerButton(string towerId)
    {
        TDNetworkManager.Instance.SellTower(towerId);
    }
    
    /// <summary>
    /// 点击开始游戏按钮
    /// </summary>
    public void OnStartGameButton()
    {
        TDNetworkManager.Instance.StartGame();
    }
    
    private Vector3 GetTowerPlacePosition()
    {
        // TODO: 实现获取放置位置的逻辑
        // 例如：从鼠标点击位置获取
        return new Vector3(5, 0, 5);
    }
    
    private void OnDestroy()
    {
        // 移除事件监听
        EngineEventManager.Instance.RemoveEventListener(EventID.LOGIN_SUCCESS, OnLoginSuccess);
        EngineEventManager.Instance.RemoveEventListener(EventID.ROOM_CREATED, OnRoomCreated);
        EngineEventManager.Instance.RemoveEventListener(EventID.GAME_START, OnGameStart);
        EngineEventManager.Instance.RemoveEventListener(EventID.WAVE_START, OnWaveStart);
        EngineEventManager.Instance.RemoveEventListener(EventID.GAME_OVER, OnGameOver);
        EngineEventManager.Instance.RemoveEventListener(EventID.STATE_SYNCED, OnStateSynced);
    }
}
