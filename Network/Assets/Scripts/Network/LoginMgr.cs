#define DEBUG_LAN     //自己内网测试版
//#define DEBUG_WAN     //自己外网测试版

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using tsf4g_tdr_csharp;
using net;
using WLGame;
using CS;
using protocol;
using UnityEngine.SceneManagement;

/// <summary>
/// 记录用户登录的一些凭据信息
/// </summary>
public class LoginMgr : LogicSystem
{
#if !MOYOSDK_APPSTORE && !TENCENTSDK && !TENCENTSDK_NEW && !APPSTORE && (ANDROIDSDK || IPHONESDK)
    public const string RegisterUrl = "http://dntg.dotazuile.com:7600/account/register/";
    public const string LoginServerUrl = "http://dntg.dotazuile.com:7600/account/auth/";
    public const string SelectServerUrl = "http://dntg.dotazuile.com:7600/resource/gameshard/select/";
    public const string NoticeUrl = "http://dntg.dotazuile.com:7600/announce/announce/";
    public const string ServerPayCallbackUrl = "http://dntg.dotazuile.com:7600/resource/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://dntg.dotazuile.com:7600/resource/pay/queryAppOrderId";
    public const string ResQueryUrl = "http://dntg.dotazuile.com:9200/queryDownload/";
    public const string GetBalance = "http://dntg.dotazuile.com:7600/resource/pay/getBalance";//for tencent only,
    public const string HandleClientOrder = "http://dntg.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#elif TENCENTSDK
    public const string RegisterUrl = "http://dntgtx.dotazuile.com:7600/account/register/";
    public const string LoginServerUrl = "http://dntgtx.dotazuile.com:7600/account/auth/";
    public const string SelectServerUrl = "http://dntgtx.dotazuile.com:7600/resource/gameshard/select/";
    public const string NoticeUrl = "http://dntgtx.dotazuile.com:7600/announce/announce/";
    public const string ServerPayCallbackUrl = "http://dntgtx.dotazuile.com:7600/resource/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://dntgtx.dotazuile.com:7600/resource/pay/queryAppOrderId";
    public const string ResQueryUrl = "http://dntgtx.dotazuile.com:9200/queryDownload/";
    public const string GetBalance = "http://dntgtx.dotazuile.com:7600/resource/pay/getBalance";//for tencent only,
	public const string HandleClientOrder = "http://dntgtx.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#elif TENCENTSDK_NEW
    public const string RegisterUrl = "http://dntgfengmo.dotazuile.com:7600/account/register/";
    public const string LoginServerUrl = "http://dntgfengmo.dotazuile.com:7600/account/auth/";
    public const string SelectServerUrl = "http://dntgfengmo.dotazuile.com:7600/resource/gameshard/select/";
    public const string NoticeUrl = "http://dntgfengmo.dotazuile.com:7600/announce/announce/";
    public const string ServerPayCallbackUrl = "http://dntgfengmo.dotazuile.com:7600/resource/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://dntgfengmo.dotazuile.com:7600/resource/pay/queryAppOrderId";
    public const string ResQueryUrl = "http://dntgfengmo.dotazuile.com:9200/queryDownload/";
    public const string GetBalance = "http://dntgfengmo.dotazuile.com:7600/resource/pay/getBalance";//for tencent only,
	public const string HandleClientOrder = "http://dntgfengmo.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#elif APPSTORE
	public const string RegisterUrl = "http://dntgios.dotazuile.com:7600/account/register/";
	public const string LoginServerUrl = "http://dntgios.dotazuile.com:7600/account/auth/";
	public const string SelectServerUrl = "http://dntgios.dotazuile.com:7600/resource/gameshard/select/";
	public const string NoticeUrl = "http://dntgios.dotazuile.com:7600/announce/announce/";
	public const string ServerPayCallbackUrl = "http://dntgios.dotazuile.com:7600/resource/pay/payCallback";
	public const string Channel = "APPSTORE";
	public const string QueryAppOrderIdUrl = "http://dntgios.dotazuile.com:7600/resource/pay/queryAppOrderId";
	public const string ResQueryUrl = "http://dntgios.dotazuile.com:9200/queryDownload/";
	public const string GetBalance = "http://dntgios.dotazuile.com:7600/resource/pay/getBalance";//for tencent only,
	public const string HandleClientOrder = "http://dntgios.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#elif MOYOSDK_APPSTORE
#if !MOYOSDK_APP
		    public const string RegisterUrl = "http://yueyu.dotazuile.com:7600/moyo/account/register/";
		    public const string LoginServerUrl = "http://yueyu.dotazuile.com:7600/moyo/account/auth/";
		    public const string SelectServerUrl = "http://yueyu.dotazuile.com:7600/moyo/resource/gameshard/select/";
		    public const string NoticeUrl = "http://yueyu.dotazuile.com:7600/moyo/announce/announce/";
		    public const string PayCallbackUrl = "http://yueyu.dotazuile.com:7600/moyo/pay/payClientQuery";
		    public const string ServerPayCallbackUrl = "http://yueyu.dotazuile.com:7600/moyo/pay/payCallback";
		    public const string Channel = "moyosdkappstore";
		    public const string QueryAppOrderIdUrl = "http://yueyu.dotazuile.com:7600/moyo/pay/queryAppOrderId";
		    public const string PayMoneyUrl = "http://yueyu.dotazuile.com:7600/moyo/pay/payMoney";
		    public const string ResQueryUrl = "http://yueyuupdate.dotazuile.com:9200/queryDownload/";     //内测资源更新地址
            public const string GetBalance = "http://yueyu.dotazuile.com:7600/moyo/account/getBalance";
            public const string HandleClientOrder = "http://dntg.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#else
		    public const string RegisterUrl = "http://apple.dotazuile.com:7600/moyo/account/register/";
		    public const string LoginServerUrl = "http://apple.dotazuile.com:7600/moyo/account/auth/";
		    public const string SelectServerUrl = "http://apple.dotazuile.com:7600/moyo/resource/gameshard/select/";
		    public const string NoticeUrl = "http://apple.dotazuile.com:7600/moyo/announce/announce/";
		    public const string PayCallbackUrl = "http://apple.dotazuile.com:7600/moyo/pay/payClientQuery";
		    public const string ServerPayCallbackUrl = "http://apple.dotazuile.com:7600/moyo/pay/payCallback";
		    public const string Channel = "moyosdkappstore";
		    public const string QueryAppOrderIdUrl = "http://apple.dotazuile.com:7600/moyo/pay/queryAppOrderId";
		    public const string PayMoneyUrl = "http://apple.dotazuile.com:7600/moyo/pay/payMoney";
		    public const string ResQueryUrl = "http://appleupdate.dotazuile.com:9200/queryDownload/";     //内测资源更新地址
            public const string GetBalance = "http://apple.dotazuile.com:7600/moyo/account/getBalance";
            public const string HandleClientOrder = "http://dntg.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#endif
#if MOYOSDK_HM
	    public const int channelid = 3;
#elif MOYOSDK_KY
	    public const int channelid = 2;
#elif MOYOSDK_TB
	    public const int channelid = 4;
#elif MOYOSDK_ITOOLS
	    public const int channelid = 5;
#elif MOYOSDK_PP
	    public const int channelid = 6;
#elif MOYOSDK_XY
	    public const int channelid = 7;
#elif MOYOSDK_I4
	    public const int channelid = 8;
#else
	    public const int channelid = 1;
#endif
#elif DEBUG_WAN
    public const string RegisterUrl = "http://dntgfengmo.dotazuile.com:7600/account/register/";
    public const string LoginServerUrl = "http://dntgfengmo.dotazuile.com:7600/account/auth/";
    public const string SelectServerUrl = "http://dntgfengmo.dotazuile.com:7600/resource/gameshard/select/";
    public const string NoticeUrl = "http://dntgfengmo.dotazuile.com:7600/announce/announce/";
    public const string ServerPayCallbackUrl = "http://dntgfengmo.dotazuile.com:7600/resource/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://dntgfengmo.dotazuile.com:7600/resource/pay/queryAppOrderId";
    public const string ResQueryUrl = "http://dntgfengmo.dotazuile.com:9200/queryDownload/";
    public const string GetBalance = "http://dntgfengmo.dotazuile.com:7600/resource/pay/getBalance";//for tencent only,
    public const string HandleClientOrder = "http://dntgfengmo.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#elif DEBUG_LAN
    public const string RegisterUrl = "http://192.168.1.139:7600/account/register";
    public const string LoginServerUrl = "http://192.168.1.139:7600/account/auth";
    public const string SelectServerUrl = "http://192.168.1.139:7600/resource/gameshard/select";
    public const string NoticeUrl = "http://192.168.1.139:7600/announce/announce";
    public const string PayCallbackUrl = "http://192.168.1.139:7600/pay/payClientQuery/";
    public const string ServerPayCallbackUrl = "http://192.168.1.139:7600/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://192.168.1.139:7600/pay/queryAppOrderId";
    public const string ResQueryUrl = "http://192.168.1.139:9200/queryDownload/";
    public const string GetBalance = "http://192.168.1.139:7600/resource/pay/getBalance";
    public const string HandleClientOrder = "http://192.168.1.139:7600/resource/pay/handleClientOrder";//for tencent only,
#else
    public const string RegisterUrl = "http://120.132.94.157:8080/hdls/account/register";
    public const string LoginServerUrl = "http://120.132.94.157:8080/hdls/account/auth";
    public const string SelectServerUrl = "http://120.132.94.157:8080/hdls/resource/gameshard/select";
    public const string NoticeUrl = "http://120.132.94.157:8080/hdls/announce/announce";
    public const string PayCallbackUrl = "http://120.132.94.157:7200/hdls/pay/payClientQuery/";
    public const string ServerPayCallbackUrl = "http://120.132.94.157:7100/hdls/pay/payCallback";
    public const string Channel = "test";
    public const string QueryAppOrderIdUrl = "http://120.132.94.157:7300/hdls/pay/queryAppOrderId";
	public const string ResQueryUrl = "http://120.132.94.157:9200/queryDownload/";     //内测资源更新地址
    public const string GetBalance = "http://120.132.94.157:7600/moyo/account/getBalance";
    public const string HandleClientOrder = "http://dntg.dotazuile.com:7600/resource/pay/handleClientOrder";//for tencent only,
#endif
    private static string moyoCurrentChannel = "";
    private static LoginMgr sLoginMgr;
    private const string PREF_LAST_USERNAME = "LastLoginUser";
    private const string PREF_LAST_PASSWORD = "LastLoginUserPwd";

