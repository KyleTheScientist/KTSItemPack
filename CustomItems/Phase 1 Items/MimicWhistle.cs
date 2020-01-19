using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using System.Reflection;

namespace CustomItems
{
    class MimicWhistle : PlayerItem
    {
        /*
        string[] mimicIDs =
        {
            "2ebf8ef6728648089babb507dec4edb7",
            "d8d651e3484f471ba8a2daa4bf535ce6",
            "abfb454340294a0992f4173d6e5898a8",
            "d8fd592b184b4ac9a3be217bc70912a2",
            "6450d20137994881aff0ddd13e3d40c8",
        };
        */

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
            string longDesc = "Summons a mimic.\n\n" +
                "Surprisingly, mimics can be tamed! Sort of. They seem to respond well to " +
                "the sound of a whistle at least.\n\nGrants coolness while held.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 1000f);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Coolness, 3);

            //Set some other fields
            item.consumable = true;
            item.consumableHandlesOwnDuration = true;
            item.canStack = true;
            item.numberOfUses = 2;
            item.quality = ItemQuality.A;
        }

        protected override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_BOSS_bulletbros_anger_01", base.gameObject);
            IEnumerator chests = SpawnChests(user);
            GameManager.Instance.StartCoroutine(chests);
            //StartCoroutine(ItemBuilder.HandleDuration(this, 2f, user, null));
        }

        public static IEnumerator SpawnChests(PlayerController user)
        {
            Tools.Print("Spawning chests");
            var room = user.CurrentRoom;
            IntVector2? pos = room.GetRandomVisibleClearSpot(2, 2);
            IntVector2? pos2 = room.GetRandomVisibleClearSpot(2, 2);
            yield return new WaitForSeconds(2f);
            if (pos.HasValue && pos2.HasValue)
            {
                var rm = GameManager.Instance.RewardManager;
                Chest chest = rm.SpawnRoomClearChestAt(pos.Value);
                chest.overrideMimicChance = 1;
                Chest chest2 = rm.SpawnRoomClearChestAt(pos2.Value);
                chest2.overrideMimicChance = 1;

                chest.MaybeBecomeMimic();
                chest2.MaybeBecomeMimic();

                chest.ForceOpen(user);

                chest2.forceContentIds = new List<int>();
                chest2.contents = new List<PickupObject>();
                chest2.ForceOpen(user);

                bool better = rm.GetQualityFromChest(chest) > rm.GetQualityFromChest(chest2);

                float dist;
                AIActor mimic = room.GetNearestEnemy(chest.sprite.WorldCenter, out dist);
                AIActor mimic2 = room.GetNearestEnemy(chest2.sprite.WorldCenter, out dist);

                AIActor toRemoveDrops = !better ? mimic : mimic2;
                toRemoveDrops.CanDropItems = false;
                toRemoveDrops.AdditionalSafeItemDrops.Clear();
            }
            yield break;
        }
    }
}
