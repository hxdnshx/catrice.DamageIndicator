using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BoosterImplants;
using CellMenu;
using Dissonance;
using DropServer;
using GameData;
using Gear;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Il2CppSystem.Threading;
using Il2CppSystem.Threading.Tasks;
using LevelGeneration;
using Player;
using TMPro;
using UnityEngine;
using Object = Il2CppSystem.Object;

using Player;
using TMPro;
using UnityEngine;
using System.Threading;
using System.Transactions;
using Agents;
using Il2CppSystem.Resources;
using PlayFab.ClientModels;
using SNetwork;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;
using GTFO.API;

using NetworkingManager = GTFO.API.NetworkAPI;

namespace catrice.DamageIndicator
{


    //From https://github.com/Flowaria/MTFO.Ext.PartialData
    [HarmonyPatch(typeof(CM_PageRundown_New), "PlaceRundown")]
    public class PrepareInjection
    {
        public static bool InjectedFlag = false;
        // Token: 0x06000005 RID: 5 RVA: 0x00002404 File Offset: 0x00000604
        [HarmonyPostfix]
        public static void PostFix()
        {
            if (InjectedFlag == false)
            {
                InjectedFlag = true;
                GameObject gameObject = new GameObject();
                gameObject.AddComponent<DamageIndicator>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
            }
        }

        // Token: 0x0400000B RID: 11
        private static bool _isInjected;

        // Token: 0x0400000C RID: 12
        public static GameObject _obj;
    }

    //From: https://github.com/Endskill/PlaytimeTimer

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DamageInfo
    {
        public double TotalDamage;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public double[] PlayerDamage;
        
    }

    public class DamageIndicator : MonoBehaviour
    {


        // Token: 0x06000003 RID: 3 RVA: 0x0000209F File Offset: 0x0000029F
        public DamageIndicator(IntPtr intPtr) : base(intPtr)
        {
        }

        private System.Threading.Timer _intervalTimer;
        private bool update_ = false;


        public void Awake()
        {
            DamageInfo = new DamageInfo();
            DamageInfo.PlayerDamage = new double[5];
            Instance = this;
            bool flag = this._intervalTimer == null;
            if (flag)
            {
                this._intervalTimer = new System.Threading.Timer(delegate (object arg) { this.update_ = true; }, null,
                    TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(1.0));
            }

            PUI_Inventory inventory = GuiManager.Current.m_playerLayer.Inventory;
            PUI_InventoryIconDisplay iconDisplay = inventory.m_iconDisplay;
            foreach (RectTransform rectTransform in iconDisplay.GetComponentsInChildren<RectTransform>(true))
            {
                bool flag2 = rectTransform.name == "Background Fade";
                if (flag2)
                {
                    TextMeshPro refTextMesh = null;
                    foreach (Il2CppSystem.Collections.Generic.KeyValuePair<InventorySlot, PUI_InventoryItem> keyValuePair in inventory.m_inventorySlots)
                    {
                        bool flag3 = this.TextMesh[0] == null;
                        if (flag3)
                        {
                            refTextMesh = keyValuePair.Value.m_slim_archetypeName;
                            break;
                        }
                    }

                    for (int index = 0; index < 4; index++)
                    {
                        var myFade = UnityEngine.Object.Instantiate(rectTransform.gameObject, rectTransform.parent);
                        var myRect = myFade.GetComponent<RectTransform>();
                        myFade.gameObject.SetActive(true);
                        foreach (var childObj in myFade.GetComponentsInChildren<Transform>(true))
                        {
                            if (childObj.name == "TimerShowObject") // Remove TimerShowComponent.
                                childObj.gameObject.active = false;
                        }

                        var realIndex = index +
                                        (EntryPoint.AccInstalled ? 1 : 0) +
                                        (EntryPoint.PlaytimeInstalled ? 2 : 0);
                        myFade.transform.localPosition = new Vector3(-70, -52 + -35 * (realIndex), 0);
                        this.TextMesh[index] = UnityEngine.Object.Instantiate<TextMeshPro>(refTextMesh);
                        {
                            GameObject gameObject = new GameObject($"DamageIndicator{index}")
                            {
                                layer = 5,
                                hideFlags = HideFlags.HideAndDontSave
                            };
                            gameObject.transform.SetParent(myRect.transform, false);
                            this.TextMesh[index].transform.SetParent(gameObject.transform, false);
                            RectTransform component = this.TextMesh[index].GetComponent<RectTransform>();
                            component.anchoredPosition = new Vector2(-5f, 9f);
                            this.TextMesh[index].SetText("-");
                            this.TextMesh[index].ForceMeshUpdate();
                        }
                    }

                }
            }


            NetworkingManager.RegisterEvent<DamageInfo>("DamageIndicator", (senderId, packet) =>
            {
                // This action will be invoked whenever the current user receives the event
                DamageInfo = packet;
                Logger.Log($"Received New DamageInfo, Total:{packet.TotalDamage}");
            });
        }

