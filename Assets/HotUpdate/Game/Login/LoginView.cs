using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginView : BaseUIView
{
    public override void OnCreate()
    {
        base.OnCreate();

        // AccountServiceManager.Instance.OnLocalAccountCheck += OnLocalAccountCheck;
        // AccountServiceManager.Instance.CheckLocalAccount();
        LoginManager.Instance.CheckAndAutoLogin();
    }
    public override void OnRefresh()
    {
    }
    public override void OnClickBtn(ButtonPro btn)
    {

        if (Btn.ContainsKey("btn_login"))
        {
             LogUtlis.Info("点击了登录按钮");
            
            LoginNet.Instance.SendLogin();

             LoadingManager.Instance.SwitchScene(LoadSceneType.Main);


             Close();
        }
        

    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
}
