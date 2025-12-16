using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public class PlaySceneEditor
    {
        static PlaySceneEditor()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Handles.BeginGUI();
            GUI.color = Color.green;

            if (GUI.Button(new Rect(5, 10, 50, 30), "Main"))
            {
                SwitchScene("Main");
            }

            if (GUI.Button(new Rect(5, 50, 50, 30), "UI开发"))
            {
                SwitchScene("UI开发");
            }
            if (GUI.Button(new Rect(5, 90, 50, 30), "技能预览"))
            {
                SwitchScene("SkillEditorScene");
            }

            Handles.EndGUI();
        }

        public static void SwitchScene(string sceneName)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string scenePath = $"{Application.dataPath}/Scenes/{sceneName}.unity";
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = false;
            }
        }


    }

}