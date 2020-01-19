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
        }

        private bool droneActive;
        protected override void DoEffect(PlayerController user)
        {
            base.DoEffect(user);
            if (!droneActive)
            {
                AkSoundEngine.PostEvent("Play_OBJ_mine_beep_01", user.gameObject);
                //moveSpeed = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
                //user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, 0, user);

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
                //user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, moveSpeed, user);
                droneActive = false;
            }

        }

        protected override void OnPreDrop(PlayerController user)
        {
            base.OnPreDrop(user);
            if (droneActive)
            {
                DetonateDrone();
                //user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, moveSpeed, user);
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

        private void DetonateDrone()
        {
            Exploder.Explode(extant_drone.GetComponent<tk2dSprite>().WorldCenter, StickyBomb.explosionData, Vector2.zero);
            GameObject.Destroy(extant_drone);
        }

        public static void BuildDronePrefab()
        {
            if (dronePrefab != null) return;

            dronePrefab = SpriteBuilder.SpriteFromResource(spritePaths[0], null, false).gameObject;
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




        public class DroneBehaviour : BraveBehaviour
        {
            private PlayerController owner;
            private float droneMoveSpeed = 10f;
            private Vector2 velocity = new Vector2();

            void Start()
            {
                //PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(specRigidbody, null, false);

                spriteAnimator.Play("idle");
                sprite.renderer.material.shader = ShaderCache.Acquire(PlayerController.DefaultShaderName);
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
                if (Vector2.Distance(owner.unadjustedAimPoint.XY(), this.sprite.WorldCenter) < .2f)
                {
                    velocity = Vector2.Lerp(velocity, Vector2.zero, .5f);
                }
                else
                {
                    velocity = Vector2.Lerp(velocity, vector * droneMoveSpeed, .1f);
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
