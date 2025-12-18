using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginView : BaseUIView
{
    public override void OnCreate()
    {
        base.OnCreate();

        AccountServiceManager.Instance.OnLocalAccountCheck += OnLocalAccountCheck;
        AccountServiceManager.Instance.CheckLocalAccount();
    }
    public override void OnRefresh()
    {
    }
    private void OnLocalAccountCheck(bool hasAccount)
    {
        if (hasAccount)
        {
            AccountServiceManager.Instance.LoginWithLocalAccount();
        }
        else
        {
               UIManager.Instance.OpenWindow<AccountSignView>("AccountSignView");
        }
    }
    public override void OnClickBtn(ButtonPro btn)
    {

        if (Btn.ContainsKey("btn_login"))
        {
             LogUtlis.Info("点击了登录按钮");
             LoadingManager.Instance.SwitchScene(LoadSceneType.Main);
             Close();
        }
        

    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
}
