using dgame;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using WLGame;

// 网络数据包
public class ShortNetPack
{
    private byte[] _data = null;
    private int _size = 0;
    private int _writeIndex = 0;

    public ShortNetPack()
    {
    }

    public ShortNetPack(int size)
    {
        _data = new byte[size];
        _size = size;
        _writeIndex = 0;
    }

    ~ShortNetPack()
    {
        _data = null;
    }

    public string Name
    {
        set;
        get;
    }

//    public void SetStringBuffer(LuaStringBuffer data)
//    {
//        _data = data.buffer;
//        _size = _data.Length;
//        _writeIndex = _size;
//    }

//    public LuaStringBuffer GetStringBuffer()
//    {
//        return new LuaStringBuffer(_data);
//    }

    // 返回写入数据的长度
    public int Write(byte[] data, int index, int length)
    {
        if (index >= length)
            return 0;

        int writeLen = length - index;
        if (_writeIndex + writeLen > _size)
            writeLen = _size - _writeIndex;

        Buffer.BlockCopy(data, index, _data, _writeIndex, writeLen);
        _writeIndex += writeLen;

        return writeLen;
    }

    public bool IsFull()
    {
        return (_data != null && _writeIndex == _size);
    }

    public byte[] GetBytes()
    {
        return _data;
    }

    public int GetLength()
    {
        return _data.Length;
    }
}

class ShortConnect : MonoBehaviour
{
    private static GameObject m_parentGameObject = null;
    private const int BUFFER_LEN = 4096;
    private Socket m_Socket;
    private Action<ShortNetPack> m_RspCallback = null;
    private byte[] m_SendBuffer = null;                     //发送数据包的缓冲区
    private byte[] m_RecvBuffer = new byte[BUFFER_LEN];     //接受数据包的缓冲区
    private ShortNetPack m_NetPack = null;
    private float m_timeOut = 5.0f;

    public static void CreateSesion(ShortNetPack sendPack, Action<ShortNetPack> callback)
    {
        if (m_parentGameObject == null)
        {
            m_parentGameObject = new GameObject("ShortConnectMgr");
            GameObject.DontDestroyOnLoad(m_parentGameObject);
        }

        string sesionName = "Sesion" + (m_parentGameObject.transform.childCount + 1).ToString();
        GameObject sesionGo = new GameObject(sesionName);
        sesionGo.transform.parent = m_parentGameObject.transform;

        ShortConnect connect = sesionGo.AddComponent<ShortConnect>();
        byte[] msgData = connect.BuildMsgPack(sendPack);
        connect.StartConnect(msgData, callback);
    }

    byte[] BuildMsgPack(ShortNetPack sendPack)
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(stream);

        // 创建包头
        protocol.Header header = new protocol.Header();
        header.seq = ProtocolDGlUtility.GetNextSequeue();
        header.sessionid = LoginMgr.Instance.Roleid.ToString();
        header.roleid = LoginMgr.Instance.Roleid;
        header.uid = LoginMgr.Instance.s_uid;
        header.msg_full_name = sendPack.Name;

        // 写入包头
        stream.Position = sizeof(uint) * 2;
        //ProtoBuf.Serializer.Serialize(stream, header);
        uint headerLength = (uint)(stream.Length - sizeof(uint) * 2);
        // 写入包体
        binaryWriter.Write(sendPack.GetBytes());
        uint totalLength = (uint)stream.Length;
        // 写入长度
        stream.Position = 0;
        binaryWriter.Write(totalLength);
        binaryWriter.Write(headerLength);
        binaryWriter.Flush();

