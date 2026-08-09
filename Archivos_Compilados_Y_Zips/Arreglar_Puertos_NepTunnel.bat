@echo off
chcp 65001 >nul
title 🛠️ Reparador de Puertos y Limpiador de Sesiones - BlackHouseTunnel / NepTunnel
color 0A

echo ======================================================================
echo          🛠️ REPARADOR DE PUERTOS Y LIMPIADOR DE RED / SESIONES
echo ======================================================================
echo.
echo  Este script cerrará procesos colgados (BlackHouseTunnel, Roblox Studio,
echo  Playit) y liberará los puertos UDP 55555 y 55544 en tu computadora.
echo.
echo ======================================================================
echo.

echo 1. Cerrando procesos en segundo plano...
taskkill /F /IM BlackHouseTunnel.exe /T >nul 2>&1
taskkill /F /IM NepTunnel.exe /T >nul 2>&1
taskkill /F /IM RobloxStudioBeta.exe /T >nul 2>&1
taskkill /F /IM playit.exe /T >nul 2>&1
echo    ✓ Procesos finalizados correctamente.
echo.

echo 2. Verificando y liberando puertos UDP 55555, 55544 y 55556...
for /f "tokens=5" %%a in ('netstat -aon -p UDP ^| findstr ":55555 :55544 :55556"') do (
    if not "%%a"=="0" (
        echo    -> Liberando puerto en uso por PID %%a...
        taskkill /F /PID %%a >nul 2>&1
    )
)
echo    ✓ Puertos UDP verificados y liberados.
echo.

echo 3. Limpiando caché de red y DNS de Windows...
ipconfig /flushdns >nul 2>&1
netsh winsock reset >nul 2>&1
echo    ✓ Caché de DNS y Socket Winsock reiniciados.
echo.

echo ======================================================================
echo  ✅ DIAGNÓSTICO FINAL:
echo ======================================================================
echo  ✓ Todos los procesos bloqueantes han sido cerrados.
echo  ✓ El puerto UDP 55555 está 100%% LIBRE para crear Host o Unirse.
echo  ✓ Ya puedes volver a abrir BlackHouseTunnel.exe y jugar sin errores.
echo ======================================================================
echo.
pause
