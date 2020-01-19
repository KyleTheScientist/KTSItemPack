using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ItemAPI;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace CustomItems
{
    class CursedRing : PlayerItem
    {
        private List<Projectile> affectedProjectiles = new List<Projectile>();

        private Color bulletTint = new Color(.43f, 0f, .28f);
        public static List<PlayerController> cursedPlayers = new List<PlayerController>();

        private List<Tuple<HealthHaver, float, float>> bossData = new List<Tuple<HealthHaver, float, float>>();
        private static FieldInfo m_damageCap = typeof(HealthHaver).GetField("m_damageCap", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_bossDpsCap = typeof(HealthHaver).GetField("m_bossDpsCap", BindingFlags.NonPublic | BindingFlags.Instance);

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Cursed Ring"; //The name of the item
            string resourceName = "CustomItems/Resources/P1/cursed_ring"; //Refers to an embedded png in the project. Make sure to embed your resources!

            //Create new GameObject
            GameObject obj = new GameObject();
            obj.name = itemName;

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<CursedRing>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Til' Death Do You Part";
            string longDesc = "Temporarily allows your bullets to draw on the power of the High Dragun.\n\n" +
                "This ring has been cursed with a strange magic, and cannot be removed.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 2000f);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 2.5f);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.A;
            item.CanBeDropped = false;

            try
            {
                Hook pickupHook = new Hook(
                        typeof(PlayerItem).GetMethod("Pickup"),
                        typeof(CursedRing).GetMethod("CursedPickup")
                        );

                Hook shopHook = new Hook(
                        typeof(ShopItemController).GetMethod("Interact"),
                        typeof(CursedRing).GetMethod("CursedPurchase")
                        );
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }

        public static void CursedPurchase(Action<ShopItemController, PlayerController> orig, ShopItemController self, PlayerController player)
        {
            if (self.item is PlayerItem)
            {
                bool ringEquipped = player.CurrentItem != null && player.CurrentItem.name.Contains("Cursed Ring");
                if (player.maxActiveItemsHeld > 1 && ringEquipped)
                {
                    MethodInfo changeItem = typeof(PlayerController).GetMethod("ChangeItem", BindingFlags.NonPublic | BindingFlags.Instance);
                    changeItem.Invoke(player, new object[] { 1 });
                }
                if (cursedPlayers.Contains(player) && !self.name.Contains("Cursed Ring") && ringEquipped)
                {
                    if (player.maxActiveItemsHeld <= player.activeItems.Count)
                        return;
                }
            }
            orig(self, player);
        }

        public static void CursedPickup(Action<PlayerItem, PlayerController> orig, PlayerItem self, PlayerController player)
        {
            bool ringEquipped = player.CurrentItem != null && player.CurrentItem.name.Contains("Cursed Ring");
            if (player.maxActiveItemsHeld > 1 && ringEquipped)
            {
                MethodInfo changeItem = typeof(PlayerController).GetMethod("ChangeItem", BindingFlags.NonPublic | BindingFlags.Instance);
                changeItem.Invoke(player, new object[] { 1 });
            }
            if (cursedPlayers.Contains(player) && !self.name.Contains("Cursed Ring") && ringEquipped)
            {
                if (player.maxActiveItemsHeld <= player.activeItems.Count)
                    return;
            }
            orig(self, player);
        }

        public override void Pickup(PlayerController player)
        {
            if (!cursedPlayers.Contains(player)) cursedPlayers.Add(player);
            base.Pickup(player);

            //Ensure that this item is always at the 0th index, otherwise it can be dropped by dropping backpack
            int index = -1;
            for (int i = 0; i < player.activeItems.Count; i++)
            {
                if (player.activeItems[i] == this)
                {
                    index = i;
                    break;
                }
            }
            if (index != 0)
            {
                var t = player.activeItems[0];
                player.activeItems[0] = this;
                player.activeItems[index] = t;
            }
        }

        protected override void DoEffect(PlayerController user)
        {
            user.OnPreFireProjectileModifier += ApplyEffectToProjectile;
            user.SetOverrideShader(ShaderCache.Acquire("Brave/LitCutoutUberPhantom"));
            SetBossCapEnabled(false);

            StartCoroutine(ItemBuilder.HandleDuration(this, 12f, user, OnFinish));
        }

        private Projectile ApplyEffectToProjectile(Gun gun, Projectile projectile)
        {
            //if (affectedProjectiles.Contains(projectile)) return projectile;
            projectile.ignoreDamageCaps = true;
            projectile.BossDamageMultiplier = 1.5f;
            projectile.HasDefaultTint = true;
            projectile.DefaultTintColor = bulletTint;
            affectedProjectiles.Add(projectile);
            return projectile;
        }

        private void OnFinish(PlayerController user)
        {
            SetBossCapEnabled(true);
            user.ClearOverrideShader();
            user.OnPreFireProjectileModifier -= ApplyEffectToProjectile;
            foreach (Projectile p in affectedProjectiles)
            {
                p.ignoreDamageCaps = false;
                p.HasDefaultTint = false;
                p.DefaultTintColor = Color.white;
            }
        }


        private void SetBossCapEnabled(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
                    {
                        var healthHaver = enemy.healthHaver;
                        if (healthHaver == null || !healthHaver.IsBoss)
                            continue;
                        bossData.Add(new Tuple<HealthHaver, float, float>(healthHaver, (float)m_damageCap.GetValue(healthHaver), (float)m_bossDpsCap.GetValue(healthHaver)));

                        m_damageCap.SetValue(healthHaver, -1f);
                        m_bossDpsCap.SetValue(healthHaver, -1f);
                    }
                }
                else
                {
                    foreach (var boss in bossData)
                    {
                        HealthHaver healthHaver = boss.x;
                        float damageCap = boss.y;
                        float dpsCap = boss.z;

                        m_damageCap.SetValue(healthHaver, damageCap);
                        m_bossDpsCap.SetValue(healthHaver, dpsCap);
                    }
                    bossData.Clear();
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message + ": " + e.StackTrace);
            }
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }

        public class Tuple<X, Y, Z>
        {
            public X x;
            public Y y;
            public Z z;

            public Tuple(X x, Y y, Z z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
    }
}
