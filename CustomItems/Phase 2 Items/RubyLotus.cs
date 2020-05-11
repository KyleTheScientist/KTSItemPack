using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using ItemAPI;
using UnityEngine;

namespace CustomItems
{
    class RubyLotus : PassiveItem
    {
        private static readonly string[] spritePaths =
        {
            "CustomItems/Resources/P2/ruby_lotus_001",
            "CustomItems/Resources/P2/ruby_lotus_002",
            "CustomItems/Resources/P2/ruby_lotus_003",
        };

        private static int[] spriteIDs;

        public static void Init()
        {
            string itemName = "Ruby Lotus"; //The name of the item
            string resourceName = spritePaths[0]; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();
            var item = obj.AddComponent<RubyLotus>();

            spriteIDs = new int[spritePaths.Length];

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            spriteIDs[0] = item.sprite.spriteId;
            spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(spritePaths[1], item.sprite.Collection);
            spriteIDs[2] = SpriteBuilder.AddSpriteToCollection(spritePaths[2], item.sprite.Collection);

            string shortDesc = "Blooms from Blood";
            string longDesc = "A rare flower that blooms in times of hardship.\n\n" +
                "Traditionally, Lotus flowers grow in harsh, muddy waters. " +
                "The Blobulonian Empire genetically engineered these flowers to instead grow and bloom " +
                "on bloody battlefields. The crystal inside releases a pheromone that bolsters strength " +
                "when blood abounds.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton, 1f);
        }

        protected override void Update()
        {
            base.Update();
            EvaluateStats();
        }

        private float healthPercent = 0, lastHealthPercent = -1;
        private void EvaluateStats()
        {
            if (!this.Owner?.healthHaver || !this.Owner.stats) return;
            healthPercent = this.Owner.healthHaver.GetCurrentHealthPercentage();
            if (healthPercent == lastHealthPercent) return;

            RemoveStat(PlayerStats.StatType.Damage);

            if (healthPercent <= 2 / 3f)
                AddStat(PlayerStats.StatType.Damage, healthPercent <= 1 / 3f ? .5f : .25f);
            this.Owner.stats.RecalculateStats(Owner, true);
            SetFlowerSprite(healthPercent);

            lastHealthPercent = healthPercent;
        }

        int id;
        public void SetFlowerSprite(float healthPercent)
        {
            if (healthPercent <= 1 / 3f)
                id = spriteIDs[2];
            else if (healthPercent <= 2 / 3f)
                id = spriteIDs[1];
            else
                id = spriteIDs[0];
            sprite.SetSprite(id);
            SetDockItemSprite(id);
        }

        FieldInfo m_dockItems = typeof(MinimapUIController).GetField("dockItems", BindingFlags.NonPublic | BindingFlags.Instance);
        private void SetDockItemSprite(int id)
        {
            List<Tuple<tk2dSprite, PassiveItem>> dockItems = (List<Tuple<tk2dSprite, PassiveItem>>)m_dockItems.GetValue(Minimap.Instance.UIMinimap);
            for (int i = 0; i < dockItems.Count; i++)
            {
                if (dockItems[i].Second is RubyLotus)
                {
                    dockItems[i].First.SetSprite(this.sprite.Collection, id);
                }
            }
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
