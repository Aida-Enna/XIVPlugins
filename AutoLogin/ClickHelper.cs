using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoLogin
{
    //Shamelessly ripped wholesale from ECommons
    public static unsafe class ClickHelperExtensions
    {
        public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon)
        {
            var btnRes = target.AtkComponentBase.OwnerNode->AtkResNode;
            var evt = (AtkEvent*)btnRes.AtkEventManager.Event;

            addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
        }
    }
}
