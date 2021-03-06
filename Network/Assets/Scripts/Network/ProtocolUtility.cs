using System;
using System.Collections;
using tsf4g_tdr_csharp;
using WLGame;
using UnityEngine;

// 删除
using CS;
using protocol;

public class ProtocolDGlUtility
{
    public static uint s_seq = 0;
    //public static string    s_account;
    public static ulong      s_roleid = 0;
    public static string    s_sessionid = "";
    //public static ulong     s_uid;


    // 帮忙记下一些需要记下的数据，可能不应该放在这里。
    //public static ulong s_uid;	//账户的唯一标识, account -- uid 有一个唯一的对应关系
    //public static PLAT_NUM s_plat;
    //public static ulong s_body_roleid;		//角色唯一标识


    /// <summary>
    /// 根据msgId来生成一个数据包
    /// </summary>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public static net.DGPKG BuildDGMsg(object body)
    {
        net.DGPKG pkg = new net.DGPKG();
        Header header = new Header();
        header.seq = GetNextSequeue();
        header.sessionid = LoginMgr.Instance.SessionId;
        header.roleid = LoginMgr.Instance.Roleid;
        header.uid = LoginMgr.Instance.s_uid;

        pkg.header = header;
        pkg.body = body;

        return pkg;
    }

#if false
    /// <summary>
    /// 解析数据，将byte[]转换为CSPKG数据结构
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static System.Object UnpackDGMsg(byte[] data, int recvLength, ref int parsedLength)
    {
        parsedLength = 0;

        if (recvLength >= sizeof(uint) * 2)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream(data, 0, sizeof(uint) * 2);
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);
            uint needLen = binaryReader.ReadUInt32();

            if (recvLength < needLen)
            {
                return null;
            }

            net.DGPKG msg = new net.DGPKG();
            msg.doDeserialize(data);
//             if (msg.header.msg_full_name == typeof(AccountLoginRsp).FullName)
//             {
//                 AccountLoginRsp tmp = (AccountLoginRsp)msg.body;
//                 //s_account = tmp.account;
//                 LoginMgr.Instance.s_uid = tmp.uid;
//                 s_sessionid = tmp.sessionid;
//             }
            parsedLength = (int)msg.totalLength;
            return msg;
        }

        return null;
    }
#endif

    /// <summary>
    /// 解析数据，将byte[]转换为CSPKG数据结构
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static System.Object UnpackDGMsg(byte[] data, int recvLength, ref int parsedLength)
    {
        parsedLength = 0;

        if (recvLength >= sizeof(uint) * 2)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream(data, 0, sizeof(uint));
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);

            // 整个包的长度
            int packetLen = (int)binaryReader.ReadUInt32();
            if (recvLength < packetLen)
            {
                return null;
            }

            // 整个包
            byte[] packet = new byte[packetLen];
            System.Buffer.BlockCopy(data, 0, packet, 0, packetLen);

            // 过滤
            byte[] finalPacket = LoginMgr.Instance.FilterReceivedBytes(packet);

            // 反序列化
            net.DGPKG msg = new net.DGPKG();
            msg.doDeserialize(finalPacket);
            parsedLength = packetLen;
            return msg;
        }

        return null;
    }

    public static uint GetNextSequeue()
    {
        ++s_seq;
        if (s_seq >= (ushort)Network.kCSMsgCallArrayLength)
        {
            s_seq = 1;
        }
        return s_seq;
    }

    public static void Reset()
    {
        s_seq = 0;
        s_roleid = 0;
        s_sessionid = "";
    }
}