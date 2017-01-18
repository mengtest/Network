using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

class Http : MonoBehaviour
{
    private static Http m_instance = null;
    private Dictionary<string, string> m_getfieldDict = new Dictionary<string, string>();
    private Dictionary<string, string> m_postfieldDict = new Dictionary<string, string>();
    private Action<bool, string> m_postcallback = null;
    private Action<bool, string> m_getcallback = null;
    private string m_url = "";
    private string m_cookie = "";

    public string cookie
    {
        get { return m_cookie; }
    }

    public static Http Instance()
    {
        if (m_instance == null)
        {
            GameObject go = new GameObject("HTTP");
            m_instance = go.AddComponent<Http>();
            DontDestroyOnLoad(go);
        }
        return m_instance;
    }

    public void AddField(string key, string value)
    {
        m_postfieldDict.Add(key, value);
    }

    public void AddGetField(string key, string value)
    {
        m_getfieldDict.Add(key, value);
    }
    public void Post(string url, Action<bool, string> callback,bool needTips=true,bool isHaveCookie=true)
    {
		//Debug.LogError("url " + url);
        if(needTips == true)
        {
            //UI3System.lockScreen(902/*"正在连接服务器。。。"*/, 5.0f);
        }
        m_url = url;
        m_postcallback = callback;
        if (isHaveCookie)
        {
            StartCoroutine(POST(url, m_postfieldDict));
            m_postfieldDict.Clear();
        }
        else
        {
            StartCoroutine(POST(url));
            m_postfieldDict.Clear();
        }
		//StartCoroutine(StartGet(url, m_fieldDict));
    }

    void OnDestroy()
    {
        m_instance = null;
    }

    IEnumerator POST(string url, Dictionary<string, string> fields)
    {
        //Debug.Log("POST " + url);
        WWWForm form = new WWWForm();
        foreach (KeyValuePair<string, string> field in fields)
        {
            form.AddField(field.Key, field.Value);
        }

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Cookie", m_cookie);
        //Hashtable headers = new Hashtable();
        //headers.Add("Cookie", m_cookie);
        byte[] rawData = form.data;
        //Debug.Log("Post WWW");
        WWW www = new WWW(url, rawData, headers);
        while (!www.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        //// 保存cookie
        if (www.responseHeaders.ContainsKey("SET-COOKIE"))
        {
            m_cookie = www.responseHeaders["SET-COOKIE"];
            //Debug.Log("m_cookie " + m_cookie);
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            //POST请求失败
            Debug.Log("Post WWW Error : " + www.error);
            Debug.Log("Post WWW Text : " + www.text);
            OnCallback(false, www.text);
        } else {
            //POST请求成功
            OnCallback(true, www.text);
        }
    }

    IEnumerator POST(string url)
    {
        //Debug.LogError("url " + url);
        WWW www = new WWW(url);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            //POST请求失败
            OnCallback(false, www.error);
        }
        else
        {
            //POST请求成功
            OnCallback(true, www.text);
        }
    }

    void OnCallback(bool success, string text)
    {
        //Debug.Log("post OnCallback " + success + " " + text);
        Action<bool, string> callback = m_postcallback;
        if (callback != null)
        {
            callback(success, text);
            if (callback == m_postcallback)
            {
                m_postcallback = null;
            }
        }
        //UI3System.unlockScreen();
    }
	
    void OnGetCallback(bool success, string text)
    {
        //Debug.Log("OnGetCallback " + success + " " + text);
        Action<bool, string> callback = m_getcallback;
        if (callback != null)
        {
            callback(success, text);
            if (callback == m_getcallback)
            {
                m_getcallback = null;
            }
        }
        //UI3System.unlockScreen();
    }
    public void Get(string url, Action<bool, string> callback, bool needTips = true)
    {
        if (needTips == true)
        {
            //UI3System.lockScreen(902/*"正在连接服务器。。。"*/, 5.0f);
        }

        m_url = url;
        m_getcallback = callback;
        StartCoroutine(StartGet(url, m_getfieldDict));
        m_getfieldDict.Clear();
        //Debug.Log("Get");
    }
	public IEnumerator StartGet(string url,Dictionary<string,string> args)
    {
        //Debug.Log("StartGet " + url);
        List<string> keys = new List<string>(args.Keys);
        if(keys.Count > 0)
        {
            url += "?";
        }
        for(int i = 0;i < keys.Count;i++)
        {
            url += keys[i] + "=" + args[keys[i]] + "&";
        }
        if(keys.Count > 0)
        {
            url = url.Substring(0, url.Length - 1);
        }
        //Debug.Log("StartGet " + url);
        WWW www = new WWW(url);
        yield return www;
        if(string.IsNullOrEmpty(www.error) == true)
        {
            Debug.Log("StartGet www ok");
        }
        //Debug.Log("StartGet " + www.error + " " + www.text);
        //Debug.Log("StartGet " + keys.Count);
        OnGetCallback(string.IsNullOrEmpty(www.error), www.text);
    }

}
