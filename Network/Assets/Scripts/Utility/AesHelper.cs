using System;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// 这里定义了一个用于AES加密和解密的类AesHelper。
/// 有必要对本接口作如下说明：
/// 1. 本接口的实现中，所使用的AES算法为最流行的Rijndael
/// 2. keysize必须是256比特位、blocksize必须是128比特位、mode必须是CBC、padding mode必须是PKCS7
/// </summary>
/// <Author> xubing </Author>
/// <date> 2015.08.27 </date>
public class AesHelper
{
    /// <summary>
    /// 构造方法
    /// </summary>
    public AesHelper(byte[] key, byte[] iv)
    {
        aesInstance = new RijndaelManaged();
        aesInstance.Mode = CipherMode.CBC;
        aesInstance.KeySize = 256;
        aesInstance.BlockSize = 128;
        aesInstance.Padding = PaddingMode.PKCS7;
        this.key = key;
        this.iv = iv;
    }

    /// <summary>
    /// 加密
    /// </summary>
    public byte[] Encrypt(byte[] plainText)
    {
        ICryptoTransform encryptor = aesInstance.CreateEncryptor(key, iv);
        return Perform(plainText, encryptor);
    }

    /// <summary>
    /// 解密
    /// </summary>
    public byte[] Decrypt(byte[] cipherText)
    {
        ICryptoTransform decryptor = aesInstance.CreateDecryptor(key, iv);
        return Perform(cipherText, decryptor);
    }

    private static byte[] Perform(byte[] data, ICryptoTransform ct)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    private RijndaelManaged aesInstance;
    private byte[] key;
    private byte[] iv;
}