    private int m_iPingFailCnt = 0;             //Ping连续失败的次数
    private int m_iPingSuccCnt = 0;             //ping连续成功次数
    private float m_fLastPingSuccTime = 0.0f;   //最后一次Ping成功的时间
    private float m_fLastPingTime = 0.0f;       //最后一次ping的时间
    private bool m_bNeedPing = false;           //是否需要ping
    private uint m_uPingInterval = 5;           //ping的间隔
    private bool m_bNeedCheckPing = true;
    public bool m_bNeedRecon = true;

    private AesHelper m_aesHelper = null;        // 用于对与游戏服务器之间的交互消息作加密解密
    private bool m_bFilterGameServerMsg = false; // 与游戏服务器之间的交互消息是否需要作加密解密(这个是客户端本地开关，动态开关)
    private bool m_bStaticallyFilterGameServerMsg = false; // 与游戏服务器之间的交互消息是否需要作加密解密(这个是服务器告知的，值为false时，则开关优先级高于m_bFilterGameServerMsg)


    public   string ReadSdkCurCh()
    {
		moyoCurrentChannel = "";
        #if MOYOSDK_BAIDU
                moyoCurrentChannel = "MOYOSDK_BAIDU";
        #elif MOYOSDK_360
                moyoCurrentChannel =  "MOYOSDK_360";
        #elif MOYOSDK_MI
               moyoCurrentChannel =  "MOYOSDK_MI";
		#elif MOYOSDK_UC
               moyoCurrentChannel = "MOYOSDK_UC";
		#elif MOYOSDK_HM
               moyoCurrentChannel = "MOYOSDK_HM";
		#elif MOYOSDK_KY
               moyoCurrentChannel = "MOYOSDK_KY";
		#elif MOYOSDK_TB
			moyoCurrentChannel = "MOYOSDK_TB";
		#elif MOYOSDK_ITOOLS
			moyoCurrentChannel = "MOYOSDK_ITOOLS";
		#elif MOYOSDK_PP
			moyoCurrentChannel = "MOYOSDK_PP";
		#elif MOYOSDK_XY
			moyoCurrentChannel = "MOYOSDK_XY";
		#elif MOYOSDK_I4
			moyoCurrentChannel = "MOYOSDK_I4";
		#elif MOYOSDK_APP
			moyoCurrentChannel = "MOYOSDK_APP";
		#elif MOYOSDK_HUAWEI
			moyoCurrentChannel = "MOYOSDK_HUAWEI";
		#elif MOYOSDK_OPPO
			moyoCurrentChannel = "MOYOSDK_OPPO";
		#elif MOYOSDK_SOGOU
			moyoCurrentChannel = "MOYOSDK_SOGOU";
        #elif MOYOSDK_LETV
			        moyoCurrentChannel = "MOYOSDK_LETV";
        #elif MOYOSDK_WDJ
			        moyoCurrentChannel = "MOYOSDK_WDJ";
#endif
        return moyoCurrentChannel;
    }
    public static LoginMgr Instance
    {
        get
        {
            if (sLoginMgr == null)
            {
                sLoginMgr = new LoginMgr();
            }
            return sLoginMgr;
        }
    }

