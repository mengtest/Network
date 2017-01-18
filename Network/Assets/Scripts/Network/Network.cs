using UnityEngine;
using System.Collections;
using System;
using net;
using tsf4g_tdr_csharp;
using System.Collections.Generic;
using System.IO;
using WLGame;

public enum CSMsgResult
{
    NoError = 0,
    NetworkError = 1,
    InternalError = 2,
    MsgTimeOut = 3,
    PingTimeOut = 4,
    MsgWaitSendTimeout = 5,         //等待消息发送超时
    MsgWaitResponseTimeout = 6,     //消息已经发送，等待响应超时
}

public delegate void CSMsgDelegate(CSMsgResult result, DGPKG msg);

/// <summary>
/// Network组件
/// </summary>
public class Network : MonoBehaviour
{
    private static Network sInstance;
    public static Network Instance
    {
        get
        {
            if (sInstance == null)
            {
                GameObject go = GameObject.Find("Network");
                if (go)
                {
                    sInstance = go.GetComponent<Network>();
                }
                else
                {
                    go = new GameObject("Network");
                    sInstance = go.AddComponent<Network>();
                }
                GameObject.DontDestroyOnLoad(go);

            }
            return sInstance;
        }
    }

    /// <summary>
    /// 后续协议是否加解密
    /// </summary>
    public static bool bCryptProtocol = false;
    /// <summary>
    /// 密钥
    /// </summary>
    public static byte[] CryptKey = null;
    /// <summary>
    /// 加解密Buffer
    /// </summary>
    public static byte[] CryptBuf = null;

    /// <summary>
    /// 刷新密钥
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void RefreshCryptKey(uint a, uint b)
    {
        byte[] a4 = BitConverter.GetBytes(a);
        byte[] b4 = BitConverter.GetBytes(b);
        byte[] c8 = new byte[9];
        //StringUtility.StringToUTF8Bytes(LoginInfo.Instance.GetAuthRet().open_id, ref c8);

        System.Buffer.BlockCopy( a4, 0, CryptKey, 0, 4 );
        System.Buffer.BlockCopy( b4, 0, CryptKey, 4, 4 );
        System.Buffer.BlockCopy( c8, 0, CryptKey, 8, 8);
    }

    void Awake()
    {
        sSendBufferArray = new byte[TConndServerHandler.TdrPackageBufferLength];
        mTdrSendBufferWriter = new TdrWriteBuf(ref sSendBufferArray, TConndServerHandler.TdrPackageBufferLength);

        ReadTrafficStatistics();
        bCryptProtocol = false;
        CryptKey = new byte[16];
        CryptBuf = new byte[TConndServerHandler.TdrPackageBufferLength];

        // for test
        bCryptProtocol = true;
        byte[] keys = System.Text.Encoding.Default.GetBytes("WLWLWLWLWLWLWLWL");
        System.Buffer.BlockCopy(keys, 0, CryptKey, 0, 16);

        InvokeRepeating("HandleHeartMsg", 0, 10f);//发送心跳包
    }

    public const int MaxPkgPerFrame = 10;       //单帧处理包的数量，避免lag
    byte[] sSendBufferArray;                    //所有发送的数据使用的Buffer
    TdrWriteBuf mTdrSendBufferWriter;           //发送数据使用的Writer，用来序列化数据
    TConndServerHandler mGameServerHandler;     //连接游戏服务器用的TConnd连接

    List<MsgInfo> mCSMsgWaitSendList = new List<MsgInfo>();         //一个将要发送的csmsg队列
    List<MsgInfo> mCSMsgAlreadySentList = new List<MsgInfo>();      //一个已经发送,等待相应的消息列表

    public const int kCSMsgCallArrayLength = 128;
    private bool mNetworkActive = false;    //网络是否活跃，在用户连接服务器以及登出之前,这个数据为true,其余时间为false
    private bool mBClientInConnectSvrStatus = false;
    private TConndSocketEventDelegate mConnectSvrSuccessDelegate = null;

