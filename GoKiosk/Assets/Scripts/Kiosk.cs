using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;

// 360 video kiosk for Oculus Go.
// Menu: look at a flag for DwellTime seconds to play that language.
// Returns to the menu when the video ends or the headset is taken off.
public class Kiosk : MonoBehaviour
{
    const float ArmDelay = 1.5f;     // seconds after menu appears before gaze counts
    const float DwellTime = 2f;      // seconds of gaze needed to select
    const float GazeAngle = 11f;     // degrees from flag centre that count as "looking at it"
    const float MenuDistance = 3f;
    const float FlagSpacing = 32f;   // degrees between flags
    const float MenuPitch = -12f;    // flags slightly below eye level
    const float RealignAngle = 60f;  // looking this far from the menu centre...
    const float RealignAfter = 1f;   // ...for this long re-centres the menu
    const float SettleDelay = 0.3f;  // wait for tracking to settle after the headset is put on
    // Skybox/Panoramic maps the video's centre (u = 0.5) to +X, i.e. yaw 90°.
    const float VideoCentreYaw = 90f;

    struct Lang { public string File, Label; public Texture2D Flag; }

    Lang[] langs;
    Camera cam;
    Transform menuRoot;
    Transform[] flagQuads;
    Transform reticle;
    Material reticleMat;
    VideoPlayer player;
    RenderTexture videoTexture;
    Material skyboxMat;
    TextMesh status;

    bool inMenu;
    float menuShownAt;
    float menuYaw;                   // yaw of the central flag; the video front is aligned to it
    float alignAt = -1;              // time to (re)align the menu to the viewer's gaze, -1 = none
    float lookingAwaySince = -1;
    bool wasPresent = true;
    int gazedIndex = -1;
    float gazeStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => new GameObject("Kiosk").AddComponent<Kiosk>();

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StartWatchdog();

        langs = new[]
        {
            new Lang { File = "ENG.mp4", Label = "English", Flag = Flags.UK() },
            new Lang { File = "FRA.mp4", Label = "Français", Flag = Flags.France() },
            new Lang { File = "NED.mp4", Label = "Nederlands", Flag = Flags.Netherlands() },
        };

        cam = new GameObject("Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.05f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);

        BuildMenu();
        BuildReticle();
        BuildPlayer();
        ShowMenu();
    }

    // ---------- construction ----------

    void BuildMenu()
    {
        menuRoot = new GameObject("Menu").transform;
        flagQuads = new Transform[langs.Length];
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        for (int i = 0; i < langs.Length; i++)
        {
            float yaw = (i - (langs.Length - 1) / 2f) * FlagSpacing;
            var dir = Quaternion.Euler(-MenuPitch, yaw, 0) * Vector3.forward;

            var quad = MakeQuad("Flag " + langs[i].Label, 1.2f, 0.6f, Shader.Find("Unlit/Texture"));
            quad.GetComponent<Renderer>().material.mainTexture = langs[i].Flag;
            quad.SetParent(menuRoot, false);
            quad.localPosition = dir * MenuDistance;
            quad.localRotation = Quaternion.LookRotation(dir);
            flagQuads[i] = quad;

            var label = MakeText(langs[i].Label, font, 0.05f);
            label.transform.SetParent(quad, false);
            label.transform.localPosition = new Vector3(0, -0.5f, 0);
        }

        var title = MakeText("Look at your language", font, 0.06f);
        title.transform.SetParent(menuRoot, false);
        title.transform.localPosition = new Vector3(0, 0.9f, MenuDistance);

        status = MakeText("", font, 0.04f);
        status.transform.SetParent(menuRoot, false);
        status.transform.localPosition = new Vector3(0, -1.6f, MenuDistance);
    }

    void BuildReticle()
    {
        // Radial-fill ring: alpha encodes angle, cutoff reveals the ring as progress grows.
        var ring = MakeQuad("Reticle", 0.12f, 0.12f, Shader.Find("Unlit/Transparent Cutout"));
        reticleMat = ring.GetComponent<Renderer>().material;
        reticleMat.mainTexture = Flags.RingTexture(128);
        ring.SetParent(cam.transform, false);
        ring.localPosition = new Vector3(0, 0, 2f);
        reticle = ring;

        var dot = MakeQuad("Dot", 0.025f, 0.025f, Shader.Find("Unlit/Transparent Cutout"));
        var dotMat = dot.GetComponent<Renderer>().material;
        dotMat.mainTexture = Flags.DotTexture(32);
        dotMat.SetFloat("_Cutoff", 0.5f);
        dot.SetParent(ring, false);
        dot.localPosition = new Vector3(0, 0, -0.001f);
    }

    void BuildPlayer()
    {
        videoTexture = new RenderTexture(3840, 1920, 0);
        skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMat.SetTexture("_MainTex", videoTexture);
        skyboxMat.SetFloat("_Mapping", 1);   // latitude-longitude
        skyboxMat.SetFloat("_ImageType", 0); // 360
        skyboxMat.SetFloat("_Layout", 0);    // mono

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = videoTexture;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.loopPointReached += _ => ShowMenu();
        player.prepareCompleted += vp => { vp.Play(); };
        // Only show the 360 view once the new video has drawn its first frame (avoids stale frames).
        player.frameReady += (vp, frame) =>
        {
            vp.sendFrameReadyEvents = false; // only needed for the first frame; per-frame events cost performance
            if (inMenu) return;
            RenderSettings.skybox = skyboxMat;
            cam.clearFlags = CameraClearFlags.Skybox;
        };
        player.errorReceived += (_, msg) => { Debug.LogError("GOKIOSK video error: " + msg); ShowMenu(); status.text = "Video error"; };
    }

