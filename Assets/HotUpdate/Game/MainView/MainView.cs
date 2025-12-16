using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainView : BaseUIView
{
    public override void OnCreate()
    {
        base.OnCreate();
    }
    public override void OnRefresh()
    {
    }
    public override void OnClickBtn(ButtonPro btn)
    {

        if (Btn.ContainsKey("btn_login"))
        {
             
        }
        

    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
}