    public LoginMgr()
    {
        sUserName = PlayerPrefs.GetString(PREF_LAST_USERNAME, "");
        sPassword = PlayerPrefs.GetString(PREF_LAST_PASSWORD, "");
        uRoleid = 0;
        sSessionId = "";
		uPort = 0;
        lstRoleRecord = null;
        plat = CS.PLAT_NUM.ANDROID;
        nSelectServerId = 0;
    }

    private string sUserName;
    public string UserName
    {
        get { return sUserName; }
        set { sUserName = value; }
    }

    private string sPassword;
    public string Password
    {
        get { return sPassword; }
        set { sPassword = value; }
    }

    public ulong s_uid;

    private ulong uRoleid;
    public ulong Roleid
    {
        get { return uRoleid; }
        set { uRoleid = value; } 
    }

    private string sSessionId;
    public string SessionId
    {
        get { return sSessionId; }
        set { sSessionId = value; } 
    }

    private CS.PLAT_NUM plat;
    public CS.PLAT_NUM PlatNum
    {
        get { return plat; }
        set { PlatNum = value; }
    }

    private string sSvrHost = "";
    public string SvrHost
    {
        get { return sSvrHost; }
        set { sSvrHost = value; } 
    }

    private ushort uPort;
    public ushort Port
    {
        get { return uPort; }
        set { uPort = value; }
    }

