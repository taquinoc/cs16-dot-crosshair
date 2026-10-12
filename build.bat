@echo off
cd /d "%~dp0"
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
echo Compilando MiraPontinho.exe ...
"%CSC%" /nologo /target:winexe /out:MiraPontinho.exe /win32icon:app.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll src\MiraPontinho.cs
if errorlevel 1 (
  echo.
  echo FALHA ao compilar.
) else (
  echo OK! Gerado MiraPontinho.exe
)
pause
