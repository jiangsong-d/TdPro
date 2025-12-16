using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI相关数据
/// </summary>
public class UIData
{
    // 当前设计如下:
    // 基础根Canvas Order起始为10000
    // 最大Layer数量10，单层Layer最大窗口数量20
    // 每层Layer相差1000 Order
    // 每个Window相差50 Order(内部预留了50来做自定义Order排版)

    //Layer起始SortingOrder
    //单窗口之前相差的SortingOrder
    public const int LayerStartOrder = 1000;
    public const int PerWindowOrder = 50;
}
