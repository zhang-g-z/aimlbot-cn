using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIMLbot.Utils;
using System.Xml;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Tagx.Common
{
    [CustomTagAttribute]
    public class wechat : AIMLTagHandler
    {
        public wechat() : base() { }
        protected override string ProcessChange()
        {
            if (this.templateNode.Attributes.Count == 1 && this.templateNode.Attributes[0].Name.ToLower() == "to")
            {
                string toOpenid = this.templateNode.Attributes[0].Value;
                StringBuilder templateResult = new StringBuilder();
                foreach (XmlNode childNode in this.templateNode.ChildNodes)
                {
                    if (childNode.Name == "get")
                    {
                        if (childNode.Attributes.Count == 1 && childNode.Attributes[0].Name.ToLower() == "name" && this.user.Predicates.containsSettingCalled(childNode.Attributes[0].Value))
                        {
                            templateResult.Append(this.user.Predicates.grabSetting(childNode.Attributes[0].Value));
                        }
                    }
                    else if (childNode.Name == "star")
                    {
                        if (childNode.Attributes.Count == 1 && childNode.Attributes[0].Name.ToLower() == "index")
                        {
                            int index = Convert.ToInt32(childNode.Attributes[0].Value);
                            index--;
                            if ((index >= 0) & (index < this.query.InputStar.Count))
                            {
                                templateResult.Append(this.query.InputStar[index]);
                            }
                        }
                    }
                    else
                    {
                        templateResult.Append(childNode.InnerText);
                    }
                }
                string text = templateResult.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    if (this.bot.wechat != null)
                    {
                        //发送客服消息
                        ThreadPool.QueueUserWorkItem(delegate(object obj)
                        {
                            ResponseMsg msg = new ResponseMsg();
                            msg.ToUser = toOpenid;
                            msg.MsgType = "text";
                            msg.Content = text;
                            this.bot.wechat.Response(msg);
                        });
                    }
                }
            }
            return string.Empty;
        }
    }
}