    private List<CS.RoleRecord> lstRoleRecord;
    public List<CS.RoleRecord> RoleRecordList
    {
        get { return lstRoleRecord; }
        set { lstRoleRecord = value; }
    }

    /*
    private List<GameShard> lstServerList = null;
    public List<GameShard> ServerList
    {
        get { return lstServerList; }
        set { lstServerList = value; }
    }
     */

    private int nSelectServerId = 0;
    public int SelectServerId
    {
        get { return nSelectServerId; }
        set { nSelectServerId = value; }
    }

    private uint createTime = 0;
    public uint CreateTime
    {
        get { return createTime; }
        set { createTime = value; }
    }

    private int nHistoryServerId = 0;
    public int HistoryServerId
    {
        set { nHistoryServerId = value; }
        get { return nHistoryServerId; }
    }

    private string str_gcode = "";
    public string gcode
    {
        set { str_gcode = value; }
        get { return str_gcode; }
    }

	private string str_token = "";
	public string token
	{
		set { str_token = value; }
		get { return str_token; }
	}
	
	private bool isAccoutLogin = false;
	public bool IsAccountLogin
	{
		set { isAccoutLogin = value; }
		get { return isAccoutLogin; }
	}
	
    /*
	private GameShard lastGameShard = null;
	public GameShard LastGameShard
	{
		set { lastGameShard = value; }
		get { return lastGameShard; }
	}
     */

