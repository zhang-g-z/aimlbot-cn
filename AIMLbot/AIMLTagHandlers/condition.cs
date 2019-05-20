using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace AIMLbot.AIMLTagHandlers
{
    public class condition : AIMLbot.Utils.AIMLTagHandler
    {
        public condition(AIMLbot.Bot bot,
                        AIMLbot.User user,
                        AIMLbot.Utils.SubQuery query,
                        AIMLbot.Request request,
                        AIMLbot.Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
            this.isRecursive = false;
        }
        protected override string ProcessChange()
        {
            if (this.templateNode.Name.ToLower() == "condition")
            {
                // heuristically work out the type of condition being processed

                if (this.templateNode.Attributes.Count == 2) // block
                {
                    string name = "";
                    string value = "";

                    if (this.templateNode.Attributes[0].Name == "name")
                    {
                        name = this.templateNode.Attributes[0].Value;
                    }
                    else if (this.templateNode.Attributes[0].Name == "value")
                    {
                        value = this.templateNode.Attributes[0].Value;
                    }

                    if (this.templateNode.Attributes[1].Name == "name")
                    {
                        name = this.templateNode.Attributes[1].Value;
                    }
                    else if (this.templateNode.Attributes[1].Name == "value")
                    {
                        value = this.templateNode.Attributes[1].Value;
                    }

                    if ((name.Length > 0) & (value.Length > 0))
                    {
                        string actualValue = this.user.Predicates.grabSetting(name);
                        Regex matcher = new Regex(value.Replace(" ", "\\s").Replace("*", "[\\sA-Z0-9]+"), RegexOptions.IgnoreCase);
                        if (matcher.IsMatch(actualValue))
                        {
                            return this.templateNode.InnerXml;
                        }
                    }
                }
                else if (this.templateNode.Attributes.Count == 1) // single predicate
                {
                    if (this.templateNode.Attributes[0].Name == "name")
                    {
                        string name = this.templateNode.Attributes[0].Value;
                        foreach (XmlNode childLINode in this.templateNode.ChildNodes)
                        {
                            if (childLINode.Name.ToLower() == "li")
                            {
                                if (childLINode.Attributes.Count == 1)
                                {
                                    if (childLINode.Attributes[0].Name.ToLower() == "value")
                                    {
                                        string actualValue = this.user.Predicates.grabSetting(name);
                                        Regex matcher = new Regex(childLINode.Attributes[0].Value.Replace(" ", "\\s").Replace("*", "[\\sA-Z0-9]+"), RegexOptions.IgnoreCase);
                                        if (matcher.IsMatch(actualValue))
                                        {
                                            return childLINode.InnerXml;
                                        }
                                    }
                                }
                                else if (childLINode.Attributes.Count == 0)
                                {
                                    return childLINode.InnerXml;
                                }
                            }
                        }
                    }
                }
                else if (this.templateNode.Attributes.Count == 0) // multi-predicate
                {
                    foreach (XmlNode childLINode in this.templateNode.ChildNodes)
                    {
                        if (childLINode.Name.ToLower() == "li")
                        {
                            if (childLINode.Attributes.Count == 2)
                            {
                                string name = "";
                                string value = "";
                                string exists = "";
                                string contains = "";
                                if (childLINode.Attributes[0].Name == "name")
                                {
                                    name = childLINode.Attributes[0].Value;
                                }
                                else if (childLINode.Attributes[0].Name == "value")
                                {
                                    value = childLINode.Attributes[0].Value;
                                }
                                else if (childLINode.Attributes[0].Name == "exists")
                                {
                                    exists = childLINode.Attributes[0].Value;
                                }
                                else if (childLINode.Attributes[0].Name == "contains")
                                {
                                    contains = childLINode.Attributes[0].Value;
                                }
                                if (childLINode.Attributes[1].Name == "name")
                                {
                                    name = childLINode.Attributes[1].Value;
                                }
                                else if (childLINode.Attributes[1].Name == "value")
                                {
                                    value = childLINode.Attributes[1].Value;
                                }
                                else if (childLINode.Attributes[1].Name == "exists")
                                {
                                    exists = childLINode.Attributes[1].Value;
                                }
                                else if (childLINode.Attributes[1].Name == "contains")
                                {
                                    contains = childLINode.Attributes[1].Value;
                                }
                                if ((name.Length > 0) & (value.Length > 0 | exists.Length > 0 | contains.Length > 0))
                                {
                                    string actualValue = this.user.Predicates.grabSetting(name);
                                    Regex matcher = new Regex(value.Replace(" ", "\\s").Replace("*","[\\sA-Z0-9]+"), RegexOptions.IgnoreCase);
                                    if (!string.IsNullOrEmpty(value) && matcher.IsMatch(actualValue))
                                    {
                                        return childLINode.InnerXml;
                                    }
                                    else if (exists == "true" && !string.IsNullOrEmpty(actualValue))
                                    {
                                        return childLINode.InnerXml;
                                    }
                                    else if (exists == "false" && string.IsNullOrEmpty(actualValue))
                                    {
                                        return childLINode.InnerXml;
                                    }
                                    else if (!string.IsNullOrEmpty(contains) && !string.IsNullOrEmpty(actualValue) && actualValue.Contains(contains))
                                    {
                                        return childLINode.InnerXml;
                                    }
                                }
                            }
                            else if (childLINode.Attributes.Count == 0)
                            {
                                return childLINode.InnerXml;
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}
