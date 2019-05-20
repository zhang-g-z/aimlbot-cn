using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AIMLbot.Utils
{
    /// <summary>
    /// Encapsulates a node in the graphmaster tree structure
    /// </summary>
    [Serializable]
    public class Node
    {
        #region Attributes

        /// <summary>
        /// Contains the child nodes of this node
        /// </summary>
        private Dictionary<string, Node> children = new Dictionary<string, Node>();

        /// <summary>
        /// The number of direct children (non-recursive) of this node
        /// </summary>
        public int NumberOfChildNodes
        {
            get
            {
                return this.children.Count;
            }
        }

        /// <summary>
        /// The template (if any) associated with this node
        /// </summary>
        public string template = string.Empty;

        /// <summary>
        /// The AIML source for the category that defines the template
        /// </summary>
        public string filename = string.Empty;

        /// <summary>
        /// The word that identifies this node to it's parent node
        /// </summary>
        public string word=string.Empty;

        #endregion

        #region Methods

        #region Add category
        public void addCategory(string path, string template, string filename)
        {
            if (template.Length == 0)
            {
                throw new XmlException("路径: " + path + "，文件 : " + filename + " 没有包含任何标签");
            }
            if (path.Trim().Length == 0)
            {
                this.template = template;
                this.filename = filename;
                return;
            }
            List<string> words = new List<string>();
            string w = "";
            foreach (char c in path.ToCharArray())
            {
                if (Regex.IsMatch(c.ToString(), @"[\u4e00-\u9fa5]+"))
                {
                    if (w != "")
                        words.Add(w.ToString());
                    w = "";
                    words.Add(c.ToString());
                }
                else if(c.ToString()=="*")
                {
                    w = "";
                    words.Add(c.ToString());
                }
                else
                {
                    if (c != ' ')
                        w += c.ToString();
                    else if (w != "")
                    {
                        words.Add(w.ToString());
                        w = "";
                    }
                }
            }
            if (w != "")
                words.Add(w.ToString());
            string firstWord = Normalize.MakeCaseInsensitive.TransformInput(words[0]);
            string newPath = path.Substring(firstWord.Length, path.Length - firstWord.Length).Trim();
            if (this.children.ContainsKey(firstWord))
            {
                Node childNode = this.children[firstWord];
                childNode.addCategory(newPath, template, filename);
            }
            else
            {
                Node childNode = new Node();
                childNode.word = firstWord;
                childNode.addCategory(newPath, template, filename);
                this.children.Add(childNode.word, childNode);
            }
        }

        #endregion

        #region Evaluate Node

        /// <summary>
        /// Navigates this node (and recusively into child nodes) for a match to the path passed as an argument
        /// whilst processing the referenced request
        /// </summary>
        /// <param name="path">The normalized path derived from the user's input</param>
        /// <param name="query">The query that this search is for</param>
        /// <param name="request">An encapsulation of the request from the user</param>
        /// <param name="matchstate">The part of the input path the node represents</param>
        /// <param name="wildcard">The contents of the user input absorbed by the AIML wildcards "_" and "*"</param>
        /// <returns>The template to process to generate the output</returns>
        public string evaluate(string path, SubQuery query, Request request, MatchState matchstate, StringBuilder wildcard)
        {
            if (request.StartedOn.AddMilliseconds(request.bot.TimeOut) < DateTime.Now)
            {
                request.bot.writeToLog("文件读取超时. 用户ID: " + request.user.UserID + ",输入内容: \"" + request.rawInput + "\"");
                request.hasTimedOut = true;
                return string.Empty;
            }
            path = path.Trim();
            if (this.children.Count == 0)
            {
                if (path.Length > 0)
                {
                    this.storeWildCard(path, wildcard);
                }
                return this.template;
            }
            if (path.Length == 0)
            {
                return this.template;
            }
            List<string> splitPath = new List<string>();
            string w = "";
            foreach (char c in path.ToCharArray())
            {
                if (Regex.IsMatch(c.ToString(), @"[\u4e00-\u9fa5]+"))
                {
                    if (w != "")
                        splitPath.Add(w.ToString());
                    w = "";
                    splitPath.Add(c.ToString());
                }
                else if (c.ToString() == "*")
                {
                    w = "";
                    splitPath.Add(c.ToString());
                }
                else
                {
                    if (!Regex.IsMatch(c.ToString(), @"[ \r\n\t]+"))
                        w += c.ToString();
                    else if (w != "")
                    {
                        splitPath.Add(w.ToString());
                        w = "";
                    }
                }
            }
            if (w != "")
                splitPath.Add(w.ToString());
            string firstWord = Normalize.MakeCaseInsensitive.TransformInput(splitPath[0]);
            string newPath = path.Substring(firstWord.Length, path.Length - firstWord.Length);
            if (this.children.ContainsKey("_"))
            {
                Node childNode = (Node)this.children["_"];
                StringBuilder newWildcard = new StringBuilder();
                this.storeWildCard(splitPath[0], newWildcard);
                string result = childNode.evaluate(newPath, query, request, matchstate, newWildcard);
                if (result.Length>0)
                {
                    if (newWildcard.Length > 0)
                    {
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                break;
                        }
                    }
                    return result;
                }
            }
            if (this.children.ContainsKey(firstWord))
            {
                MatchState newMatchstate = matchstate;
                if (firstWord == "<THAT>")
                {
                    newMatchstate = MatchState.That;
                }
                else if (firstWord == "<TOPIC>")
                {
                    newMatchstate = MatchState.Topic;
                }

                Node childNode = (Node)this.children[firstWord];
                StringBuilder newWildcard = new StringBuilder();
                string result = childNode.evaluate(newPath, query, request, newMatchstate, newWildcard);
                if (result.Length > 0)
                {
                    if (newWildcard.Length > 0)
                    {
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                        }
                    }
                    return result;
                }
            }
            if (this.children.ContainsKey("*"))
            {
                Node childNode = (Node)this.children["*"];
                StringBuilder newWildcard = new StringBuilder();
                this.storeWildCard(splitPath[0], newWildcard);
                string result = childNode.evaluate(newPath, query, request, matchstate, newWildcard);
                if (result.Length > 0)
                {
                    if (newWildcard.Length > 0)
                    {
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                break;
                        }
                    }
                    return result;
                }
            }
            if ((this.word == "_") || (this.word == "*"))
            {
                this.storeWildCard(splitPath[0], wildcard);
                return this.evaluate(newPath, query, request, matchstate, wildcard);
            }
            wildcard = new StringBuilder();
            return string.Empty;
        }

        /// <summary>
        /// Correctly stores a word in the wildcard slot
        /// </summary>
        /// <param name="word">The word matched by the wildcard</param>
        /// <param name="wildcard">The contents of the user input absorbed by the AIML wildcards "_" and "*"</param>
        private void storeWildCard(string word, StringBuilder wildcard)
        {
            //if (wildcard.Length > 0)
            //{
            //    wildcard.Append(" ");
            //}
            wildcard.Append(word);
        }
        #endregion

        #endregion
    }
}