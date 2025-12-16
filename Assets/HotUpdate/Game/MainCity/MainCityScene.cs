using UnityEngine;
using Cinemachine;
using cfg.Building;

/// <summary>
/// 主城场景管理器
/// 职责：
/// 1. 场景初始化和销毁
/// 2. 协调各个Manager（MapManager、WorldManager等）
/// 3. 处理场景级逻辑
/// 4. 管理场景资源
/// </summary>
public class MainCityScene : BaseScene
{
  
    public override void Awake()
    {   
       
        
    }

    public override void OnCreate()
    {
        LogUtlis.Info($"场景名:{SceneName} OnCreate");
    }
    public override void OnRefresh()
    {
    }

    // 可用，子类扩展实现
    public override void OnEnable()
    {
    }

    public override void Update(float dt)
    {
       
    }
    public override void FixedUpdate(float dt)
    {

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // 清理顺序很重要
        
        // SavePlayerData();

        LogUtlis.Info("[MainCityScene] 主城场景已销毁");
    }
}

    