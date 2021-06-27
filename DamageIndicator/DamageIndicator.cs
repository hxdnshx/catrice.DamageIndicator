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
using SNetwork;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using CancellationToken = Il2CppSystem.Threading.CancellationToken;

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
                        myFade.transform.localPosition = new Vector3(-70, -52 + -35 * (index + 1), 0);
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
        }

        public int counter = 0;
        public bool isListener = false;

        public void Update()
        {
            bool update = this.update_;
            bool is_show = false;
            if (update)
            {
                if (Clock.ExpeditionProgressionTime < 2f)
                {
                    PlayerDamage[0] = 0;
                    PlayerDamage[1] = 0;
                    PlayerDamage[2] = 0;
                    PlayerDamage[3] = 0;
                    isListener = true;
                    TotalDamage = 0;
                    LocalPlayerSlot = 0;//PlayerManager.GetLocalPlayerSlotIndex();
                }
                else
                {
                    counter++;
                    if (counter > 420 && isListener == false)
                    {
                        counter = 0;
                        is_show = true;
                    }
                }

                this.update_ = false;
                string posttxt = "";
                double allDamage = 0;
                for (int index = 0; index < 4; index++)
                {
                    allDamage += PlayerDamage[index];
                }

                for (int index = 0; index < 4; index++)
                {
                    string txt = "";
                    if (PlayerDamage[index] == 0)
                    {
                        txt = $"{playerName_[index]}: -";
                    }
                    else
                    {
                        txt =
                            $"{playerName_[index]}: {Math.Floor(PlayerDamage[index])}({Math.Floor((100 * PlayerDamage[index]) / allDamage)}%)";
                    }
                    this.TextMesh[index].SetText(txt);
                    if (is_show)
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

            }
        }
        

        public TextMeshPro[] TextMesh = new TextMeshPro[4] {null, null, null, null};


        public static DamageIndicator Instance { get; set; }

        public int LocalPlayerSlot = 0;


        //last slot is for unknown source damages.
        public double[] PlayerDamage = new double[5]{0, 0, 0, 0, 0};
        public double TotalDamage = 0;

        public static string[] playerName_ = new string[4] {"Red", "Gre", "Blu", "Pur"};

        public TextMeshPro SuccessReport1;
        public TextMeshPro SuccessReport2;
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
        public static void Prefix_ReceiveBulletDamage(Dam_SyncedDamageBase __instance, out StateData __state, pBulletDamageData data)//
        {
            
            Agents.Agent src;
            data.source.TryGet(out src);
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            Logger.Log($"Damage Called, PreLife: {__instance.Health}");
            __state = new StateData { BeforeLife = __instance.Health, PlayerSrc = playerSrc };
            

        }

        public static void Postfix_ReceiveBulletDamage(Dam_SyncedDamageBase __instance, StateData __state, pBulletDamageData data)//
        {
            
            var damage = __state.BeforeLife - __instance.Health;

            //Logger.Log($"Damage Called, Final Damage: {damage}");
            Logger.Log($"Damage Called, {data.damage.Get(100)}, {data.precisionMulti.Get(10)} {__state} {__instance.Health} {data.source}");

            var inst = DamageIndicator.Instance;
            inst.PlayerDamage[__state.PlayerSrc?.PlayerSlotIndex??4] += damage;
            inst.TotalDamage += damage;
            inst.isListener = false;

        }

        public static void Prefix_ReceiveMeleeDamage(Dam_SyncedDamageBase __instance, pFullDamageData data)//, out StateData __state
        {
            /*
            Agents.Agent src;
            data.source.TryGet(out src);
            PlayerAgent playerSrc = src.gameObject.GetComponent<PlayerAgent>();
            __state = new StateData { BeforeLife = __instance.Health, PlayerSrc = playerSrc };
            */

        }

        public static void Postfix_ReceiveMeleeDamage(Dam_SyncedDamageBase __instance, pFullDamageData data)//, StateData __state
        {
            /*
            var damage = __state.BeforeLife - __instance.Health;

            //Logger.Log($"Damage Called, Final Damage: {damage}");

            var inst = DamageIndicator.Instance;
            inst.PlayerDamage[__state.PlayerSrc?.PlayerSlotIndex ?? inst.LocalPlayerSlot] += damage;
            inst.TotalDamage += damage;*/
        }

        public static void Prefix_BulletDamage(Dam_SyncedDamageBase __instance, out StateData __state, float dam, Agent sourceAgent, Vector3 position, Vector3 direction,
            Vector3 normal, bool allowDirectionalBonus, [Optional] float staggerMulti,
            [Optional] float precisionMulti)
        {

            Logger.Log($"Damage Called, Final Damage: {dam}");
            PlayerAgent playerSrc = sourceAgent.gameObject.GetComponent<PlayerAgent>();
            __state = new StateData { BeforeLife = __instance.Health, PlayerSrc = playerSrc };
        }

        public static void Postfix_BulletDamage(Dam_SyncedDamageBase __instance, StateData __state, float dam, Agent sourceAgent, Vector3 position, Vector3 direction,
            Vector3 normal, bool allowDirectionalBonus, [Optional] float staggerMulti,
            [Optional] float precisionMulti)
        {
            if (__state.PlayerSrc == null) return;
            var damage = __state.BeforeLife - __instance.Health;

            Logger.Log($"Damage Called, Final Damage: {damage}");

            var inst = DamageIndicator.Instance;
            inst.PlayerDamage[__state.PlayerSrc?.PlayerSlotIndex ?? 4] += damage;
            inst.TotalDamage += damage;
        }

        public static void Prefix_ProcessReceivedDamage(Dam_EnemyDamageBase __instance, out StateData __state, float damage, Agent damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact, [Optional] int limbID, [Optional] float staggerDamageMulti)

        {

            //Logger.Log($"Damage2 Called, Final Damage: {damage}");
            PlayerAgent playerSrc = damageSource.gameObject.GetComponent<PlayerAgent>();
            __state = new StateData { BeforeLife = __instance.Health, PlayerSrc = playerSrc };
        }

        public static void Postfix_ProcessReceivedDamage(Dam_EnemyDamageBase __instance, StateData __state, float damage, Agent damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact, [Optional] int limbID, [Optional] float staggerDamageMulti)

        {
            if (__state.PlayerSrc == null) return;
            var damage_real = __state.BeforeLife - __instance.Health;

            //Logger.Log($"Damage2 Called, Final Damage: {damage} {damage_real}");

            var inst = DamageIndicator.Instance;
            inst.PlayerDamage[__state.PlayerSrc?.PlayerSlotIndex ?? 4] += damage_real;
            inst.TotalDamage += damage_real;
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
        public static List<TextMeshPro> pendingFix = new List<TextMeshPro>();

        public static void Postfix_Update(CM_PageExpeditionSuccess __instance)
        {
            counter++;
            if (counter < 60) return;
            counter = 0;
            foreach (var item in pendingFix)
            {
                item.ForceMeshUpdate();
            }
        }

        public static void Postfix_Setup(CM_PageExpeditionSuccess __instance, bool active)
        {
            Logger.Log("Finish Called.");
            if (active == false) return;
            var inst = DamageIndicator.Instance;
            var go = __instance?.gameObject;
            if (go == null) return;
            if (inst.SuccessReport1 == null)
            {
                pendingFix.Clear();
                foreach (var trans in go.GetComponentsInChildren<RectTransform>(true))
                {
                    if (trans.name == "Name")
                    {
                        var txt = trans.gameObject?.GetComponent<TextMeshPro>();
                        if (txt != null)
                        {
                            pendingFix.Add(txt);
                        }
                    }
                    if (trans.name != "HeaderText") continue;
                    {
                        var myHeader = UnityEngine.Object.Instantiate(trans.gameObject, trans.parent);
                        myHeader.transform.localPosition = new Vector3(0, 70, 0);
                        inst.SuccessReport1 = myHeader.GetComponent<TextMeshPro>();
                        pendingFix.Add(inst.SuccessReport1);
                    }
                    {
                        var myHeader = UnityEngine.Object.Instantiate(trans.gameObject, trans.parent);
                        myHeader.transform.localPosition = new Vector3(0, 100, 0);
                        inst.SuccessReport2 = myHeader.GetComponent<TextMeshPro>();
                        pendingFix.Add(inst.SuccessReport2);
                    }
                }
            }

            double allDamage = 0;
            for (int index = 0; index < 4; index++)
            {
                allDamage += inst.PlayerDamage[index];
            }

            string posttxt = "";
            bool isSecond = false;
            for (int index = 0; index < 4; index++)
            {
                string txt = "";
                if (inst.PlayerDamage[index] == 0)
                {
                    txt = $"{DamageIndicator.playerName_[index]}:      0(  0%)";
                }
                else
                {
                    txt =
                        $"{DamageIndicator.playerName_[index]}: {Math.Floor(inst.PlayerDamage[index]),6:f0}({Math.Floor((100 * inst.PlayerDamage[index]) / allDamage),3:f0}%)";
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
            
        }

        private static Regex expr = new Regex("(Red|Blu|Gre|Pur): (\\d+)\\((\\d+)%\\)");
        public static void Postfix__Setup_b__22_2(ref SNet_Player fromPlayer, string msg)
        {
            var inst = DamageIndicator.Instance;
            if (inst == null) return;
            if (inst.isListener == false) return;
            var result = expr.Matches(msg);
            inst.TotalDamage = 0;
            foreach (Match ele in result)
            {
                var dam = double.Parse(ele.Groups[2].Value);
                var percent = double.Parse(ele.Groups[2].Value);
                //Logger.Log($" tosdfafd {msg} -{ele.Groups[1]}-{ele.Groups[2]}-{ele.Groups[3]}-");
                
                switch (ele.Groups[1].Value.ToLower())
                {
                    case "red":
                        inst.PlayerDamage[0] = dam;
                        break;
                    case "gre":
                        inst.PlayerDamage[1] = dam;
                        break;
                    case "blu":
                        inst.PlayerDamage[2] = dam;
                        break;
                    case "pur":
                        inst.PlayerDamage[3] = dam;
                        break;
                    default:
                        break;
                }

                inst.TotalDamage += dam;
            }
        }
    }
}
