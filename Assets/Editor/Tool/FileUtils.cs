using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 简单的文件工具集，提供编辑器脚本常用的路径/文件/目录操作的包装。
/// 这个实现主要用于替代或兼容工程中未找到的 FileUtils，方法命名与原调用处一致。
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// 把路径格式化为 Unity 风格（使用正斜杠 '/'.）
    /// </summary>
    public static string FormatToUnityPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        return path.Replace("\\", "/");
    }

    /// <summary>
    /// 返回指定文件或目录所在的上级文件夹名称（仅名称，不含路径）。
    /// 例如：给出 ".../Assets/MyFolder/file.png" 返回 "MyFolder"。
    /// </summary>
    public static string GetFloderName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // 规范化
        path = FormatToUnityPath(path).TrimEnd('/');

        // 如果是文件路径，取其目录
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (string.IsNullOrEmpty(path))
            return string.Empty;

        path = FormatToUnityPath(path).TrimEnd('/');
        return Path.GetFileName(path);
    }

    /// <summary>
    /// 获取目录下的文件列表（返回 FileInfo[]）。
    /// 支持传入 Unity 形式的 Assets/ 开头路径或绝对路径。
    /// </summary>
    public static FileInfo[] GetFiles(string dirPath, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (string.IsNullOrEmpty(dirPath))
            return new FileInfo[0];

        string fullPath = dirPath;
        // 支持 Asset 相对路径（以 Assets 开头）
        if (dirPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            fullPath = dirPath.Replace("Assets", Application.dataPath);
        }

        fullPath = FormatToUnityPath(fullPath);

        if (!Directory.Exists(fullPath))
            return new FileInfo[0];

        try
        {
            var dir = new DirectoryInfo(fullPath);
            return dir.GetFiles(searchPattern, searchOption);
        }
        catch (Exception)
        {
            return new FileInfo[0];
        }
    }

    /// <summary>
    /// 获取目录下的子目录（返回 DirectoryInfo[]）。
    /// 支持传入 Unity 形式的 Assets/ 开头路径或绝对路径。
    /// </summary>
    public static DirectoryInfo[] GetDirs(string dirPath, SearchOption option = SearchOption.TopDirectoryOnly)
    {
        if (string.IsNullOrEmpty(dirPath))
            return new DirectoryInfo[0];

        string fullPath = dirPath;
        if (dirPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            fullPath = dirPath.Replace("Assets", Application.dataPath);
        }

        fullPath = FormatToUnityPath(fullPath);

        if (!Directory.Exists(fullPath))
            return new DirectoryInfo[0];

        try
        {
            var dir = new DirectoryInfo(fullPath);
            return dir.GetDirectories();
        }
        catch (Exception)
        {
            return new DirectoryInfo[0];
        }
    }

    /// <summary>
    /// 确保目录存在（不存在则创建）。
    /// </summary>
    public static void EnsureDirectory(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath))
            return;
        string fullPath = dirPath;
        if (dirPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            fullPath = dirPath.Replace("Assets", Application.dataPath);
        fullPath = FormatToUnityPath(fullPath);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }
}
