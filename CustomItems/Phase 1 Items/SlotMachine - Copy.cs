using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Collections;
using Dungeonator;
using ItemAPI;
using Random = UnityEngine.Random;
namespace CustomItems
{
    class SlotMachine2 : PlayerItem
    {
        string slotRoll = "Play_OBJ_Chest_Synergy_Slots_01";
        string slotLose = "Play_OBJ_metronome_fail_01";
        string slotWin = "Play_OBJ_Chest_Synergy_Win_01";

        public static void Init()
        {
            //The name of the item
            string itemName = "Slot Machine";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/slot_machine";

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<SlotMachine>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Just One More Spin";
            string longDesc = "Prototype slot machine designed long ago by Daisuke himself.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 250f);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
            DefineEffects();
        }

        public static List<Effect> effects = new List<Effect>();

        public struct Effect
        {
            public PlayerStats.StatType statToEffect;
            public float amount;
            public int pickupID;
            public float min, max;
            public StatModifier.ModifyMethod modifyMethod;
            public Action<Effect, PlayerController> action;
            public string notificationHeader;
            public string notificationText;
            public List<Effect> subEffects;
        }

        protected override void DoEffect(PlayerController user)
        {
            //Pick random effect
            int r = Random.Range(0, effects.Count);
            Effect effect = effects[0];

            //Do the effect
            effect.action.Invoke(effect, user);

            //
            Notify(effect.notificationHeader, effect.notificationText);
        }

        public static void DefineEffects()
        {
            effects.Add(new Effect()
            {
                notificationText = "Accuracy up",
                action = GenericStatModifier,
                statToEffect = PlayerStats.StatType.Accuracy,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = .2f,
            });

            effects.Add(new Effect()
            {
                notificationText = "Movement speed up",
                action = GenericStatModifier,
                statToEffect = PlayerStats.StatType.MovementSpeed,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = 1f,
            });

            effects.Add(new Effect()
            {
                notificationText = "Health up!",
                action = HealthModifier,
                //statToEffect = PlayerStats.StatType.Health,
                min = -1,
                max = -1,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = 1f,
            });

            effects.Add(new Effect()
            {
                notificationText = "Health down!",
                action = HealthModifier,
                //statToEffect = PlayerStats.StatType.Health,
                min = 2,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = -1f,
            });
        }

        public static void GiveItem(Effect effect, PlayerController user)
        {

        }

        public static void HealthModifier(Effect effect, PlayerController user)
        {
            if(user.characterIdentity == PlayableCharacters.Robot)
            {

            }
            else if(user.healthHaver.GetCurrentHealth() > effect.min)
            {

            }
        }

        public static void GenericStatModifier(Effect effect, PlayerController user)
        {
            float currentStatValue = user.stats.GetBaseStatValue(effect.statToEffect);
            if (effect.modifyMethod == StatModifier.ModifyMethod.MULTIPLICATIVE)
                user.stats.SetBaseStatValue(effect.statToEffect, currentStatValue * effect.amount, user);
            else
                user.stats.SetBaseStatValue(effect.statToEffect, currentStatValue + effect.amount, user);
        }



        private void Notify(string header, string text)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }


    }
}
