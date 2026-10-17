# Roadmap

Feedback from the first field test (Oct 2026). The kiosk works as-is and can be used in Liège and NL.

## Decided / open items
1. **Wi-Fi and kiosk exit.** Headsets ship already starting at boot (see `headset-setup/provision.ps1`). Recipients shouldn't have to set up Wi-Fi before the kiosk starts: some won't, and then they can't get to the menus any more.
   - [ ] Add a way to leave kiosk mode on the headset itself, e.g. holding a key combination, so Wi-Fi can be set up later.
2. **Autostart after the first launch.** This is normal Android behaviour: a fresh install ignores the boot event until it has been launched once. `provision.ps1` does that first launch, or run `adb shell monkey -p com.wtf.gokiosk 1`. ✅
3. **4-hour standby timer.** Probably not needed: the display turns off when the headset is taken off, and the kiosk never needs the controller.
   - [ ] Confirm on a device that no "find controller" screen blocks the kiosk after a long standby.
4. **Gallery or Skybox?** Neither. The kiosk has its own built-in 360 player. ✅
5. **More languages (German, Spanish, ...).** No AI dubbing, by choice, to avoid debates about it. Translations are already in the organizer wiki, and the voice-overs are recorded by vegan volunteers. The German version (recorded by Clara) may be the most used one, so it comes first.
   - [x] German (DEU) and Spanish (SPA) added, with audio levelled to match ENG.
   - [ ] Collect the other language versions from the organizer wiki.
   - [x] Menu with 5 flags (26° apart). More languages will need a second row or a menu built from the videos on the headset.
6. **mystats API / secure internet access.** To be discussed.
7. **[Bug] Headset taken off between video selection and playback.** If the viewer removes the headset after picking a video but before it has loaded, the audio starts but the headset comes back to the menu, and that audio keeps playing over the menu until another video is selected.
   - [ ] Reproduce on a device and handle it (e.g. pause/cancel loading on headset removal, return to the menu on resume).
