set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0

set UNITY_WORKSPACE=../..
set LUBAN_MUITL_TOOL=%WORKSPACE%\Tools\LubanMultiLangTool\LuBanMutilLangConvert.exe
set CONFIG_PATH=%CONF_ROOT%/Configs/
set CODE_PATH=%UNITY_WORKSPACE%/Assets/HotUpdate/HotScripts/Config
set LANGUAGE_READ_TEMPLATE=%WORKSPACE%\Tools\LubanMultiLangTool\language_read_templete_gen_key.txt

%LUBAN_MUITL_TOOL% -genLanguage true -configPath %CONFIG_PATH% -codePath %CODE_PATH% -genLanguageKeyTemplateFile %LANGUAGE_READ_TEMPLATE%

pause