set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0
set UNITY_WORKSPACE=../..
set GO_WORKSPACE=../../TdSever
set LUBAN_MUITL_TOOL=%WORKSPACE%\Tools\LubanMultiLangTool\LuBanMutilLangConvert.exe
set CONFIG_PATH=%CONF_ROOT%/Configs/
set CS_CODE_PATH=%UNITY_WORKSPACE%/Assets/HotUpdate/Config
set GO_CODE_PATH=%GO_WORKSPACE%/config/tables
set CS_DATA_PATH=%UNITY_WORKSPACE%/Assets/Res/Config
set GO_DATA_PATH=%GO_WORKSPACE%/data/configs
set LANGUAGE_READ_TEMPLATE=%WORKSPACE%\Tools\LubanMultiLangTool\language_read_templete_gen_key.txt

echo ================================
echo 正在生成客户端C#配置表...
echo ================================

rd /s /q %CS_CODE_PATH%
mkdir %CS_CODE_PATH%

dotnet %LUBAN_DLL% ^
	-t client ^
    -c cs-bin^
    -d bin ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CS_CODE_PATH%^
	-x outputDataDir=%CS_DATA_PATH%

echo.
echo ================================
echo 正在生成服务端Golang配置表...
echo ================================

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
echo 所有配置表生成完成！
echo C# 代码: %CS_CODE_PATH%
echo C# 数据: %CS_DATA_PATH%
echo Go 代码: %GO_CODE_PATH%
echo Go 数据: %GO_DATA_PATH%
echo ================================
echo.

rem %LUBAN_MUITL_TOOL% -genLanguage false -configPath %CONFIG_PATH% -codePath %CS_CODE_PATH% -genLanguageKeyTemplateFile %LANGUAGE_READ_TEMPLATE%

pause
