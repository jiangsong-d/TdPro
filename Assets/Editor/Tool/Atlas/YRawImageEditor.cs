using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

[CanEditMultipleObjects, CustomEditor(typeof(YRawImage), true)]
public class YRawImageEditor : RawImageEditor
{
    private YRawImage mTarget;

    protected override void OnEnable()
    {
        base.OnEnable();
        mTarget = target as YRawImage;
        // Mirror YImage default-material logic if needed
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

        GUILayout.Space(10);
        GUILayout.BeginVertical();
        GUI.color = Color.green;
        if (GUILayout.Button("选择Texture", EditorStyles.miniButtonMid))
        {
            // Open TextureSelectTool via reflection to avoid compile-time type issues
            Type toolType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // try direct name
                    toolType = asm.GetType("TextureSelectTool");
                    if (toolType == null)
                    {
                        // search types for matching name
                        foreach (var t in asm.GetTypes())
                        {
                            if (t.Name == "TextureSelectTool")
                            {
                                toolType = t;
                                break;
                            }
                        }
                    }
                }
                catch { }
                if (toolType != null) break;
            }

            if (toolType == null)
            {
                Debug.LogError("TextureSelectTool type not found.");
            }
            else
            {
                object winInstance = null;
                try
                {
                    var openMethod = toolType.GetMethod("Open", BindingFlags.Public | BindingFlags.Static);
                    if (openMethod != null)
                    {
                        winInstance = openMethod.Invoke(null, new object[] { mTarget.texture as Texture2D });
                    }
                    else
                    {
                        winInstance = EditorWindow.GetWindow(toolType);
                    }

                    if (winInstance != null)
                    {
                        var field = toolType.GetField("SelectCallback");
                        if (field != null)
                        {
                            Action<Texture2D> cb = (Texture2D tex) =>
                            {
                                mTarget.texture = tex;
                                mTarget.SetAllDirty();
                                EditorUtility.SetDirty(mTarget);
                            };
                            field.SetValue(winInstance, cb);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Open TextureSelectTool failed: " + e.Message);
                }
            }
        }
        GUI.color = Color.white;
        GUILayout.EndVertical();

        // no object picker handling needed when using TextureSelectTool

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("编辑", GUILayout.Height(15)))
        {
            if (mTarget.texture != null)
            {
                EditorGUIUtility.PingObject(mTarget.texture);
                Selection.activeObject = mTarget.texture;
            }
        }
        if (GUILayout.Button("重置", GUILayout.Height(15)))
        {
            Init();
        }
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void Init()
    {
        if (mTarget == null) mTarget = target as YRawImage;
        mTarget.texture = null;
        mTarget.Alpha = 0;
        EditorUtils.RebuildTransf(mTarget.rectTransform);
        EditorUtility.SetDirty(mTarget);
    }

    public static class DisableDefaultRawImageMenu
    {
        [MenuItem("GameObject/UI/Raw Image", true)]
        static bool DisableImage()
        {
            return false;
        }
    }
} 