    // ---------- states ----------

    void ShowMenu()
    {
        if (player.isPlaying || player.isPrepared) player.Stop();
        ClearVideoTexture();
        inMenu = true;
        status.text = "";
        AlignMenu();
        menuRoot.gameObject.SetActive(true);
        reticle.gameObject.SetActive(true);
        SetProgress(0);

        cam.clearFlags = CameraClearFlags.SolidColor;
        RenderSettings.skybox = null;
        Debug.Log("GOKIOSK menu");
    }

    void ClearVideoTexture()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = videoTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;
    }

    // Put the central flag where the viewer is looking (3DoF, so yaw only) and restart the arm delay.
    void AlignMenu()
    {
        menuYaw = cam.transform.eulerAngles.y;
        menuRoot.position = cam.transform.position;
        menuRoot.rotation = Quaternion.Euler(0, menuYaw, 0);
        menuShownAt = Time.time;
        gazedIndex = -1;
        lookingAwaySince = -1;
        alignAt = -1;
        if (inMenu)
        {
            menuRoot.gameObject.SetActive(true);
            reticle.gameObject.SetActive(true);
        }
        Debug.Log("GOKIOSK menu aligned to yaw " + menuYaw);
    }

    // Headset was (re)mounted: go to the menu and align it once tracking has settled.
    void OnMounted()
    {
        if (!inMenu) ShowMenu();
        // Hide the menu until it is re-aligned, so the old angle never flashes up.
        menuRoot.gameObject.SetActive(false);
        reticle.gameObject.SetActive(false);
        alignAt = Time.time + SettleDelay;
    }

    void Play(int i)
    {
        string path = Path.Combine(Application.persistentDataPath, langs[i].File);
        if (!File.Exists(path))
        {
            Debug.LogError("GOKIOSK missing video: " + path);
            status.text = "Missing " + langs[i].File;
            menuShownAt = Time.time;
            gazedIndex = -1;
            return;
        }

        inMenu = false;
        menuRoot.gameObject.SetActive(false);
        reticle.gameObject.SetActive(false);

        // The skybox shader turns content by -_Rotation, so this puts the video's centre on the central flag.
        skyboxMat.SetFloat("_Rotation", Mathf.Repeat(VideoCentreYaw - menuYaw, 360f));
        // Skybox is switched on by frameReady once the first new frame exists.

        player.sendFrameReadyEvents = true;
        player.url = path;
        player.Prepare();
        Debug.Log("GOKIOSK play " + path);
    }

    // ---------- per frame ----------

    void Update()
    {
        // Headset taken off -> back to menu; put back on -> re-align the menu to the new wearer.
        bool present = XRDevice.userPresence != UserPresenceState.NotPresent;
        if (!present && !inMenu) ShowMenu();
        if (present && !wasPresent) OnMounted();
        wasPresent = present;

        if (alignAt >= 0 && Time.time >= alignAt) AlignMenu();
        if (!inMenu) return;

        // Viewer looking well away from the flags for a while -> bring the menu to them.
        float off = Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y, menuYaw));
        if (off > RealignAngle)
        {
            if (lookingAwaySince < 0) lookingAwaySince = Time.time;
            else if (Time.time - lookingAwaySince > RealignAfter) AlignMenu();
        }
        else lookingAwaySince = -1;

        bool armed = Time.time - menuShownAt > ArmDelay;
        int looking = -1;
        var fwd = cam.transform.forward;
        for (int i = 0; i < flagQuads.Length; i++)
        {
            var toFlag = flagQuads[i].position - cam.transform.position;
            if (Vector3.Angle(fwd, toFlag) < GazeAngle) { looking = i; break; }
        }

        for (int i = 0; i < flagQuads.Length; i++)
            flagQuads[i].localScale = Vector3.one * (i == looking && armed ? 1.12f : 1f);

        if (!armed || looking < 0) { gazedIndex = -1; SetProgress(0); return; }

        if (looking != gazedIndex) { gazedIndex = looking; gazeStart = Time.time; }
        float progress = (Time.time - gazeStart) / DwellTime;
        SetProgress(progress);
        if (progress >= 1f) Play(looking);
    }

    void SetProgress(float p)
    {
        // Pixels with alpha >= cutoff are drawn; alpha = 1 - angle fraction.
        reticleMat.SetFloat("_Cutoff", Mathf.Lerp(1.001f, 0.001f, Mathf.Clamp01(p)));
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && !inMenu) ShowMenu();
        if (!paused) OnMounted();
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused && !inMenu) ShowMenu();
    }

    // ---------- helpers ----------

    // Also start the watchdog when launched by hand, so a reboot isn't needed to enable it.
    static void StartWatchdog()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var cls = new AndroidJavaClass("com.wtf.gokiosk.KioskWatchdog"))
        using (var intent = new AndroidJavaObject("android.content.Intent", activity, cls))
            activity.Call<AndroidJavaObject>("startService", intent);
#endif
    }

    static Transform MakeQuad(string name, float w, float h, Shader shader)
    {
        var go = new GameObject(name);
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0),
            new Vector3(-w / 2, h / 2, 0), new Vector3(w / 2, h / 2, 0),
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().material = new Material(shader);
        return go.transform;
    }

    static TextMesh MakeText(string text, Font font, float size)
    {
        var go = new GameObject("Text");
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        go.GetComponent<MeshRenderer>().material = font.material;
        return tm;
    }
}
