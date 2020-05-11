using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using DirectionType = DirectionalAnimation.DirectionType;
using AnimationType = ItemAPI.CompanionBuilder.AnimationType;
namespace CustomItems
{
    public class BigSlime
    {
        public static GameObject prefab;
        private static readonly string guid = "big_slime42069";

        public static void Init()
        {
            string itemName = "Big Slime";
            string resourceName = "CustomItems/Resources/P3/MySon/item_sprite";

            GameObject obj = new GameObject();
            var item = obj.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Coolest Kid On the Block";
            string longDesc = "This kid is so cool that people are starting to call YOU cool by association.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            item.quality = PickupObject.ItemQuality.C;
            item.CompanionGuid = guid;
            item.Synergies = new CompanionTransformSynergy[0];
            item.AddPassiveStatModifier(PlayerStats.StatType.Coolness, 3f);
            foreach (var syn in GameManager.Instance.SynergyManager.synergies)
            {
                if (syn.NameKey == "#STRAFEPUB")
                {
                    syn.MandatoryGunIDs = new List<int>() { Gungeon.Game.Items["strafe_gun"].PickupObjectId };
                    syn.OptionalItemIDs = new List<int>() { Gungeon.Game.Items["devolver"].PickupObjectId, item.PickupObjectId };
                }
            }
            BuildPrefab();
        }

        public static void BuildPrefab()
        {
            if (prefab != null || CompanionBuilder.companionDictionary.ContainsKey(guid))
                return;

            prefab = CompanionBuilder.BuildPrefab("Big Slime", guid, "CustomItems/Resources/P3/MySon/Idle/son_idle_001", new IntVector2(1, 0), new IntVector2(9, 9));

            var companion = prefab.AddComponent<CompanionController>();
            companion.aiActor.MovementSpeed = 5f;

            prefab.AddAnimation("idle_right", "CustomItems/Resources/P3/MySon/Idle", fps: 5, AnimationType.Idle, DirectionType.TwoWayHorizontal);
            prefab.AddAnimation("idle_left", "CustomItems/Resources/P3/MySon/Idle", fps: 5, AnimationType.Idle, DirectionType.TwoWayHorizontal);
            prefab.AddAnimation("run_right", "CustomItems/Resources/P3/MySon/MoveRight", fps: 7, AnimationType.Move, DirectionType.TwoWayHorizontal);
            prefab.AddAnimation("run_left", "CustomItems/Resources/P3/MySon/MoveLeft", fps: 7, AnimationType.Move, DirectionType.TwoWayHorizontal);

            var bs = prefab.GetComponent<BehaviorSpeculator>();
            bs.MovementBehaviors.Add(new CompanionFollowPlayerBehavior() { IdleAnimations = new string[] { "idle" } });
        }

    }
}
