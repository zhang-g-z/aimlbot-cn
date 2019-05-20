using System;
using System.Collections.Generic;
using System.Text;
using AIMLbot;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ConsoleBot
{
    class Program
    {
        static void Main(string[] args)
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("config", "Settings.xml"));
            Bot myBot = new Bot();
            myBot.loadSettings(settingsPath);
            User myUser = new User("oegwGtx2ja_ldFfAoFqj_6pnBfUk", myBot);
            myBot.isAcceptingUserInput = false;
            myBot.loadAIMLFromFiles();
            if(myBot.GlobalSettings.containsSettingCalled("tagx"))
            {
                string[] tagxs = myBot.GlobalSettings.grabSetting("tagx").Split(',');
                foreach (string tagdll in tagxs)
                    myBot.loadCustomTagHandlers(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("tagx", tagdll)));
            }
            myBot.isAcceptingUserInput = true;
            while (true)
            {
                Console.Write("You: ");
                string input = Console.ReadLine();
                if (input.ToLower() == "quit")
                {
                    break;
                }
                else
                {
                    Request r = new Request(input, myUser, myBot);
                    Result res = myBot.Chat(r);
                    Console.WriteLine("Bot: " + res.Output);
                }
            }
        }
    }
}