    private List<MsgInfo> m_lstBeforeReconnNotSendMsg = new List<MsgInfo>();//重连前还没发送的消息
    public bool m_bIsReconnecting = false;


    /// <summary>
    /// 内部处理消息回调和超时用的数据结构
    /// </summary>
    class MsgInfo
    {
        public MsgInfo(DGPKG msg, CSMsgDelegate callback)
        {
            mMsg = msg;
            mCallBack = callback;
            mValid = true;
            mSentTime = UnityEngine.Time.realtimeSinceStartup;
        }

        private float mSentTime;
        public float SentTime
        {
            get { return mSentTime; }
            set { mSentTime = value; }
        }

        public void Set(DGPKG msg, CSMsgDelegate callback)
        {
            mMsg = msg;
            mCallBack = callback;
            mValid = true;
            mSentTime = UnityEngine.Time.realtimeSinceStartup;
        }
        public void Reset()
        {
            mMsg = null;
            mCallBack = null;
            mValid = false;
        }
        bool mValid;
        public bool Valid
        {
            get { return mValid; }
        }
        DGPKG mMsg;
        public DGPKG Msg
        {
            get { return mMsg; }
        }
        public CSMsgDelegate Callback
        {
            get { return mCallBack; }
        }
        CSMsgDelegate mCallBack;
    }

    MsgInfo[] mMsgInfoArray = new MsgInfo[kCSMsgCallArrayLength];
    private MsgInfo SetMsgInfo(DGPKG msg, CSMsgDelegate callback)
    {
        int i = (int)msg.header.seq % kCSMsgCallArrayLength;
        if (mMsgInfoArray[i] == null)
        {
            mMsgInfoArray[i] = new MsgInfo(msg, callback);
        }
        else
        {
            mMsgInfoArray[i].Set(msg, callback);
        }
        return mMsgInfoArray[i];
    }
    private MsgInfo GetMsgInfo(int msgSeq)
    {
        int i = msgSeq % kCSMsgCallArrayLength;
        MsgInfo msgInfo = mMsgInfoArray[i];
        if (msgInfo != null && msgInfo.Valid)
        {
            return msgInfo;
        }
        return null;
    }
    /// <summary>
    /// 向游戏服务器发送数据包
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    private void SendCSMsg(ref DGPKG msg, CSMsgDelegate callback)
    {
        MsgInfo msginfo = SetMsgInfo(msg, callback);
        mCSMsgWaitSendList.Add(msginfo);
    }

    /// <summary>
    /// 发送登录消息
    /// </summary>
    /// <param name="loginMsg"></param>
    public void SendCSLoginMsg(ref DGPKG loginMsg, CSMsgDelegate callback, bool bLockScreen = false)
    {
        if (mGameServerHandler != null && mGameServerHandler.IsConnected())
        {
            if (bLockScreen)
            {
                
                //UI3System.lockScreen(902/*" 正在连接..."*/, 10.0f, () => {
                //        UISystemTip.ShowTip(901/*"网络已断开，点击重新连接"*/, ReConnect);
                //    });

            }

            MsgInfo msginfo = SetMsgInfo(loginMsg, callback);
            Send(ref loginMsg);
            mCSMsgAlreadySentList.Add(msginfo);
        }
        else if ( LoginMgr.Instance.m_bNeedRecon )
        {
            // Modified by xubing@2015.10.10
            // 目前在已上线版本中发现一个偶现但多次出现的bug：
            // CS.ReconnectReq有一定机率会被扔到集合m_lstBeforeReconnNotSendMsg当中，原因不明。
            // 所以，为避免这个情况再发生，这里作一下过滤
            if (loginMsg.header.msg_full_name != "CS.ReconnectReq")
            {
                MsgInfo msg = new MsgInfo(loginMsg, callback);
                m_lstBeforeReconnNotSendMsg.Add(msg);
            }

            //UISystemTip.ShowTip(901/*"网络已断开，点击重新连接"*/, ReConnect);
        }      
    }

