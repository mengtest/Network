/*
 *  用于处理服务器主动通知的网络包
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net;
using UnityEngine;

namespace WLGame
{
    class NotifyMsgHandleMgr : LogicSystem
    {
        private static NotifyMsgHandleMgr sInstance = null;
        public static NotifyMsgHandleMgr Instance
        {
            get
            {
                if (sInstance == null)
                {
                    sInstance = new NotifyMsgHandleMgr();
                    sInstance.Init();
                }
                return sInstance;
            }
        }

        private Dictionary<string, CSMsgDelegate> m_dicHandle = new Dictionary<string, CSMsgDelegate>();

        public override void Init()
        {
            RegistHandle(typeof(CS.NotifyGiftBagItem).FullName, OnNotifyGiftBagItem);
            RegistHandle(typeof(CS.GmRsp).FullName, OnNotifyGmRsp);
            RegistHandle(typeof(CS.NotifyRetryRoleLogin).FullName, OnNotifyRetryRoleLogin);
            RegistHandle(typeof(CS.NotifyRechargeResult).FullName, OnNotifyRechargeResult);
            RegistHandle(typeof(CS.MineStateInfoNotify).FullName, OnMineStateInfoNotify);  
        }

        public void RegistHandle(string sFullName, CSMsgDelegate oHandle)
        {
            if (m_dicHandle.ContainsKey(sFullName))
                return;
            m_dicHandle.Add(sFullName, oHandle);
        }

        public void UnRegistHandle(string sFullName, CSMsgDelegate callback)
        {
            if (m_dicHandle.ContainsKey(sFullName))
            {
                m_dicHandle.Remove(sFullName);
            }
        }

        public void HandleMsg(DGPKG msg)
        {
            CSMsgDelegate handle = null;
            if (m_dicHandle.TryGetValue(msg.header.msg_full_name, out handle) )
            {
                handle(CSMsgResult.NoError, msg);
            }
        }

        private void OnNotifyGiftBagItem(CSMsgResult result, DGPKG msg)
        {
            if (result != CSMsgResult.NoError || msg == null)
            {
                return;
            }
            CS.NotifyGiftBagItem rsp = (CS.NotifyGiftBagItem)msg.body;
            //PlayerDataMgrSys.Instance.AddGiftBagItem(rsp.gift_bag_item);
        }

        private void OnNotifyGmRsp(CSMsgResult result, DGPKG msg)
        {
            CS.GmRsp rsp = (CS.GmRsp)msg.body;
            if (result != CSMsgResult.NoError || msg == null)
            {
                return;
            }
            if ( rsp.result == 0 )
            {
                //GmMgr.Instance.DisplayMsg(rsp.msg);
            }
        }

        private void OnNotifyRetryRoleLogin(CSMsgResult result, DGPKG msg)
        {
            LoginMgr.Instance.m_bNeedRecon = false;
            Network.Instance.Reset();

            // 先解除锁屏（如果有的话）
            //UI3System.unlockScreen();

			//UISystemTip.ShowTip(903/*"已退出登录，点击重新登录"*/, OnNotifyRetryRoleLoginCallback);
        }
	
		private void OnNotifyRetryRoleLoginCallback()
		{
			//SceneSys sceneSys = SceneSys.Instance;
			//sceneSys.GotoLoginScene();
			//UISdkSys.Instance.Logout(true);

            //lijing 2015.12.30 guide
            //GFrame.Guide.GuideMgr.Instance.Clear();
            //FateSys.Instance.Clear();

            // @统计：登出游戏
            //DCAccount.logout();
		}
        private void OnNotifyRechargeResult(CSMsgResult result, DGPKG msg)
        {
            Debug.LogError("OnNotifyRechargeResult " + result);
            if (result == CSMsgResult.NoError && msg != null && msg.header.msg_full_name == typeof (CS.NotifyRechargeResult).FullName)
            {
                Debug.LogError("OnNotifyRechargeResult " + msg.header.msg_full_name);
                var Rsp = (CS.NotifyRechargeResult)msg.body;
                if (Rsp == null)
                {
                    Debug.LogError("OnNotifyRechargeResult Rsp == null");
                    return;
                }
                Debug.LogError("OnNotifyRechargeResult " + Rsp.result);
                if (Rsp.result != 0)
                {
                    //UITextSys.Instance.ShowText(Rsp.result);
                    return;
                }

                /*
                int amount = (int)(Rsp.new_diamond_num) - (int)(PlayerDataMgrSys.Instance.Diamond);
                Debug.LogError("Rechage Rsp.result == 0 here " + Rsp.new_diamond_num + " " + Rsp.new_vip_exp + " " + Rsp.new_vip_level + " " + amount);
                if (amount < 0)
                    amount = 0;
                Debug.LogError("Rechage Rsp.result == 0 here " + Rsp.new_diamond_num + " " + Rsp.new_vip_exp + " " + Rsp.new_vip_level + " " + amount);
                DCCoin.gain("充值", DCCoinType.DIAMOND, Rsp.add_diamond_num, Rsp.new_diamond_num);
                DCVirtualCurrency.paymentSuccess(Rsp.order_id,"",Rsp.recharge_money_num, "CNY", "支付宝");
                PlayerDataMgrSys.Instance.Diamond = Rsp.new_diamond_num;
                PlayerDataMgrSys.Instance.VipExp = Rsp.new_vip_exp;
                PlayerDataMgrSys.Instance.VipLevel = Rsp.new_vip_level;

                 */

               // Debug.LogError("Rechage Rsp.result == 0 here " + PlayerDataMgrSys.Instance.Diamond + " " + PlayerDataMgrSys.Instance.VipExp + " " + PlayerDataMgrSys.Instance.VipLevel);

//                EventDispatcher eventDistpatcher = EventDispatcher.Instance;
//                if (eventDistpatcher != null)
//                {
//                    Debug.LogError("if (eventDistpatcher != null)");
//                    eventDistpatcher.Dispatch(EnmEvn.EVN_PAY_SUCCESSFUL, null);
//                }
//                UIRechargeSuccess recharge = UI3System.createWindow<UIRechargeSuccess>();
//                if (recharge != null)
//                {
//                    Debug.LogError("recharge != null");
//                    recharge.SetValue((int)Rsp.add_diamond_num);
//                    recharge.bringTop();
//                    recharge.showWithScale();
//                    UIVIP uivip = UI3System.findWindow<UIVIP>();
//                    if (uivip != null && uivip.gameObject.activeInHierarchy == true)
//                    {
//                        Debug.LogError("uivip != null && uivip.gameObject.activeInHierarchy == true");
//                        uivip.updateObj();
//                    }
//                }

//                // 发送充值成功消息
//#if !UNITY_IPHONE
//                LuaManager.Instance.SendMsg(LuaMessage.RechargeSucceed);
//#endif
				//UISdkSys.Instance.OnPay(LoginMgr.Instance.Roleid + "",Rsp.order_id,(int)Rsp.recharge_money_num,"CNY");
            }
        }

        private void OnMineStateInfoNotify(CSMsgResult result, DGPKG msg)
        {
            Debug.LogError("OnMineStateInfoNotify " + result);
            if (result == CSMsgResult.NoError && msg != null && msg.header.msg_full_name == typeof(CS.MineStateInfoNotify).FullName)
            {
                Debug.LogError("OnMineStateInfoNotify " + msg.header.msg_full_name);
                var Rsp = (CS.MineStateInfoNotify)msg.body;
                if (Rsp == null)
                {
                    Debug.LogError("OnMineStateInfoNotify Rsp == null");
                    return;
                }
                // 发送充值成功消息
                //#if !UNITY_IPHONE
                //LuaManager.Instance.SendMsg(LuaMessage.MineStateInfoNotify,Rsp);
                //#endif

            }
        }
    }
}
