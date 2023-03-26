using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Veda
{
    public class Functions
    {


        public static bool ListContainsPlayer(List<PlayerData> PlayerList, string PlayerName, uint WorldId = 0)
        {
            int Result = 0;
            if (WorldId == 0)
            {
                Result = PlayerList.FindIndex(x => x.Name == PlayerName);
            }
            else
            {
                Result = PlayerList.FindIndex(x => x.Name == PlayerName && x.HomeworldId == WorldId);
            }
            if (Result == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void OpenWebsite(string URL)
        {
            Process.Start(new ProcessStartInfo { FileName = URL, UseShellExecute = true });
        }

        public static SeString BuildSeString(string PluginName, string Message, ushort Color = ColorType.Normal)
        {
            List<Payload> FinalPayload = new();
            //List<string> PluginNameBrokenUp = Regex.Split(PluginName, @"\s+").Where(s => s != string.Empty).ToList();
            //int plugincounter = 0;
            //Color chart is here: https://i.imgur.com/XJywfW2.png
            //foreach (string PluginWord in PluginNameBrokenUp)
            //{
                //plugincounter++;
                if (Regex.Match(PluginName, "<c.*?>").Success) //starting a color tag?
                {
                    ushort code = Convert.ToUInt16(Regex.Match(Regex.Match(PluginName, "<c.*?>").Value, @"\d+").Value);
                    FinalPayload.Add(new UIForegroundPayload(code));
                    FinalPayload.Add(new TextPayload("[" + PluginName.Replace(Regex.Match(PluginName, "<c.*?>").Value, "") + "] "));
                    FinalPayload.Add(new UIForegroundPayload(0));
                }
                else
                {
                    //if (plugincounter < PluginNameBrokenUp.Count())
                    //{
                    //    FinalPayload.Add(new TextPayload(PluginWord + " "));
                    //}
                    //else
                    //{
                        FinalPayload.Add(new TextPayload("[" + PluginName + "] "));
                    //}
                }
            //}
            if (Color == ColorType.Normal)
            {
                List<string> MessageBrokenUp = Regex.Split(Message, @"\s+").Where(s => s != string.Empty).ToList();
                int counter = 0;
                //if (!string.IsNullOrWhiteSpace(PluginName)) { FinalPayload.Add(new TextPayload("[" + PluginName + "] ")); }
                foreach (string Word in MessageBrokenUp)
                {
                    counter++;
                    if (Regex.Match(Word, "<c.*?>").Success) //starting a color tag?
                    {
                        ushort code = Convert.ToUInt16(Regex.Match(Regex.Match(Word, "<c.*?>").Value, @"\d+").Value);
                        FinalPayload.Add(new UIForegroundPayload(code));
                        if (Regex.Match(Word, "</c>").Success) //ending a color tag
                        {
                            List<string> WordBrokenUp = Regex.Split(Word, "</c>").ToList();
                            FinalPayload.Add(new TextPayload(WordBrokenUp[0].Replace(Regex.Match(WordBrokenUp[0], "<c.*?>").Value, "")));
                            FinalPayload.Add(new UIForegroundPayload(0));
                            if (counter < MessageBrokenUp.Count())
                            {
                                FinalPayload.Add(new TextPayload(WordBrokenUp[1] + " "));
                            }
                            else
                            {
                                FinalPayload.Add(new TextPayload(WordBrokenUp[1]));
                            }
                        }
                        else
                        {
                            if (counter < MessageBrokenUp.Count())
                            {
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<c.*?>").Value, "") + " "));
                            }
                            else
                            {
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<c.*?>").Value, "")));
                            }
                            FinalPayload.Add(new UIForegroundPayload(0));
                        }
                    }
                    else if (Regex.Match(Word, "<g.*?>").Success) //starting a color tag?
                    {
                        ushort code = Convert.ToUInt16(Regex.Match(Regex.Match(Word, "<g.*?>").Value, @"\d+").Value);
                        FinalPayload.Add(new UIGlowPayload(code));
                        if (Regex.Match(Word, "</g>").Success) //ending a color tag
                        {
                            List<string> WordBrokenUp = Regex.Split(Word, "</g>").ToList();
                            FinalPayload.Add(new TextPayload(WordBrokenUp[0].Replace(Regex.Match(WordBrokenUp[0], "<g.*?>").Value, "")));
                            FinalPayload.Add(new UIGlowPayload(0));
                            if (counter < MessageBrokenUp.Count())
                            {
                                FinalPayload.Add(new TextPayload(WordBrokenUp[1] + " "));
                            }
                            else
                            {
                                FinalPayload.Add(new TextPayload(WordBrokenUp[1]));
                            }
                        }
                        else
                        {
                            if (counter < MessageBrokenUp.Count())
                            {
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<g.*?>").Value, "") + " "));
                            }
                            else
                            {
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<g.*?>").Value, "")));
                            }
                            FinalPayload.Add(new UIGlowPayload(0));
                        }
                    }
                    else
                    {
                        if (counter < MessageBrokenUp.Count())
                        {
                            FinalPayload.Add(new TextPayload(Word + " "));
                        }
                        else
                        {
                            FinalPayload.Add(new TextPayload(Word));
                        }
                    }
                }
                SeString FinalSeString = new(FinalPayload);
                return FinalSeString;
            }
            else
            {
                List<Payload> payloadList = new()
                        {
                            new TextPayload("[" + PluginName + "] "),
                            new UIForegroundPayload(Color),
                            new TextPayload(Message),
                            new UIForegroundPayload(0)
                        };
                SeString seString = new(payloadList);
                return seString;
            }
        }
    }
}