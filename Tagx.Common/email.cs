using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIMLbot.Utils;
using System.Xml;
using System.Threading;

namespace Tagx.Common
{
    [CustomTagAttribute]
    public class email : AIMLTagHandler
    {
        public email() : base() { }
        protected override string ProcessChange()
        {
            if (this.templateNode.Attributes.Count == 1 && this.templateNode.Attributes[0].Name.ToLower() == "to")
            {
                string toEmail = this.templateNode.Attributes[0].Value;
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
                    if (this.bot.GlobalSettings.containsSettingCalled("email_user")
                && this.bot.GlobalSettings.containsSettingCalled("email_pop")
                && this.bot.GlobalSettings.containsSettingCalled("email_password")
                && this.bot.GlobalSettings.containsSettingCalled("email_smtp"))
                    {
                        //发送邮件
                        ThreadPool.QueueUserWorkItem(delegate(object obj)
                        {
                            string myMail = this.bot.GlobalSettings.grabSetting("email_user");
                            System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
                            msg.IsBodyHtml = false;
                            msg.From = new System.Net.Mail.MailAddress(myMail);
                            msg.To.Add(new System.Net.Mail.MailAddress(toEmail));
                            msg.Subject = "邮件通知";
                            msg.SubjectEncoding = Encoding.UTF8;
                            msg.Body = text;
                            msg.BodyEncoding = Encoding.UTF8;
                            System.Net.Mail.SmtpClient sm = new System.Net.Mail.SmtpClient(this.bot.GlobalSettings.grabSetting("email_smtp"), int.Parse(this.bot.GlobalSettings.grabSetting("email_pop")));
                            sm.EnableSsl = true;
                            sm.UseDefaultCredentials = false;
                            sm.Credentials = new System.Net.NetworkCredential(myMail, this.bot.GlobalSettings.grabSetting("email_password"));
                            sm.Send(msg);
                        });
                    }
                }
            }
            return string.Empty;
        }
    }
}
