using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Reflection;
using ItemAPI;


namespace CustomItems
{
    class IcePack : PassiveItem
    {
        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            string itemName = "Ice Pack"; //The name of the item
            string resourceName = "CustomItems/Resources/P1/ice_pack"; //Refers to an embedded png in the project. Make sure to embed your resources!

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<IcePack>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Active Items Cool Passively";
            string longDesc = "Increases active item capacity, and cools the items held within, thereby speeding up the cooldown process.\n\n" +
                "Most of the complaints about this item are related to wet shirts.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 1, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 2, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (player.GetComponent<IcePackBehaviour>() != null)
            {
                Destroy(player.GetComponent<IcePackBehaviour>());
            }
            player.gameObject.AddComponent<IcePackBehaviour>().parent = this;
        }

        private class IcePackBehaviour : BraveBehaviour
        {
            FieldInfo remainingDamageCooldown = typeof(PlayerItem).GetField("remainingDamageCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo remainingTimeCooldown = typeof(PlayerItem).GetField("remainingTimeCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
            public IcePack parent;

            void FixedUpdate()
            {
                PlayerController player = this.GetComponent<PlayerController>();
                if (player == null || !player.passiveItems.Contains(parent))
                {
                    Destroy(this);
                    return;
                }

                if (player.activeItems == null) return;

                foreach (PlayerItem item in player.activeItems)
                {
                    if (item == null) continue;
                    float maxTime = item.timeCooldown;
                    float maxDamage = item.damageCooldown;
                    try
                    {

                        var curRemTime = (float)remainingTimeCooldown.GetValue(item);
                        var curRemDmg = (float)remainingDamageCooldown.GetValue(item);

                        if (curRemDmg <= 0 || curRemDmg <= 0) continue;

                        remainingTimeCooldown.SetValue(item, curRemTime - (maxTime * .01f * Time.deltaTime));
                        remainingDamageCooldown.SetValue(item, curRemDmg - (maxDamage * .01f * Time.deltaTime));
                    }
                    catch (Exception e)
                    {
                        ETGModConsole.Log(e.Message + ": " + e.StackTrace);
                    }
                }
            }

        }
    }
}
