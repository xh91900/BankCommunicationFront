using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    class CPPDLL
    {
        //加密或解密
        [DllImport("TriDES.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RunDesFile(string InFileName, string OutFileName, int bType, int bMode, string Key, int keylen);

        //生成CRC校验
        [DllImport("TriDES.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint CRC_GetCRC(byte[] data, uint crcval, int len);

        //将CRC校验码装换为字符串
        [DllImport("TriDES.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CRC_ToStr(uint crcval);
    }

    /// <summary>
    /// Des加密与解密
    /// </summary>
    public class DESEncrypt
    {
        /// <summary>
        /// locker对象
        /// </summary>
        private static object locker = new object();

        string Key;
        int keylen = 16;

        public DESEncrypt(string key)
        {
            Key = key;
        }
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="inFileName">源文件</param>
        /// <param name="outFileName">生成文件</param>
        /// <returns>bool</returns>
        public bool Encrypt(string inFileName, string outFileName)
        {
            //返回值0表示加密成功，-1加密失败
            int result = CPPDLL.RunDesFile(inFileName, outFileName, 0, 1, Key, keylen);
            return result >= 0;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="inFileName">源文件</param>
        /// <param name="outFileName">生成文件</param>
        /// <returns>bool</returns>
        public bool Decrypt(string inFileName, string outFileName)
        {
            int result = CPPDLL.RunDesFile(inFileName, outFileName, 1, 1, Key, keylen);
            return result >= 0;   
        }

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <returns>string</returns>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public static string GetCRC(byte[] data)
        {
            try
            {
                lock (locker)
                {
                    int len = data.Length;
                    uint crc = CPPDLL.CRC_GetCRC(data, 0, len);
                    IntPtr strcrc = System.Runtime.InteropServices.Marshal.AllocHGlobal(8);
                    strcrc = CPPDLL.CRC_ToStr(crc);
                    return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(strcrc); 
                }
            }
            catch (AccessViolationException e)
            {
                LogMessage.GetLogInstance().LogError(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// 得到三位数的随机数
        /// </summary>
        /// <returns>三位数的随机数</returns>
        public static int GetRoadom()
        {
            Random rand = new Random();
            return rand.Next(100, 999);
        }
    }
}
