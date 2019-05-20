using System;
using System.IO;
using System.Net;
using System.Text;

namespace ConsoleBot
{
    public class HttpHelper
    {
        #region 属性
        private string accept;
        private System.Net.CookieContainer cc;
        private int connectTimeOutRetryTryMaxTimes;
        private string contentType;
        private int currentTry;
        private System.Text.Encoding encoding;
        private Uri responseUri;
        private string userAgent;
        private IWebProxy webProxy = null;
        #endregion
        #region 构造函数
        public HttpHelper()
        {
            this.contentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            this.userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1) Gecko/20090624 Firefox/3.5";
            this.encoding = System.Text.Encoding.UTF8;
            this.connectTimeOutRetryTryMaxTimes = 5;
            this.cc = new System.Net.CookieContainer();
        }
        public HttpHelper(System.Net.CookieContainer cc)
        {
            this.contentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            this.userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1) Gecko/20090624 Firefox/3.5";
            this.encoding = System.Text.Encoding.GetEncoding("utf-8");
            this.connectTimeOutRetryTryMaxTimes = 5;
            this.cc = cc;
        }
        public HttpHelper(string contentType, string accept, string userAgent)
        {
            this.contentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            this.userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1) Gecko/20090624 Firefox/3.5";
            this.encoding = System.Text.Encoding.GetEncoding("utf-8");
            this.connectTimeOutRetryTryMaxTimes = 5;
            this.contentType = contentType;
            this.accept = accept;
            this.userAgent = userAgent;
        }
        public HttpHelper(System.Net.CookieContainer cc, string contentType, string accept, string userAgent)
        {
            this.contentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            this.userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.1) Gecko/20090624 Firefox/3.5";
            this.encoding = System.Text.Encoding.GetEncoding("utf-8");
            this.connectTimeOutRetryTryMaxTimes = 5;
            this.cc = cc;
            this.contentType = contentType;
            this.accept = accept;
            this.userAgent = userAgent;
        }
        #endregion
        #region 函数
        private int GetDynamicDelay()
        {
            return 0;
        }
        public string GetHtml(string url)
        {
            return this.GetHtml(url, this.cc);
        }
        public string GetHtml(string url, System.Net.CookieContainer cookieContainer)
        {
            this.currentTry++;
            HttpWebResponse response = null;
            Stream responseStream = null;
            StreamReader reader = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CookieContainer = cookieContainer;
                request.ContentType = this.contentType;
                request.Accept = this.accept;
                request.UserAgent = this.userAgent;
                request.Method = "GET";
                if (this.webProxy != null)
                {
                    request.Proxy = this.webProxy;
                }
                response = (HttpWebResponse)request.GetResponse();
                this.responseUri = response.ResponseUri;
                responseStream = response.GetResponseStream();
                reader = new StreamReader(responseStream, this.encoding);
                string str = reader.ReadToEnd();
                this.currentTry = 0;
                return str;
            }
            catch (Exception exception)
            {
                if (this.currentTry <= this.connectTimeOutRetryTryMaxTimes)
                {
                    this.GetHtml(url, cookieContainer);
                }
                this.currentTry = 0;
                return string.Empty;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (responseStream != null)
                    responseStream.Close();
                if (response != null)
                    response.Close();
            }
        }
        public string GetHtml(string url, string postData, bool isPost)
        {
            return this.GetHtml(url, postData, isPost, this.cc);
        }
        public string GetHtml(string url, string postData, bool isPost, System.Net.CookieContainer cookieContainer)
        {
            this.currentTry++;
            Stream responseStream = null;
            StreamReader reader = null;

            try
            {
                byte[] bytes = this.Encoding.GetBytes(postData);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CookieContainer = cookieContainer;
                request.ContentType = this.contentType;
                request.Accept = this.accept;
                request.UserAgent = this.userAgent;
                request.Method = isPost ? "POST" : "GET";
                request.ContentLength = bytes.Length;
                if (this.webProxy != null)
                {
                    request.Proxy = this.webProxy;
                }
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
                responseStream = ((HttpWebResponse)request.GetResponse()).GetResponseStream();
                reader = new StreamReader(responseStream, this.encoding);
                string str = reader.ReadToEnd();

                this.currentTry = 0;
                return str;
            }
            catch (Exception exception)
            {
                if (this.currentTry <= this.connectTimeOutRetryTryMaxTimes)
                {
                    this.GetHtml(url, postData, isPost, cookieContainer);
                }
                this.currentTry = 0;
                return string.Empty;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (responseStream != null) responseStream.Close();
            }
        }
        #endregion
        #region 属性
        public int ConnectTimeOutRetryTryMaxTimes
        {
            get { return this.connectTimeOutRetryTryMaxTimes; }
            set { this.connectTimeOutRetryTryMaxTimes = value; }
        }
        public System.Net.CookieContainer CookieContainer
        {
            get { return this.cc; }
            set { this.cc = value; }
        }
        public System.Text.Encoding Encoding
        {
            get { return this.encoding; }
            set { this.encoding = value; }
        }
        public Uri ResponseUri
        {
            get { return this.responseUri; }
            set { this.responseUri = value; }
        }
        #endregion
    }
}