        public int counter = 0;
        public bool isListener = false;

        public void Update()
        {
            bool update = this.update_;
            bool is_show = false;
            bool isSend = false;
            if (update)
            {
                if (Clock.ExpeditionProgressionTime < 2f)
                {
                    DamageInfo.PlayerDamage[0] = 0;
                    DamageInfo.PlayerDamage[1] = 0;
                    DamageInfo.PlayerDamage[2] = 0;
                    DamageInfo.PlayerDamage[3] = 0;
                    isListener = true;
                    DamageInfo.TotalDamage = 0;
                    LocalPlayerSlot = 0;//PlayerManager.GetLocalPlayerSlotIndex();
                }
                else
                {
                    counter++;
                    if (counter > 240 && isListener == false)
                    {
                        counter = 0;
                        is_show = true;
                    }

                    if ((counter % 10) == 0 && isListener == false)
                        isSend = true;
                }

                this.update_ = false;
                string posttxt = "";
                double allDamage = 0;
                for (int index = 0; index < 4; index++)
                {
                    allDamage += DamageInfo.PlayerDamage[index];
                }


                for (int index = 0; index < 4; index++)
                {
                    string txt = "";
                    if (DamageInfo.PlayerDamage[index] == 0)
                    {
                        txt = $"{playerName_[index]}: -";
                    }
                    else
                    {
                        txt =
                            $"{playerName_[index]}: {Math.Floor(DamageInfo.PlayerDamage[index])}({Math.Floor((100 * DamageInfo.PlayerDamage[index]) / allDamage)}%)";
                    }
                    this.TextMesh[index].SetText(txt);
                    if (is_show && ConfigManager.IsHideDamage == false)
                    {
                        if ((index % 2) == 0)
                        {
                            posttxt = txt;
                        }
                        else
                        {
                            txt = $"{posttxt}, {txt}";
                            PlayerChatManager.WantToSentTextMessage(PlayerManager.GetLocalPlayerAgent(), txt);
                        }
                    }
                    this.TextMesh[index].ForceMeshUpdate();
                }


                if (isSend)
                {
                    Logger.Log($"Damage Info Sent. Total: {DamageInfo.TotalDamage}");
                    NetworkingManager.InvokeEvent("DamageIndicator", DamageInfo);
                }

            }
        }
        

        public TextMeshPro[] TextMesh = new TextMeshPro[4] {null, null, null, null};


        public static DamageIndicator Instance { get; set; }

        public int LocalPlayerSlot = 0;


        //last slot is for unknown source damages.

        public static string[] playerName_ = new string[4] {"Red", "Gre", "Blu", "Pur"};

        public TextMeshPro SuccessReport1;
        public TextMeshPro SuccessReport2;

        public float[] beforeDamage = new float[5]{0, 0, 0, 0, 0};
        public DamageInfo DamageInfo;

        public GameObject MapObj { get; set; } = null;
        public SpriteMask Mask { get; set; } = null;
        
    }

    public struct StateData
    {
        public float BeforeLife;
        public PlayerAgent PlayerSrc;
    }

    class DamageIndicatorHooks : MonoBehaviour
    {

        // Token: 0x06000003 RID: 3 RVA: 0x0000209F File Offset: 0x0000029F
        public DamageIndicatorHooks(IntPtr intPtr) : base(intPtr)
        {
        }
        public static void Prefix_ReceiveBulletDamage(Dam_SyncedDamageBase __instance, pBulletDamageData data)//
        {
            
            Agents.Agent src;
            data.source.TryGet(out src);
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] = __instance.Health;


        }

        public static void Postfix_ReceiveBulletDamage(Dam_SyncedDamageBase __instance, pBulletDamageData data)//
        {
            
            Agents.Agent src;
            data.source.TryGet(out src);
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            var damage = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;

            var damage_real = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;

            //Logger.Log($"Damage2 Called, Final Damage: {damage} {damage_real}");

            inst.DamageInfo.PlayerDamage[playerSrc?.PlayerSlotIndex ?? 4] += damage_real;
            inst.isListener = false;

        }

        public static float beforeHealth = 0;

