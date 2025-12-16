set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0

set UNITY_WORKSPACE=../..
set LUBAN_MUITL_TOOL=%WORKSPACE%\Tools\LubanMultiLangTool\LuBanMutilLangConvert.exe
set CONFIG_PATH=%CONF_ROOT%/Configs/
set CODE_PATH=%UNITY_WORKSPACE%/Assets/HotUpdate/HotScripts/Config
set LANGUAGE_READ_TEMPLATE=%WORKSPACE%\Tools\LubanMultiLangTool\language_read_templete_gen_key.txt

rd /s /q %CODE_PATH%
mkdir %CODE_PATH%

%LUBAN_MUITL_TOOL% -genLanguage true -configPath %CONFIG_PATH% -codePath %CODE_PATH% -genLanguageKeyTemplateFile %LANGUAGE_READ_TEMPLATE%

del %CONF_ROOT%\Configs\#language.xls
copy %CONF_ROOT%\Trans.xls %CONF_ROOT%\Configs\#language.xls

dotnet %LUBAN_DLL% ^
	-t all ^
    -c cs-bin^
    -d bin ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CODE_PATH%^
    -x outputDataDir=..\..\Assets\Res\Config^
    -x tableImporter.valueTypeNameFormat=Data{0}
	
%LUBAN_MUITL_TOOL% -genLanguage false -configPath %CONFIG_PATH% -codePath %CODE_PATH% -genLanguageKeyTemplateFile %LANGUAGE_READ_TEMPLATE%

pause