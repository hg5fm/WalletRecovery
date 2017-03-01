@echo off
set msbuild=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe 

IF NOT EXIST %msbuild% goto error

%msbuild% WalletRecovery.sln 
goto end

:error
echo Please make sure that Microsoft .Net Framework 4 is installed! 
echo https://www.microsoft.com/net/download/framework

:end