    //判断当前网络是否连接上
    public bool IsConnect
    {
        get
        {
            return (mGameServerHandler != null && mGameServerHandler.IsConnected());
        }
    }

    public void ReSendBeforeReconNotSendMsg()
    {
        List<MsgInfo> copyCSMsgAlreadySentList = new List<MsgInfo>(mCSMsgAlreadySentList);
        mCSMsgAlreadySentList.Clear();
        for (int i = 0; i < copyCSMsgAlreadySentList.Count; ++i)
        {
            MsgInfo msginfo = copyCSMsgAlreadySentList[i];
            DGPKG msg = msginfo.Msg;
            if (msg.header.msg_full_name != "CS.ReconnectReq")
            {
                SendCSLoginMsg(ref msg, msginfo.Callback);
            }
        }
        copyCSMsgAlreadySentList.Clear();

        for (int i = 0; i < m_lstBeforeReconnNotSendMsg.Count; ++i )
        {
            DGPKG msg = m_lstBeforeReconnNotSendMsg[i].Msg;
            SendCSLoginMsg(ref msg,  m_lstBeforeReconnNotSendMsg[i].Callback);
        }
        m_lstBeforeReconnNotSendMsg.Clear();
    }

    private void ReConnect()
    {
        if ( m_bIsReconnecting )
        {
            return;
        }
        m_bIsReconnecting = true;
        ConnectGameServer(LoginMgr.Instance.OnReConnectSuccess);
    }

    /// <summary>
    /// 在内部调用的一个发送CS数据报接口
    /// 由于被调用处已经做了Socket合法性检查, 所以这个私有函数内部就不做检查了
    /// 今后扩展前记得在外面做这个处理
    /// </summary>
    /// <param name="msg"></param>
    private void Send(ref DGPKG msg)
    {
        byte[] sendData = msg.doSerialize();
        int sendDataLen = sendData.Length;
        //Debug.Log("Send Msg [ID:]" + msg.header.msg_full_name + " [Len:]" + sendDataLen);

        // 过滤
        byte[] finalSentData = LoginMgr.Instance.FilterSentBytes(sendData);

        // 发送
        mGameServerHandler.Send(finalSentData, finalSentData.Length);

        if (msg.header.msg_full_name != "CS.P" )
        {
            LoginMgr.Instance.SetLastPingTime();
        }
        
    }

    /// <summary>
    /// 在游戏主循环调用
    /// </summary>
    public void Update()
    {
        if (mGameServerHandler != null /*&& WGPlatform.MSDKFlag != 0 && 
                (LoginInfo.Instance.GetAuthRet() != null)*/)
        {
//			Debug.LogError("Network Update");
            HandleMsgSendQueue();
            HandleRecvBytes();
            mGameServerHandler.Update();
            HandleTimeOutMsg();
        }
    }

    /// <summary>
    /// 发送心跳包
    /// </summary>
    void HandleHeartMsg()
    {
        if (mGameServerHandler!=null && mGameServerHandler.IsConnected())
        {
            /*
            if (PlayerDataMgrSys.Instance.Roleid > 0)
            {
                //只管定时发送给服务端，客户端不做任何处理
                CS.Ping req = new CS.Ping();
                DGPKG Msg = ProtocolDGlUtility.BuildDGMsg(req);
                Send(ref Msg);
            }
             */
        }
    }

    private void UnlockScreen()
    {
        //UI3System.unlockScreen();
    }

    private bool IsMsgTimeout(MsgInfo msg)
    {
        float now = UnityEngine.Time.realtimeSinceStartup;
        bool bRet =  now > msg.SentTime + 10.0f;//30秒超时

		if (bRet)
		{
			//Debuger.Log(" IsMsgTimeout Use time :" + (now - msg.SentTime));
		}

        return bRet;
    }

