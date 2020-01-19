using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace CustomItems
{
    class LightningGuon : IounStoneOrbitalItem
    {

        public static Hook guonHook;
        public static bool speedUp = false;
        public static PlayerOrbital orbitalPrefab;

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            string itemName = "Ring of Guon Swiftness"; //The name of the item
            string resourceName = "CustomItems/Resources/P1/ring_of_guon_swiftness"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<LightningGuon>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "You Spin Me Right 'Round";
            string longDesc = "Increases the rotational velocity of orbitals.\n\n" +
                "The ring emits an energy much like the one from which guons draw" +
                "their power, thereby increasing their speed.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            item.quality = PickupObject.ItemQuality.D;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            item.Identifier = IounStoneIdentifier.GENERIC;
        }

        public static void BuildPrefab()
        {
            string kissMyButt = "If you're digging through my code you can kiss my butt";
            string.IsNullOrEmpty(kissMyButt);

            if (LightningGuon.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("CustomItems/Resources/P1/swiftness_guon");
            prefab.name = "Swiftness Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(7, 13));
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 2.5f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }

        public override void Pickup(PlayerController player)
        {
            foreach (var orbital in player.orbitals)
            {
                var o = (PlayerOrbital)orbital;
                o.orbitDegreesPerSecond = 180f;
            }

            if (player.GetComponent<LightningGuonBehaviour>() != null)
            {
                player.GetComponent<LightningGuonBehaviour>().Destroy();
            }
            player.gameObject.AddComponent<LightningGuonBehaviour>();

            speedUp = true;
            guonHook = new Hook(
                typeof(PlayerOrbital).GetMethod("Initialize"),
                typeof(LightningGuon).GetMethod("GuonInit")
            );

            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            if (player.GetComponent<LightningGuonBehaviour>() != null)
            {
                player.GetComponent<LightningGuonBehaviour>().Destroy();
            }
            guonHook.Dispose();
            speedUp = false;

            return base.Drop(player);
        }
        protected override void OnDestroy()
        {
            guonHook.Dispose();

            if (Owner && Owner.GetComponent<LightningGuonBehaviour>() != null)
            {
                Owner.GetComponent<LightningGuonBehaviour>().Destroy();
            }
            speedUp = false;
            base.OnDestroy();
        }

        public static void GuonInit(Action<PlayerOrbital, PlayerController> orig, PlayerOrbital self, PlayerController player)
        {
            self.orbitDegreesPerSecond = speedUp ? 180f : 90f;
            orig(self, player);
        }

        private class LightningGuonBehaviour : BraveBehaviour
        {
            PlayerController owner;

            void Start()
            {
                owner = GetComponent<PlayerController>();
            }

            void FixedUpdate()
            {
                foreach (var orbital in owner.orbitals)
                {
                    var o = (PlayerOrbital)orbital;
                    o.orbitDegreesPerSecond = 180f;
                }
            }

            public void Destroy()
            {
                foreach (var orbital in owner.orbitals)
                {
                    var o = (PlayerOrbital)orbital;
                    o.orbitDegreesPerSecond = 90;
                }
                Destroy(this);
            }
        }
    }
}
