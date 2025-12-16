using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// Image实现使用PolygonCollider2D描述不规则碰撞检测
/// </summary>
[ExecuteInEditMode]
public class YRawImage : RawImage, IGrayMember
{

    /// <summary>
    /// 是否使用默认shader
    /// </summary>
    public bool UseDefaultShader = false;
    private string mCurrentPath = "";
    private float mCurrentAlpha = 0;
    protected override void Awake()
    {
        base.Awake();
        if (Application.isPlaying)
        {
            if (texture == null)
            {
                Color n = color;
                n.a = 0;
                color = n;
            }
        }
    }



    /// <summary>
    /// 异步加载图片在加载未完成时调用SetGray会出现图片Alpha为0的情况
    /// 在此设置一标志量记录SetGray的目标值，在图片加载完成后重新SetGray
    /// </summary>
    private bool grayWaitLoad = false;
    public bool IsGray { get; private set; }

    private Color oldColor;

    public void SetGrayEffect(bool isGray)
    {
        grayWaitLoad = isGray;
        if (texture == null)
            return;
        if (IsGray == isGray)
            return;
        IsGray = isGray;
        if (isGray)
        {
            oldColor = color;
            color = new Color32(254, 254, 254, (byte)(255 * Alpha));
        }
        else
        {
            color = oldColor;
        }
    }
    /// <summary>
    /// 加载图片
    /// </summary>
    /// <param name="path">图片路径</param>
    /// <param name="textureType">图片格式</param>
    /// <param name="resetSize">是否重置图片尺寸</param>
    /// <param name="targetAlpha">目标透明度</param>
    /// <param name="isAsync">是否异步加载</param>
    public UniTask LoadTexture(string path,TextureType textureType = TextureType.JPG, bool resetSize = true,float targetAlpha = 1, bool isAsync = false)
    {
        if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;
        if (mCurrentPath == path) return UniTask.CompletedTask;
        mCurrentPath = path;

        if (isAsync)
        {
            return LoadTextureAsyncImpl(path,textureType,resetSize,targetAlpha);
        }

        try
        {
            var tex = LoadManager.Instance.LoadTexture(path,textureType);
            if (tex != null)
            {
                texture = tex;
                Alpha = targetAlpha;
                if (resetSize)
                    SetNativeSize();
                if (grayWaitLoad)
                    SetGrayEffect(true);
            }
            else
            {
                Clear();
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"LoadTexture error: {e.Message}");
            Clear();
        }

        return UniTask.CompletedTask;
    }

    private async UniTask LoadTextureAsyncImpl(string path,TextureType textureType ,bool resetSize,float targetAlpha = 1)
    {
        try
        {
            var tex = await LoadManager.Instance.LoadTextureAsync(path,textureType);
            if (tex != null)
            {   
                Alpha= targetAlpha;
                texture = tex;
                if (resetSize)
                    SetNativeSize();
                if (grayWaitLoad)
                    SetGrayEffect(true);

            }
            else
            {
                Clear();
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"LoadTextureAsync error: {e.Message}");
            Clear();
        }
    }
    public float Alpha
    {
        get { return color.a; }
        set
        {
            Color n = color;
            n.a = Mathf.Clamp(value, 0, 1);
            color = n;
            mCurrentAlpha = n.a;
        }
    }

    public void Clear(bool initAlpha = true)
    {
        if (initAlpha)
            Alpha = 0;

        texture = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Clear();
    }
}
