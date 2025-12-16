using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpdetaFramework;
/// <summary>
/// �������
/// </summary>
public class DownloadPackageOver : IStateNode
{
    private StateMachine Machine;

    public void OnCreate(StateMachine machine)
    {
        Machine = machine;
    }
    public void OnEnter()
    {
        PatchEventDefine.PatchStepsChange.SendEventMessage("");
        Machine.ChangeState<UpdetaComplete>();
    }
    public void OnUpdate()
    {
    }
    public void OnExit()
    {
    }
}