    private void RemoveOneTimeoutWaitSendMsg()
    {
        //foreach (MsgInfo msginfo in mCSMsgWaitSendList)
        for(int i = 0;i < mCSMsgWaitSendList.Count;i++)
        {
            MsgInfo msginfo = mCSMsgWaitSendList[i];
            if (msginfo != null && IsMsgTimeout(msginfo))
            {
                Debuger.Log(msginfo.Msg.header.msg_full_name + " : MsgWaitSendTimeout");
                mCSMsgWaitSendList.Remove(msginfo);
                try
                {
                    msginfo.Callback(CSMsgResult.MsgWaitSendTimeout, msginfo.Msg);
                }
                catch (Exception)
                {
                }
                return ;
            }
        }
    }

    private void RemoveOneTimeoutAreadySentMsg()
    {
        //foreach (MsgInfo msginfo in mCSMsgAlreadySentList)
        for(int i = 0;i < mCSMsgAlreadySentList.Count;i++)
        {
            MsgInfo msginfo = mCSMsgAlreadySentList[i];
            if (msginfo != null && IsMsgTimeout(msginfo))
            {
                Debuger.Log(msginfo.Msg.header.msg_full_name + " : MsgWaitResponseTimeout");
                mCSMsgAlreadySentList.Remove(msginfo);
                if ( msginfo.Msg != null && msginfo.Callback != null )
                {
                    msginfo.Callback(CSMsgResult.MsgWaitResponseTimeout, msginfo.Msg);
                }
                return;
            }
        }
    }

    private void RemoveWaitSendMsg(MsgInfo msginfo)
    {
        mCSMsgWaitSendList.Remove(msginfo);
    }

    private void RemoveAlreadySentMsg(MsgInfo msginfo)
    {
        mCSMsgAlreadySentList.Remove(msginfo);
    }

    private void HandleTimeOutMsg()
    {
        //如果有超时消息，每帧删除一个超时的消息，并触发对应的处理callback
        RemoveOneTimeoutWaitSendMsg();
        //RemoveOneTimeoutAreadySentMsg();
    }

    /// <summary>
    /// 处理发送的消息对列
    /// </summary>
    private void HandleMsgSendQueue()
	{
//		Debug.LogError("Network HandleMsgSendQueue");
        LoginMgr login = LoginMgr.Instance;
        //如果网络断了，会在下一次发送数据的时候做重连处理。
        if (mCSMsgWaitSendList.Count > 0)
		{
//			Debug.LogError("Network HandleMsgSendQueue mCSMsgWaitSendList.Count > 0 " + mCSMsgWaitSendList.Count);
            //如果这个时候Socket已经挂了，自动重连服务器
            //if (mGameServerHandler.IsShutDown() && login.IsAccountLogin )
            //{
                //login.IsRoleLogin = false;
                //ConnectGameServer(login.OnReConnectSuccess);                
            //}
            if ( mGameServerHandler.IsConnected() )
			{
//				Debug.LogError("Network HandleMsgSendQueue mGameServerHandler.IsConnected()");
                //Socket在连接状态才发送网络数据，其他状态不做处理，等待socekt连接成功和rolelogin成功        
                //if ( login.IsRoleLogin )
                {
                    //foreach (MsgInfo msginfo in mCSMsgWaitSendList)
                    for(int i = 0;i < mCSMsgWaitSendList.Count;i++)
                    {
                        MsgInfo msginfo = mCSMsgWaitSendList[i];
                        if (msginfo != null)
                        {
                            DGPKG msg = msginfo.Msg;
                            Send(ref msg);
							mCSMsgAlreadySentList.Add(msginfo);
//							Debug.LogError("Network HandleMsgSendQueue msginfo != null mCSMsgAlreadySentList.Add(msginfo)");
                        }
                    }
                    mCSMsgWaitSendList.Clear();
                }
//                 else
//                 {//只处理登陆协议
//                     foreach (MsgInfo msginfo in mCSMsgWaitSendList)
//                     {
//                         string msg_full_name = msginfo.Msg.header.msg_full_name;
//                         if ( msg_full_name == typeof(CS.RoleLoginReq).FullName )
//                         {
//                             DGPKG msg = msginfo.Msg;
//                             Send(ref msg);
//                             mCSMsgAlreadySentList.AddLast(msginfo);
//                             mCSMsgWaitSendList.Remove(msginfo);
//                             break;
//                         }
//                     }
//                }//end else //只处理登陆协议
            }//end else if 
        }
    }

