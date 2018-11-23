# baidu_minprogram_pay
# 百度小程序支付



#### 说明

```
AlipaySignature.cs：RSA操作类
BDResponseHandler.cs：主要RSA参数排序、加签和验签
BDHelper.cs：获取百度的SessionKey(AESDecode和SearchOrderPayStatus暂时还没空测试，建议不要用这个)
```

### 支付下单示例：

```
//准备签名参数，不需要排序
var ht = new Hashtable
{
	["appKey"] = HttpUtility.UrlEncode(BD_Pay_AppKey),
	["dealId"] = HttpUtility.UrlEncode(BD_Pay_DealId),
	["tpOrderId"] = HttpUtility.UrlEncode(out_trade_no),
};

//初始化RSA签名类
var resp = new BDResponseHandler(ht, BD_Pay_PublicKey, BD_Dev_PrivateKey);

//初始化订单详细信息，这个使用时需要做JSON系列化
var bizInfo = new
{
	appKey = BD_Pay_AppKey, //百度电商开放平台appKey,用以表示应用身份的唯一ID，在应用审核通过后进行分配，一经分配后不会发生更改，来唯一确定一个应用
	dealId = BD_Pay_DealId, //跳转百度收银台支付必带参数之一，是百度收银台的财务结算凭证，与账号绑定的结算协议一一对应，每笔交易将结算到dealId对应的协议主体。
	tpOrderId = out_trade_no, //业务方订单唯一编号
	totalAmount = price.ToString(), //订单金额，单位为人民币分
	returnData = returnData, //业务方用于透传的业务变量
	displayData = displayData, //收银台定制页面展示属性，非定制业务请置空
	dealTitle = priceremark, //订单的名称
};

//订单详细信息，这个一定要包一层 
var tpData = new
{
	tpData = bizInfo,
};

//订单信息
var orderInfo = new
{
	dealId = BD_Pay_DealId, //跳转百度收银台支付必带参数之一，是百度收银台的财务结算凭证，与账号绑定的结算协议一一对应，每笔交易将结算到dealId对应的协议主体。   
	appKey = BD_Pay_AppKey,  //百度电商开放平台appKey,用以表示应用身份的唯一ID，在应用审核通过后进行分配，一经分配后不会发生更改，来唯一确定一个应用
	totalAmount = price.ToString(),   //订单金额，单位为人民币分
	tpOrderId = out_trade_no,  //业务方订单唯一编号
	dealTitle = priceremark,   //订单的名称
	rsaSign = resp.GetRSASign(),   //对appKey+dealId+tpOrderId进行RSA签名后的数据，防止订单被伪造。
	bizInfo = JsonConvert.SerializeObject(tpData),  //订单详细信息，需要是一个可解析为JSON Object的字符串。字段内容见
};

//给前端使用的数据
var outPutObj = new
{
	orderInfo = orderInfo,
	bannedChannels = "",  //需要隐藏的支付方式  Alipay BDWallet WeChat
};

```





### 【支付回调验签示例】

```
var requHandler = new BDResponseHandler(System.Web.HttpContext.Current, BD_Pay_PublicKey);
var status = requHandler.GetParameter("status").ToInt32();   //1：未支付；2：已支付；-1：订单取消
var totalMoney = requHandler.GetParameter("totalMoney").ToInt32();  //订单的实际金额，单位：分
var tpOrderId = requHandler.GetParameter("tpOrderId");            //业务方订单号
if (status == 2 && !string.IsNullOrWhiteSpace(tpOrderId) && totalMoney > 0 && requHandler.IsBDPaySign())  ////验证签名
{
	//TODO 订单状态和订单金额等数据是否一致

	//TODO 这里是你的业务更新代码

	//isConsumed是否标记核销   1未消费 2已消费  小程序接入为支付成功即消费场景，该字段需设置为2。
    var outObj = new { errno = 0, msg = "success", data = new { isConsumed = 2 } };
} 
```



##### 1、【平台公钥】和【开发者公钥】 之间的概念和使用

```
平台公钥是平台生成的，平台自己保存私钥，开发者保存公钥，主要作用是：让开发者验证百度调用开发者的支付回调 

开发者公钥是开发者生成的，开发者自己保存私钥，公钥提交到平台，主要作用是：让平台校验来自开发者的下单请求 
```

##### 2、下单支付rsaSign参数公式

```
对appKey+dealId+tpOrderId进行RSA签名，不是加密！！！
```

##### 3、开发者公钥和私钥最好用linux平台的openssl生成

##### 4、“设置中心”  -- “开发者公钥” 格式不能换行和不能有头和尾

```
需要删除-----BEGIN RSA    和 --------END RSA ，以及删除换行。说白了就是要整理成一行
```

##### 5、后台任何修改都有可能会导致服务里的审核状态由【已审核】变成【审核中】

```
如果服务状态是【审核中】的话，这时候签名校验一定是失败的，只能等到变成【已审核】再测试
```

##### 6、 下单参数【bizInfo 】对象必需进行系列化

```
里面还需要再包一层，用tpData做为KEY
```


