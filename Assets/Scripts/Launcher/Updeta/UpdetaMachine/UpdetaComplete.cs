using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpdetaFramework;
/// <summary>
/// 更新流程完成
/// </summary>
public class UpdetaComplete : IStateNode
{
    private PatchOperation owner;

    public void OnCreate(StateMachine machine)
    {
        owner = machine.Owner as PatchOperation;
    }
    public void OnEnter()
        {
            PatchEventDefine.PatchStepsChange.SendEventMessage("初始化游戏");
            owner.SetFinish();
            Launcher.Instance.StartCoroutine(StartGameLogic());
        }
    
    private IEnumerator StartGameLogic()
    {
        //  加载热更新DLL
        yield return HybridManager.Instance.LoadAllHotUpdateAssemblies();
        
        //  初始化游戏管理器
        yield return HybridManager.Instance.InitGameManager();
        
        LogUtlis.Info("游戏启动完成");
    }
    public void OnUpdate()
    {
    }
    public void OnExit()
    {
    }
}
