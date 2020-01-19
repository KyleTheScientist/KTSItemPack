using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;

namespace CustomItems
{
    class BloodBank : PlayerItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Blood Bank";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/blood_bank";

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<BloodBank>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Cash 4 Blood";
            string longDesc = "A medically-sound device that pays you for blood.\n\n" +
                "This item was brought to the Gungeon in a mysterious golden chest. " +
                "Etched into the back is the letter 'I'.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1.5f);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
        }

        //Removes one heart from the player, gives them 1 armor
        protected override void DoEffect(PlayerController user)
        {
            float currency = user.carriedConsumables.Currency;
            if (CanBeUsed(user))
            {
                AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", base.gameObject);
                user.carriedConsumables.Currency += 10;
                user.healthHaver.ApplyHealing(-.5f);
            }
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return user.healthHaver.GetCurrentHealth() > .5f;
        }
    }
}
