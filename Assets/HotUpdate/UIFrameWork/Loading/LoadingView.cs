using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingView : BaseUIView
{
    // Start is called before the first frame update
    private Slider slider;
    public override void Awake()
    {
        slider = Obj["slider_progress"].GetComponent<Slider>();
        
        Tmp["tmp_Des"].text = LanguageManager.GetLangVal("396");

    }

    public void SetProgress(float val,string modelName=null)
    {
        slider.value = (float)val;

        Tmp["tmp_Des"].text = modelName;
        /*// 只有为null的时候会跳过，为""则会进行赋值
         if (modelName != null)
         {
             SetModelName(modelName);
         }*/
        Tmp["tmp_ProgressText"].text = val * 100f + "%";
    }
} 
