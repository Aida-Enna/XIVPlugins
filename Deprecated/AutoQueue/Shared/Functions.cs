using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;

namespace Veda
{
    public class Functions
    {
        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private static ProcessChatBoxDelegate? _processChatBox;

        private static unsafe delegate* unmanaged<Utf8String*, int, IntPtr, void> _sanitizeString = null!;

        private static class Signatures
        {
            internal const string SendChat = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";
            internal const string SanitiseString = "E8 ?? ?? ?? ?? EB 0A 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8D AE";
        }

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

        public static void GetChatSignatures(ISigScanner sigScanner)
        {
            if (sigScanner.TryScanText(Signatures.SendChat, out var processChatBoxPtr))
            {
                _processChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(processChatBoxPtr);
            }

            unsafe
            {
                if (sigScanner.TryScanText(Signatures.SanitiseString, out var sanitisePtr))
                {
                    _sanitizeString = (delegate* unmanaged<Utf8String*, int, IntPtr, void>)sanitisePtr;
                }
            }
        }

        public static void Send(string message)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(SanitizeText(message));
                if (bytes.Length == 0) throw new ArgumentException("The message is empty", nameof(message));

                if (bytes.Length > 500)
                    throw new ArgumentException("The message is longer than 500 bytes", nameof(message));

                SendMessageUnsafe(bytes);
            }
            catch (Exception e)
            {

            }
        }

        private static unsafe void SendMessageUnsafe(byte[] message)
        {
            if (_processChatBox == null) throw new InvalidOperationException("Could not find signature for chat sending");

            try
            {
                var uiModule = (IntPtr)UIModule.Instance();

                using var payload = new ChatPayload(message);
                var mem1 = Marshal.AllocHGlobal(400);
                Marshal.StructureToPtr(payload, mem1, false);

                _processChatBox(uiModule, mem1, IntPtr.Zero, 0);

                Marshal.FreeHGlobal(mem1);
            }
            catch (Exception e)
            {

            }
        }

        private static unsafe string SanitizeText(string text)
        {

            var uText = Utf8String.FromString(text);

            _sanitizeString(uText, 0x27F, IntPtr.Zero);
            var sanitised = uText->ToString();

            uText->Dtor();
            IMemorySpace.Free(uText);

            return sanitised;
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct ChatPayload : IDisposable
        {
            [FieldOffset(0)] private readonly IntPtr textPtr;
            [FieldOffset(16)] private readonly ulong textLen;
            [FieldOffset(8)] private readonly ulong unk1;
            [FieldOffset(24)] private readonly ulong unk2;

            internal ChatPayload(byte[] stringBytes)
            {
                textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
                Marshal.Copy(stringBytes, 0, textPtr, stringBytes.Length);
                Marshal.WriteByte(textPtr + stringBytes.Length, 0);

                textLen = (ulong)(stringBytes.Length + 1);

                unk1 = 64;
                unk2 = 0;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(textPtr);
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