        byte[] result = stream.ToArray();
        return result;
    }

    ShortNetPack ParseMsgPack(byte[] data)
    {
        if (data == null || data.Length < 8)
            return null;

        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);
        int packLen = (int)binaryReader.ReadUInt32();
        int headLen = (int)binaryReader.ReadUInt32();
        int nobodyLen = 8 + headLen;
        int bodyLen = data.Length - nobodyLen;
        if (bodyLen < 0)
        {
            return null;
        }

        stream.Position = nobodyLen;
        byte[] bodyData = binaryReader.ReadBytes(bodyLen);
        ShortNetPack pack = new ShortNetPack(bodyData.Length);
        pack.Write(bodyData, 0, bodyData.Length);
        return pack;
    }

    void StartConnect(byte[] sendata, Action<ShortNetPack> rspCallback)
    {
        //UI3System.lockScreen(902, m_timeOut);
        try
        {
			Debug.LogError("StartConnect");
            string server = LoginMgr.Instance.SvrHost;
            int port = LoginMgr.Instance.Port;
//			IPAddress[] ipAddr = Dns.GetHostAddresses(server);
			IPAddress[] ipAddresses = Dns.GetHostAddresses(server);
			if(ipAddresses == null || ipAddresses.Length <= 0)
			{
				Debug.LogError("StartConnect ipAddresses == null || ipAddresses.Length <= 0");
				return;
			}
			Debug.LogError("ipAddress.AddressFamily " + ipAddresses[0].AddressFamily);
			m_Socket = new Socket(ipAddresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			m_Socket.BeginConnect(ipAddresses[0], port, new AsyncCallback(OnConnectCallBack), null);
        }
        catch (System.Net.Sockets.SocketException e)
        {
            Debug.LogError("Sesion Create Failed, ErrorCode:" + ((SocketError)e.ErrorCode).ToString());
            Shutdown();            
        }

        m_SendBuffer = sendata;
        m_RspCallback = rspCallback;
    }

    void OnDestroy()
    {
        //UI3System.unlockScreen();
    }

    void Update()
    {
        if (m_NetPack != null && m_NetPack.IsFull())
        {
            try
            {
                ShortNetPack pack = ParseMsgPack(m_NetPack.GetBytes());
                m_RspCallback(pack);
                pack = null;
            }
            finally
            {
                m_NetPack = null;
                Shutdown();
            }
            return;
        }

        if ((m_timeOut -= Time.deltaTime) <= 0)
        {
            Shutdown();
        }
    }

    void Shutdown()
    {
        try
        {
            if (m_Socket != null)
            {
                m_Socket.Shutdown(SocketShutdown.Both);
                m_Socket.Close();
            }
            m_Socket = null;
        }
        catch (System.Net.Sockets.SocketException)
        {
            m_Socket = null;
        }

        m_SendBuffer = null;
        m_RecvBuffer = null;
        m_NetPack = null;

        Destroy(gameObject);
    }

    void OnConnectCallBack(IAsyncResult ar)
    {
        try
        {
            m_Socket.EndConnect(ar);
            m_Socket.BeginSend(m_SendBuffer, 0, m_SendBuffer.Length, 0, new AsyncCallback(OnSendCallBack), 0);
            m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, 0, new AsyncCallback(OnRecvCallBack), 0);
        }
        catch (System.Net.Sockets.SocketException e)
        {
            Debuger.LogError("Sesion Connect Error, ErrorCode : " + ((SocketError)e.ErrorCode).ToString());
        }
    }

    void OnSendCallBack(IAsyncResult ar)
    {
        try
        {
            m_Socket.EndSend(ar);
        }
        catch (System.Net.Sockets.SocketException e)
        {
            Debuger.LogError("Sesion Send Error, ErrorCode : " + ((SocketError)e.ErrorCode).ToString());
        }
    }

    void OnRecvCallBack(IAsyncResult ar)
    {
        try
        {
            int nRecvLength = m_Socket.EndReceive(ar);
            if (nRecvLength == 0)
            {
                return;
            }

            if (m_NetPack == null)
            {
                int nHeadSize = sizeof(uint) * 1;
                if (nRecvLength < nHeadSize)
                {
                    m_Socket.BeginReceive(m_RecvBuffer, nRecvLength, m_RecvBuffer.Length, 0, new AsyncCallback(OnRecvCallBack), 0);
                    return;
                }

                System.IO.MemoryStream stream = new System.IO.MemoryStream(m_RecvBuffer, 0, nHeadSize);
                System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(stream);
                int totalLength = binaryReader.ReadInt32();
                m_NetPack = new ShortNetPack(totalLength);
                stream.Dispose();
                binaryReader.Close();
                binaryReader = null;
                stream = null;
            }

            m_NetPack.Write(m_RecvBuffer, 0, nRecvLength);

            if (m_NetPack.IsFull() == false)
            {
                m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, 0, new AsyncCallback(OnRecvCallBack), 0);
            }
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // SetErrorState(ex.SocketErrorCode);
        }
    }
}
