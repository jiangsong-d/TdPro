using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Reflection;
using System.Linq;

/// <summary>
/// 自定义Image组件，支持X/Y轴翻转
/// </summary>
[ExecuteInEditMode]
public class YImage : Image, IGrayMember
{
    /// <summary>
    /// 是否使用默认shader
    /// </summary>
    public bool UseDefaultShader = false;
    public bool IsFlipX;
    public bool IsFlipY;
    public bool CanRaycast = true;

    private Collider2D collider2d;
    //当前正在加载的资源路径
    private string mCurrentPath = "";
    private float mCurrentAlpha = 0;
    private string CurrentPath
    {
        get
        {
            if (string.IsNullOrEmpty(mCurrentPath) && sprite != null)
            {
                mCurrentPath = sprite.name;
            }
            return mCurrentPath;
        }
        set
        {
            mCurrentPath = value;
        }
    }
    private long mResRef = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            SetCollider(collider2D);
        }
        base.Start();
    }



    private void SetCollider(Collider2D collider2D)
    {
        collider2d = collider2D;
    }

    public override bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (!CanRaycast)
        {
            return false;
        }
        else if (collider2d != null)
        {
            var worldPoint = Vector3.zero;
            var isInside = RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                sp,
                eventCamera,
                out worldPoint
            );
            if (isInside)
                isInside = collider2d.OverlapPoint(worldPoint);
            return isInside;
        }
        else
        {
            return base.IsRaycastLocationValid(sp, eventCamera);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (sprite == null)
        {
            vh.Clear();
            return;
        }
        base.OnPopulateMesh(vh);
        Flip(vh, IsFlipX, IsFlipY, rectTransform);
    }

    private void Flip(VertexHelper vh, bool x, bool y, RectTransform rectTransf)
    {
        if (x)
        {
            UIVertex vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.position.x += (rectTransf.rect.center.x - vertex.position.x) * 2;
                vh.SetUIVertex(vertex, i);
            }
        }
        if (y)
        {
            UIVertex vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.position.y += (rectTransf.rect.center.y - vertex.position.y) * 2;
                vh.SetUIVertex(vertex, i);
            }
        }
    }

    public bool IsGray { get; private set; }

    private Color oldColor;
    /// <summary>
    /// 设置灰色效果
    /// </summary>
    public void SetGrayEffect(bool isGray)
    {
        if (!this)
        {
            //Debug.Error("AorImage is destoryed but still try to access it.");
            return;
        }

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

    public float Alpha
    {
        get
        {
            return color.a;
        }
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
        sprite = null;
        mCurrentPath = "";
        IsFlipX = false;
        IsFlipY = false;
        if (mResRef != 0)
        {
            mResRef = 0;
        }
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Clear();
    }

    /// <summary>
    /// 参数：path, resetSize(是否恢复图片默认尺寸), isAsync(是否异步)
    /// 返回：UniTask（可 await）
    /// </summary>
    public UniTask LoadSpriteAtlas(string path, bool resetSize = true, bool isAsync = false)
    {
        if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;
        if (isAsync)
        {
            return LoadSpriteFromAtlasAsyncImpl(path, resetSize);
        }
        else
        {
            try
            {
                var sp = LoadManager.Instance.LoadSpriteAtlasSprite(path);
                if (sp != null)
                {
                    sprite = sp;
                    CurrentPath = path;
                    if (resetSize)
                    {
                        SetNativeSize();
                    }
                    mCurrentAlpha = color.a;
                }
                else
                {
                    Clear(false);
                }
            }
            catch (Exception e)
            {
                LogUtlis.Error($"LoadSpriteFromAtlas error: {e.Message}");
                Clear(false);
            }
            return UniTask.CompletedTask;
        }
    }

    private async UniTask LoadSpriteFromAtlasAsyncImpl(string path, bool resetSize)
    {
        try
        {
            var sp = await LoadManager.Instance.LoadSpriteAtlasSpriteAsync(path);
            if (sp != null)
            {
                sprite = sp;
                CurrentPath = path;
                if (resetSize)
                {
                    SetNativeSize();
                }
                mCurrentAlpha = color.a;
            }
            else
            {
                Clear(false);
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"LoadSpriteFromAtlasAsync error: {e.Message}");
            Clear(false);
        }
    }

    /// <summary>
    /// 从单个Sprite资源加载 Sprite（同步或异步）
    /// 参数：path, resetSize(是否恢复图片默认尺寸), isAsync(是否异步)
    /// </summary>
    public UniTask LoadSpriteSingle(string path, bool resetSize = true, bool isAsync = false)
    {
        if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;
        if (isAsync)
        {
            return LoadSpriteSingleAsyncImpl(path, resetSize);
        }
        else
        {
            try
            {
                var sp = LoadManager.Instance.LoadSpriteSingle(path);
                if (sp != null)
                {
                    sprite = sp;
                    CurrentPath = path;
                    if (resetSize)
                    {
                        SetNativeSize();
                    }
                    mCurrentAlpha = color.a;
                }
                else
                {
                    Clear(false);
                }
            }
            catch (Exception e)
            {
                LogUtlis.Error($"LoadSpriteSingle error: {e.Message}");
                Clear(false);
            }
            return UniTask.CompletedTask;
        }
    }

    private async UniTask LoadSpriteSingleAsyncImpl(string path, bool resetSize)
    {
        try
        {
            var spPath = "Sprite/" + path + ".png";
            var sp = await LoadManager.Instance.LoadSpriteAsync(path);
            if (sp != null)
            {
                sprite = sp;
                CurrentPath = path;
                if (resetSize)
                {
                    SetNativeSize();
                }
                mCurrentAlpha = color.a;
            }
            else
            {
                Clear(false);
            }
        }
        catch (Exception e)
        {
            LogUtlis.Error($"LoadSpriteSingleAsync error: {e.Message}");
            Clear(false);
        }
    }

}
