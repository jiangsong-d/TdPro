using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseUIItem
{
    public GameObject Form;
    public BaseUIItem(GameObject go)
    {
        Form = go;
        GetBindComponents(go);
    }

    // 绑定节点数据
    public virtual void GetBindComponents(GameObject go) { }
}
