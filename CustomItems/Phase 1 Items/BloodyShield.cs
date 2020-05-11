using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;


namespace CustomItems
{
    class BloodShield : PlayerItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Blood Shield";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/armor_shield_heart_idle_001";
                
            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<BloodShield>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Iron from Blood";
            string longDesc = "Trades hearts for armor.\n\n" +
                "For carbon-based species, blood naturally contains hemoglobin, a molecule composed of " +
                "iron and heme groups. This item collects that iron from your blood and forges it into armor.\n\n" +
                "Approved by 100% of all doctors everywhere!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1.5f);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            //ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton, 1f);
        }

        //Removes one heart from the player, gives them 1 armor
        protected override void DoEffect(PlayerController user)
        {
            float curHealth = user.healthHaver.GetCurrentHealth();
            if (curHealth > 1)
            {
                AkSoundEngine.PostEvent("Play_OBJ_dead_again_01", base.gameObject);
                user.healthHaver.ForceSetCurrentHealth(curHealth - 1);
                user.healthHaver.Armor += 1;
            }
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return user.healthHaver.GetCurrentHealth() > 1;
        }
    }
}
