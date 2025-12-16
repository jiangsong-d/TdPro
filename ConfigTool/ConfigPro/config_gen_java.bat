set WORKSPACE=..

set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll

set CONF_ROOT=%~dp0

dotnet %LUBAN_DLL% ^
    -t all ^
    -c java-json ^
    -d json  ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=E:\petServer\game_server\src\main\java\cfg ^
    -x outputDataDir=E:\petServer\game_server\gameConfig ^
	-x tableImporter.valueTypeNameFormat=Data{0}
	
pause