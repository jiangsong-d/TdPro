using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 让不可见的交互响应控件不参与绘制，降低显卡资源消耗
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class AorEmptyGraphic : Graphic
{
    /// <summary>
    /// 是否使用默认shader
    /// </summary>
    public bool UseDefaultShader = false;
    public bool m_Maskable = true;

    public AorEmptyGraphic()
    {
        useLegacyMeshGeneration = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        //base.OnPopulateMesh(vh);
        vh.Clear();
    }
}