    public void SavePrefsInfo()
    {
        PlayerPrefs.SetString(PREF_LAST_USERNAME, UserName);
        PlayerPrefs.SetString(PREF_LAST_PASSWORD, Password);
        PlayerPrefs.Save();
    }

    //sdk session id，需要发给服务器验证，
	private string sdksid = "";
	public string SdkSid
	{
		get
		{
			return sdksid;
		}
		set
		{
			sdksid = value;
		}
	}

    //sdk uid，有些sdk需要发给服务器验证，
	private string sdkuid = "";
	public string SdkUid
	{
		get
		{
			return sdkuid;
		}
		set
		{
			sdkuid = value;
		}
	}

    
    //tencent sdk session_id
    private string session_id = "";
    public string SdkSessionId
    {
        get
        {
            return session_id;
        }
        set
        {
            session_id = value;
        }
    }

    //tencent sdk session_type
    private string session_type = "";
    public string SdkSessionType
    {
        get
        {
            return session_type;
        }
        set
        {
            session_type = value;
        }
    }
    
    //tencent
    public string sdkOpenid = "";
    public string SdkOpenid
    {
        get
        {
            return sdkOpenid;
        }
        set
        {
            sdkOpenid = value;
        }
    }


    public string sdkChannel = "";
    public string SdkChannel
    {
        get
        {
            return sdkChannel;
        }
        set
        {
            sdkChannel = value;
        }
    }

    //tencent json string of user info
    private string extraJsonStrForLoginResult = "";
    public string ExtraJsonStrForLoginResult
    {
        get
        {
            return extraJsonStrForLoginResult;
        }
        set
        {
            extraJsonStrForLoginResult = value;
        }
    }
    //sdkchannelid
    private int sdkchannelid = 0;
    public int SdkChannelid
    {
        get
        {
            return sdkchannelid;
        }
        set
        {
            sdkchannelid = value;
        }
    }

    //应用宝的选完服就发这个协议
    private string loginApp = "qq";
    public string LoginApp
    {
        get
        {
            return loginApp;
        }
        set
        {
            loginApp = value;
        }
    }
    /// <summary>
    /// 重连服务器成功的回调
    /// </summary>
    public void OnReConnectSuccess()
    {
        Debuger.Log("OnReConnectSuccess!!");
        ReConnReq(ReConnRsp);
    }

    /// <summary>
    /// 发送重连请求
    /// </summary>
    void ReConnReq(CSMsgDelegate callback)
    {
        // 保证加密解密开关是关闭的
        LoginMgr.Instance.ToggleGameServerMsgFilter(false);

        var Req = new ReconnectReq();
        Req.uid = s_uid;
        Req.gcode = gcode;
        DGPKG msg = ProtocolDGlUtility.BuildDGMsg(Req);
        Network.Instance.SendCSLoginMsg(ref msg, callback);
    }

    void ReConnRsp(CSMsgResult result, DGPKG msg)
    {
        //role login 成功
        if ( result == CSMsgResult.NoError )
        {
            // 开启加密解密开关
            LoginMgr.Instance.ToggleGameServerMsgFilter(true);

            ReconnectRsp rsp = msg.body as ReconnectRsp;
            if (rsp == null)
                return;
            Debuger.LogError("reconnec rsp : " + rsp.result);
            if ( rsp.result == 0 )
            {
                Network.Instance.ReSendBeforeReconNotSendMsg();
                //重连成功
                return;
            }
            else
            {
                Debug.LogWarning("ReConnRsp, rsp.result: " + rsp.result);
            }
        }

        //SceneSys.Instance.GotoLoginScene();
        m_bNeedPing = false;        
    }

