pushd "%~dp0\LogChipperSetup\Release"

:: msiexec /i "LogChipperSetup.msi" EDITA1="C:\Temp\test.log" EDITA2="localhost" EDITA3="514" EDITA4="TCP" /qn

msiexec /i LogChipperSetup.msi /qn

pause
net start LogChipper