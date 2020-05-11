using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Reflection;
using ItemAPI;

namespace CustomItems
{
    class MimicWhistle : PlayerItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Mimic Whistle";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/mimic_whistle";

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<MimicWhistle>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Here Doggy!";
            string longDesc = "Summons mimics.\n\n" +
                "Surprisingly, mimics can be tamed! Sort of. They seem to come to " +
                "the sound of a whistle at least. \n\nThey won't bite, will they?";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 3000f);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            //Set some other fields
            item.consumableHandlesOwnDuration = true;
            item.canStack = true;
            item.numberOfUses = 1;
            item.quality = ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Flynt, 1f);
            CustomSynergies.Add("Mimicreed", new List<string>() { "kts:mimic_whistle", "mimic_tooth_necklace" }, ignoreLichEyeBullets: true);
        }

        protected override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_BOSS_bulletbros_anger_01", base.gameObject);
            GameManager.Instance.StartCoroutine(SpawnMimic(user));
            GameManager.Instance.StartCoroutine(SpawnMimic(user));
            if (LastOwner.HasMTGConsoleID("mimic_tooth_necklace")) //Extra mimic
                GameManager.Instance.StartCoroutine(SpawnMimic(user));
            //StartCoroutine(ItemBuilder.HandleDuration(this, 2f, user, null));
        }

        public static IEnumerator SpawnMimic(PlayerController user)
        {
            var room = user.CurrentRoom;
            IntVector2 pos = room.GetRandomVisibleClearSpot(3, 3);
            yield return new WaitForSeconds(2f);
            var rm = GameManager.Instance.RewardManager;
            Chest chest = rm.SpawnRoomClearChestAt(pos);
            chest.overrideMimicChance = 1;
            chest.MaybeBecomeMimic();
            chest.ForceOpen(user);

            AIActor mimic = room.GetNearestEnemy(chest.sprite.WorldCenter, out float dist);
            if (UnityEngine.Random.value >= 1 / 4f)
            {
                mimic.CanDropItems = false;
                mimic.AdditionalSafeItemDrops.Clear();
            }
            yield break;
        }
    }
}