    void Auth()
    {
        //WGPlatform.Instance.AuthRequest(WGPlatform.LastSelectedPlat, RefreshTokenAuthResponse);
    }

    public void RefreshToken(bool bForceLogout = false)
    {
        Network.Instance.Reset();
    }

    //在第一次角色登录成功后，拉取数据
    public void GetDataAfterFirstRoleLogin()
    {
        //登录成功后先加载一部分进入主场景就马上用到的配置
        /*
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.OnlineAwardConf);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.HeroBasicAttributes);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.EquipBaseInfo);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.HeroUpgrade);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.HeroRiseStar);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.HeroUpgradeLimit);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.MaterialBaseInfo);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.PartnerHero);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.FightCapacityParam);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.AttributeTransferCoefficient);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.HeroColorAdd);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.RuneSuitAddition);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.RuneBaseInfo);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.EquipAttribute);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.LevelExp);
        ConfigCenterMgrSys.Instance.GetLogicFile(ConfigName.MainTask);

         */
		//ResManager.Instance().LoadPackage("Active_files.unity3d", null);
    }

//     void SendGetArenaRankReq()
//     {
//         var oGetArenaRankReq = new CS.GetMyArenaRankReq();
//         oGetArenaRankReq.roleid = Roleid;
//         net.DGPKG sendSMG = ProtocolDGlUtility.BuildDGMsg(oGetArenaRankReq);
//         Network.Instance.SendCSLoginMsg(ref sendSMG, OnGetArenaRankRsp);
//     }
// 
//     void OnGetArenaRankRsp(CSMsgResult result, net.DGPKG msg)
//     {
//         if (result != CSMsgResult.NoError)
//         {
//             return;
//         }
//         var rsp = (CS.GetMyArenaRankRsp)msg.body;
//         if (rsp.result == 0)
//         {
//             PlayerDataMgrSys.Instance.Rank = rsp.rank;
//         }
//     }

	//svr
	enum RequestType
	{
		SVR,
		UNAME,
	}
	RequestType m_request_type;

