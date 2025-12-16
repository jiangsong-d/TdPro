#!/bin/bash

# Linux/Mac Protobuf 生成脚本 (C#)

echo "========================================="
echo "  塔防游戏 - Unity C# Protobuf 生成脚本"
echo "========================================="

# 检查 protoc 是否安装
if ! command -v protoc &> /dev/null; then
    echo "错误: 未安装 protoc"
    echo "安装方法: brew install protobuf"
    exit 1
fi

echo "protoc 版本: $(protoc --version)"

# 设置路径
PROTO_DIR="proto"
OUTPUT_DIR="../../../Assets/HotUpdate/Network/Proto"

# 创建输出目录
mkdir -p $OUTPUT_DIR

# 生成 C# 代码
echo ""
echo "正在生成 C# 代码..."
protoc --csharp_out=$OUTPUT_DIR $PROTO_DIR/tower_defense.proto

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ C# 代码生成成功！"
    echo "输出目录: $OUTPUT_DIR"
    echo ""
    echo "请确保 Unity 项目已安装 Google.Protobuf 包"
else
    echo ""
    echo "❌ 生成失败"
    exit 1
fi

echo ""
echo "========================================="
echo "  完成"
echo "========================================="
