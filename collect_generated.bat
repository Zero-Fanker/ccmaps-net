mkdir CNCMapBin
set outputPath=".\CNCMapBin"

for /r %%f in (*.dll *.exe) do (
    copy  %%f %outputPath%
)

pause