        public static void Prefix_ReceiveMeleeDamage(Dam_EnemyDamageBase __instance, ref pFullDamageData data)//, out StateData __state
        {
            Agents.Agent src;
            data.source.TryGet(out src);
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            //Debug.Log($"Pre-Melee Damage: limb{data.limbID} {data.skipLimbDestruction} {data.damageNoiseLevel}");
            inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] = __instance.Health;
            data.skipLimbDestruction = false;
        }

        public static void Postfix_ReceiveMeleeDamage(Dam_EnemyDamageBase __instance, ref pFullDamageData data)//, StateData __state
        {
            Agents.Agent src;
            data.source.TryGet(out src);
            //Debug.Log($"Post-Melee Damage: limb{data.limbID} {data.skipLimbDestruction}");
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            var damage = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;//
            

            var damage_real = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;

            //Logger.Log($"Damage2 Called, Final Damage: {damage} {damage_real}");

            inst.DamageInfo.PlayerDamage[playerSrc?.PlayerSlotIndex ?? 4] += damage_real;
            inst.isListener = false;
        }

        public static void Prefix_BulletDamage(Dam_SyncedDamageBase __instance, float dam, Agent sourceAgent)
        {

            PlayerAgent playerSrc = sourceAgent.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] = __instance.Health;
        }

        public static void Postfix_BulletDamage(Dam_SyncedDamageBase __instance, float dam, Agent sourceAgent)
        {
            PlayerAgent playerSrc = sourceAgent.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            var damage_real = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;

            //Logger.Log($"Damage2 Called, Final Damage: {damage} {damage_real}");

            inst.DamageInfo.PlayerDamage[playerSrc?.PlayerSlotIndex ?? 4] += damage_real;
            inst.isListener = false;
        }

        public static void Prefix_ProcessReceivedDamage(Dam_EnemyDamageBase __instance,Agent damageSource)

        {

            //Logger.Log($"Damage2 Called, Final Damage: {damage}");
            PlayerAgent playerSrc = damageSource.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] = __instance.Health;
        }

        public static void Postfix_ProcessReceivedDamage(Dam_EnemyDamageBase __instance, Agent damageSource)

        {
            PlayerAgent playerSrc = damageSource.gameObject.GetComponent<PlayerAgent>();
            var inst = DamageIndicator.Instance;
            var damage_real = inst.beforeDamage[playerSrc?.PlayerSlotIndex ?? 4] - __instance.Health;

            //Logger.Log($"Damage2 Called, Final Damage: {damage} {damage_real}");

            inst.DamageInfo.PlayerDamage[playerSrc?.PlayerSlotIndex ?? 4] += damage_real;
            inst.isListener = false;
        }


        public static void Prefix_DoSendChatMessage(ref PlayerChatManager.pChatMessage data)
        {
            //SNet_Player fromPlayer;
            //SNet_Player toPlayer;
           // data.fromPlayer.TryGetPlayer(out fromPlayer);
            //data.toPlayer.TryGetPlayer(out toPlayer);

            Logger.Log("Msg Sent");
            
            //Logger.Log($"Send {data.fromPlayer.lookup.ToString()??"Invalid"} to {data.fromPlayer.lookup.ToString() ?? "Invalid"}: {data.message.m_data.Substring(0,50)}");
        }

        public static int counter;
        public static HashSet<TextMeshPro> pendingFix = new HashSet<TextMeshPro>();

        public static void Postfix_Update(CM_PageExpeditionSuccess __instance)
        {
            counter++;
            if (counter < 120) return;
            counter = 0;
            foreach (var item in pendingFix)
            {
                //if(item.isActiveAndEnabled)
                if(item != null)
                    item.ForceMeshUpdate();
            }
        }

        public static void Postfix_SetPageActive(CM_PageMap __instance, bool active)
        {
            var inst = DamageIndicator.Instance;
            if (inst.MapObj == null)
            {
                var go = __instance.gameObject;
                foreach (var trans in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (trans.name == "MapMover")
                    {
                        inst.MapObj = trans.gameObject;
                        break;
                    }
                }

                if (inst.MapObj != null)
                {
                    inst.MapObj.transform.parent = go.transform.parent;
                    inst.MapObj.AddComponent<SpriteMask>();
                }
            }
        }

        public static bool isFirst = false;
        public static void Postfix_OnEnable(CM_PageExpeditionSuccess __instance)
        {
            if (isFirst)
            {
                isFirst = false;
                return;
            }
            Logger.Log("Finish Called.");
            //if (active == false) return;
            var inst = DamageIndicator.Instance;
            var go = __instance?.gameObject;
            if (go == null) return;
            if (true)
            {
                //pendingFix.Clear();
                try
                {

                    foreach (var trans in go.GetComponentsInChildren<RectTransform>(true))
                    {

                        //Logger.Log($"Found ele:{trans?.name}");
                        if (trans?.name == "Name")
                        {
                            var txt = trans.gameObject?.GetComponent<TextMeshPro>();
                            if (txt != null)
                            {
                                pendingFix.Add(txt);
                            }
                        }
                        if (trans?.name != "HeaderText") continue;
                        if (!(trans.parent?.name?.Contains("Anchor") ?? false))
                            continue;
                        if (inst != null && inst.SuccessReport1 == null)
                        {
                            {
                                var myHeader = UnityEngine.Object.Instantiate(trans.gameObject, trans.parent);
                                myHeader.transform.localPosition = new Vector3(0, 70, 0);
                                var localizer = myHeader.GetComponent<TMP_Localizer>();
                                if (localizer != null)
                                {
                                    localizer.enabled = false;
                                    Destroy(localizer);
                                }
                                inst.SuccessReport1 = myHeader.GetComponent<TextMeshPro>();

                            }
                            {
                                var myHeader = UnityEngine.Object.Instantiate(trans.gameObject, trans.parent);
                                myHeader.transform.localPosition = new Vector3(0, 100, 0);
                                var localizer = myHeader.GetComponent<TMP_Localizer>();
                                if (localizer != null)
                                {
                                    localizer.enabled = false;
                                    Destroy(localizer);
                                }

                                inst.SuccessReport2 = myHeader.GetComponent<TextMeshPro>();

                            }
                            if (inst.SuccessReport1 != null)
                                pendingFix.Add(inst.SuccessReport1);
                            if (inst.SuccessReport2 != null)
                                pendingFix.Add(inst.SuccessReport2);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString());
                    throw;
                }
            }

            Logger.Log($"Finish Display.");
            if (inst?.SuccessReport1 == null)
            {
                Logger.Log("no report instance found, return.");
                return;
            }

            double allDamage = 0;
            for (int index = 0; index < 4; index++)
            {
                allDamage += inst.DamageInfo.PlayerDamage[index];
            }

            //if (allDamage == 0) return;


            Logger.Log("Fill Damage Data.");

            string posttxt = "";
            bool isSecond = false;
            for (int index = 0; index < 4; index++)
            {
                Logger.Log($"Fill Damage Data For Player {index}");
                string txt = "";
                if (inst.DamageInfo.PlayerDamage[index] == 0)
                {
                    txt = $"{DamageIndicator.playerName_[index]}:      0(  0%)";
                }
                else
                {
                    txt =
                        $"{DamageIndicator.playerName_[index]}: {Math.Floor(inst.DamageInfo.PlayerDamage[index]),6:f0}({Math.Floor((100 * inst.DamageInfo.PlayerDamage[index]) / allDamage),3:f0}%)";
                }
                if ((index % 2) == 0)
                {
                    posttxt = txt;
                }
                else
                {
                    txt = $"{posttxt}, {txt}";
                    var obj = isSecond ? inst.SuccessReport1 : inst.SuccessReport2;
                    obj.SetText(txt);
                    obj.ForceMeshUpdate();
                    isSecond = true;
                }

            }

            Logger.Log("Fill Damage Data Complete.");

        }

        private static Regex expr = new Regex("(Red|Blu|Gre|Pur): (\\d+)\\((\\d+)%\\)");
        private static Regex expr2 = new Regex("(Red|Blu|Gre|Pur): -");
        public static bool Prefix__Setup_b__22_2(string msg, SNet_Player srcPlayer, SNet_Player dstPlayer)
        {
            var inst = DamageIndicator.Instance;
            if (inst == null) return true;
            var result = expr.Matches(msg);
            var fnd = expr2.Matches(msg);
            if (inst.isListener == false)
            {
                return true;
                return result.Count <= 0 && fnd.Count <= 0;
            }
            inst.DamageInfo.TotalDamage = 0;
            foreach (Match ele in result)
            {
                var dam = double.Parse(ele.Groups[2].Value);
                var percent = double.Parse(ele.Groups[2].Value);
                //Logger.Log($" tosdfafd {msg} -{ele.Groups[1]}-{ele.Groups[2]}-{ele.Groups[3]}-");
                
                switch (ele.Groups[1].Value.ToLower())
                {
                    case "red":
                        inst.DamageInfo.PlayerDamage[0] = dam;
                        break;
                    case "gre":
                        inst.DamageInfo.PlayerDamage[1] = dam;
                        break;
                    case "blu":
                        inst.DamageInfo.PlayerDamage[2] = dam;
                        break;
                    case "pur":
                        inst.DamageInfo.PlayerDamage[3] = dam;
                        break;
                    default:
                        break;
                }

                inst.DamageInfo.TotalDamage += dam;
            }

            
            return result.Count <= 0 && fnd.Count <= 0;

        }

        public static void UseChainedPuzzleOrUnlock_Postfix(SNet_Player user)
        {
            Logger.Log($"{user?.GetName()} Activated Door.");
        }
    }
}
