using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;
using AgoraNative;
using System.Net;
using System.IO;

using Newtonsoft.Json.Linq;
using Random = UnityEngine.Random;

public class ClientScript : MonoBehaviour
{
    [SerializeField] private string APP_ID = "";

    private string TOKEN = "";

    private string CHANNEL_NAME = "YOUR_CHANNEL_NAME";

    private IRtcEngine mRtcEngine;
    private uint remoteUid = 0;
    private const float Offset = 100;
    public Text logText;
    private Logger _logger;
    private Dropdown _winIdSelect;
    private Button _startShareBtn;
    private Button _stopShareBtn;
    
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private Dictionary<uint, AgoraNativeBridge.RECT> _dispRect;
#endif

    // Use this for initialization
    void Start()
    {
        CHANNEL_NAME = "linker_test";
        TOKEN = get_token();
        //CHANNEL_NAME = ControlServerInMain.roomName;

        _logger = new Logger(logText);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        _dispRect = new Dictionary<uint, AgoraNativeBridge.RECT>();
#endif
        if (mRtcEngine != null)
        {
            Debug.Log("Agora engine exists already!!");
            return;
        }
        CheckAppId();
        InitEngine();
        JoinChannel();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private string get_token()
    {
        var json = new JObject();
        string method = "get_token";

        json.Add("roomName", CHANNEL_NAME);
        return request_server(json, method);
    }
    private void CheckAppId()
    {
        _logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in VideoCanvas!!!!!");
    }

    private void JoinChannel()
    {
        mRtcEngine.JoinChannelByKey(TOKEN, CHANNEL_NAME);
    }

    private void InitEngine()
    {
        mRtcEngine = IRtcEngine.GetEngine(APP_ID);
        mRtcEngine.SetLogFile("log.txt");
        //mRtcEngine.EnableAudio();
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        mRtcEngine.OnJoinChannelSuccess += OnJoinChannelSuccessHandler;
        mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        mRtcEngine.OnWarning += OnSDKWarningHandler;
        mRtcEngine.OnError += OnSDKErrorHandler;
        mRtcEngine.OnConnectionLost += OnConnectionLostHandler;
        mRtcEngine.OnUserJoined += OnUserJoinedHandler;
        mRtcEngine.OnUserOffline += OnUserOfflineHandler;
    }


    private void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        _logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        _logger.UpdateLog(string.Format("onJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}", channelName,
            uid, elapsed));
    }

    private void OnLeaveChannelHandler(RtcStats stats)
    {
        Debug.Log("OnLeaveChannelSuccess");
        _logger.UpdateLog("OnLeaveChannelSuccess");
    }

    private void OnUserJoinedHandler(uint uid, int elapsed)
    {
        if (remoteUid == 0) remoteUid = uid;
        _logger.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
        var json = new JObject();
        string method = "is_class_master";

        json.Add("classMaster", Convert.ToString(uid));
        json.Add("roomName", CHANNEL_NAME);
        if (Convert.ToBoolean(request_server(json, method)))
        {
            makeVideoView(uid);
        }
    }

    private void OnUserOfflineHandler(uint uid, USER_OFFLINE_REASON reason)
    {
        remoteUid = 0;
        _logger.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
        var json = new JObject();
        string method = "is_class_master";

        json.Add("roomName", CHANNEL_NAME);
        if (Convert.ToBoolean(request_server(json, method)))
        {
            DestroyVideoView(uid);
        }
    }

    private void OnSDKWarningHandler(int warn, string msg)
    {
        _logger.UpdateLog(string.Format("OnSDKWarning warn: {0}, msg: {1}", warn, msg));
    }

    private void OnSDKErrorHandler(int error, string msg)
    {
        _logger.UpdateLog(string.Format("OnSDKError error: {0}, msg: {1}", error, msg));
    }

    private void OnConnectionLostHandler()
    {
        _logger.UpdateLog("OnConnectionLost ");
    }

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit");
        if (mRtcEngine != null)
        {
            mRtcEngine.LeaveChannel();
            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }
    void OnDisable()
    {
        Debug.Log("OnDisable");
        if (mRtcEngine != null)
        {
            mRtcEngine.LeaveChannel();
            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }

    private void DestroyVideoView(uint uid)
    {
        var go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            Destroy(go);
        }
    }

    private void makeVideoView(uint uid)
    {
        var go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        var videoSurface = makePlaneSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.Renderer);
            videoSurface.SetGameFps(30);
            videoSurface.EnableFilpTextureApply(true, false);
        }
        Debug.Log("HERE?");
    }

    // VIDEO TYPE 1: 3D Object
    public VideoSurface makePlaneSurface(string goName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // set up transform
        go.transform.Rotate(270.0f, 270.0f, 180.0f);
        go.transform.position = new Vector3(0.9f, 0.09881967f, -0.1224516f);
        go.transform.localScale = new Vector3(0.5f, 0.25f, .5f);

        // configure videoSurface
        var videoSurface = go.AddComponent<VideoSurface>();
        Debug.Log("CREATE");
        return videoSurface;
    }

    // Video TYPE 2: RawImage
    public VideoSurface makeImageSurface(string goName)
    {
        var go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // to be renderered onto
        go.AddComponent<RawImage>();
        // make the object draggable
        go.AddComponent<UIElementDrag>();
        var canvas = GameObject.Find("VideoCanvas");
        if (canvas != null)
        {
            go.transform.parent = canvas.transform;
            Debug.Log("add video view");
        }
        else
        {
            Debug.Log("Canvas is null video view");
        }

        // set up transform
        go.transform.Rotate(0.0f, 0.0f, 180.0f);
        var xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        var yPos = Random.Range(Offset, Screen.height / 2f - Offset);
        go.transform.localPosition = new Vector3(0, 0, 0f);
        go.transform.localScale = new Vector3(16f, 9f, 1f);

        // configure videoSurface
        var videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
    public void onClickExit()
    {
        if (mRtcEngine != null)
        {
            mRtcEngine.LeaveChannel();
            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
        // Scene 이동
        Debug.Log("EXIT");

    }
    private string request_server(JObject req, string method)
    {
        string url = "http://34.64.85.29:8080/";
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url + method);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            streamWriter.Write(req.ToString());
        }

        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        string characterSet = httpResponse.CharacterSet;
        Debug.Log(characterSet);
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.UTF8, true))
        {
            var result = streamReader.ReadToEnd();
            Debug.Log(result);
            return result;
        }
    }

    //void myTest(GameObject go)
    //{
    //}
}