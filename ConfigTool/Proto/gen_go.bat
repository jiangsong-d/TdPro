@echo off
chcp 65001 >nul

REM Protobuf 协议生成脚本 (Go)
REM 生成 Go 代码

echo =========================================
echo   塔防游戏 - Go Protobuf 生成脚本
echo =========================================

REM 检查 protoc 是否安装
where protoc >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo 错误: 未安装 protoc
    echo 安装方法: https://grpc.io/docs/protoc-installation/
    pause
    exit /b 1
)

echo protoc 版本:
protoc --version

REM 安装 Go protobuf 插件
echo.
echo 正在检查 Go protobuf 插件...
where protoc-gen-go >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo 安装 protoc-gen-go...
    go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
)

REM 创建输出目录
set PROTO_DIR=proto
set OUTPUT_DIR=..\..\..\TowerDefenseServer\proto

if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

REM 生成 Go 代码
echo.
echo 正在生成 Go 代码...
protoc --go_out=%OUTPUT_DIR% --go_opt=paths=source_relative %PROTO_DIR%/tower_defense.proto

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Go 代码生成成功！
    echo 输出目录: %OUTPUT_DIR%
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
