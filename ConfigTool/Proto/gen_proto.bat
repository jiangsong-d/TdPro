@echo off

..\Tools\protoc\bin\protoc.exe --csharp_out=../../Assets/HotUpdate/Protoc --proto_path=proto proto/*.proto

pause
