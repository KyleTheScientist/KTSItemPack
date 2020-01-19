using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
namespace CustomItems
{
    public class MagicMirror : PlayerItem
    {

        public static void Init()
        {
            string itemName = "Magic Mirror";
            string resourceName = "CustomItems/Resources/P2/magic_mirror";

            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MagicMirror>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Kaliber's Looking Glass";
            string longDesc = "Reflects nearby bullets.\n\n" +
                "Forged around a shard of glass found at the foot of a shattered mirror. " +
                "It eminates a dark energy that frightens bullets to their core.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            item.quality = ItemQuality.B;
        }

        protected override void DoEffect(PlayerController user)
        {
            base.DoEffect(user);
            foreach (var projectile in GetBullets())
            {
                PassiveReflectItem.ReflectBullet(projectile, true, user, 10f, 1f, 1f, 0f);
            }
        }

        private static List<Projectile> GetBullets()
        {
            List<Projectile> list = new List<Projectile>();
            var allProjectiles = StaticReferenceManager.AllProjectiles;
            for (int i = 0; i < allProjectiles.Count; i++)
            {
                Projectile projectile = allProjectiles[i];
                if (projectile && projectile.sprite && !projectile.ImmuneToBlanks && !projectile.ImmuneToSustainedBlanks)
                {
                    if (projectile.Owner != null)
                    {
                        if (projectile.isFakeBullet || projectile.Owner is AIActor || (projectile.Shooter != null && projectile.Shooter.aiActor != null) || projectile.ForcePlayerBlankable)
                        {
                            list.Add(projectile);
                        }
                    }
                }
            }
            return list;
        }

    }
}
