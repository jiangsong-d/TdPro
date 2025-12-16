using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using YooAsset.Editor;
using BuildReport = UnityEditor.Build.Reporting.BuildReport;
using HybridCLR.Editor.Commands;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using YooAsset;
// 明确引用AesEncryptionServices类
// 若类在Assets/Editor下且无命名空间，直接new即可，无需using
// 引入自定义AES加密类
// 需确保 AesEncryptionServices.cs 在同一命名空间或全局可见

public static class BuildPathHelper
{
    /// <summary>
    /// 获取资源包名称（图片显示为"DefaultPackage"）
    /// </summary>
    public static string GetPackageName() => "DefaultPackage";

    /// <summary>
    /// 获取构建输出目录（Unity生成的中间文件）
    /// </summary>
    public static string GetBuildOutputPath() => Path.Combine(Application.dataPath, "Library/YooAssetBuild", GetPackageName());

    /// <summary>
    /// 获取内置资源目录（StreamingAssets下）
    /// 匹配实际的Yoo文件夹名称（大写Y）
    /// </summary>
    public static string GetBuildinFilePath() => Path.Combine(Application.streamingAssetsPath, "yoo");

    /// <summary>
    /// 获取热更新DLL输出目录（Assets/HotCodeDll）
    /// 与图片中的目录位置一致
    /// </summary>
    public static string GetHotUpdateDllDir() => Path.Combine(Application.dataPath, "HotCodeDll");

    /// <summary>
    /// 获取AOT元数据目录（StreamingAssets/Il2CppMetadata）
    /// 与图片中的目录位置一致
    /// </summary>
    public static string GetAotMetadataDir() => Path.Combine(Application.streamingAssetsPath, "Il2CppMetadata");
}

public static class BuildPipeline
{
    private const string OFFLINE_MODE_SYMBOL = "RESOURCE_OFFLINE";      // 离线资源符号
    private const string ASSETBUNDLE_MODE_SYMBOL = "RESOURCE_ASSETBUNDLE"; // AssetBundle资源符号

    private static string apkName;

    //版本号文件路径
    private static string VersionFilePath => Path.Combine(Application.dataPath, "version.txt");

    public static bool isBuildApk = false; // 是否构建APK

    // 菜单：构建内部测试全量包
    [MenuItem("YooAsset/构建/内部测试全量包")]
    public static void BuildInternalTestPackage()
    {
        Debug.Log("开始构建内部测试全量包...");
        Debug.Log("步骤1/4: 设置离线模式宏");
        isBuildApk = true; // 设置为构建APK
        apkName = "InternalTest";
        SetScriptingDefineSymbol(OFFLINE_MODE_SYMBOL); // 设置编译符号为离线模式
        Debug.Log("步骤2/4: 构建全量资源包");
        BuildFullPackage();                            // 构建全量资源包
        Debug.Log("步骤3/4: 构建APK");
        BuildPlayer(true);                             // 构建包含所有资源的APK
        Debug.Log("========== 全量包构建完成 ==========");
        
    }

    // 构建支持热更的全量包
    [MenuItem("YooAsset/构建/支持热更的全量包")]
    public static void BuildFullPackageWithHotUpdate()
    {
        Debug.Log("开始构建支持热更的全量包...");
        isBuildApk = true; // 构建APK
        apkName = "FullRelease";
        // 使用 AssetBundle 模式符号（或不使用离线模式），确保运行时为 HostPlayMode 支持热更
        SetScriptingDefineSymbol(ASSETBUNDLE_MODE_SYMBOL);
        Debug.Log("步骤2/4: 构建全量资源到 StreamingAssets（用于首次安装）");
        BuildFullPackage(); // 复用全量包构建逻辑（会把资源复制到 StreamingAssets）
        Debug.Log("步骤3/4: 构建APK");
        BuildPlayer(true);
        Debug.Log("========== 支持热更的全量包构建完成 ==========");
    }

    // 菜单：构建发布版APK
    [MenuItem("YooAsset/构建/发布版APK")]
    public static void BuildReleasePackage()
    {
        Debug.Log("开始构建发布版APK...");
        apkName = "Release";
        isBuildApk = true; // 设置为构建APK
        SetScriptingDefineSymbol(ASSETBUNDLE_MODE_SYMBOL); // 设置编译符号为AssetBundle模式
        BuildEssentialPackage();                           // 构建核心资源包
        BuildPlayer(false);                                // 构建仅包含核心资源的APK
    }