	Socket clientSocket = null;
	void connectServer(string m_ip, int m_port, AsyncCallback asyCallBack)
	{
		Debug.LogError("connectServer " + m_ip + " " + m_port);
		IPAddress[] ipAddresses = Dns.GetHostAddresses(m_ip);
		if(ipAddresses == null || ipAddresses.Length <= 0)
		{
			Debug.LogError("connectServer ipAddresses == null || ipAddresses.Length <= 0");
			return;
		}
		IPEndPoint ipEndpoint = new IPEndPoint(ipAddresses[0], m_port);
		clientSocket = new Socket(ipAddresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		Debug.LogError("ipAddress.AddressFamily " + ipAddresses[0].AddressFamily);
		bool success = false;
		IAsyncResult result = clientSocket.BeginConnect(ipEndpoint, asyCallBack, clientSocket);
	}

    //发送ping，在第一次角色登录成功后
    public void SendPing()
    {
        return;//暂不需要ping
        m_fLastPingTime = Time.time;
        var ping = new CS.P();
        net.DGPKG sendSMG = ProtocolDGlUtility.BuildDGMsg(ping);
        Network.Instance.SendCSLoginMsg(ref sendSMG, OnPingRsp);
        Debuger.Log("Send Ping : " + Time.time);
    }

    void OnPingRsp(CSMsgResult result, net.DGPKG msg)
    {
        if ( result != CSMsgResult.NoError )
        {
            m_iPingSuccCnt = 0;
            m_iPingFailCnt++;
            return;
        }
        CS.PRsp rsp = (CS.PRsp)msg.body;
        if ( rsp.result == 0 )
        {
            m_iPingFailCnt = 0;
            m_iPingSuccCnt++;
            m_fLastPingSuccTime = UnityEngine.Time.time;
            m_uPingInterval = rsp.interval;
        }

        if ( rsp.result != 0 )
        {
            /*
            //TODO show login
            UI3System.destroyAllWindowsOnLoadScene();
            SceneManager.LoadScene("Entry");

            UILogin login = UI3System.createWindow<UILogin>();
            login.show();
             */
            //login.bringTop();
            m_bNeedPing = false;
        }
    }

    protected override void OnUpdate()
    {
        if ( m_bNeedPing && Time.time > m_uPingInterval + m_fLastPingTime )
        {
            SendPing();
        }
        else if ( m_iPingFailCnt > 60 && m_bNeedCheckPing)
        {
            //TODO show login
            //UI3System.destroyAllWindows();
            //SceneManager.LoadScene("Entry");
            //UILogin login = UI3System.createWindow<UILogin>();
            //login.show();
            //login.bringTop();
            m_bNeedPing = false;
            m_bNeedCheckPing = false;
        }
    }

    public void SetLastPingTime()
    {
        m_fLastPingTime = Time.time;
    }

    // 初始化AesHelper成员
    public void InitAesHelper(byte[] key, byte[] iv)
    {
        m_aesHelper = new AesHelper(key, iv);
    }

    // 消息加密解密开关
    public void ToggleGameServerMsgFilter(bool on)
    {
        if (m_bStaticallyFilterGameServerMsg)
        {
            m_bFilterGameServerMsg = on;
        }
        else
        {
            m_bFilterGameServerMsg = false;
        }
    }

    // 消息加密解密开关
    public void StaticallyToggleGameServerMsgFilter(bool on)
    {
        m_bStaticallyFilterGameServerMsg = on;
    }

    // 过滤即将被发往游戏服务器的消息
    public byte[] FilterSentBytes(byte[] s)
    {
        if (m_bFilterGameServerMsg)
        {
            int lengthIndicatorSize = sizeof(uint);

            System.IO.MemoryStream stream = new System.IO.MemoryStream(s);
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);
            int packLen = (int)binaryReader.ReadUInt32(); // 消息包的总长度

            // 待加密的部分
            byte[] plaintext = binaryReader.ReadBytes(packLen - lengthIndicatorSize);

            // 加密
            byte[] ciphertext = m_aesHelper.Encrypt(plaintext);

            // 修正packLen
            packLen = lengthIndicatorSize + ciphertext.Length;

            // 重组新消息包
            byte[] outBytes = new byte[packLen];
            byte[] packLenBytes = System.BitConverter.GetBytes(packLen);
            packLenBytes.CopyTo(outBytes, 0);
            ciphertext.CopyTo(outBytes, packLenBytes.Length);

            // 返回新消息包
            return outBytes;
        }
        else
        {
            return s;
        }
    }

    // 过滤从游戏服务器收到的消息
    public byte[] FilterReceivedBytes(byte[] s)
    {
        if (m_bFilterGameServerMsg)
        {
            int lengthIndicatorSize = sizeof(uint);

            System.IO.MemoryStream stream = new System.IO.MemoryStream(s);
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);
            int packLen = (int)binaryReader.ReadUInt32(); // 消息包的总长度

            // 待解密的部分
            byte[] ciphertext = binaryReader.ReadBytes(packLen - lengthIndicatorSize);

            // 解密
            byte[] plaintext = m_aesHelper.Decrypt(ciphertext);

            // 修正packLen
            packLen = lengthIndicatorSize + plaintext.Length;

            // 重组新消息包
            byte[] outBytes = new byte[packLen];
            byte[] packLenBytes = System.BitConverter.GetBytes(packLen);
            packLenBytes.CopyTo(outBytes, 0);
            plaintext.CopyTo(outBytes, packLenBytes.Length);

            // 返回新消息包
            return outBytes;
        }
        else
        {
            return s;
        }
    }
}
