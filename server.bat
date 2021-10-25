@echo off

:: --HAS ENDING BACKSLASH
set batdir=%~dp0
:: --MISSING ENDING BACKSLASH
:: set batdir=%CD%
pushd "%batdir%"
dotnet msbuild ./project/FPS.sln /restore /t:Build /p:Configuration=Debug /v:normal /p:GodotTargetPlatform=windows
editor\godot.windows.opt.tools.64.mono.exe --path ./project -server