    TConndServerHandler.UnpackDelegate unpackDelegate = ProtocolDGlUtility.UnpackDGMsg;
    /// <summary>
    /// 处理收到的字节
    /// </summary>
    private void HandleRecvBytes()
	{
//		Debug.Log("HandleRecvBytes");
        for (int i = 0; i < MaxPkgPerFrame; i++)
        {
            if (mGameServerHandler != null)
			{
//				Debug.Log("ProcessMsg" + (mGameServerHandler != null));
                DGPKG msg = mGameServerHandler.ParseProtocolData(ref unpackDelegate) as DGPKG;
                if ( msg == null )
				{
//					Debug.Log("ProcessMsg : " + (msg == null));
                    return;
                }                
                ProcessMsg(msg);
            }
        }
    }

    public void ProcessMsg(DGPKG msg)
	{
//		Debug.Log("Start ProcessMsg");
        // 通过模块注册消息响应函数处理
        if (msg == null)
            return;
		
//		Debug.Log("ProcessMsg : " + (msg != null));
        if (msg.header.seq == 0)
        {
            NotifyMsgHandleMgr.Instance.HandleMsg(msg); 
        }
        else
        {
            MsgInfo info = GetMsgInfo((int)msg.header.seq);
            if (info == null) // 最大的可能是，seq对应的响应来了多次(因为在目前的消息重发机制下，客户端可能会将具有同一seq的请求重发多次)
            {
                RemoveAlreadySentMsgBySeq(msg.header.seq);
            }
            else
            {
                RemoveAlreadySentMsg(info);

                //ssGame.NetDelay = Time.realtimeSinceStartup*1000 - msg.stHead.dwClientTime;
                if (info.Callback != null)
                {
                    if (msg.header != null && msg.body != null)
                    {
//                        Debug.Log("ProcessMsg : " + msg.header.msg_full_name);
                        //Debug.Log("ProcessMsg here GetType: " + msg.body.GetType());
                        //Debug.Log("ProcessMsg rsper: " + info.Callback.Target);
                        string fullName = msg.header.msg_full_name;
                        string typeName = msg.body.GetType().FullName;
                        if (fullName == typeName)
                        {
                            info.Callback(CSMsgResult.NoError, msg); 
                        }
                        else
                        {
                            Debug.LogError(string.Format("{0} != {1}", fullName, typeName));
                        }
                        info.Reset();
                    }
                }
            }

            // 只要还有尚未被响应的请求，这里就不主动解锁
            if (!IsAnyRequestNotResponded())
            {
                UnlockScreen();
            }
        }         
    }

    /// <summary>
    /// 当前是否与GameSvr连接着
    /// </summary>
    public bool IsConnected
    {
        get
        {
            return (mGameServerHandler != null) && mGameServerHandler.IsConnected();
        }
    }

    /// <summary>
    /// 解决最后一个包的问题.
    /// </summary>
    /// <param name="delayInMS"></param>
    public void Reset(int delayInMS = 0)
    {
        //ssGame.Instance.CancelInvokeHeartBeat();
        Debuger.Log("Reset = " + delayInMS);
        
        CancelInvoke("ResetNetworkImpl");
        if (delayInMS == 0)
            ResetNetworkImpl();
        else
            Invoke("ResetNetworkImpl", delayInMS / 1000.0f);
    }

