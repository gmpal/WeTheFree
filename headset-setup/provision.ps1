# Prepares an unlocked Oculus Go as a kiosk in one go:
# install the APK, copy the videos, then launch the kiosk once so it autostarts on every later boot.
# Do Wi-Fi and the standby timer in the Oculus menus BEFORE running this; the kiosk hides those menus.
#
# Usage: .\provision.ps1 -VideoList .\belgium.txt
#   The list has one "CODE = path" line per language, in menu order (the first flag goes at the top):
#     ENG = C:\Users\gmpal\Videos\wtf\go_leveled\ENG.mp4
#     FRA = D:\export\french_final.mp4
#   CODE picks the flag (see Flags.For in GoKiosk/Assets/Scripts/Flags.cs); unknown codes get a grey flag
#   with the code written on it. Blank lines and lines starting with # are ignored.
# Or:    .\provision.ps1 -VideoDir C:\Users\gmpal\Videos\wtf\go_leveled
#   uses every CODE.mp4 in the folder, alphabetically.
param(
    [Parameter(Mandatory, ParameterSetName = 'List')] [string]$VideoList,
    [Parameter(Mandatory, ParameterSetName = 'Dir')] [string]$VideoDir,
    [string]$Apk = "$PSScriptRoot\..\GoKiosk\Build\GoKiosk.apk"
)
$ErrorActionPreference = 'Stop'
$pkg = 'com.wtf.gokiosk'
$dest = "/sdcard/Android/data/$pkg/files"

function Invoke-Adb { & adb.exe @args; if ($LASTEXITCODE) { throw "adb $args failed" } }

# Ordered CODE -> local path.
$videos = [ordered]@{}
if ($VideoList) {
    foreach ($line in Get-Content $VideoList) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^\s*([A-Za-z]+)\s*=\s*(.+?)\s*$') { throw "Bad line in ${VideoList}: $line" }
        $videos[$Matches[1].ToUpper()] = $Matches[2].Trim('"')
    }
} else {
    foreach ($v in Get-ChildItem $VideoDir -Filter *.mp4 | Sort-Object Name) { $videos[$v.BaseName.ToUpper()] = $v.FullName }
}
if ($videos.Count -eq 0) { throw 'No videos given.' }
foreach ($code in $videos.Keys) { if (-not (Test-Path $videos[$code])) { throw "$code video not found: $($videos[$code])" } }

$devices = @(adb devices | Select-String '\tdevice$')
if ($devices.Count -ne 1) { throw "Connect exactly one headset (found $($devices.Count))." }

Write-Host 'Installing kiosk...'
Invoke-Adb install -r $Apk

Write-Host "Copying videos ($($videos.Keys -join ', '))..."
Invoke-Adb shell mkdir -p $dest
# Remove videos that are not in the list, so the menu shows exactly these languages.
foreach ($name in @(adb shell ls $dest) | Where-Object { $_ -match '\.mp4$' }) {
    if (-not $videos.Contains([IO.Path]::GetFileNameWithoutExtension($name).ToUpper())) {
        Write-Host "  removing $name"
        Invoke-Adb shell rm "$dest/$name"
    }
}
foreach ($code in $videos.Keys) {
    $local = Get-Item $videos[$code]
    $remoteSize = adb shell stat -c %s "$dest/$code.mp4" 2>$null
    if ($remoteSize -eq "$($local.Length)") { Write-Host "  $code.mp4 already there"; continue }
    Invoke-Adb push $local.FullName "$dest/$code.mp4"
}
# Menu order for the kiosk.
$order = New-TemporaryFile
[IO.File]::WriteAllText($order, ($videos.Keys -join "`n") + "`n")
Invoke-Adb push $order.FullName "$dest/languages.txt" | Out-Null
Remove-Item $order

# A fresh install is in Android's "stopped" state and ignores BOOT_COMPLETED until launched once.
Write-Host 'Launching kiosk once (enables autostart)...'
Invoke-Adb shell am start -S -n "$pkg/com.unity3d.player.UnityPlayerActivity" | Out-Null
# Android saves the "launched" flag to disk after a delay; unplugging or rebooting sooner loses it.
Start-Sleep 30
if (adb shell dumpsys package $pkg | Select-String 'stopped=true') {
    throw 'The kiosk is still marked as never launched, so it will not autostart. Launch it once from Library > Unknown Sources.'
}

Write-Host 'Done. The kiosk is running and will start on every boot. It is now safe to unplug or reboot.'
