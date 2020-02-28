set VER=2.3.1

del CNCMaps_*.zip
del CNCMaps_setup_*.exe
del CNCMaps/bin/*.*
del CNCMaps/obj/*.*
del CNCMaps GUI/obj/*.*
del CNCMaps GUI/obj/*.*

set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
REM MSBUILD="%PROGRAMFILES(X86)%\MSBuild\14.0\Bin\msbuild.exe"
set MAKENSIS="%PROGRAMFILES(X86)%\nsis\makensis.exe"

%MSBUILD% CNCMaps.sln /p:Configuration=Release
%MAKENSIS% nsisinstaller-rls.nsi

call collect_generated.bat

pause
exit



%MSBUILD% CNCMaps.sln /p:Configuration=Debug
%MAKENSIS% nsisinstaller-dbg.nsi

