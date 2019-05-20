using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Web;

namespace AIMLbot.Utils
{
    public class WeChat
    {
        public static WeChat Instance(string appid, string appsecret)
        {
            if (HttpContext.Current.Cache["wechat"] == null)
                HttpContext.Current.Cache["wechat"] = new WeChat(appid, appsecret);
            return (WeChat)HttpContext.Current.Cache["wechat"];
        }
        private string access_token;
        private DateTime access_token_time;
        private string appid;
        private string appsecret;
        private int attemptCount = 0;
        private bool Flag = true;
        private long GetStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
        private HttpHelper http = null;
        public WeChat(string appid, string appsecret)
        {
            this.appid = appid;
            this.appsecret = appsecret;
            this.access_token_time = DateTime.Now;
            this.access_token = "";
            http = new HttpHelper();
        }
        private void init()
        {
            if (string.IsNullOrEmpty(access_token) || access_token_time.AddSeconds(7000) < DateTime.Now)
            {
                JObject token = JObject.Parse(http.GetHtml("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=" + appid + "&secret=" + appsecret));
                if (token["access_token"] != null)
                {
                    access_token = token["access_token"].ToString();
                    access_token_time = DateTime.Now;
                    Flag = true;
                }
                else
                    Flag = false;
            }
        }
        public RequestXML Handle(string postStr)
        {
            //封装请求类  
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(postStr);
            XmlElement rootElement = doc.DocumentElement;
            //MsgType  
            XmlNode MsgType = rootElement.SelectSingleNode("MsgType");
            //接收的值--->接收消息类(也称为消息推送)  
            RequestXML requestXML = new RequestXML();
            requestXML.ToUserName = rootElement.SelectSingleNode("ToUserName").InnerText;
            requestXML.FromUserName = rootElement.SelectSingleNode("FromUserName").InnerText;
            requestXML.CreateTime = rootElement.SelectSingleNode("CreateTime").InnerText;
            requestXML.MsgType = MsgType.InnerText;

            //根据不同的类型进行不同的处理  
            switch (requestXML.MsgType)
            {
                case "text": //文本消息  
                    requestXML.Content = rootElement.SelectSingleNode("Content").InnerText;
                    break;
                case "image": //图片  
                    requestXML.PicUrl = rootElement.SelectSingleNode("PicUrl").InnerText;
                    requestXML.MediaId = rootElement.SelectSingleNode("MediaId").InnerText;
                    break;
                case "event": //事件推送 支持V4.5+  
                    requestXML.Events = rootElement.SelectSingleNode("Event").InnerText;
                    requestXML.EventKey = rootElement.SelectSingleNode("EventKey").InnerText;
                    if (requestXML.Events == "scancode_push")
                    {
                        XmlNode ScanCodeInfo = rootElement.SelectSingleNode("ScanCodeInfo");
                        requestXML.ScanType = ScanCodeInfo.SelectSingleNode("ScanType").InnerText;
                        requestXML.ScanResult = ScanCodeInfo.SelectSingleNode("ScanResult").InnerText;
                    }
                    break;
                case "location": //位置  
                    requestXML.Location_X = rootElement.SelectSingleNode("Location_X").InnerText;
                    requestXML.Location_Y = rootElement.SelectSingleNode("Location_Y").InnerText;
                    requestXML.Scale = rootElement.SelectSingleNode("Scale").InnerText;
                    requestXML.Label = rootElement.SelectSingleNode("Label").InnerText;
                    break;
                case "link": //链接  
                    requestXML.Url = rootElement.SelectSingleNode("Url").InnerText;
                    break;
                case "voice": //语音
                    requestXML.Recognition = rootElement.SelectSingleNode("Recognition").InnerText;
                    requestXML.MediaId = rootElement.SelectSingleNode("MediaId").InnerText;
                    break;
                case "video": //视频
                case "shortvideo": //小视频
                    requestXML.ThumbMediaId = rootElement.SelectSingleNode("ThumbMediaId").InnerText;
                    requestXML.MediaId = rootElement.SelectSingleNode("MediaId").InnerText;
                    break;
            }
            return requestXML;
        }
        public void Response(ResponseMsg msg)
        {
            init();
            if (Flag)
            {
                string postjson = "";
                switch (msg.MsgType)
                {
                    case "text":
                        postjson = "{\"touser\":\"" + msg.ToUser + "\",\"msgtype\":\"text\",\"text\":{\"content\":\"" + msg.Content + "\"}}";
                        break;
                    case "image":
                    case "voice":
                        postjson = "{\"touser\":\"" + msg.ToUser + "\",\"msgtype\":\"" + msg.MsgType + "\",\"" + msg.MsgType + "\":{\"media_id\":\"" + msg.MediaId + "\"}}";
                        break;
                    case "video":
                    case "shortvideo":
                        postjson = "{\"touser\":\"" + msg.ToUser + "\",\"msgtype\":\"video\",\"video\":{\"media_id\":\"" + msg.MediaId + "\",\"thumb_media_id\":\"" + msg.ThumbMediaId + "\",\"title\":\"" + msg.Title + "\",\"description\":\"" + msg.Description + "\"}}";
                        break;
                }
                string returnHtml = http.GetHtml("https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + access_token, postjson, true);
                JObject returnMsg = JObject.Parse(returnHtml);
                if (returnMsg["errcode"].ToString() == "42001" && attemptCount < 2)
                {
                    attemptCount += 1;
                    access_token = "";
                    Response(msg);
                }
                attemptCount = 0;
            }
        }
        public string GetMenu()
        {
            init();
            if (Flag)
                return http.GetHtml("https://api.weixin.qq.com/cgi-bin/menu/get?access_token=" + access_token);
            else
                return string.Empty;
        }
        public bool ModifyMenu(string jsonMenustring)
        {
            init();
            if (Flag)
            {
                JObject result = JObject.Parse(http.GetHtml("https://api.weixin.qq.com/cgi-bin/menu/create?access_token=" + access_token, jsonMenustring, true));
                if ((int)result["errcode"] == 0)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public string Subscribe(string next_openid)
        {
            init();
            if (Flag)
                return http.GetHtml("https://api.weixin.qq.com/cgi-bin/user/get?access_token=" + access_token + "&next_openid=" + next_openid);
            else
                return string.Empty;
        }
        public string GetUserInfo(string openid)
        {
            init();
            if (Flag)
                return http.GetHtml("https://api.weixin.qq.com/cgi-bin/user/info?access_token=" + access_token + "&openid=" + openid + "&lang=zh_CN");
            else
                return string.Empty;
        }
        public string ExecuteUrl(string url)
        {
            init();
            if (Flag)
                return http.GetHtml(string.Format(url, access_token));
            else
                return string.Empty;
        }
        public string ExecuteUrl(string url, string postData)
        {
            init();
            if (Flag)
                return http.GetHtml(string.Format(url, access_token), postData, true);
            else
                return string.Empty;
        }
        #region 生成回复消息
        public string GetNewsXml(RequestXML requestXML, string title, string content, string url)
        {
            string resxml = string.Format(@"<xml>
<ToUserName><![CDATA[{0}]]></ToUserName>
<FromUserName><![CDATA[{1}]]></FromUserName>
<CreateTime>{2}</CreateTime>
<MsgType><![CDATA[news]]></MsgType>
<ArticleCount>1</ArticleCount>
<Articles>
<item>
<Title><![CDATA[{3}]]></Title> 
<Description><![CDATA[{4}]]></Description>
<PicUrl><![CDATA[http://library.gufe.edu.cn/weixin/images/house.jpg]]></PicUrl>
<Url><![CDATA[{5}]]></Url>
</item>
</Articles>
</xml>", requestXML.FromUserName, requestXML.ToUserName, GetStamp(), title, content, url);
            return resxml;
        }
        public string GetText(RequestXML requestXML, string text)
        {
            string resxml = string.Format(@"<xml>
<ToUserName><![CDATA[{0}]]></ToUserName>
<FromUserName><![CDATA[{1}]]></FromUserName>
<CreateTime>{2}</CreateTime>
<MsgType><![CDATA[text]]></MsgType>
<Content><![CDATA[{3}]]></Content>
</xml>", requestXML.FromUserName, requestXML.ToUserName, GetStamp(), text);
            return resxml;
        }
        public string GetImage(RequestXML requestXML, string media_id)
        {
            string resxml = string.Format(@"<xml>
<ToUserName><![CDATA[{0}]]></ToUserName>
<FromUserName><![CDATA[{1}]]></FromUserName>
<CreateTime>{2}</CreateTime>
<MsgType><![CDATA[image]]></MsgType>
<Image>
<MediaId><![CDATA[{3}]]></MediaId>
</Image>
</xml>", requestXML.FromUserName, requestXML.ToUserName, GetStamp(), media_id);
            return resxml;
        }
        public string GetView(RequestXML requestXML, string url)
        {
            string resxml = string.Format(@"<xml>
<ToUserName><![CDATA[{0}]]></ToUserName>
<FromUserName><![CDATA[{1}]]></FromUserName>
<CreateTime>{2}</CreateTime>
<MsgType><![CDATA[url]]></MsgType>
<Url><![CDATA[{3}]]></Url>
</xml>", requestXML.FromUserName, requestXML.ToUserName, GetStamp(), url);
            return resxml;
        }
        #endregion
    }
    public class RequestXML
    {
        private string toUserName = "";
        /// <summary>  
        /// 消息接收方微信号，一般为公众平台账号微信号  
        /// </summary>  
        public string ToUserName
        {
            get { return toUserName; }
            set { toUserName = value; }
        }
        private string fromUserName = "";
        /// <summary>  
        /// 消息发送方微信号  
        /// </summary>  
        public string FromUserName
        {
            get { return fromUserName; }
            set { fromUserName = value; }
        }
        private string createTime = "";
        /// <summary>  
        /// 创建时间  
        /// </summary>  
        public string CreateTime
        {
            get { return createTime; }
            set { createTime = value; }
        }
        private string msgType = "";
        /// <summary>  
        /// 信息类型 地理位置:location,文本消息:text,消息类型:image  
        /// </summary>  
        public string MsgType
        {
            get { return msgType; }
            set { msgType = value; }
        }
        private string content = "";
        /// <summary>  
        /// 信息内容  
        /// </summary>  
        public string Content
        {
            get { return content; }
            set { content = value; }
        }
        private string location_X = "";
        /// <summary>  
        /// 地理位置纬度  
        /// </summary>  
        public string Location_X
        {
            get { return location_X; }
            set { location_X = value; }
        }
        private string location_Y = "";
        /// <summary>  
        /// 地理位置经度  
        /// </summary>  
        public string Location_Y
        {
            get { return location_Y; }
            set { location_Y = value; }
        }
        private string scale = "";
        /// <summary>  
        /// 地图缩放大小  
        /// </summary>  
        public string Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        private string label = "";
        /// <summary>  
        /// 地理位置信息  
        /// </summary>  
        public string Label
        {
            get { return label; }
            set { label = value; }
        }
        private string picUrl = "";
        /// <summary>  
        /// 图片链接，开发者可以用HTTP GET获取  
        /// </summary>  
        public string PicUrl
        {
            get { return picUrl; }
            set { picUrl = value; }
        }
        private string events = "";
        /// <summary>  
        /// 事件
        /// </summary>  
        public string Events
        {
            get { return events; }
            set { events = value; }
        }
        private string eventKey = "";
        /// <summary>  
        /// 事件内容
        /// </summary>  
        public string EventKey
        {
            get { return eventKey; }
            set { eventKey = value; }
        }
        private string recognition = "";
        /// <summary>  
        /// 语音内容
        /// </summary>  
        public string Recognition
        {
            get { return recognition; }
            set { recognition = value; }
        }
        private string mediaId = "";
        /// <summary>  
        /// 资源ID
        /// </summary>  
        public string MediaId
        {
            get { return mediaId; }
            set { mediaId = value; }
        }
        private string thumbMediaId = "";
        /// <summary>  
        /// 资源ID
        /// </summary>  
        public string ThumbMediaId
        {
            get { return thumbMediaId; }
            set { thumbMediaId = value; }
        }
        private string url = "";
        /// <summary>  
        /// 连接信息
        /// </summary>  
        public string Url
        {
            get { return url; }
            set { url = value; }
        }
        private string scanType = "";
        /// <summary>  
        /// 扫描类型
        /// </summary>  
        public string ScanType
        {
            get { return scanType; }
            set { scanType = value; }
        }
        private string scanResult = "";
        /// <summary>  
        /// 扫描结果
        /// </summary>  
        public string ScanResult
        {
            get { return scanResult; }
            set { scanResult = value; }
        }
    }
    public class ResponseMsg
    {
        private string touser = "";
        //发送到用户
        public string ToUser
        {
            get { return touser; }
            set { touser = value; }
        }
        private string msgtype = "";
        /// <summary>
        /// 类型
        /// </summary>
        public string MsgType
        {
            get { return msgtype; }
            set { msgtype = value; }
        }
        private string content = "";
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Content
        {
            get { return content; }
            set { content = value; }
        }
        private string mediaId = "";
        /// <summary>  
        /// 资源ID
        /// </summary>  
        public string MediaId
        {
            get { return mediaId; }
            set { mediaId = value; }
        }
        private string thumbMediaId = "";
        /// <summary>  
        /// 资源ID
        /// </summary>  
        public string ThumbMediaId
        {
            get { return thumbMediaId; }
            set { thumbMediaId = value; }
        }
        private string title = "";
        /// <summary>  
        /// 标题
        /// </summary>  
        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string description = "";
        /// <summary>  
        /// 描述
        /// </summary>  
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
    }
}
