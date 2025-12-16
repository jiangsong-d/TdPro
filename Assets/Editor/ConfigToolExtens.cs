#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ConfigToolExtens : UnityEditor.Editor
{
    [MenuItem("配置表工具/导入配置")]
    public static void OutPutTables()
    {
        string path = Directory.GetParent(Application.dataPath).FullName;
        LogUtlis.Info("path + " + path);
        Process.Start(new ProcessStartInfo()
        {
            FileName = path + "\\ConfigTool\\ConfigPro\\config_gen.bat",
            WorkingDirectory = path + "\\ConfigTool\\ConfigPro"
        });
    }
    [MenuItem("配置表工具/打开Excel文件夹")]
    public static void OpenExcelDir()
    {
        string path = Directory.GetParent(Application.dataPath).FullName;
        Process.Start(path + "\\ConfigTool\\ConfigPro\\Configs");
    }
}
#endif