#!/bin/bash

# Protobuf 协议生成脚本 (Go)
# 生成 Go 代码

echo "========================================="
echo "  塔防游戏 - Go Protobuf 生成脚本"
echo "========================================="

# 检查 protoc 是否安装
if ! command -v protoc &> /dev/null; then
    echo "错误: 未安装 protoc"
    echo "安装方法: https://grpc.io/docs/protoc-installation/"
    exit 1
fi

echo "protoc 版本: $(protoc --version)"

# 安装 Go protobuf 插件
echo ""
echo "正在检查 Go protobuf 插件..."
if ! command -v protoc-gen-go &> /dev/null; then
    echo "安装 protoc-gen-go..."
    go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
fi

# 创建输出目录
PROTO_DIR="proto"
OUTPUT_DIR="../../../TowerDefenseServer/proto"

mkdir -p $OUTPUT_DIR

# 生成 Go 代码
echo ""
echo "正在生成 Go 代码..."
protoc \
  --go_out=$OUTPUT_DIR \
  --go_opt=paths=source_relative \
  $PROTO_DIR/tower_defense.proto

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Go 代码生成成功！"
    echo "输出目录: $OUTPUT_DIR"
else
    echo ""
    echo "❌ 生成失败"
    exit 1
fi

echo ""
echo "========================================="
echo "  完成"
echo "========================================="
