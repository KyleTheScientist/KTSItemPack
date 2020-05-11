using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Collections;
using Dungeonator;
using ItemAPI;

namespace CustomItems
{
    class SlotMachine : PlayerItem
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
        }

        protected override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent(slotRoll, base.gameObject);
            StartCoroutine(this.HandleDuration(user));
        }


        float duration = 1.5f;
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
                yield return null;
            }
            this.IsCurrentlyActive = false;

            ChooseEffect(user);
            yield break;
        }

        private void ChooseEffect(PlayerController user)
        {
            RoomHandler room = user.CurrentRoom;
            float r = UnityEngine.Random.value;

            if (r <= .5)
                GoodEffect(user);
            else
                BadEffect(user);
        }

        private void GoodEffect(PlayerController user)
        {

            var health = user.healthHaver;
            float r = UnityEngine.Random.value;

            string header = "You Win!";
            string text = "";

            if (r < .01)
            {
                var room = user.CurrentRoom;
                IntVector2? pos = room.GetRandomAvailableCell();
                if (pos.HasValue)
                {
                    Chest chest = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(pos.Value);
                    chest.ForceUnlock();
                }
                text = "+1 Chest";
            }
            else if (r < .05)
            {
                health.FullHeal();
                header = "<3 <3 <3";
                text = "Full Heal";
            }
            else if (r < .2)
            {
                user.carriedConsumables.KeyBullets++;
                text = "+1 Key";
            }
            else if (r < .4)
            {
                health.ApplyHealing(.5f);
                text = "+1 Health";
            }
            else if (r < .6)
            {
                health.Armor++;
                text = "+1 Armor";
            }
            else if (r < .85)
            {
                user.carriedConsumables.Currency += 10;
                text = "$";
            }
            else if (r < .95)
            {
                user.carriedConsumables.Currency += 25;
                text = "$ $";
            }
            else
            {
                user.carriedConsumables.Currency += 50;
                text = "$ $ $";
            }

            AkSoundEngine.PostEvent(slotWin, base.gameObject);
            Notify(header, text, true);
        }

        private void BadEffect(PlayerController user)
        {
            float r = UnityEngine.Random.value;
            var health = user.healthHaver;

            string header = "You Lose.";
            string text = "";

            if (r < .001)
            {
                health.ApplyDamage(.5f, Vector2.zero, "Gambling Addiction");
                user.carriedConsumables.KeyBullets = 0;
                user.carriedConsumables.Currency = 0;
                header = "6 6 6";
                text = "Cleaned out!";
            }
            else if (r < .1)
            {
                if (user.carriedConsumables.KeyBullets > 0)
                    user.carriedConsumables.KeyBullets--;
                text = "-1 Key";
            }
            else if (r < .3)
            {
                health.ApplyDamage(.5f, Vector2.zero, "Gambling Addiction");
                text = "Take 1 Damage";
            }
            else if(r < .8f)
            {
                if (user.carriedConsumables.Currency > 0)
                    user.carriedConsumables.Currency -= 10;
                text = "-10 Casings";
            }else
            {
                if (user.carriedConsumables.Currency > 0)
                    user.carriedConsumables.Currency -= 25;
                text = "-25 Casings";
            }

            AkSoundEngine.PostEvent(slotLose, base.gameObject);
            Notify(header, text, false);
        }


        private void Notify(string header, string text, bool win)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                null,
                -1,
                win ? UINotificationController.NotificationColor.GOLD : UINotificationController.NotificationColor.SILVER, 
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
