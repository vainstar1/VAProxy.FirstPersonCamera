@echo off
setlocal
set CONFIG=Release
set TFM=net48
set PROJECT_NAME=VAProxy.FirstPersonCamera
set OUT_DIR=bin\%CONFIG%\%TFM%
set TARGET_DLL=VAProxy.FirstPersonCamera.dll
set MOD_DEST=C:\Program Files (x86)\Steam\steamapps\common\VA Proxy Demo\BepInEx\plugins
dotnet build "%PROJECT_NAME%.csproj" -c %CONFIG%
if %errorlevel% neq 0 exit /b 1
if not exist "%MOD_DEST%" mkdir "%MOD_DEST%"
copy "%OUT_DIR%\%TARGET_DLL%" "%MOD_DEST%\" /Y
