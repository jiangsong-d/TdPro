set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0
set GO_WORKSPACE=../../TdSever
set CONFIG_PATH=%CONF_ROOT%/Configs/
set GO_CODE_PATH=%GO_WORKSPACE%/config/tables
set GO_DATA_PATH=%GO_WORKSPACE%/data/configs

rd /s /q %GO_CODE_PATH%
mkdir %GO_CODE_PATH%

rd /s /q %GO_DATA_PATH%
mkdir %GO_DATA_PATH%

dotnet %LUBAN_DLL% ^
	-t server ^
    -c go-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%GO_CODE_PATH% ^
	-x outputDataDir=%GO_DATA_PATH%

echo.
echo ================================
echo Golang配置表代码生成完成！
echo 代码路径: %GO_CODE_PATH%
echo 数据路径: %GO_DATA_PATH%
echo ================================
echo.

pause
