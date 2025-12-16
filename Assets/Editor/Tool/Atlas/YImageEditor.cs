using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using Utils;

[CanEditMultipleObjects, CustomEditor(typeof(YImage), true)]
public class YImageEditor : ImageEditor
{
    private YImage mTarget;

    protected override void OnEnable()
    {
        base.OnEnable();
        this.mTarget = (YImage)target;
        if (mTarget != null && !mTarget.UseDefaultShader)
        {
            if (mTarget.material == null || mTarget.material.shader.name == UIUtils.UnityUIDefaultShaderName)
            {
                Canvas canvas = mTarget.transform.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    mTarget.material = UIUtils.ImageDefaultWorldMat;
                }
                else
                {
                    mTarget.material = UIUtils.ImageDefaultMat;
                }
                mTarget.SetMaterialDirty();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UseDefaultShader"));
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        GUILayout.BeginVertical();

        GUI.color = Color.green;
        if (GUILayout.Button("选择Sprite", EditorStyles.miniButtonMid))
        {
            var temp = SpriteSelectTool.Open(mTarget.sprite);
            temp.SelectCallback = OnSelectSprite;
        }
        GUI.color = Color.white;

        var tempBool = GUILayout.Toggle(mTarget.IsFlipX, "左右翻转", EditorStyles.toolbarButton);
        if (tempBool != mTarget.IsFlipX)
        {
            mTarget.IsFlipX = tempBool;
            EditorUtils.RebuildTransf(mTarget.rectTransform);
        }
        tempBool = GUILayout.Toggle(mTarget.IsFlipY, "上下翻转", EditorStyles.toolbarButton);
        if (tempBool != mTarget.IsFlipY)
        {
            mTarget.IsFlipY = tempBool;
            EditorUtils.RebuildTransf(mTarget.rectTransform);
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("编辑", GUILayout.Height(15)))
        {
            if (mTarget.sprite == null)
                return;
            SpriteSelectTool.EditorSprite(mTarget.sprite);
        }
        if (GUILayout.Button("重置", GUILayout.Height(15)))
        {
            Init();
        }

        GUILayout.EndHorizontal();
    }

    private void Init()
    {
        mTarget.Clear();
        EditorUtils.RebuildTransf(mTarget.rectTransform);
        EditorUtility.SetDirty(mTarget);
    }

    private void OnSelectSprite(Sprite arg0)
    {
        mTarget.sprite = arg0;
        EditorUtility.SetDirty(mTarget);
    }
    public static class DisableDefaultImageMenu
    {
        [MenuItem("GameObject/UI/Image", true)]
        static bool DisableImage()
        {
            return false;
        }
        [MenuItem("CONTEXT/Image/ImgToYImg",false,2000)]
        public  static void ImgToYImg(MenuCommand menuCommand)
        {
            Image img = menuCommand.context as Image;
            ImgToYImg(img);

        }
        [MenuItem("GameObject/UI/ImgToYImg",false,2100)]
        public static void GoImgToYImg(MenuCommand menuCommand)
        {
            var go = menuCommand.context as GameObject;
            var images=go.transform.GetAllComponent<Image>();
            foreach (var img in images)
            {
                ImgToYImg(img);
            }
        }

        static void ImgToYImg(Image img)
        {
            if(img!=null && img.GetType()!=typeof(YImage))
            {
                LogUtlis.Info("ImgToYImg");
                var sprite= img.sprite;
                var color= img.color;
                var padding = img.raycastPadding;
                var canRaycast= img.raycastTarget;
                var canMask= img.maskable;
                var contextGo= img.gameObject;
                var ty = img.type;
                var fillcenter= img.fillCenter;
                var fillmethod= img.fillMethod;
                var fillamount= img.fillAmount;
                var fillClockwise= img.fillClockwise;
                var fillOrigin= img.fillOrigin;
                DestroyImmediate( img);
                var yimg= contextGo.AddComponent<YImage>();
                yimg.sprite=sprite;
                yimg.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Res/Material/Default_UI.mat");
                yimg.color=color;
                yimg.raycastPadding=padding;
                yimg.raycastTarget=canRaycast;
                yimg.maskable=canMask;
                yimg.type=ty;
                yimg.fillCenter=fillcenter;
                yimg.fillMethod=fillmethod;
                yimg.fillAmount=fillamount;
                yimg.fillClockwise=fillClockwise;
                yimg.fillOrigin=fillOrigin;


                if (contextGo.name.Contains("@_image"))
                {
                    contextGo.name = contextGo.name.Replace("@_image", "@_img");
                }
            }
            else
            {
                img.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Res/Material/Default_UI.mat");
            }
        }
    }
}