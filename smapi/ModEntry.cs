using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Characters;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Harmony;
using Microsoft.Xna.Framework;
using System.Threading;
using Netcode;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Reflection.Emit;
using StardewHack;
using System.ComponentModel;

namespace FourShadesOfGray
{
    public class Mod : Hack<Mod>
    {
        public override void HackEntry(IModHelper helper)
        {

            CanGetPregnantPatch.Initialize(Monitor);
            // PickPersonalFarmEventPatch.Initialize(Monitor);

            HarmonyInstance harmony = HarmonyInstance.Create(ModManifest.UniqueID);

            // Patch(typeof(Utility), "pickPersonalFarmEvent", BabyProbability);

            // example patch, you'll need to edit this for your patch
            harmony.Patch(
               original: AccessTools.Method(typeof(NPC), nameof(NPC.canGetPregnant)),
               prefix: new HarmonyMethod(typeof(CanGetPregnantPatch), nameof(CanGetPregnantPatch.Postfix))
            );

            //harmony.Patch(
            //    original: AccessTools.Method(typeof(Utility), nameof(Utility.pickPersonalFarmEvent)),
            //    transpiler: new HarmonyMethod(typeof(PickPersonalFarmEventPatch), nameof(PickPersonalFarmEventPatch.babyProbability))
            //);

            Monitor.Log($"{harmony.GetPatchedMethods()}", LogLevel.Debug);

            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
        }

        void BabyProbability()
        {
            var code = FindCode(
                OpCodes.Ldloc_0,
                OpCodes.Callvirt,
                Instructions.Ldc_R8(0.05)
            ); //[2].operand = 0.99f;

            Monitor.Log($"{code[2].operand}", LogLevel.Debug);

            code[2].operand = 0.99;

            Monitor.Log($"{code[2].operand}", LogLevel.Debug);
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // var api = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            // IsRoommateToken token = new IsRoommateToken();
            // api.RegisterToken(ModManifest, "IsRoommate", token);

            GameLocation sewer = Game1.locations.Where(location => location.Name == "Sewer").First();
            if (sewer.characters.Count >= 1)
            {
                NPC krobus = sewer.characters.Where(character => character.Name == "Krobus").First();
                sewer.characters.RemoveAt(krobus.id);
                sewer.addCharacter(new NPC(new AnimatedSprite("Characters\\Krobus", 0, 16, 24), new Vector2(31f, 17f) * 64f, "Sewer", 2, "Krobus", datable: true, null, helper.Content.Load<Texture2D>("Portraits\\Krobus", ContentSource.GameContent)));
            }
        }
    }
    internal class IsRoommateToken
    {
        #region Meta
        public bool IsMutable() { return true; }

        public bool AllowsInput() { return true; }

        public bool RequiresInput() { return true; }

        public bool CanHaveMultipleValues(string input) { return false; }

        public IEnumerable<string> GetValidInputs()
        {
            Dictionary<string, string> NPCDispositions = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
            return NPCDispositions.Keys;
        }

        public bool HasBoundedValues(string input, out IEnumerable<string> allowedValues)
        {
            allowedValues = GetValidInputs();
            return true;
        }
        #endregion

        #region State
        public bool UpdateContext()
        {
            string wasRoommate = _isRoommate;
            _isRoommate = IsRoommate(_input);
            return wasRoommate != _isRoommate;
        }

        public bool IsReady()
        {
            return _isRoommate != null;
        }

        public IEnumerable<string> GetValues(string input)
        {
            if (input == null)
            yield return IsRoommate(input);
        }
        #endregion

        private string _input;
        private string _isRoommate;

