package com.wtf.gokiosk;

import android.app.ActivityManager;
import android.app.Service;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.os.Handler;
import android.os.IBinder;
import android.os.PowerManager;
import android.os.Process;
import android.util.Log;

// Keeps the kiosk in front: started on boot, relaunches the Unity activity whenever it is not foreground.
// Stop it for maintenance with: adb shell am force-stop com.wtf.gokiosk  (until next reboot)
public class KioskWatchdog extends Service {
    static final String TAG = "GOKIOSK";
    static final long BOOT_DELAY_MS = 8000;   // let the Oculus shell finish starting first
    static final long CHECK_MS = 3000;

    public static class BootReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Log.i(TAG, "boot completed, starting watchdog");
            context.startService(new Intent(context, KioskWatchdog.class));
        }
    }

    final Handler handler = new Handler();
    final Runnable check = new Runnable() {
        @Override
        public void run() {
            // Screen off (headset idle/off the head) pauses the kiosk too — only act while the screen is on.
            PowerManager pm = (PowerManager) getSystemService(POWER_SERVICE);
            if (pm.isInteractive() && !isForeground()) {
                Log.i(TAG, "kiosk not in front, relaunching");
                Intent i = new Intent();
                i.setClassName(getPackageName(), "com.unity3d.player.UnityPlayerActivity");
                i.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_REORDER_TO_FRONT);
                startActivity(i);
            }
            handler.postDelayed(this, CHECK_MS);
        }
    };
    boolean running;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (!running) {
            running = true;
            handler.postDelayed(check, BOOT_DELAY_MS);
        }
        return START_STICKY;
    }

    boolean isForeground() {
        ActivityManager am = (ActivityManager) getSystemService(ACTIVITY_SERVICE);
        for (ActivityManager.RunningAppProcessInfo p : am.getRunningAppProcesses()) {
            if (p.pid == Process.myPid())
                return p.importance == ActivityManager.RunningAppProcessInfo.IMPORTANCE_FOREGROUND;
        }
        return false;
    }

    @Override
    public void onDestroy() {
        handler.removeCallbacks(check);
        running = false;
    }

    @Override
    public IBinder onBind(Intent intent) { return null; }
}
