@echo off
setlocal

rem Public demo for synthetic text data generation.
rem Results are written to ..\LocalArtifacts\GenData.

cd /d "%~dp0"
dotnet run -c Release --project ..\src\ObbTextGenerator\ObbTextGenerator.csproj -- --config-file config_text.yaml
