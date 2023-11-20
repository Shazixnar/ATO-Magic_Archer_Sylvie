﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using Obeliskial_Essentials;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;

namespace TraitMod
{
    [HarmonyPatch]
    internal class Traits
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardData), "GetIgnoreBlock")]
        public static void GetIgnoreBlockPostfix(ref bool __result, ref CardData __instance)
        {
            if ((bool) (UnityEngine.Object) MatchManager.Instance)
            {
            Hero heroHeroActive = MatchManager.Instance.GetHeroHeroActive();
            if (heroHeroActive != null && AtOManager.Instance.CharacterHaveTrait(heroHeroActive.SubclassName, "sylviesharpwithhawk") && __instance.HasCardType(Enums.CardType.Ranged_Attack))
                __result = true;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hero), "AssignTrait")]
        public static void AssignTraitPostfix(ref String traitName, ref Hero __instance)
        {
            if (traitName == "sylviesharpwithhawk")
            {
                __instance.Pet = "harleyrare";
            }
        }
        // list of your trait IDs
        public static string[] myTraitList = { "sylviearchersintuition", "sylvieelementlock" };

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
            if (_trait == "sylviearchersintuition")
            {
                if (!((UnityEngine.Object)MatchManager.Instance != (UnityEngine.Object)null) || !((UnityEngine.Object)_castedCard != (UnityEngine.Object)null))
                    return;
                if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey(_trait) && MatchManager.Instance.activatedTraits[_trait] > traitData.TimesPerTurn - 1)
                    return;
                if (_castedCard.GetCardTypes().Contains(Enums.CardType.Ranged_Attack) && _character.HeroData != null)
                {
                    if (!MatchManager.Instance.activatedTraits.ContainsKey("sylviearchersintuition"))
                    {
                        MatchManager.Instance.activatedTraits.Add("sylviearchersintuition", 1);
                    }
                    else
                    {
                        Dictionary<string, int> activatedTraits = MatchManager.Instance.activatedTraits;
                        activatedTraits["sylviearchersintuition"] = activatedTraits["sylviearchersintuition"] + 1;
                    }
                    MatchManager.Instance.SetTraitInfoText();
                    _character.ModifyEnergy(1, true);
                    if (_character.HeroItem != null)
                    {
                        _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Archer's Intuition", "") + TextChargesLeft(MatchManager.Instance.activatedTraits["sylviearchersintuition"], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
                        EffectsManager.Instance.PlayEffectAC("energy", true, _character.HeroItem.CharImageT, false, 0f);
                    }
                    NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
                    for (int i = 0; i < teamNPC.Length; i++)
                    {
                        if (teamNPC[i] != null && teamNPC[i].Alive)
                        {
                            teamNPC[i].SetAuraTrait(_character, "sight", 5);
                        }
                    }
                }
            }
            
            if (_trait == "sylvieelementlock")
            {
                if (_target != null && _target.Alive)
                {
                    bool flag = false;
                    if (_auxString == "burn")
                    {
                        flag = true;
                        _target.SetAuraTrait(_character, "sight", _auxInt);
                    }
                    if (_auxString == "chill")
                    {
                        flag = true;
                        _target.SetAuraTrait(_character, "sight", _auxInt);
                    }
                    if (_auxString == "spark")
                    {
                        flag = true;
                        _target.SetAuraTrait(_character, "sight", _auxInt);
                    }
                    if (flag)
                    {
                        _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Element Lock", ""), Enums.CombatScrollEffectType.Trait);
                    }
                }
            }
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
            if (_acId == "burn" && _type == "set" && !flag2 && __instance.TeamHaveTrait("sylviepowerfulenchantment"))
            { // burn on enemy can recude 0.7% fire resistance and 0.3% pierce resistance per stack
                __result.ResistModifiedPercentagePerStack = -0.7f;
                __result.ResistModified3 = Enums.DamageType.Piercing;
                __result.ResistModifiedPercentagePerStack3 = -0.3f;
            }
            if (_acId == "chill" && _type == "set" && !flag2 && __instance.TeamHaveTrait("sylviepowerfulenchantment"))
            { // chill on enemy can increase 1 piercing and cold damage received per 5 stack
                __result.IncreasedDamageReceivedType = Enums.DamageType.Piercing;
                __result.IncreasedDirectDamageChargesMultiplierNeededForOne = 5;
                __result.IncreasedDirectDamageReceivedPerStack = 1;
                __result.IncreasedDamageReceivedType2 = Enums.DamageType.Cold;
                __result.IncreasedDirectDamageChargesMultiplierNeededForOne2 = 5;
                __result.IncreasedDirectDamageReceivedPerStack2 = 1;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetEvent")]
        public static void SetEventPrefix(ref Character __instance, ref Enums.EventActivation theEvent, Character target = null)
        {
            if (theEvent == Enums.EventActivation.Hitted && !__instance.IsHero && target != null && target.IsHero && target.HaveTrait("sylviepowerfulenchantment") && __instance.HasEffect("spark"))
            { // if NPC with spark is hit by hero with trait "sylviepowerfulenchantment"
                // apply 30% of spark charges as Lightning damage
                __instance.IndirectDamage(Enums.DamageType.Lightning, Functions.FuncRoundToInt((float)__instance.GetAuraCharges("spark") * 0.3f));
            }
        }
    }
}
