using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace CustomItems
{
    public class Thermometer : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Thermometer"; //The name of the item
            string resourceName = "CustomItems/Resources/P2/thermometer"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();
            var item = obj.AddComponent<Thermometer>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Check your Temperature";
            string longDesc = "Coolness increases damage.\n\n" +
                "Stay frosty!";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 1);
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton, 1f);
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        protected override void Update()
        {
            base.Update();
            EvaluateStats();
        }

        private float coolness = 0, lastCoolness = -1;
        private void EvaluateStats()
        {
            if (!this.Owner || !this.Owner.stats) return;

            coolness = GetTrueTotalCoolness(this.Owner);
            if (coolness == lastCoolness) return;

            RemoveStat(PlayerStats.StatType.Damage);
            AddStat(PlayerStats.StatType.Damage, coolness * .05f);
            this.Owner.stats.RecalculateStats(Owner, true);

            lastCoolness = coolness;
        }

        public float GetTrueTotalCoolness(PlayerController player)
        {
            float coolness = player.stats.GetStatValue(PlayerStats.StatType.Coolness);
            if (PassiveItem.IsFlagSetForCharacter(player, typeof(ChamberOfEvilItem)))
            {
                float sixthChamberCoolness = player.stats.GetStatValue(PlayerStats.StatType.Curse);
                coolness += sixthChamberCoolness * 2f;
            }
            return coolness;
        }

        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier();
            modifier.amount = amount;
            modifier.statToBoost = statType;
            modifier.modifyType = method;

            foreach (var m in passiveStatModifiers)
            {
                if (m.statToBoost == statType) return; //don't add duplicates
            }

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }


        //Removes a stat
        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
    }
}
