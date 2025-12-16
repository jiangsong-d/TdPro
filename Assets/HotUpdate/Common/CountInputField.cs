using System;
using UnityEngine;
using UnityEngine.UI;

public class CountInputField : BaseViewComponent
{
    public Action<int> OnCountChanged;

    private int _count;
    private int _step;
    private int _maxCount;

    public override void OnCreate()
    {
        _count = 1;
        ShowCount();
    }

    public override void OnClickBtn(Button btn)
    {
        if (btn == Btn["btn_Add"])
        {
            if (_count >= _maxCount)
            {
                // TODO: 2025/05/21 提示超过最大值
                return;
            }

            _count = Mathf.Min(_count + _step, _maxCount);
            ShowCount();
            OnCountChanged?.Invoke(_count);
        }
        else if (btn == Btn["btn_Minus"])
        {
            if (_count <= 0)
            {
                // TODO: 2025/05/21 提示超过最小值
                return;
            }

            _count = Mathf.Max(_count - _step, 1);
            ShowCount();
            OnCountChanged?.Invoke(_count);
        }
        else if (btn == Btn["btn_Max"])
        {
            if (_count == _maxCount) return;
            _count = _maxCount;
            ShowCount();
            OnCountChanged?.Invoke(_count);
        }
    }

    public void Show(int step, int maxCount)
    {
        _maxCount = maxCount;
        _step = step;
    }

    #region 子类重写

    /// <summary>
    /// 显示数量
    /// </summary>
    protected virtual void ShowCount()
    {
        Tmp["tmp_Count"].text = _count.ToString();
    }

    #endregion
}