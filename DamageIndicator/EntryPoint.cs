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
    [BepInPlugin(GUID, "DamageIndicator", "1.4.3")]
    [BepInProcess("GTFO.exe")]
    [BepInDependency("com.kasuromi.nidhogg")]
    [BepInDependency(GUIDPlaytime, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(GUIDAcc, BepInDependency.DependencyFlags.SoftDependency)]
    public class EntryPoint : BasePlugin
    {
        public const string GUID = "com.catrice.DamageIndicator";

        public const string GUIDAcc = "com.catrice.AccuracyShow";
        public const string GUIDPlaytime = "dev.gtfomodding.Playtime";


        public static bool PlaytimeInstalled = false;
        public static bool AccInstalled = false;
        public override void Load()
        {

            Logger.LogInstance = Log;

            //ClassInjector.RegisterTypeInIl2Cpp<BoosterHack>();
            var harmony = new Harmony(GUID);


            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(GUIDPlaytime, out _))
            {
                PlaytimeInstalled = true;
            }

            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(GUIDAcc, out _))
            {
                AccInstalled = true;
            }

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

            if(false){
                var hotReloadInjectPoint = typeof(Dam_EnemyDamageBase).GetMethod("ProcessReceivedDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_ProcessReceivedDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_ProcessReceivedDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            {
                // Hook function that not impl may cause crash?
                var hotReloadInjectPoint = typeof(Dam_EnemyDamageBase).GetMethod("ReceiveMeleeDamage");
                if (hotReloadInjectPoint == null) Logger.Log("Fail to Inject ReceiveMeleeDamage");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix_ReceiveMeleeDamage");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_ReceiveMeleeDamage");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatch), new HarmonyMethod(hotReloadPatchPost));
            }

            {
                var hotReloadInjectPoint = typeof(Dam_EnemyDamageBase).GetMethod("ReceiveBulletDamage");
                if (hotReloadInjectPoint == null) Logger.Log("Fail to Inject ReceiveBulletDamage");
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
                var hotReloadInjectPoint = typeof(CM_PageExpeditionSuccess).GetMethod("OnEnable");
                if (hotReloadInjectPoint == null) Logger.Log("Fail to Inject OnEnable");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix_OnEnable");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_OnEnable");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }

            {
                var hotReloadInjectPoint = typeof(CM_PageExpeditionSuccess).GetMethod("Update");
                if (hotReloadInjectPoint == null) Logger.Log("Fail to Inject Update");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix_Update");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_Update");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }

            if(false){
                var hotReloadInjectPoint = typeof(CM_PageMap).GetMethod("SetPageActive");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Postfix_SetPageActive");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Postfix_SetPageActive");
                harmony.Patch(hotReloadInjectPoint, null, new HarmonyMethod(hotReloadPatchPost));
            }


            {
                var hotReloadInjectPoint = typeof(PUI_GameEventLog).GetMethod("_Setup_b__17_2");
                if (hotReloadInjectPoint == null) Logger.Log("Fail to Inject GetColoredAgentName");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("Prefix__Setup_b__17_2");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("Prefix__Setup_b__17_2");
                harmony.Patch(hotReloadInjectPoint,  new HarmonyMethod(hotReloadPatchPost));
            }

            if(false){
                var hotReloadInjectPoint = typeof(LG_SecurityDoor).GetMethod("UseChainedPuzzleOrUnlock");
                var hotReloadPatch = typeof(DamageIndicatorHooks).GetMethod("UseChainedPuzzleOrUnlock_Postfix");
                var hotReloadPatchPost = typeof(DamageIndicatorHooks).GetMethod("UseChainedPuzzleOrUnlock_Postfix");
                harmony.Patch(hotReloadInjectPoint, new HarmonyMethod(hotReloadPatchPost));
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