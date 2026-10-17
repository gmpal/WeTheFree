# Prepares an unlocked Oculus Go as a kiosk in one go:
# install the APK, copy the videos, then launch the kiosk once so it autostarts on every later boot.
# Do Wi-Fi and the standby timer in the Oculus menus BEFORE running this; the kiosk hides those menus.
#
# Usage: .\provision.ps1 -VideoDir C:\Users\gmpal\Videos\wtf\go_leveled
param(
    [Parameter(Mandatory)] [string]$VideoDir,
    [string]$Apk = "$PSScriptRoot\..\GoKiosk\Build\GoKiosk.apk"
)
$ErrorActionPreference = 'Stop'
$pkg = 'com.wtf.gokiosk'
$dest = "/sdcard/Android/data/$pkg/files"

function Invoke-Adb { & adb.exe @args; if ($LASTEXITCODE) { throw "adb $args failed" } }

$devices = @(adb devices | Select-String '\tdevice$')
if ($devices.Count -ne 1) { throw "Connect exactly one headset (found $($devices.Count))." }

Write-Host 'Installing kiosk...'
Invoke-Adb install -r $Apk

Write-Host 'Copying videos...'
Invoke-Adb shell mkdir -p $dest
foreach ($v in Get-ChildItem $VideoDir -Filter *.mp4) {
    Invoke-Adb push $v.FullName "$dest/$($v.Name)"
}

# A fresh install is in Android's "stopped" state and ignores BOOT_COMPLETED until launched once.
Write-Host 'Launching kiosk once (enables autostart)...'
Invoke-Adb shell monkey -p $pkg -c android.intent.category.LAUNCHER 1 | Out-Null

Write-Host 'Done. The kiosk is running and will start on every boot.'
