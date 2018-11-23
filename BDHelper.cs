using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using net91com.Core.Extensions;
using net91com.Core.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com._91ios.BaiDuMiniProgram
{
    public class BDHelper
    {

        public static void JsCode2Json(string jscode)
        {

        }

        /// <summary>
        /// Get_SessionKey
        /// openid session_key
        /// </summary>
        /// <param name="appKey">百度小程序的appKey</param>
        /// <param name="appSec">百度小程序的appSec</param>
        /// <param name="jsocde">Authorization Code</param>
        /// <returns></returns>
        public static Dictionary<string, string> Get_SessionKey(string appKey, string appSec, string jsocde)
        {
            var dic = new Dictionary<string, string>();
            try
            {
                var url = $"https://openapi.baidu.com/nalogin/getSessionKeyByCode?code={jsocde}&client_id={appKey}&sk={appSec}";
                WebClient client = new WebClient();
                var outStr = client.DownloadString(url);
                if (!string.IsNullOrWhiteSpace(outStr))
                {
                    LogHelper.WriteCustomNoAdd("[Get_SessionKey]:" + outStr, "BD_Info\\");
                    var json = JsonConvert.DeserializeObject<JToken>(outStr);
                    if (json != null && json["openid"] != null && json["session_key"] != null)
                    {
                        dic["openid"] = json["openid"].ToString();
                        dic["session_key"] = json["session_key"].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteException("[Get_SessionKey] appKey:" + appKey + "\r\nException Message:" + e.Message + e.Source + e.StackTrace, e);
            }
            return dic;
        }

        /// <summary>
        /// 解密用户信息
        /// 随机填充内容	16字节
        /// 用户数据长度	4字节，大端序无符号32位整型
        /// 用户数据 由用户数据长度描述
        /// app_key 与app_key长度相同
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sessionKey"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AESDecode(string text, string sessionKey, string iv)
        {
            //对称解密使用的算法为 AES-192 - CBC，数据采用PKCS#7填充；
            //对称解密的目标密文为 Base64_Decode(data)；
            //对称解密秘钥 AESKey = Base64_Decode(session_key), AESKey 是24字节；
            //对称解密算法初始向量 为Base64_Decode(iv)，其中iv由数据接口返回。

            var dic = new Dictionary<string, string>();
            byte[] aesKey = Convert.FromBase64String(sessionKey);
            byte[] ivBt = Convert.FromBase64String(iv);
            byte[] encrypted = Convert.FromBase64String(text);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = aesKey;
            rDel.IV = ivBt;
            rDel.Mode = CipherMode.CBC;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(encrypted, 0, encrypted.Length);
            var datalen = BitConverter.ToInt32(resultArray, 16);
            var userData = Encoding.UTF8.GetString(resultArray, 20, datalen);
            var appKey = Encoding.UTF8.GetString(resultArray, 20 + datalen, resultArray.Length - 20 - datalen);
            dic["datalen"] = datalen.ToString();
            dic["userData"] = userData;   //nickName(用户名) avatarUrl(用户头像) gender(性别:值为0时是女性，为1时是男性。)
            dic["appKey"] = appKey;
            return dic;
        }

        /// <summary>
        /// 支付状态查询
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appKey"></param>
        /// <param name="bdOrderId"></param>
        /// <param name="bdSiteId"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static bool SearchOrderPayStatus(long appId, string appKey, long bdOrderId, long bdSiteId, string privateKey)
        {
            try
            {
                Hashtable ht = new Hashtable
                {
                    ["appKey"] = appKey,
                    ["appId"] = appId.ToString(),
                    ["orderId"] = bdOrderId.ToString(),
                    ["siteId"] = bdSiteId.ToString(),
                };

                var resp = new BDResponseHandler(ht, null, privateKey);
                var sign = resp.GetRSASign();
                var url = $"https://dianshang.baidu.com/platform/entity/openapi/queryorderdetail?appId={appId}&appKey={appKey}&orderId={bdOrderId}&siteId={bdSiteId}&sign={sign}";
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                var outStr = client.DownloadString(url);
                LogHelper.WriteCustomNoAdd("[SearchOrderPayStatus]:" + outStr, "BD_Info\\");
                if (!string.IsNullOrWhiteSpace(outStr))
                {
                    var json = JsonConvert.DeserializeObject<JToken>(outStr);
                    if (json != null && json["errno"].ToInt32() == 0)  //请求成功
                    {
                        //payStatus -1未支付,1支付成功    //refundStatus -1未退费,1退费中,2退费成功,9退费失败  //verification -1未核销,1已核销
                        return json["data"]["data"]["payStatus"]["statusNum"].ToInt32() == 1 && json["data"]["data"]["refundStatus"]["statusNum"].ToInt32() == -1 && json["data"]["data"]["verification"]["statusNum"].ToInt32() == -1;
                    }
                }
            }
            catch (Exception e)
            {
                var obj = new { appId = appId, appKey = appKey, bdOrderId = bdOrderId, bdSiteId = bdSiteId, Exception = e };
                LogHelper.WriteException("[SearchOrderPayStatus] " + JsonConvert.SerializeObject(obj), e);
            }
            return false;
        }



    }
}
