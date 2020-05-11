using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace CustomItems
{
    public class Drone : PlayerItem
    {
        private static GameObject dronePrefab;
        private GameObject extant_drone;
        private static tk2dSpriteCollectionData droneCollection;
        private static string[] spritePaths = new string[]
        {
            "CustomItems/Resources/P2/drone/drone_idle_001",
            "CustomItems/Resources/P2/drone/drone_idle_002",

            "CustomItems/Resources/P2/drone/drone_forward_001",
            "CustomItems/Resources/P2/drone/drone_forward_002",

            "CustomItems/Resources/P2/drone/drone_back_001",
            "CustomItems/Resources/P2/drone/drone_back_002",

            "CustomItems/Resources/P2/drone/drone_left_001",
            "CustomItems/Resources/P2/drone/drone_left_002",

            "CustomItems/Resources/P2/drone/drone_right_001",
            "CustomItems/Resources/P2/drone/drone_right_002",
        };

        private static float cooldown = 500;
        public static void Init()
        {
            string itemName = "Toy Drone";
            string resourceName = "CustomItems/Resources/P2/drone_controller";

            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Drone>();

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Zoom Zoom Boom";
            string longDesc = "A toy for children that was recalled soon after its release.\n\n" +
                "In a remarkable feat of entrepreneurialism, the maker of this toy sold its patent to the " +
                "Imperial Hegemony of Man, who repurposed it for combat.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, cooldown);
            item.quality = ItemQuality.C;
            BuildDronePrefab();
            item.AddToSubShop(ItemBuilder.ShopType.Trorc, 1f);
            CustomSynergies.Add("The Fire and the Flames", new List<string>() { "kts:toy_drone", "napalm_strike" });
            CustomSynergies.Add("Apache Thunder", new List<string>() { "kts:toy_drone", "air_strike" });
            CustomSynergies.Add("Remote Control", new List<string>() { "kts:toy_drone" }, new List<string>() { "remote_bullets", "3rd_party_controller" });
        }

        private bool droneActive;
        protected override void DoEffect(PlayerController user)
        {
            base.DoEffect(user);
            if (!droneActive)
            {
                AkSoundEngine.PostEvent("Play_OBJ_mine_beep_01", user.gameObject);

                extant_drone = GameObject.Instantiate(dronePrefab, user.transform);
                extant_drone.AddComponent<DroneBehaviour>().SetOwner(user);
                var pos = GetFreePosition(user);
                if (pos == Vector2.zero)
                    pos = user.sprite.WorldTopCenter;
                extant_drone.transform.position = pos;
                droneActive = true;
            }
            else
            {
                DetonateDrone();
                droneActive = false;
            }

        }

        protected override void OnPreDrop(PlayerController user)
        {
            base.OnPreDrop(user);
            if (droneActive)
            {
                DetonateDrone();
                droneActive = false;
            }
        }

        public static Vector2 GetFreePosition(PlayerController user)
        {
            var position = user.sprite.WorldTopCenter;
            Vector2 result = Vector2.zero;
            var cells = user.CurrentRoom.Cells;
            float num = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                float num2 = Vector2.Distance(position, cells[i].ToCenterVector2());
                if (!GameManager.Instance.Dungeon.data[cells[i]].HasWallNeighbor() && num2 < num)
                {
                    result = cells[i].ToCenterVector2();
                    num = num2;
                }
            }
            return result;
        }

        ExplosionData explosionData = new ExplosionData()
        {
            damageRadius = 5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 50f,
            doDestroyProjectiles = false,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = false,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = false,
            ss = new ScreenShakeSettings(),
            playDefaultSFX = true,
        };
        private void DetonateDrone()
        {
            Vector2 position = extant_drone.GetComponent<tk2dSprite>().WorldCenter;
            var defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            explosionData.effect = defaultExplosion.effect;
            explosionData.ignoreList = defaultExplosion.ignoreList;
            if (LastOwner.HasMTGConsoleID("remote_bullets") || LastOwner.HasMTGConsoleID("3rd_party_controller")) //remote bullets = x2 damage
                explosionData.damage = 100f;
            else
                explosionData.damage = 50f;
            
            if (LastOwner.HasMTGConsoleID("air_strike")) //air strike = +4 'splosions
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (Mathf.Abs(i) + Mathf.Abs(j) == 1)
                            Exploder.Explode(position + new Vector2(i * 3f, j * 3f), explosionData, Vector2.zero);
            }

            if (LastOwner.HasMTGConsoleID("napalm_strike")) //napalm strike = fire circle
                DoNapalmSynergy(position);

            Exploder.Explode(position, explosionData, Vector2.zero);
            GameObject.Destroy(extant_drone);

        }

        public static void BuildDronePrefab()
        {
            if (dronePrefab != null) return;

            dronePrefab = SpriteBuilder.SpriteFromResource(spritePaths[0], null).gameObject;
            dronePrefab.name = "Drone";

            //setup rigidbody
            var item = Gungeon.Game.Items["dog"].GetComponent<CompanionItem>();
            var baseRigidbody = EnemyDatabase.GetOrLoadByGuid(item.CompanionGuid).specRigidbody;
            var spriteAnimator = dronePrefab.AddComponent<tk2dSpriteAnimator>();

            var body = dronePrefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(new IntVector2(10, 5), new IntVector2(10, 5));
            body.CollideWithTileMap = true;
            body.CollideWithOthers = true;

            if (droneCollection == null)
            {
                droneCollection = SpriteBuilder.ConstructCollection(dronePrefab, "Drone_Collection");
                GameObject.DontDestroyOnLoad(droneCollection);
                for (int i = 0; i < spritePaths.Length; i++)
                {
                    SpriteBuilder.AddSpriteToCollection(spritePaths[i], droneCollection);
                }
                SpriteBuilder.AddAnimation(spriteAnimator, droneCollection, new List<int>() { 0, 1 }, "idle", tk2dSpriteAnimationClip.WrapMode.Loop);
                SpriteBuilder.AddAnimation(spriteAnimator, droneCollection, new List<int>() { 2, 3 }, "forward", tk2dSpriteAnimationClip.WrapMode.Loop);
                SpriteBuilder.AddAnimation(spriteAnimator, droneCollection, new List<int>() { 4, 5 }, "back", tk2dSpriteAnimationClip.WrapMode.Loop);
                SpriteBuilder.AddAnimation(spriteAnimator, droneCollection, new List<int>() { 6, 7 }, "left", tk2dSpriteAnimationClip.WrapMode.Loop);
                SpriteBuilder.AddAnimation(spriteAnimator, droneCollection, new List<int>() { 8, 9 }, "right", tk2dSpriteAnimationClip.WrapMode.Loop);
            }

            GameObject.DontDestroyOnLoad(dronePrefab);
            FakePrefab.MarkAsFakePrefab(dronePrefab);
            dronePrefab.SetActive(false);
        }

        GoopDefinition napalm;
        DeadlyDeadlyGoopManager ddgm;
        private void DoNapalmSynergy(Vector2 position)
        {
            if (!ddgm)
            {
                napalm = Tools.sharedAuto1.LoadAsset<GoopDefinition>("napalmgoopthatworks");
                ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(napalm);
            }
            ddgm.AddGoopCircle(position, 3f);
        }

        public class DroneBehaviour : BraveBehaviour
        {
            private PlayerController owner;
            private float droneMoveSpeed = 10f;
            private Vector2 velocity = new Vector2();

            void Start()
            {
                spriteAnimator.Play("idle");
                sprite.renderer.material.shader = ShaderCache.Acquire(PlayerController.DefaultShaderName);
                SpriteOutlineManager.AddOutlineToSprite(sprite, Color.black);
            }

            void FixedUpdate()
            {
                if (owner == null) return;
                BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
                Vector2 vector;
                if (instanceForPlayer == null) return;

                if (instanceForPlayer.IsKeyboardAndMouse(false))
                {
                    vector = owner.unadjustedAimPoint.XY() - this.sprite.WorldCenter;
                }
                else
                {
                    if (instanceForPlayer.ActiveActions == null) return;
                    vector = instanceForPlayer.ActiveActions.Aim.Vector;
                }

                vector.Normalize();
                float speed = (owner.HasMTGConsoleID("remote_bullets") || owner.HasMTGConsoleID("3rd_party_controller")) ? droneMoveSpeed * 2f : droneMoveSpeed;
                if (Vector2.Distance(owner.unadjustedAimPoint.XY(), this.sprite.WorldCenter) < .2f)
                {
                    velocity = Vector2.Lerp(velocity, Vector2.zero, .5f);
                }
                else
                {
                    velocity = Vector2.Lerp(velocity, vector * speed, .1f);
                }
                specRigidbody.Velocity = velocity;

                string clip = GetAnimationDirection(velocity);
                if (clip != spriteAnimator.CurrentClip.name)
                    spriteAnimator.Play(clip);

            }


            private string GetAnimationDirection(Vector2 velocity)
            {
                if (velocity.magnitude < .5f) return "idle";
                if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                {
                    return (velocity.x > 0) ? "right" : "left";
                }
                else
                {
                    return (velocity.y > 0) ? "back" : "forward";
                }
            }

            public void SetOwner(PlayerController owner)
            {
                this.owner = owner;
                try
                {
                }
                catch (Exception e) { Tools.PrintException(e); }
            }
        }

        protected override void AfterCooldownApplied(PlayerController user)
        {
            if (droneActive)
            {
                ClearCooldowns();
            }
            this.CurrentDamageCooldown = Mathf.Min(CurrentDamageCooldown, cooldown);
        }
    }
}
