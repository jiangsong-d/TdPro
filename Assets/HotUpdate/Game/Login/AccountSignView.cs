using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加 TextMesh Pro 命名空间

/// <summary>
/// 账号登录注册界面
/// </summary>
public class AccountSignView : BaseUIView
{
    private string username;
    private string password;
    
    public override void OnCreate()
    {
        base.OnCreate();
    
    }
    

    public override void OnRefresh()
    {
        username = Obj["obj_Account"].GetComponent<TMP_InputField>().text;
        password = Obj["obj_Password"].GetComponent<TMP_InputField>().text;
    }
    
    public override void OnClickBtn(ButtonPro btn)
    {
        // 获取最新的用户名密码
        OnRefresh();
        
        // 验证输入
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            LogUtlis.Info("[登录界面] 请输入用户名和密码");
            return;
        }

        if (btn==Btn["btn_Sign"])
        {
            // 注册按钮
            LogUtlis.Info($"[登录界面] 注册账号: {username}");
            AccountServiceManager.Instance.Register(username, password);
             Close();
        }
        if (btn==Btn["btn_login"])
        {
            // 登录按钮
            LogUtlis.Info($"[登录界面] 登录账号: {username}");
            AccountServiceManager.Instance.Login(username, password);
             Close();
        }
    }

    public void OnOkCallBack()
    {
        Close();
    }
    
    public override void OnDestroy()
    {
        // 取消事件订阅
        // if (AccountServiceManager.Instance != null)
        // {
        //     AccountServiceManager.Instance.OnConnectionTest -= OnConnectionTest;
        //     AccountServiceManager.Instance.OnLoginResult -= OnLoginResult;
        //     AccountServiceManager.Instance.OnRegisterResult -= OnRegisterResult;
        // }
        
        base.OnDestroy();
    }
}
