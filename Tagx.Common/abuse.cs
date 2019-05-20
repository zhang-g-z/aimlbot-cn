using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIMLbot.Utils;

namespace Tagx.Common
{
    [CustomTagAttribute]
    public class abuse : AIMLTagHandler
    {
        public abuse(): base() { }
        protected override string ProcessChange()
        {
            string myname = this.user.Predicates.grabSetting("myname");
            if (string.IsNullOrEmpty(myname))
            {
                return "你奶奶个熊，找骂是不是。";
            }
            else
            {
                return string.Format("{0}，你是找抽是不是，老子打死你。", myname);
            }
        }
    }
}