    private void ResetNetworkImpl()
    {
        Debuger.Log("ResetNetworkImpl");
        //MenuLoading.ForceDeleteObject();

        for (int i = 0; i < kCSMsgCallArrayLength; i++)
        {
            MsgInfo info = GetMsgInfo(i);
            if (info != null )
            {
                RemoveAlreadySentMsg(info);
                RemoveWaitSendMsg(info);
                info.Reset();
            }
        }

        if (mCSMsgWaitSendList != null)
            mCSMsgWaitSendList.Clear();

        /*if (mCSMsgSentList != null)
            mCSMsgSentList.Clear();*/

        if (mGameServerHandler != null)
        {
            mGameServerHandler.Close();
        }

        //bCryptProtocol = false;
        // for test
        bCryptProtocol = true;
        byte[] keys = System.Text.Encoding.Default.GetBytes("WLWLWLWLWLWLWLWL");
        System.Buffer.BlockCopy(keys, 0, CryptKey, 0, 16);

        /*if (ViewSplash.Instance != null)
        {
            ViewSplash.Instance.handleShowLogin();
        }*/
    }

    public void RealConnectSvr(TConndSocketEventDelegate successDelegate)
	{
		//Debug.LogError("unity RealConnectSvr");
        if (mGameServerHandler != null)
		{
			//Debug.LogError("unity mGameServerHandler");
            LoginMgr Inst = LoginMgr.Instance;
            //Debuger.LogError("Connect to srv: [ip:] " + Inst.SvrHost + " [port:]" + Inst.Port);        
            mGameServerHandler.Connect(Inst.SvrHost, Inst.Port, successDelegate, GameServerSocketErrorCallBack, 30);

           // UI3System.lockScreen(902/*"正在连接..."*/, 10.0f, () => {
            //    UISystemTip.ShowTip(901/*"网络已断开，点击重新连接"*/, ReConnect);
             //   });
        }
       // yield return null;
    }
    
    /// <summary>
    /// 连接游戏服务器，设置和服务器连接状态的一些回调
    /// </summary>
    /// <param name="successDelegate"></param>
    /// <param name="errorhandler"></param>
    public void ConnectGameServer(TConndSocketEventDelegate successDelegate)
	{
		//Debug.LogError("unity ConnectGameServer");
        if (mGameServerHandler != null)
            mGameServerHandler.Close();
		
        if (mGameServerHandler == null || mGameServerHandler.IsShutDown())
		{
			//Debug.LogError("unity mGameServerHandler mGameServerHandler.IsShutDown()");
            mGameServerHandler = new TConndServerHandler();
            this.mConnectSvrSuccessDelegate = successDelegate;
            mNetworkActive = true;
            mBClientInConnectSvrStatus = true;
            //MenuLoading.Create(Localization.instance.Get("Connecting"));

			RealConnectSvr(ConnecGameSvrSuccessCallback);
            //StartCoroutine(RealConnectSvr(ConnecGameSvrSuccessCallback));
        }else
        {
            Debug.LogError("unity ConnectGameServer (mGameServerHandler == null || mGameServerHandler.IsShutDown()) == false");
        }
    }

    void ConnecGameSvrSuccessCallback()
    {
        m_bIsReconnecting = false;
        //Debuger.Log("Connect to srv sucess!!");
        if (mConnectSvrSuccessDelegate != null)
        {
            mConnectSvrSuccessDelegate();
        }

        mConnectSvrSuccessDelegate = null;
        mBClientInConnectSvrStatus = false;

        //UnlockScreen();
    }

    /// <summary>
    /// 游戏服务器连接出错回调
    /// </summary>
    /// <param name="stopReason"></param>
    void GameServerSocketErrorCallBack(int errorCode)
    {
        m_bIsReconnecting = false;
        mConnectSvrSuccessDelegate = null;
        mBClientInConnectSvrStatus = false;

        HandleNetworkError(CSMsgResult.NetworkError, errorCode);

        Invoke("DelayUnlockScreen", 5.0f);
    }

    void DelayUnlockScreen()
    {
        UnlockScreen();
        //UISystemTip.ShowTip(901/*"网络已断开，点击重新连接"*/, ReConnect);
    }