    // 菜单：构建热更新资源包
    [MenuItem("YooAsset/构建/热更新资源包")]
    public static void BuildHotfixPackage()
    {
        Debug.Log("开始构建热更新资源包...");
        isBuildApk = false; // 设置为不构建APK
        BuildIncrementalPackage(); // 构建热更资源包
    }

    /// <summary>
    /// 设置编译符号（宏定义），用于区分不同构建模式
    /// </summary>
    private static void SetScriptingDefineSymbol(string symbol)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup)
            .Split(';')
            .ToList();

        // 移除已有的相关符号，避免重复
        symbols.Remove(OFFLINE_MODE_SYMBOL);
        symbols.Remove(ASSETBUNDLE_MODE_SYMBOL);

        // 添加目标符号
        if (!symbols.Contains(symbol))
        {
            symbols.Add(symbol);
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbols));
        Debug.Log($"已设置编译符号: {symbol}");
    }

    /// <summary>
    /// 构建全量资源包
    /// </summary>
    private static void BuildFullPackage()
    {
        PrepareBuildEnvironment(); // 清理并准备构建环境
        string currentVersion = GetVersion("apk");
        string newVersion = GetNextVersion(currentVersion, true);
        var buildParams = new ScriptableBuildParameters
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            BuildOutputRoot = BuildPathHelper.GetBuildOutputPath(),
            BuildinFileRoot = BuildPathHelper.GetBuildinFilePath(),
            BuildPipeline = nameof(ScriptableBuildPipeline),
            PackageName = BuildPathHelper.GetPackageName(),
            PackageVersion = newVersion, // 修订号升级
            BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll, // 只复制指定标签的资源
            BuildBundleType = (int)EBuildBundleType.AssetBundle, // 核心资源包
            EnableSharePackRule = true,
            CompressOption = ECompressOption.LZ4,
            DisableWriteTypeTree = false // 全量包不禁用TypeTree写入// 全量包不禁用TypeTree写入
        };
        Debug.Log($"准备构建");
        ExecuteBuild(buildParams, "全量资源包");
        if (apkName == "FullRelease")
        { 
            cleanResSever(); // 清理远端资源服务器
            RunSync(); // 执行资源同步脚本
        }
        SetVersion("apk", newVersion);
    }

    /// <summary>
    /// 构建核心资源包（用于发布版）
    /// </summary>
    private static void BuildEssentialPackage()
    {
        PrepareBuildEnvironment();

        string currentVersion = GetVersion("apk");
        string newVersion = GetNextVersion(currentVersion, true);

        var buildParams = new ScriptableBuildParameters
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            BuildOutputRoot = BuildPathHelper.GetBuildOutputPath(),
            BuildinFileRoot = BuildPathHelper.GetBuildinFilePath(),
            BuildPipeline = nameof(ScriptableBuildPipeline),
            PackageName = BuildPathHelper.GetPackageName(),
            PackageVersion = newVersion, // 修订号升级
            BuildinFileCopyOption = EBuildinFileCopyOption.None, // 只复制指定标签的资源
            BuildBundleType = (int)EBuildBundleType.AssetBundle, // 核心资源包
            EnableSharePackRule = true,
            CompressOption = ECompressOption.LZ4,
            DisableWriteTypeTree = false
        };
        ExecuteBuild(buildParams, "核心资源包");
        cleanResSever(); // 清理远端资源服务器
        RunSync(); // 执行资源同步脚本
        SetVersion("apk", newVersion);
    }

    /// <summary>
    /// 构建热更新资源包
    /// </summary>
    private static void BuildIncrementalPackage()
    {
        PrepareBuildEnvironment();

        // 读取并递增hotfix版本号
        string currentVersion = GetVersion("apk");
        string newVersion = GetNextVersion(currentVersion, false);

        var buildParams = new ScriptableBuildParameters
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            BuildOutputRoot = BuildPathHelper.GetBuildOutputPath(),
            BuildinFileRoot = BuildPathHelper.GetBuildinFilePath(),
            BuildPipeline = nameof(ScriptableBuildPipeline),
            PackageName = BuildPathHelper.GetPackageName(),
            PackageVersion = newVersion, // 修订号升级
            BuildinFileCopyOption = EBuildinFileCopyOption.None,
            BuildBundleType = (int)EBuildBundleType.AssetBundle,
            EnableSharePackRule = true,
            CompressOption = ECompressOption.LZ4,
            DisableWriteTypeTree = false
        };
        ExecuteBuild(buildParams, "热更新资源包");
        RunSync(); // 执行资源同步脚本
        SetVersion("apk", newVersion);
    }

    /// <summary>
    /// 构建前的准备工作：
    /// 1. 清理输出目录
    /// 2. 生成HybridCLR所需的DLL
    /// 3. 拷贝AOT和热更DLL到指定位置
    /// </summary>
    private static void PrepareBuildEnvironment()
    {
        Debug.Log("开始构建环境准备...");

        // 1. 清理构建缓存目录
        string buildOutputPath = BuildPathHelper.GetBuildOutputPath();
        if (isBuildApk && Directory.Exists(buildOutputPath))
        {
            Directory.Delete(buildOutputPath, true);
            Debug.Log($"已清理构建缓存: {buildOutputPath}");
        }

        // 2. 清理热更DLL目录（Assets/HotCodeDll）
        string hotUpdateDllDir = BuildPathHelper.GetHotUpdateDllDir();
        if (Directory.Exists(hotUpdateDllDir))
        {
            Directory.Delete(hotUpdateDllDir, true);
            Debug.Log($"已清理热更DLL目录: {hotUpdateDllDir}");
        }

        // 3. 清理内置资源目录（StreamingAssets/DefaultPackage）
        string streamingAssetsDir = BuildPathHelper.GetBuildinFilePath();
        if (Directory.Exists(streamingAssetsDir))
        {
            Directory.Delete(streamingAssetsDir, true);
            Debug.Log($"已清理StreamingAssets资源目录: {streamingAssetsDir}");
        }

        // 4. 特别注意：不清理AotMetadataDir（Il2CppMetadata）目录
        // 避免删除之前生成的AOT元数据DLL

        // 确保目录刷新
        AssetDatabase.Refresh();

        // 5. 生成HybridCLR所需的DLL
        Debug.Log("生成HybridCLR热更DLL和AOT元数据DLL...");
        PrebuildCommand.GenerateAll();

        // 6. 处理DLL文件：智能拷贝
        ProcessDllFiles();

        Debug.Log("构建环境准备完成 ✅");
    }

    private static List<string> GetAotFilsName()
    {
        return new List<string>
        {
            "DOTween.dll",
            "GameFrameX.Network.Runtime.dll",
            "GameFrameX.Runtime.dll",
            "Luban.Runtime.dll",
            "Newtonsoft.Json.dll",
            "ProtoBuffer.Runtime.dll",
            "System.Core.dll",
            "System.dll",
            "UniTask.dll",
            "UnityEngine.AndroidJNIModule.dll",
            "UnityEngine.CoreModule.dll",
            "mscorlib.dll",
            "spine-csharp.dll",
        };
    }
    /// <summary>
    /// 智能拷贝HybridCLR生成的DLL文件：
    /// 1. AOT元数据DLL → StreamingAssets/Il2CppMetadata
    /// 2. 热更新DLL → Assets/HotCodeDll
    /// 根据HybridCLR设置只拷贝必要的AOT DLL
    /// </summary>
    private static void ProcessDllFiles()
    {
        Debug.Log("开始处理DLL文件...");

        // ================== 处理AOT元数据DLL ==================
        // 获取HybridCLR生成的AOT元数据DLL源目录
        string aotSourceDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(
        EditorUserBuildSettings.activeBuildTarget);

        Debug.Log($"AOT元数据源目录: {aotSourceDir}");

        // 目标目录：StreamingAssets/Il2CppMetadata
        string aotTargetDir = BuildPathHelper.GetAotMetadataDir();
        Debug.Log($"AOT元数据目标目录: {aotTargetDir}");

        // 确保目标目录存在
        if (!Directory.Exists(aotTargetDir))
        {
            Directory.CreateDirectory(aotTargetDir);
            Debug.Log($"已创建AOT元数据目录: {aotTargetDir}");
        }

        // 获取项目中实际需要补充元数据的AOT DLL列表
        List<string> neededAotAssemblies = GetAotFilsName();
        // 拷贝并过滤AOT元数据DLL
        int copiedAotCount = 0;
        foreach (string dllPath in Directory.GetFiles(aotSourceDir, "*.dll"))
        {
            string fileName = Path.GetFileName(dllPath);

            // 智能过滤：只拷贝需要的AOT DLL
            if (neededAotAssemblies.Count > 0 && !neededAotAssemblies.Contains(fileName))
            {
                Debug.Log($"跳过不需要的AOT DLL: {fileName}");
                continue;
            }

            // 目标路径（保持.dll扩展名）
            string targetPath = Path.Combine(aotTargetDir, fileName);
            File.Copy(dllPath, targetPath, true);
            copiedAotCount++;
            Debug.Log($"已拷贝AOT元数据: {fileName} → {targetPath}");
        }
        Debug.Log($"完成AOT元数据拷贝，共 {copiedAotCount} 个文件");

        // ================== 处理热更新DLL ==================
        // 获取HybridCLR生成的热更新DLL源目录
        string hotUpdateSourceDir = HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(
            EditorUserBuildSettings.activeBuildTarget);

        Debug.Log($"热更新DLL源目录: {hotUpdateSourceDir}");

        // 目标目录：Assets/HotCodeDll
        string hotUpdateTargetDir = BuildPathHelper.GetHotUpdateDllDir();
        Debug.Log($"热更新DLL目标目录: {hotUpdateTargetDir}");

        // 确保目标目录存在
        if (!Directory.Exists(hotUpdateTargetDir))
        {
            Directory.CreateDirectory(hotUpdateTargetDir);
            Debug.Log($"已创建热更新DLL目录: {hotUpdateTargetDir}");
        }

        // 拷贝所有热更新DLL并添加.bytes扩展名
        int copiedHotfixCount = 0;
        foreach (string dllPath in Directory.GetFiles(hotUpdateSourceDir, "*.dll"))
        {
            string fileName = Path.GetFileName(dllPath);
            string targetPath = Path.Combine(hotUpdateTargetDir, $"{fileName}.bytes");
            ///只拷热更新程序集
            if (fileName == "HotScripts.dll")
            { File.Copy(dllPath, targetPath, true); }
            copiedHotfixCount++;
            Debug.Log($"已拷贝热更新DLL: {fileName} → {targetPath}");
        }
        Debug.Log($"完成热更新DLL拷贝，共 {copiedHotfixCount} 个文件");

        // 确保Unity刷新AssetDatabase
        AssetDatabase.Refresh();
        Debug.Log("DLL处理完成 ✅");
    }

    /// <summary>
    /// 执行资源包构建流程
    /// </summary>
    private static void ExecuteBuild(ScriptableBuildParameters buildParams, string buildName)
    {
        Debug.Log($"开始执行 {buildName} 构建...");
        Debug.Log($"构建参数: 目标平台={buildParams.BuildTarget}, 版本={buildParams.PackageVersion}");

        var pipeline = new ScriptableBuildPipeline();
        var result = pipeline.Run(buildParams, false);

        Debug.Log($"YooAsset构建完成: {(result.Success ? "成功 ✅" : "失败 ❌")}");

        if (result.Success)
        {
            Debug.Log($"✅ {buildName}构建成功 | 版本: {buildParams.PackageVersion}");

            // 资源验证和日志
            AssetDatabase.Refresh();
            string targetDir = buildParams.BuildinFileRoot;

            if (Directory.Exists(targetDir))
            {
                var files = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);
                Debug.Log($"构建成功! 资源文件数量: {files.Length}");

                if (files.Length > 0)
                {
                    Debug.Log("资源文件示例:");
                    foreach (var file in files.Take(3))
                    {
                        Debug.Log($"- {file.Replace(Application.dataPath, "Assets")}");
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"❌ {buildName}构建失败，请检查错误日志");
            throw new Exception($"{buildName} 构建失败，已中断后续流程！");
        }
    }

    /// <summary>
    /// 构建APK包
    /// </summary>
    /// <param name="includeAllResources">true=全量包，false=核心包</param>
    private static void BuildPlayer(bool includeAllResources)
    {
        Debug.Log("开始APK构建...");
        Debug.Log($"构建类型: {(includeAllResources ? "全量包" : "核心包")}");

        // 读取apk版本号
        string currentVersion = GetVersion("apk");


        // 打印StreamingAssets文件信息（用于调试）
        string streamingDir = BuildPathHelper.GetBuildinFilePath();
        if (Directory.Exists(streamingDir))
        {
            var files = Directory.GetFiles(streamingDir, "*", SearchOption.AllDirectories);
            Debug.Log($"StreamingAssets文件数: {files.Length}");
        }

        // 确保资源刷新
        AssetDatabase.Refresh();

        // 生成APK名称
        string buildType = apkName;
        string outputPath = $"Build/Android/{buildType}_{currentVersion}.apk";

        // 配置构建选项
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        // 执行构建
        BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"✅ APK构建成功: {outputPath}");
            Debug.Log($"文件大小: {report.summary.totalSize / (1024 * 1024)} MB");
        }
        else
        {
            throw new Exception($"APK构建失败: {report.summary.result}");
        }
    }
    /// <summary>
    /// 读取所有版本号（如apk=1.2.3, hotfix=1.2.7）
    /// </summary>
    private static Dictionary<string, string> ReadAllVersions()
    {
        var dict = new Dictionary<string, string>();
        if (!File.Exists(VersionFilePath))
            return dict;
        foreach (var line in File.ReadAllLines(VersionFilePath))
        {
            var parts = line.Split('=');
            if (parts.Length == 2)
                dict[parts[0].Trim()] = parts[1].Trim();
        }
        return dict;
    }
    private static string GetVersion(string key)
    {
        var dict = ReadAllVersions();
        return dict.ContainsKey(key) ? dict[key] : "1.0.0";
    }
    /// <summary>
    /// 写入所有版本号到version.txt
    /// </summary>
    private static void WriteAllVersions(Dictionary<string, string> dict)
    {
        var lines = dict.Select(kv => $"{kv.Key}={kv.Value}");
        File.WriteAllLines(VersionFilePath, lines);
    }

    /// <summary>
    /// 设置指定类型的版本号并写入文件
    /// </summary>
    private static void SetVersion(string key, string version)
    {
        var dict = ReadAllVersions();
        dict[key] = version;
        WriteAllVersions(dict);
    }

    /// <summary>
    /// 计算下一个版本号
    /// </summary>
    /// <param name="currentVersion">当前版本号</param>
    /// <param name="isFullBuild">true=次版本号+1，false=修订号+1</param>
    private static string GetNextVersion(string currentVersion, bool isFullBuild)
    {
        var match = Regex.Match(currentVersion, @"^(\d+)\.(\d+)\.(\d+)$");
        if (!match.Success)
            throw new Exception("版本号格式错误，应为 x.x.x");

        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int patch = int.Parse(match.Groups[3].Value);

        if (isFullBuild)
        {
            major++;
            minor = 0; // 全量包时次版本号归零
            patch = 0;
        }
        else
        {
            if (patch >= 99) // 假设修订号最大为999
            {
                patch = 0;
                minor++;
            }
            else
            {
                patch++;
            }
        }
        return $"{major}.{minor}.{patch}";
    }
    static void RunSync()
    {
        string gitBashPath = @"C:\Program Files\Git\bin\bash.exe";
        string scriptPath = Application.dataPath + "/sync_this_dir.sh";
        Debug.Log("脚本路径: " + scriptPath);

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gitBashPath,
                Arguments = $"--login -i \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string logContent = $"脚本执行完成，退出代码: {process.ExitCode}\n" +
                            $"标准输出:\n{output}\n" +
                            $"错误输出:\n{error}\n";

        // 保存到项目根目录下的 sync_log.txt
        string logPath = Path.Combine(Application.dataPath, "sync_log.txt");
        File.WriteAllText(logPath, logContent);

        Console.WriteLine(logContent);

        
        Console.WriteLine($"脚本执行完成，退出代码: {process.ExitCode}");
        Console.WriteLine("标准输出:\n" + output);
        Console.WriteLine("错误输出:\n" + error);
        
        Debug.Log("资源同步命令已执行");
    }
    static void cleanResSever()
    {
        // 获取脚本路径 Assets
        string gitBashPath = @"C:\Program Files\Git\bin\bash.exe";
        string scriptPath = Application.dataPath + "/clean_remote_dir.sh";
        Debug.Log("脚本路径: " + scriptPath);

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gitBashPath,
                Arguments = $"--login -i \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine($"脚本执行完成，退出代码: {process.ExitCode}");
        Console.WriteLine("标准输出:\n" + output);
        Console.WriteLine("错误输出:\n" + error);
        Debug.Log("远程服务器清理命令已执行");
    }
}