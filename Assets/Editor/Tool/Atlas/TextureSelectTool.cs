using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureSelectTool : EditorWindow
{
     public static readonly string TextureFullPath = Application.dataPath + "/Res/Texture/";
    public Action<Texture2D> SelectCallback;
    public string SelectDirName { get; set; } = "Bg";

    private string selectDirPath = "";
    private DirectoryInfo[] allDirs;
    private string[] allDirsPath;
    private string[] allDirsName;
    private List<Texture2D> allTextures = new List<Texture2D>();
    private Texture2D curSelectedTexture;
    private string searchName;
    private readonly int left = 130;
    private readonly int top = 20;
    private readonly int size = 70;
    private readonly int spaceX = 10;
    private readonly int spaceY = 20;
    private int cols, offset;
    private Vector2 rightScrollPos, leftScrollPos;
    private Dictionary<string, List<Texture2D>> folderCache = new Dictionary<string, List<Texture2D>>();

    public static TextureSelectTool Open(Texture2D init = null)
    {
        var win = GetWindow<TextureSelectTool>(true, "Texture选择器");
        win.position = new Rect(100, 50, 1280, 900);
        win.minSize = new Vector2(800, 600);
        win.curSelectedTexture = init;
        win.InitDirectories();
        // Try to honor SelectDirName: find the matching directory and load its textures
        try
        {
            if (!string.IsNullOrEmpty(win.SelectDirName) && win.allDirsName != null)
            {
                for (int i = 0; i < win.allDirsName.Length; i++)
                {
                    if (win.SelectDirName == win.allDirsName[i])
                    {
                        win.selectDirPath = win.allDirsPath[i];
                        win.LoadTexturesFromFolder(win.selectDirPath);
                        // auto-select first texture if exists
                        if (win.allTextures != null && win.allTextures.Count > 0)
                        {
                            win.curSelectedTexture = win.allTextures[0];
                        }
                        break;
                    }
                }
            }
            else if (win.allDirsPath != null && win.allDirsPath.Length > 0)
            {
                // fallback: pick first folder
                win.selectDirPath = win.allDirsPath[0];
                win.SelectDirName = win.allDirsName[0];
                win.LoadTexturesFromFolder(win.selectDirPath);
                if (win.allTextures != null && win.allTextures.Count > 0)
                    win.curSelectedTexture = win.allTextures[0];
            }
        }
        catch { }
        return win;
    }

    private void InitDirectories()
    {
        allDirs = FileUtils.GetDirs(TextureFullPath);
        allDirsPath = new string[allDirs.Length];
        allDirsName = new string[allDirs.Length];
        for (int i = 0; i < allDirs.Length; i++)
        {
            allDirsPath[i] = allDirs[i].FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            allDirsName[i] = allDirs[i].Name;
        }
    }

    private void OnGUI()
    {
        DrawMenus();
        DrawContent();
    }

    private void DrawMenus()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(10);
        if (curSelectedTexture != null)
        {
            // if (GUILayout.Button("编辑", "toolbarbutton", GUILayout.Width(80)))
            // {
            //     EditorGUIUtility.PingObject(curSelectedTexture);
            //     Selection.activeObject = curSelectedTexture;
            // }
            GUILayout.Space(6);
            GUILayout.Label(curSelectedTexture.name, "AssetLabel Partial");
        }

        GUILayout.FlexibleSpace();
        searchName = GUILayout.TextField(searchName, (GUIStyle)"SearchTextField", GUILayout.Width(200));
        if (GUILayout.Button("", "SearchCancelButton"))
        {
            searchName = "";
        }
        GUILayout.EndHorizontal();
    }

    private void DrawContent()
    {
        EditorGUILayout.BeginHorizontal();
        // left
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUILayout.Width(125));
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical("Box", GUILayout.Width(100));
        selectDirPath = null;
        if (allDirsPath != null)
        {
            for (int i = 0; i < allDirsPath.Length; i++)
            {
                if (SelectDirName == allDirsName[i])
                {
                    selectDirPath = allDirsPath[i];
                    GUI.color = Color.green;
                }
                if (GUILayout.Button(allDirsName[i]))
                {
                    SelectDirName = allDirsName[i];
                    selectDirPath = allDirsPath[i];
                    // load textures for this folder
                    LoadTexturesFromFolder(selectDirPath);
                }
                GUI.color = Color.white;
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // right
        DrawTextures();

        EditorGUILayout.EndHorizontal();
    }

    private void LoadTexturesFromFolder(string folderPath)
    {
        if (folderCache.TryGetValue(folderPath, out var cached))
        {
            allTextures = new List<Texture2D>(cached);
            return;
        }

        allTextures = new List<Texture2D>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { folderPath });
        if (guids != null)
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                    allTextures.Add(tex);
            }
        }

        folderCache[folderPath] = new List<Texture2D>(allTextures);
    }

    private void DrawTextures()
    {
        if (allTextures == null) allTextures = new List<Texture2D>();
        float availableWidth = Mathf.Max(1f, position.width - left);
        cols = Mathf.FloorToInt(availableWidth / (size + spaceX));
        cols = Mathf.Max(1, cols);
        int totalRows = (cols > 0) ? Mathf.CeilToInt(allTextures.Count / (float)cols) : 0;
        float contentHeight = totalRows * (spaceY + size) + spaceY;

        Rect viewRect = new Rect(left, top, position.width - left, position.height - top * 2);
        rightScrollPos = GUI.BeginScrollView(viewRect, rightScrollPos,
            new Rect(-spaceX, top - spaceY, position.width - 20 - left, contentHeight));

        offset = 0;
        for (int i = 0; i < allTextures.Count; i++)
        {
            var tex = allTextures[i];
            if (!string.IsNullOrEmpty(searchName) && !tex.name.ToLower().Contains(searchName.ToLower()))
            {
                offset++;
                continue;
            }
            int ii = i - offset;
            var rect = new Rect((ii % cols) * (size + spaceX), (ii / cols) * (size + spaceY) + top, size, size);
            if (GUI.Button(rect, ""))
            {
                if (curSelectedTexture == tex)
                {
                    SelectCallback?.Invoke(tex);
                    SelectCallback = null;
                    Close();
                }
                else
                {
                    curSelectedTexture = tex;
                }
            }
            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);

            var labelRect = rect;
            if (curSelectedTexture == tex)
            {
                labelRect.y += size;
                GUI.Label(labelRect, tex.name, "sv_label_3");
            }
            else
            {
                labelRect.y += size / 1.7f;
                GUI.Label(labelRect, tex.name, "MiniLabel");
            }
        }

        GUI.EndScrollView();
    }
}