    /// <summary>
    /// 清理登陆状态，关闭Socket，对于之前没有处理的消息统一回调
    /// </summary>
    /// <param name="result"></param>
    public void HandleNetworkError(CSMsgResult result, int errorCode = 0, bool showTips = true)
    {
        mShowErrTips = false;
        for (int i = 0; i < kCSMsgCallArrayLength; i++)
        {
            MsgInfo info = GetMsgInfo(i);
            if (info != null && info.Callback != null)
            {
                try
                {
                    info.Callback(result, null);
                }
                catch (Exception)
                {
                }
                RemoveAlreadySentMsg(info);
                RemoveWaitSendMsg(info);
                info.Reset();

                UnlockScreen();
            }
        }
        string msgContent = string.Empty;

        switch (result)
        {
            case CSMsgResult.MsgTimeOut:
				msgContent = LoginMgr.Instance.SvrHost + "MsgTimeOut";
                break;
            case CSMsgResult.NetworkError:
                {
                    msgContent = LoginMgr.Instance.SvrHost + "NetworkErrorWithCode :" + errorCode;
                }
                break;
            case CSMsgResult.InternalError:
				msgContent = LoginMgr.Instance.SvrHost + "InternalError";
                break;
        }

		Debug.LogError(msgContent);
    }


    bool mShowErrTips = false;
    public void DefaultErrorHandler(bool showDefaultErrMsg)
    {
        if (showDefaultErrMsg)
            mShowErrTips = true;
    }

    private void RemoveAlreadySentMsgBySeq(uint seq)
    {
        for (int i = 0; i < mCSMsgAlreadySentList.Count; i++)
        {
            MsgInfo msginfo = mCSMsgAlreadySentList[i];
            if (msginfo.Msg.header.seq == seq)
            {
                mCSMsgAlreadySentList.Remove(msginfo);
                break;
            }
        }
    }

    // 判断是否尚有请求未被响应
    private bool IsAnyRequestNotResponded()
    {
        return (mCSMsgAlreadySentList.Count > 0 || m_lstBeforeReconnNotSendMsg.Count > 0);
    }

    #region Traffic Statistics

    public const int mSaveDay = 14;    // 连续记录14天
    const int mCursor = 13;

    public int TotalLastWeek
    {
        get{
            int total = 0;
            for (int i= 0; i<7;i++)
            {
    #if (UNITY_ANDROID || UNITY_IPHONE) && (!UNITY_EDITOR)
            total += mHistoryStatisticsCDN[i];
    #else
            total += mHistoryStatisticsLAN[i];
    #endif
            }
            return total;
        }
    }

    public int TotalThisWeek
    {
        get
        {
            int total = 0;
            for (int i = 7; i < 14; i++)
            {
#if (UNITY_ANDROID || UNITY_IPHONE) && (!UNITY_EDITOR)
                total += mHistoryStatisticsCDN[i];
#else
                total += mHistoryStatisticsLAN[i];
#endif
            }
            return total;
        }
    }

    int[] mHistoryStatisticsCDN = new int[mSaveDay];
    int[] mHistoryStatisticsLAN = new int[mSaveDay];
    int[] mStatisticsDate = new int[mSaveDay];
    int mMaxFlowOnceDayCDN = 0;
    int mMaxFlowOnceDayLAN = 0;

    public int MaxFlowOnceDay
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IPHONE) && (!UNITY_EDITOR)
            return mMaxFlowOnceDayCDN;
#else
            return mMaxFlowOnceDayLAN;    //NetworkReachability.ReachableViaLocalAreaNetwork
