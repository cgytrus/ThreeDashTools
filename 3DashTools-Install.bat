@echo off
:: ___________________________________________________________________________
:: 3DashTools Installer
set version=1.0
:: Made by Hassunaama
:: Copyright (c) 2022 Hassunaama
:: ___________________________________________________________________________

if not "%os%"=="Windows_NT" goto not_windows_nt
set title=3DashTools Installer v%version% Made by Hassunaama


title %title%
goto check_Permissions

:bepinex_install
call powershell -command Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine >nul
call powershell -command (new-object System.Net.WebClient).DownloadFile('https://gist.githubusercontent.com/cgytrus/29085a6bf179893666316a36e1c92bf6/raw/bepinex-installer.ps1', 'bepinex-installer.ps1') >nul
powershell .\bepinex-installer.ps1 >nul
del /f /q bepinex-installer.ps1
set /a progress=3
goto install_5

:6dash_install
call powershell -command (new-object System.Net.WebClient).DownloadFile('https://github.com/cgytrus/SixDash/releases/download/v0.3.0/SixDash-0.3.0.zip', 'sixdash.zip') >nul
powershell -command "Expand-Archive -Force '%~dp0sixdash.zip' '%~dp0'"
del /f /q sixdash.zip
set /a progress=5
goto install_5

:the_install
call powershell -command (new-object System.Net.WebClient).DownloadFile('https://github.com/cgytrus/ThreeDashTools/releases/download/v0.2.0/ThreeDashTools-0.2.0.zip', 'threedashtools.zip') >nul
powershell -command "Expand-Archive -Force '%~dp0threedashtools.zip' '%~dp0'"
del /f /q threedashtools.zip
set /a progress=8
goto install_5

:final
set /a progress=9
PING localhost -n 4 >NUL
goto install_5

:not_windows_nt
cls
echo.
echo What are you doing?
echo 3Dash doesent work with 9x or dos :D
echo stop.
echo.
pause
exit
goto not_windows_nt

:check_Permissions
net session >nul 2>&1
if %errorLevel% == 0 (
    cd %~dp0
    goto begin_main
) else (
    echo Error: Administrator permissions needed.
    pause
    exit
)
:begin_main
cls
echo Welcome to the 3DashTools Installer.
goto install_1


:install_1
PING localhost -n 2 >NUL
echo.
echo The Installer will prepare all the files you need.
echo Meanwhile, you can minimize this window and do something else,
echo for example, watch a Youtube video.

:install_2
PING localhost -n 4 >NUL
echo.
echo User interraction won't be needed, so you can relax and let me do the work! :)
goto install_4

:install_4
PING localhost -n 4 >NUL
set /a progress=0
goto install_5

:install_5
cls
set current_time=%time:~0,5%
if /i "%current_time%" GEQ " 5:00" if /i "%current_time%" LSS "13:00" echo Good morning %username%! Welcome to the 3DashTools Installer.
if /i "%current_time%" GEQ "13:00" if /i "%current_time%" LSS "18:00" echo Good afternoon %username%! Welcome to the 3DashTools Installer.
if /i "%current_time%" GEQ "18:00" if /i "%current_time%" LEQ "23:59" echo Good evening %username%! Welcome to the 3DashTools Installer.
if /i "%current_time%" GEQ " 0:00" if /i "%current_time%" LSS " 5:00" echo Good evening %username%! Welcome to the 3DashTools Installer.
echo.
echo The Installer will prepare all the files you need.
echo Meanwhile, you can minimize this window and do something else,
echo for example, watch a Youtube video.
echo.
echo User interraction won't be needed, so you can relax and let me do the work! :)
echo.
echo Installing...
echo.
echo Progress:
if %progress%==0 echo ^|           
if %progress%==1 echo -          
if %progress%==2 echo --         
if %progress%==3 echo ---        
if %progress%==4 echo ----       
if %progress%==5 echo -----      
if %progress%==6 echo ------     
if %progress%==7 echo -------    
if %progress%==8 echo --------   
if %progress%==9 echo ---------  
if %progress%==10 echo ----------
echo.
echo This will take some time...

if "%progress%"=="0" goto bepinex_install
if "%progress%"=="3" goto 6dash_install
if "%progress%"=="5" goto the_install
if "%progress%"=="8" goto final
if "%progress%"=="9" goto end

goto install_5
:end
cls
echo The installer is ready
echo.
pause
exit
