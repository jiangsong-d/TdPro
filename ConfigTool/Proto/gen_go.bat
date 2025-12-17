@echo off

set PATH=%PATH%;..\Tools\protoc\bin
..\Tools\protoc\bin\protoc.exe --go_out=..\..\TdSever\proto --go_opt=paths=source_relative --proto_path=proto proto/*.proto

pause
