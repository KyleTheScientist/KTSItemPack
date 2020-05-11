using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;


namespace CustomItems
{
    class BossBullets
    {
        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            string itemName = "Boss Bullets"; //The name of the item
            string resourceName = "CustomItems/Resources/P1/boss_bullets_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<PassiveItem>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Show 'em Who's Boss";
            string longDesc = "Greatly increases damage dealt to bosses.\n\n" +
                "This item was created by a union of Gungeoneers who became fed up with low wages and poor benefits.\n" +
                "Viva la Revolverlucion!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.DamageToBosses, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
        }
    }
}
