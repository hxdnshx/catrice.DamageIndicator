using AssetShards;
using BepInEx;
using BepInEx.IL2CPP;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoosterImplants;
using CellMenu;
using DropServer;
using Gear;
using HarmonyLib;
using LevelGeneration;
using UnhollowerRuntimeLib;

namespace catrice.DamageIndicator
{
    [BepInPlugin(GUID, "DamageIndicator", "1.0.0")]
    [BepInProcess("GTFO.exe")]
    public class EntryPoint : BasePlugin
    {
        public const string GUID = "com.catrice.DamageIndicator";
        public override void Load()
        {

            Logger.LogInstance = Log;

            //ClassInjector.RegisterTypeInIl2Cpp<BoosterHack>();
            var harmony = new Harmony(GUID);




            {
                ClassInjector.RegisterTypeInIl2Cpp<DamageIndicator>();
                ClassInjector.RegisterTypeInIl2Cpp<DamageIndicatorHooks>();
            }

            if(false){
                var hotReloadInjectPoint = typeof(Dam_SyncedDamageBase).GetMethod("BulletDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_BulletDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_BulletDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            {
                var hotReloadInjectPoint = typeof(Dam_EnemyDamageBase).GetMethod("ProcessReceivedDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_ProcessReceivedDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_ProcessReceivedDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            if (false){
                // Hook function that not impl may cause crash?
                var hotReloadInjectPoint = typeof(Dam_SyncedDamageBase).GetMethod("ReceiveMeleeDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_ReceiveMeleeDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_ReceiveMeleeDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            if(false){
                var hotReloadInjectPoint = typeof(Dam_EnemyDamageBase).GetMethod("ReceiveBulletDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_ReceiveBulletDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_ReceiveBulletDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            if(false){
                var hotReloadInjectPoint = typeof(PlayerChatManager).GetMethod("DoSendChatMessage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_DoSendChatMessage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch));
            }

            {
                var hotReloadInjectPoint = typeof(CM_PageExpeditionSuccess).GetMethod("SetPageActive");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix_Setup");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_Setup");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }

            {
                var hotReloadInjectPoint = typeof(CM_PageExpeditionSuccess).GetMethod("Update");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix_Update");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_Update");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }


            {
                var hotReloadInjectPoint = typeof(PUI_GameEventLog).GetMethod("_Setup_b__22_2");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix__Setup_b__22_2");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix__Setup_b__22_2");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }

            harmony.PatchAll();
            //AssetShardManager.add_OnStartupAssetsLoaded((Il2CppSystem.Action)OnAssetLoaded);
        }

        private bool once = false;
        /*
        private void OnAssetLoaded()
        {
            if (once)
                return;
            once = true;

            PartialDataManager.UpdatePartialData();
            PartialDataManager.WriteToFile(Path.Combine(PartialDataManager.PartialDataPath, "persistentID.json"));
        }
        */
    }
}