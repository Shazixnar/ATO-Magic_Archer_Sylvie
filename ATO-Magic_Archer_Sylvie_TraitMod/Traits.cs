using HarmonyLib;
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
        [HarmonyPatch(typeof(Hero), "AssignTrait")]
        public static void AssignTraitPostfix(ref String traitName, ref Hero __instance)
        {
            if (traitName == "sylviearrowwithhawk")
            {
                __instance.Pet = "harleyrare";
            }
        }
        // list of your trait IDs
        public static string[] myTraitList = { "sylviearchersintuition", "sylvieelementlock", "sylviearrowwithhawk" };

        private static readonly Traits _instance = new Traits();

        public static void myDoTrait(
            string trait,
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard)
        {
            switch(trait)
            {
                case "sylviearchersintuition":
                    _instance.sylviearchersintuition(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
                    
                case "sylvieelementlock":
                    _instance.sylvieelementlock(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;

                case "sylviearrowwithhawk":
                    _instance.sylviearrowwithhawk(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
            }
        }

        // activate traits
        public void sylviearchersintuition(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || castedCard == null) return;

            // 只在使用卡牌时触发
            if (evt != Enums.EventActivation.CastCard) return;

            // 必须是远程攻击卡
            if (!castedCard.HasCardType(Enums.CardType.Ranged_Attack)) return;
            if (character.HeroData == null) return;

            TraitData data = Globals.Instance.GetTraitData(trait);
            int used = MatchManager.Instance.activatedTraits.ContainsKey(trait) ? MatchManager.Instance.activatedTraits[trait] : 0;
            if (used >= data.TimesPerTurn) return;

            // 更新次数
            MatchManager.Instance.activatedTraits[trait] = used + 1;
            MatchManager.Instance.SetTraitInfoText();

            // 返还能量
            character.ModifyEnergy(1, true);

            // combat text
            character.HeroItem?.ScrollCombatText(
                Texts.Instance.GetText("traits_Archer's Intuition", "")
                + Functions.TextChargesLeft(used + 1, data.TimesPerTurn),
                Enums.CombatScrollEffectType.Trait
            );

            // NPC sight 光环
            NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
            foreach (var npc in teamNPC)
            {
                if (npc != null && npc.Alive)
                    npc.SetAuraTrait(character, "sight", 3);
            }
        }

        public void sylvieelementlock(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || target == null || !target.Alive) return;

            if (auxString != "burn" && auxString != "chill" && auxString != "spark") return;

            int sightAmount = Functions.FuncRoundToInt((float)auxInt);
            target.SetAuraTrait(target, "sight", sightAmount);

            character.HeroItem?.ScrollCombatText(
                Texts.Instance.GetText("traits_Indomitable", ""),
                Enums.CombatScrollEffectType.Trait
            );
        }

        public void sylviearrowwithhawk(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || castedCard == null) return;

            // 只在使用卡牌时触发
            if (!castedCard.HasCardType(Enums.CardType.Ranged_Attack)) return;
            if (character.HeroData == null) return;

            TraitData data = Globals.Instance.GetTraitData(trait);
            int used = MatchManager.Instance.activatedTraits.ContainsKey(trait) ? MatchManager.Instance.activatedTraits[trait] : 0;
            if (used >= data.TimesPerTurn) return;

            // 检查手牌上限
            if (MatchManager.Instance.CountHeroHand(-1) >= 10)
            {
                Debug.Log("[TRAIT EXECUTION] Broke because player at max cards");
                return;
            }

            // 更新次数
            MatchManager.Instance.activatedTraits[trait] = used + 1;
            MatchManager.Instance.SetTraitInfoText();

            // 生成卡牌
            string newCardId = "";
            CardData newCard = null;
            do
            {
                int randPercent = MatchManager.Instance.GetRandomIntRange(0, 100, "trait", "");
                int randIndex = MatchManager.Instance.GetRandomIntRange(0, Globals.Instance.CardListByType[Enums.CardType.Ranged_Attack].Count, "trait", "");
                string id = Globals.Instance.CardListByType[Enums.CardType.Ranged_Attack][randIndex];
                id = Functions.GetCardByRarity(randPercent, Globals.Instance.GetCardData(id, false), false);
                newCardId = MatchManager.Instance.CreateCardInDictionary(id, "", false);
                newCard = MatchManager.Instance.GetCardData(newCardId);
            } while (newCard.CardClass.ToString() != "Scout");

            newCard.Vanish = true;
            newCard.EnergyReductionToZeroPermanent = true;

            MatchManager.Instance.GenerateNewCard(1, newCardId, false, Enums.CardPlace.Hand, null, null, -1, true, 0);

            // combat text
            character.HeroItem?.ScrollCombatText(
                Texts.Instance.GetText("traits_Arrow with Hawk", "")
                + Functions.TextChargesLeft(used + 1, data.TimesPerTurn),
                Enums.CombatScrollEffectType.Trait
            );

            MatchManager.Instance.ItemTraitActivated(true);
            MatchManager.Instance.CreateLogCardModification(newCard.InternalId, MatchManager.Instance.GetHero(character.HeroIndex));
        }


        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static class Trait_DoTrait_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(
                Enums.EventActivation __0,   // theEvent
                string __1,                  // trait id
                Character __2,               // character
                Character __3,               // target
                int __4,                     // auxInt
                string __5,                  // auxString
                CardData __6,                // castedCard
                Trait __instance)
            {
                string trait = __1;

                // 如果是自定义 trait，就直接调用我们的逻辑
                if (myTraitList.Contains(trait))
                {
                    myDoTrait(
                        trait,
                        __0,        // event
                        __2,        // character
                        __3,        // target
                        __4,        // auxInt
                        __5,        // auxString
                        __6         // castedCard
                    );

                    // 返回 false = 阻止原版 DoTrait 执行
                    return false;
                }

                // 否则走原版逻辑
                return true;
            }
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
                // apply 15% of spark charges as Lightning damage
                __instance.IndirectDamage(Enums.DamageType.Lightning, Functions.FuncRoundToInt((float)__instance.GetAuraCharges("spark") * 0.15f));
            }
        }
    }
}
