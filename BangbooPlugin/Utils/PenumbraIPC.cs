using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Runtime.InteropServices;


namespace BangbooPlugin.Utils
{
    public class PenumbraIPC(IDalamudPluginInterface pluginInterface) : IDisposable
    {
        private readonly RedrawAll redrawAll = new(pluginInterface);
        private readonly EventSubscriber<nint, Guid, nint, nint, nint> creatingCharacterBaseEvent = CreatingCharacterBase.Subscriber(pluginInterface, OnCreatingCharacterBase);

        public void Dispose()
        {
            creatingCharacterBaseEvent.Dispose();
        }

        public void RedrawAll(RedrawType setting)
        {
            try
            {
                redrawAll.Invoke(setting);
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error("Error: " + ex.ToString());
            }
        }

        public static unsafe void OnCreatingCharacterBase(nint gameObjectAddress, Guid _1, nint _2, nint customizePtr, nint _3)
        {
            try
            { 
            // return if not player character
            var gameObj = (GameObject*)gameObjectAddress;
            if (gameObj->ObjectKind != FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Pc) return;

            var PlayerData = Marshal.PtrToStructure<CharaCustomizeData>(customizePtr);
            if (PlayerData.Race != Race.LALAFELL) //Lalafell
                return;

            var GameObjID = gameObj->GetGameObjectId();

            string GlamString = "Bh+LCAAAAAAAAAq8V8ty2jAU/RevmYxlya/s8iJJB9IMpMlagQtoImzXkpOhmfx7JRuMJTtupoXuxPW55z59ZN6dIePwCLlgaeKcooFz9bNg2RoS6Zy+O2PKkhuazPX5VsL6Vp0IRigaOBc5CIVZUC5g4JxlGd84pzIvdj+mUvkaFtNj+9zdnjx1/Bg43xeLVjwvJnEQxAj9Q9DK0h3zBqgR0AtjHBwp1nk639ixwmPVpRopjGCBauXBJoexGW4ESyMaIhH2DxbNQ2a0IYA0ouEgONpWXtHcKC32YnysWHcwezGGRgLkHyvYU86ENEqLguOVNhmyZAm51cmjFTf6v+FuaLmQ01X6ZvhWPxTgkYm0ykY8pMslh3k37glopsX4c65SSJKiHNw1p0JAeSxt5RAJIR6JSRC7MYpIy/miEDJds19Qanw6B669VD0TOittj5QX6oBrz7Jy5XkNybxq6RaC7Oy1wj1sMujD3ABbrmQDEbo25ILTpAEI7OdDM1OvFYKyXMgNN/LwcQumEuE6GdHAtZKZvrDkIuXV9HZkxOqrkokNlKiJVZ3flZzNZ4+pmZuNxW5HOxjlQ6CyyAH19d5Aen0tNJC4rz8GknwZ6X8ZGXwZGfYh1RVFZ5sHKmWa9uEqhN11t2Pez3n61rs6u50YwaJ3JRRuuqLGa9PCTNeU81sl2X0B71IBfc+/0be+9RinhVz1bcWIZUKy6o7akXhRB8puX/t9GRdixmGs9KtJ1jENxu3etNI6V5I23SraDuN3ica9UuzmJPCnoAm8qu9irdK9u1eBW/IQtlJ8ApmAUat5A1Uaq4D3NKdrkCq4xtYhfryOCy5ZxpmpwCduS6YbPuqLWlSfSrsaOvDVJB7SpOzfPeQz9QeALj9h17s8Yutnym8TCYlgcmO7dQUpZfEv/LT6XrLFoqgWe6In4p7EHvKDKFDju84B9HV8EoXEd2O1i+fbQiMSun4Uta8xLcBtShK7KA7cqEFJEAoDEu4pceCGEcFeJ2Wt2Ici1Z1WytCg20Nrur3pj3zlCA5JuFXdevu3pKGuz1WXds1ZW3aUtcEumWXtyWDPV31s5ujGZRfVK74j3FvOeLai2tRmv4QZ5Xa2fkzcKPKVZ02PPN8jJGrku7fU9B5GOPS0n/0Gj6l6e9WlpF/gj4/fAAAA//8=";

            Plugin.PluginLog.Fatal(GlamString); //Log the string just to make sure it's what we set it

            Plugin.GlamourerApi.ApplyOutfit((ushort)GameObjID, GlamString);

            }
            catch(Exception f)
            {
                Plugin.PluginLog.Error(f.ToString());
            }
        }
    }

    public enum Race : byte
    {
        UNKNOWN = 0,
        HYUR = 1,
        ELEZEN = 2,
        LALAFELL = 3,
        MIQOTE = 4,
        ROEGADYN = 5,
        AU_RA = 6,
        HROTHGAR = 7,
        VIERA = 8
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CharaCustomizeData
    {
        [FieldOffset((int)CustomizeIndex.Race)] public Race Race;
        [FieldOffset((int)CustomizeIndex.ModelType)] public byte ModelType;
    }
}