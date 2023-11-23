using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using Obeliskial_Essentials;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace TraitMod
{
    [HarmonyPatch]
    internal class Traits
    {
        private readonly Harmony harmony = new Harmony("com.shazixnar.ThreeElementMastertraits");
        internal static ManualLogSource Log;

        internal static void LogInfo(string msg)
        {
            Log.LogInfo(msg);
        }
        private void Awake()
        {
            Essentials.medsTexts["mainperkburn2d"] = "Burn on enemies deals double damage if the target have 4 or less curses (Burn included)";
            Log = BaseUnityPlugin.Logger;
            LogInfo("com.shazixnar.ThreeElementMastertraits 1.0.0 has loaded!");
            harmony.PatchAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "BeginTurnPerks")]
        public static void BeginTurnPerksPostfix(ref Character __instance)
        {
            int _perk_burnCharges = __instance.GetAuraCharges("burn");
            int _perk_chillCharges = __instance.GetAuraCharges("chill");
            int _perk_sparkCharges = __instance.GetAuraCharges("spark");
            int _position = Traverse.Create(__instance).Field("position").GetValue<int>();
            if (!__instance.IsHero && _perk_sparkCharges > 0 && AtOManager.Instance.TeamHaveTrait("evelynelementalweaver"))
            {
                List<NPC> npcsides = MatchManager.Instance.GetNPCSides(_position);
                for (int i = 0; i < npcsides.Count; i++)
                {
                    npcsides[i].SetAura(null, Globals.Instance.GetAuraCurseData("burn"), Functions.FuncRoundToInt((float)_perk_burnCharges * 0.3f), false, Enums.CardClass.None, true, true);
                    npcsides[i].SetAura(null, Globals.Instance.GetAuraCurseData("chill"), Functions.FuncRoundToInt((float)_perk_chillCharges * 0.3f), false, Enums.CardClass.None, true, true);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "SetAura")]
        public static void SetAuraPostfix(ref Character __instance)
        {
            int _trait_burnCharges = __instance.GetAuraCharges("burn");
            int _trait_chillCharges = __instance.GetAuraCharges("chill");
            int _trait_sparkCharges = __instance.GetAuraCharges("spark");
            if (!__instance.IsHero && _trait_burnCharges == 200 && _trait_chillCharges == 200 && _trait_sparkCharges == 200 && AtOManager.Instance.TeamHaveTrait("evelynelementalamplifier"))
            {
                __instance.IndirectDamage(Enums.DamageType.Fire, 400);
                __instance.IndirectDamage(Enums.DamageType.Cold, 400);
                __instance.IndirectDamage(Enums.DamageType.Lightning, 400);
                for (int i = __instance.AuraList.Count - 1; i >= 0; i--)
                {
                    if (__instance.AuraList[i].ACData.Id == "burn" || __instance.AuraList[i].ACData.Id == "chill" || __instance.AuraList[i].ACData.Id == "spark")
                    {
                        __instance.AuraList.RemoveAt(i);
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetEvent")]
        public static void SetEventPrefix(ref Character __instance, ref Enums.EventActivation theEvent, Character target = null)
        {
            if (theEvent == Enums.EventActivation.BeginTurn && !__instance.IsHero && AtOManager.Instance.TeamHaveTrait("evelynelementalweaver") && __instance.HasEffect("spark"))
            { // if team have trait "evelynelementalweaver", burn on enemy deals 30% of spark charges as fire damage
                __instance.IndirectDamage(Enums.DamageType.Fire, Functions.FuncRoundToInt((float)__instance.GetAuraCharges("spark") * 0.3f));
            }
        }

        // list of your trait IDs
        public static string[] myTraitList = { };

        public static void myDoTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = new List<CardData>();
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            // activate traits
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                myDoTrait(_trait, ref __instance);
                return false;
            }
            return true;
        }

        public static string TextChargesLeft(int currentCharges, int chargesTotal)
        {
            int cCharges = currentCharges;
            int cTotal = chargesTotal;
            return "<br><color=#FFF>" + cCharges.ToString() + "/" + cTotal.ToString() + "</color>";
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            bool flag = false;
            bool flag2 = false;
            if (_characterCaster != null && _characterCaster.IsHero)
            {
                flag = _characterCaster.IsHero;
            }
            if (_characterTarget != null && _characterTarget.IsHero)
            {
                flag2 = true;
            }
            if (_acId == "burn" && _type == "consume" && !flag && __instance.TeamHavePerk("mainperkburn2d"))
            { // burn on enemy deals double damage if the target have 4 or less curses (Burn included)
                __result.DoubleDamageIfCursesLessThan = 5;
            }
            if (_acId == "chill" && _type == "set" && !flag2 && __instance.TeamHaveTrait("evelynelementalweaver"))
            {
                __result.ResistModified3 = Enums.DamageType.Lightning;
                __result.ResistModifiedPercentagePerStack3 = -0.3f;
            }
        }
    }
}
