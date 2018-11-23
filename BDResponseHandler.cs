using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using net91com.Core.Util;
using Newtonsoft.Json;

namespace com._91ios.BaiDuMiniProgram
{
    /// <summary>
    /// 百度电商平台 - 支付签名和验签处理类
    /// </summary>
    public class BDResponseHandler
    {
        /// <summary>
        /// 需要签名或验签的列表（不考虑排序）
        /// </summary>
        protected Hashtable Parameters = new Hashtable();

        /// <summary>
        /// 平台公钥【在我的服务里】
        /// </summary>
        protected string PublicKey;

        /// <summary>
        /// 开发者私钥【用openssl生成，然后私钥自已保存，公钥粘贴到“设置中心”--》“开发者公钥”，注意：这里的公钥要删除头和尾以及换行，说白了就是整理成一行】
        /// </summary>
        protected string PrivateKey;


        protected HttpContext HttpContext;


        /// <summary>
        /// 百度支付，根据请求URL，解析返回参数和签名检验
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="publicKey">签名私钥</param>
        public BDResponseHandler(HttpContext httpContext, string publicKey)
        {
            this.HttpContext = httpContext ?? HttpContext.Current;
            NameValueCollection collection;
            //post data
            if (this.HttpContext.Request.HttpMethod.ToUpper() == "POST")
            {
                collection = this.HttpContext.Request.Form;
                PublicKey = publicKey;
                foreach (string k in collection)
                {
                    string v = (string)collection[k];
                    this.SetParameter(k, v);
                }
            }
        }

        /// <summary>
        /// 获取所有的参数配置
        /// </summary>
        /// <returns></returns>
        public Hashtable GetParameters()
        {
            return Parameters;
        }


        /// <summary>
        /// 初始化工具类
        /// </summary>
        /// <param name="ht">需要签名或验签的列表（不考虑排序）</param>
        /// <param name="publicKey">平台公钥</param>
        /// <param name="privateKey">开发都私钥</param>
        public BDResponseHandler(Hashtable ht, string publicKey = null, string privateKey = null)
        {
            Parameters = ht;
            if (!string.IsNullOrWhiteSpace(publicKey))
                PublicKey = publicKey;
            if (!string.IsNullOrWhiteSpace(privateKey))
                PrivateKey = privateKey;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public string GetParameter(string parameter)
        {
            string s = (string)Parameters[parameter];
            return (null == s) ? "" : s;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="parameterValue"></param>
        public void SetParameter(string parameter, string parameterValue)
        {
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                if (Parameters.Contains(parameter))
                {
                    Parameters.Remove(parameter);
                }

                Parameters.Add(parameter, parameterValue);
            }
        }

        /// <summary>
        /// 用私钥获取RSA签名后的BASE64数据
        /// </summary>
        /// <returns></returns>
        public string GetRSASign()
        {
            var akeys = new string[Parameters.Keys.Count];
            Parameters.Keys.CopyTo(akeys, 0);
            IDictionary<string, string> paramsMap = new Dictionary<string, string>();
            foreach (string k in akeys)
            {
                string v = (string)Parameters[k];
                if (String.Compare("rsaSign", k, StringComparison.CurrentCultureIgnoreCase) != 0)  //这里要去掉rsaSign，防止误传
                {
                    paramsMap.Add(k, v);
                }
            }

            if (string.IsNullOrWhiteSpace(PrivateKey))
            {
                throw new Exception("请求方法【GetRSASign】 必须传参数【PrivateKey】");
            }
            
            return AlipaySignature.RSASign(paramsMap, PrivateKey, "utf-8", false, "RSA"); //AlipaySignature.RSASign(paramsMap, PrivateKey, "utf-8", true, "RSA");
        }



        /// <summary>
        /// 使用平台公钥验签，数据列表里一定要包含rsaSign
        /// </summary>
        /// <returns></returns>
        public Boolean IsBDPaySign()
        {
            var akeys = new string[Parameters.Keys.Count];
            Parameters.Keys.CopyTo(akeys, 0);
            IDictionary<string, string> paramsMap = new Dictionary<string, string>();
            foreach (string k in akeys)
            {
                paramsMap.Add(k, (string)Parameters[k]);
            }

            if (string.IsNullOrWhiteSpace(PublicKey))
            {
                throw new Exception("请求方法【IsBDPaySign】 必须传参数【PublicKey】");
            }

            //var rsaSign = GetParameter("rsaSign");
            //LogHelper.WriteCustomNoAdd("paramsMap:" + AlipaySignature.GetSignContent(paramsMap), "IsBDPaySign_Info\\");
            var flag = AlipaySignature.RSACheckBD(paramsMap, PublicKey, "utf-8", "RSA", false);

            return flag;

        }









    }
}
