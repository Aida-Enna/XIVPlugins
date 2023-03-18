using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Collections.Generic;
using System.Linq;
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

        public static SeString BuildSeString(string PluginName, string Message, ushort Color = ColorType.Normal)
        {
            if (Color == ColorType.Normal)
            {
                List<string> MessageBrokenUp = Regex.Split(Message, @"\s+").Where(s => s != string.Empty).ToList();
                List<Payload> FinalPayload = new();
                int counter = 0;
                if (!string.IsNullOrWhiteSpace(PluginName)) { FinalPayload.Add(new TextPayload("[" + PluginName + "] ")); }
                foreach (string Word in MessageBrokenUp)
                {
                    counter++;
                    if (Regex.Match(Word, "<c.>").Success) //starting a color tag?
                    {
                        switch (Regex.Match(Word, "<c.>").Value)
                        {
                            case "<cr>":
                                FinalPayload.Add(new UIForegroundPayload(ColorType.Error));
                                break;
                            case "<cg>":
                                FinalPayload.Add(new UIForegroundPayload(ColorType.Success));
                                break;
                            case "<cb>":
                                FinalPayload.Add(new UIForegroundPayload(ColorType.Info));
                                break;
                            case "<cy>":
                                FinalPayload.Add(new UIForegroundPayload(ColorType.Warn));
                                break;
                        }
                        if (Regex.Match(Word, "<ec>").Success) //ending a color tag
                        {
                            List<string> WordBrokenUp = Regex.Split(Word, "<ec>").ToList();
                            FinalPayload.Add(new TextPayload(WordBrokenUp[0].Replace(Regex.Match(WordBrokenUp[0], "<c.>").Value, "")));
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
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<c.>").Value, "") + " "));
                            }
                            else
                            {
                                FinalPayload.Add(new TextPayload(Word.Replace(Regex.Match(Word, "<c.>").Value, "")));
                            }
                            FinalPayload.Add(new UIForegroundPayload(0));
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