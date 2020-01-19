using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;
using Dungeonator;
using System.Collections;

namespace CustomItems
{
    class TerrifyingMask : PlayerItem
    {
        private FleePlayerData fleeData;

        public static void Init()
        {
            //The name of the item
            string itemName = "Terrifying Mask";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/terrifying_mask";

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<TerrifyingMask>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Hard to Look At";
            string longDesc = "A terrifying mask that frightens Bullet Kin to their cores.\n\n" +
                "Resembles a tyrant wizard who has lorded over the Gungeon for centuries.\n" +
                "It's too scary to wear for long.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1.5f);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.D;
        }

        protected override void DoEffect(PlayerController user)
        {
            if (fleeData == null || fleeData.Player != user)
            {
                fleeData = new FleePlayerData();
                fleeData.Player = user;
                fleeData.StartDistance *= 2;
            }
            user.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/VFX_Curse") as GameObject, Vector3.zero, true, false, false);
            StartCoroutine(this.HandleDuration(user));
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }

        float duration = 7f;
        private IEnumerator HandleDuration(PlayerController user)
        {
            if (this.IsCurrentlyActive)
            {
                yield break;
            }
            this.IsCurrentlyActive = true;
            this.m_activeElapsed = 0f;
            this.m_activeDuration = this.duration;
            while (this.m_activeElapsed < this.m_activeDuration && this.IsCurrentlyActive)
            {
                HandleFear(user, true);
                yield return null;
            }
            this.IsCurrentlyActive = false;
            HandleFear(user, false);
            yield break;
        }

        private void HandleFear(PlayerController user, bool active)
        {
            RoomHandler room = user.CurrentRoom;
            if (!room.HasActiveEnemies(RoomHandler.ActiveEnemyType.All)) return;

            if (active)
            {
                foreach (var enemy in room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                {
                    if (enemy.behaviorSpeculator != null)
                    {
                        enemy.behaviorSpeculator.FleePlayerData = this.fleeData;
                        FleePlayerData fleePlayerData = new FleePlayerData();
                    }
                }
                /*
                room.ApplyActionToNearbyEnemies(user.sprite.WorldCenter, 9f, (AIActor enemy, float d) =>
                {
                    if (enemy.behaviorSpeculator != null)
                        enemy.behaviorSpeculator.FleePlayerData = this.fleeData;
                });
                */
            }
            else
            {
                foreach (var enemy in room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                {
                    if (enemy.behaviorSpeculator != null && enemy.behaviorSpeculator.FleePlayerData != null)
                        enemy.behaviorSpeculator.FleePlayerData.Player = null;
                }
            }
        }
    }
}
