@echo off
chcp 65001 >nul

REM Protobuf 协议生成脚本 (C# for Unity)
REM 生成 C# 代码供Unity使用

echo =========================================
echo   塔防游戏 - Unity C# Protobuf 生成脚本
echo =========================================

REM 检查 protoc 是否安装
where protoc >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo 错误: 未安装 protoc
    echo 安装方法: https://grpc.io/docs/protoc-installation/
    echo 或下载: https://github.com/protocolbuffers/protobuf/releases
    pause
    exit /b 1
)

echo protoc 版本:
protoc --version

REM 设置路径
set PROTO_DIR=proto
set OUTPUT_DIR=..\..\..\Assets\HotUpdate\Network\Proto

REM 创建输出目录
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

REM 生成 C# 代码
echo.
echo 正在生成 C# 代码...
protoc --csharp_out=%OUTPUT_DIR% %PROTO_DIR%/tower_defense.proto

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ C# 代码生成成功！
    echo 输出目录: %OUTPUT_DIR%
    echo.
    echo 请确保 Unity 项目已安装 Google.Protobuf 包
    echo 安装方法: 在 Unity Package Manager 中添加
    echo   com.google.protobuf
) else (
    echo.
    echo ❌ 生成失败
    pause
    exit /b 1
)

echo.
echo =========================================
echo   完成
echo =========================================
pause
