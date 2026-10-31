# WeTheFree – Oculus Go 360° video kiosk

Belgium edition (ENG / FRA / NED); the `main` branch also has DEU and SPA.

An unlocked Oculus Go turned into a self-running 360° video kiosk. It boots into a menu: a "Look at your language" prompt with one flag per language around it (a triangle for 3 languages, a square for 4, a pentagon for 5, and so on). Look at a flag for about 2 seconds to play that language's video. It returns to the menu when the video ends or the headset is taken off.

![menu](headset-setup/menu_screenshot.png)

## Contents
- `GoKiosk/` – Unity 2019.4.40f1 project. The built APK is in `GoKiosk/Build/GoKiosk.apk`.
- `ROADMAP.md` – field-test feedback and open items.
- `headset-setup/SUMMARY.md` – the full log: unlocking the Go, the build settings that work on the Go, alignment, audio levelling, kiosk mode, and maintenance commands.
- `headset-setup/oculus-adb-driver/` – Windows ADB driver for the Go.
- `headset-setup/belgium.txt` – the Belgian video list (ENG / FRA / NED), see below.
- `headset-setup/Oculus-Go-Software-Unlock-Instructions.pdf` – Meta's unlock guide. The unlock zip isn't committed; get it from Meta's "Unlocking Oculus Go" post and put it in `headset-setup/unlock/Oculus_Go_SW_Unlock_v1/` (gitignored).

## Not in the repo
The videos (`*.mp4`) are gitignored. DEU, FRA, NED and SPA had their audio levelled to match ENG; see `SUMMARY.md` for the settings used.

## Choosing the languages
Make a text file with one line per language: a language code, `=`, and the path to that video. The order is the menu order; the first flag goes at the top.
```
# headset-setup/belgium.txt
ENG = C:\Users\gmpal\Videos\wtf\go_leveled\ENG.mp4
FRA = C:\Users\gmpal\Videos\wtf\go_leveled\FRA.mp4
NED = C:\Users\gmpal\Videos\wtf\go_leveled\NED.mp4
```
The code picks the flag. Built in:

| Code | Language | Code | Language | Code | Language |
|---|---|---|---|---|---|
| ENG | English (UK) | HUN | Hungarian | FIN | Finnish |
| FRA | French | RON | Romanian | ISL | Icelandic |
| NED | Dutch | BUL | Bulgarian | EST | Estonian |
| DEU | German | HRV | Croatian | LAV | Latvian |
| SPA | Spanish | GRE | Greek | LIT | Lithuanian |
| ITA | Italian | SWE | Swedish | UKR | Ukrainian |
| POR | Portuguese | DAN | Danish | GLE | Irish |
| POL | Polish | NOR | Norwegian | LTZ | Luxembourgish |
| CES | Czech | CAT | Catalan | | |

Any other code still works: it gets a grey flag with the code written on it. To add a real flag, add a `case` to `Flags.For` in `GoKiosk/Assets/Scripts/Flags.cs` and rebuild.

## Preparing a headset (do this before shipping it)
1. Optional: in the Oculus menus, connect Wi-Fi and set the standby timer. Once the kiosk runs, these menus are hidden.
2. Connect the headset by USB and run, with your video list:
   ```
   .\headset-setup\provision.ps1 -VideoList .\headset-setup\belgium.txt
   ```
   This installs the APK, copies each video to the headset as `CODE.mp4`, removes videos that aren't in the list, saves the menu order, and launches the kiosk once (Android ignores the boot event for apps that have never been launched, so without that first launch the kiosk wouldn't autostart). Videos already on the headset aren't copied again, so re-running it to change languages is quick.
3. Reboot and check that the flag menu comes up on its own.

To update only the app on a headset that's already set up: `adb install -r GoKiosk\Build\GoKiosk.apk` (keeps videos and languages).

## Screenshot from the headset
The normal `adb screencap` is black while VR runs. Instead, with the headset on:
```
adb shell touch /sdcard/Android/data/com.wtf.gokiosk/files/screenshot.req
adb pull /sdcard/Android/data/com.wtf.gokiosk/files/screenshot.png
```
## Build & install
```
"C:\Program Files\Unity\Hub\Editor\2019.4.40f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath GoKiosk -executeMethod BuildGo.Build -logFile build.log
adb install -r GoKiosk/Build/GoKiosk.apk
```
`GoKiosk/gokiosk.keystore` (password `gokiosk`) signs the APK. Keep it: updates to the installed app must be signed with the same key.