#endif
        }
    }

    public void ResetTrafficStatistics()        // 彻底清零
    {
        if (mGameServerHandler != null)
            mGameServerHandler.ResetStatistics();

        for (int i = 0; i < mSaveDay; ++i)
        {
            mHistoryStatisticsCDN[i] = 0;
            mHistoryStatisticsLAN[i] = 0;
        }

        SaveTrafficStatistics();
    }

    void SaveTrafficStatistics()
    {
        for (int i = 0; i < mSaveDay; i++)
        {
            PlayerPrefs.SetInt("KeyBytesStatisticsCDN" + i.ToString(), mHistoryStatisticsCDN[i]);
            PlayerPrefs.SetInt("KeyBytesStatisticsLAN" + i.ToString(), mHistoryStatisticsLAN[i]);
            PlayerPrefs.SetInt("KeyStatisticsDate" + i.ToString(), mStatisticsDate[i]);
        }
        PlayerPrefs.Save();
    }

    void ReadTrafficStatistics() // Awake处加载
    {
        for (int i = 0; i < mSaveDay; i++)
        {
            mHistoryStatisticsCDN[i] = PlayerPrefs.GetInt("KeyBytesStatisticsCDN" + i.ToString(), 0);
            mHistoryStatisticsLAN[i] = PlayerPrefs.GetInt("KeyBytesStatisticsLAN" + i.ToString(), 0);
            mStatisticsDate[i] = PlayerPrefs.GetInt("KeyStatisticsDate" + i.ToString(), 0);
        }

        int day = PlayerPrefs.GetInt("TrafficDay", 0);
        if (day != 0)
        {
            if (day != DateTime.Now.Day)
            {
                for (int i = 0; i < mSaveDay - 1; i++)
                {
                    mHistoryStatisticsCDN[i] = mHistoryStatisticsCDN[i + 1];
                    mHistoryStatisticsLAN[i] = mHistoryStatisticsLAN[i + 1];
                    mStatisticsDate[i] = mStatisticsDate[i + 1];
                }
                mHistoryStatisticsCDN[mCursor] = 0;
                mHistoryStatisticsLAN[mCursor] = 0;
            }
        }
        else
        {
            for (int i = 0; i < mSaveDay; i++)
            {
                mStatisticsDate[i] = (DateTime.Now.AddDays(i - mSaveDay +1 )).Day;
            }
        }

        PlayerPrefs.SetInt("TrafficDay", DateTime.Now.Day);
        mStatisticsDate[mCursor] = DateTime.Now.Day;
    }

    public void AddNetworkStatistic(int sendInByte, int recvInByte)
    {
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            mHistoryStatisticsCDN[mCursor] += (sendInByte + recvInByte);
        else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            mHistoryStatisticsLAN[mCursor] += (sendInByte + recvInByte);
    }

    public void TrafficStatistic()  // 结算截至到当前为止的流量
    {
        if (mGameServerHandler != null)
        {
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                mHistoryStatisticsCDN[mCursor] += mGameServerHandler.GetStatistics(NetworkReachability.ReachableViaCarrierDataNetwork);
            }
            else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                mHistoryStatisticsLAN[mCursor] += mGameServerHandler.GetStatistics(NetworkReachability.ReachableViaLocalAreaNetwork);
            }

            for (int i = 0; i < mSaveDay; i++)
            {
                mMaxFlowOnceDayCDN = mHistoryStatisticsCDN[i] > mMaxFlowOnceDayCDN ? mHistoryStatisticsCDN[i] : mMaxFlowOnceDayCDN;
                mMaxFlowOnceDayLAN = mHistoryStatisticsLAN[i] > mMaxFlowOnceDayLAN ? mHistoryStatisticsLAN[i] : mMaxFlowOnceDayLAN;
            }

            mGameServerHandler.ResetStatistics();   // 清除，不重复计算
        }
        SaveTrafficStatistics();
    }

    public int GetTraffic(int index)
    {
#if (UNITY_ANDROID || UNITY_IPHONE) && (!UNITY_EDITOR)
        return mHistoryStatisticsCDN[index];    //NetworkReachability.ReachableViaCarrierDataNetwork
#else
        return mHistoryStatisticsLAN[index];    //NetworkReachability.ReachableViaLocalAreaNetwork
#endif
    }

    public int GetTrafficDate(int index)
    {
        return mStatisticsDate[index];
    }
    #endregion

}
