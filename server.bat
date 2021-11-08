@echo off

:: --HAS ENDING BACKSLASH
set batdir=%~dp0
:: --MISSING ENDING BACKSLASH
:: set batdir=%CD%
pushd "%batdir%"
dotnet msbuild ./project/FPS.sln /restore /t:Build /p:Configuration=Debug /v:normal /p:GodotTargetPlatform=windows
editor\godot.windows.opt.tools.64.mono.exe --low-dpi --fixed-fps 60 --print-fps --debug-collisions --debug-navigation --audio-driver Dummy --path ./project -server