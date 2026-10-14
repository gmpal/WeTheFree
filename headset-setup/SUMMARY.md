# Oculus Go unlock – progress summary

Goal: repurpose an old Oculus Go as a dedicated 360° video player.

## Device
- Model: Oculus Go (`model:Pacific`, `product:vr_pacific`)
- Serial: `1KWPH800DS9427`
- USB ID: `VID_2833&PID_0082`
- Another serial, `1KWPH811LK8214`, appeared once in Windows Device Manager earlier, probably a different unit.

## Steps done
1. **adb couldn't see the headset at first.** Windows only showed it as a media device ("VR-Headset", class WPD), with no ADB interface.
2. **Installed the Oculus ADB driver** (`android_winusb.inf` in this folder).
3. **Entered Sideload mode** from the bootloader: power off, hold Volume Up + Power, choose *Sideload*, press Power.
   adb then showed: `1KWPH800DS9427  sideload  model:Pacific`
4. **First sideload attempt failed** (`Total xfer: 0.00x`). Sideload mode had timed out, so adb no longer saw the device.
5. **Second attempt failed too.** It was sent the wrapper `Oculus_Go_SW_Unlock_v1.zip` instead of the flashable build inside it. The headset rejected it immediately and rebooted to the normal OS.
6. **Extracted the real build:**
   `unlock\Oculus_Go_SW_Unlock_v1\unlocked_build.zip` (~745 MB, plus the instructions PDF)
7. **Flashed the unlocked build:**
   ```
   adb -s 1KWPH800DS9427 sideload unlock\Oculus_Go_SW_Unlock_v1\unlocked_build.zip
   ```
   Result: `Total xfer: 2.00x`, the normal success result for this kind of update.
8. **Unlocked the bootloader** (wipes all user data):
   ```
   fastboot devices          -> 1KWPH800DS9427  fastboot
   fastboot -s 1KWPH800DS9427 oem unlock   -> OKAY
   ```

9. **Verified the unlock.** After the factory reset, the headset showed "grab your phone to get started", and adb worked without developer mode:
   ```
   adb root                                  -> restarting adbd as root
   adb shell id                              -> uid=0(root)
   getprop ro.boot.verifiedbootstate         -> orange (= unlocked)
   getprop ro.build.version.incremental      -> 20480400000200000
   ```

## Kiosk app (GoKiosk)

A custom Unity app: look at a flag (🇬🇧 ENG / 🇫🇷 FRA / 🇳🇱 NED) for about 2 seconds to play that 360 video. It returns to the menu when the video ends or the headset is taken off.

- Project: `C:\Users\gmpal\git\wtf\GoKiosk` (Unity 2019.4.40f1, legacy built-in Oculus VR, package `com.unity.xr.oculus.android@2.38.6`)
- Package name: `com.wtf.gokiosk`. APK: `GoKiosk\Build\GoKiosk.apk`
- Build (command line):
  ```
  "C:\Program Files\Unity\Hub\Editor\2019.4.40f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath C:\Users\gmpal\git\wtf\GoKiosk -executeMethod BuildGo.Build -logFile build.log
  adb install -r C:\Users\gmpal\git\wtf\GoKiosk\Build\GoKiosk.apk
  ```
- Videos: `C:\Users\gmpal\Videos\wtf\go\{ENG,FRA,NED}.mp4` are the originals with the embedded cover image removed (3840×1920 H.264, no re-encode). On the headset they're in `/sdcard/Android/data/com.wtf.gokiosk/files/`.
  Audio levelled to ENG (EBU R128): FRA was −22.9 LUFS (+7.7 dB applied), NED −21.6 LUFS with a wide 16 LU range, which still sounded low, so it got gentle compression (`acompressor` threshold −28 dB, ratio 2.5) plus +15.4 dB, ending at −14.8 LUFS with a 7.9 LU range, ENG −15.6 LUFS. A peak limiter at −1.5 dBFS stops clipping, and audio is re-encoded as AAC 192k with the video stream copied. Result: ENG −15.6 / FRA −15.7 / NED −16.0 LUFS. The levelled files are in `C:\Users\gmpal\Videos\wtf\go_leveled\`.
  To replace a video: `adb push NEW.mp4 /sdcard/Android/data/com.wtf.gokiosk/files/ENG.mp4`

### What made the Go build work
- Legacy built-in VR with the "Oculus" device (not XR Plugin Management), OpenGL ES 3 only, IL2CPP, ARMv7 + ARM64, min API 21. Target API is 29, the lowest bundled with Unity; that's fine on the Go's Android 7.1 (API 25).
- A project keystore (`GoKiosk\gokiosk.keystore`, password `gokiosk`). The default `~\.android\debug.keystore` is in a format Unity's JDK 8 can't read.
- Shaders used only from code (`Unlit/Texture`, `Unlit/Transparent Cutout`, `Skybox/Panoramic`) are forced into the build; otherwise they render pink. Don't add built-in ones like `GUI/Text Shader`, because that breaks the build.

### Alignment
- The central flag goes where the wearer is looking whenever the headset is put on (and if they look more than 60° away for more than 1 second).
- The video's centre is aligned to the central flag. `Skybox/Panoramic` puts the video centre at yaw 90°, so `_Rotation = 90 - menuYaw`.

### Polish
- The gaze ring is black (it was white and disappeared on the white stripes of the FR/NL flags). The centre dot is white with a black outline.
- No stale frames: the video texture is cleared when a video stops, and the 360 view switches on only at the new video's first frame.
- The menu is hidden while it re-aligns after the headset is put on, so the old angle never flashes up.

### Kiosk mode
- The Oculus shell (`com.oculus.vrshell`) is a privileged system app and always wins as the home screen, so it was left untouched.
- Instead, `KioskWatchdog` (a Java service in `Assets/Plugins/Android`) starts on boot and relaunches the kiosk every 3 seconds if it isn't in front. It does nothing while the screen is off.
- Maintenance: `adb shell am force-stop com.wtf.gokiosk` stops the kiosk and watchdog until the next reboot.
- Undo kiosk: `adb uninstall com.wtf.gokiosk`

## Pending / next steps
- [ ] Test on the headset: press the Oculus button on the controller; the kiosk should come back within about 3 seconds.
- [ ] Keep the controller away from visitors; it isn't needed.
- [ ] Keep the headset on a charger between sessions; the battery is old.

## Notes
- The unlock can't be undone in practice. `fastboot oem lock` relocks, but only Oculus-signed images will boot after that.
- The headset no longer receives OTA updates.
- Source: Meta blog post "Unlocking Oculus Go" (Oct 21, 2021).