        private string IsRoommate(string input)
        {
            if (SaveGame.loaded?.player != null)
            {
                Friendship friendship = SaveGame.loaded.player?.friendshipData[input];
                if (friendship.RoommateMarriage) {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }

            if (Context.IsWorldReady)
            {
                Friendship friendship = Game1.player.friendshipData[input];
                if (friendship.RoommateMarriage) {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }

            return null;
        }
    }

    internal class PickPersonalFarmEventPatch
    {
        private static IMonitor Monitor;

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }
        public static IEnumerable<CodeInstruction> babyProbability(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            

            return instructions;
        }
    }

    internal static class CanGetPregnantPatch
    {
        private static IMonitor Monitor;

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        public static void Postfix(NPC __instance, ref bool __result)
        {
            try
            {

                Monitor.Log($"NPC: {__instance.Name} - Result: {__result}", LogLevel.Debug);
                Monitor.Log($"Married: {Game1.player.isMarried()}\nSpouse: {Game1.player.spouse}", LogLevel.Debug);
                Monitor.Log($"{(Game1.player.currentLocation == Game1.getLocationFromName(Game1.player.homeLocation))}", LogLevel.Debug);

                Monitor.Log($"Master Game?: {Game1.IsMasterGame}", LogLevel.Debug);
                if (! Game1.IsMasterGame) { return; }

                NPC spouse = __instance;
                Monitor.Log($"Horse?: {(spouse is Horse)}", LogLevel.Debug);
                if (spouse is Horse) { return; }
                Monitor.Log($"Roommate?: {spouse.isRoommate()}", LogLevel.Debug);
                if (spouse.isRoommate()) { return; }

                Farmer farmer = spouse.getSpouse();
                Monitor.Log($"Null farmer?: {(farmer == null)}", LogLevel.Debug);
                if (farmer == null) { return; }
                Monitor.Log($"Divorce Tonight?: {farmer.divorceTonight.Equals(new NetBool(true))}", LogLevel.Debug);
                if (farmer.divorceTonight.Equals(new NetBool(true))) { return; }

                int hearts = farmer.getFriendshipHeartLevelForNPC(spouse.Name);
                Friendship friendship = farmer.GetSpouseFriendship();
                List<Child> children = farmer.getChildren();

                spouse.defaultMap.Value = farmer.homeLocation.Value;

                Monitor.Log($"Upgrade Level: {Utility.getHomeOfFarmer(farmer).upgradeLevel}", LogLevel.Debug);
                if (Utility.getHomeOfFarmer(farmer).upgradeLevel < 2) { return; }
                Monitor.Log($"DaysUntilBirthing: {friendship.DaysUntilBirthing}", LogLevel.Debug);
                if (friendship.DaysUntilBirthing >= 0) { return; }
                Monitor.Log($"Hearts: {hearts}", LogLevel.Debug);
                if (hearts < 10) { return; }
                Monitor.Log($"Days Married: {farmer.GetDaysMarried()}", LogLevel.Debug);
                if (farmer.GetDaysMarried() < 7) { return; }
                Monitor.Log($"# Kids: {children.Count }", LogLevel.Debug);
                if (children.Count >= 2) { return; }

                if (children.Count == 0) {
                    __result = true;
                    Monitor.Log($"{__result}", LogLevel.Debug);
                    return;
                }

                if (children.Count != 0 && children.Count < 2) {
                    __result = children[0].Age > 2;
                    Monitor.Log($"{nameof(Postfix)}:\n Kids: {children.Count}\n Result: {__result}", LogLevel.Debug);
                }

                //if (Utility.getHomeOfFarmer(farmer).upgradeLevel >= 2 &&
                //    friendship.DaysUntilBirthing < 0 &&
                //    hearts >= 10 &&
                //    farmer.GetDaysMarried() >= 7)
                //{
                //    if (children.Count != 0)
                //    {
                //        if (children.Count < 2)
                //        {
                //            __result = children[0].Age > 2;
                //            return;
                //        }
                //        __result = false;
                //        return;
                //    }
                //    __result = true;
                //    return;
                //}
                //__result = false;
                //return;

            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(Postfix)}:\n {ex}", LogLevel.Error);
            }
        }
    }
}
