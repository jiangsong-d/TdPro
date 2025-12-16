@echo off

set java_code_path=E:\petServer\game_server\src\main\java

IF EXIST %java_code_path% (
	..\Tools\protoc\bin\protoc.exe --java_out=E:\petServer\game_server\src\main\java ./proto/*.proto
)

pause