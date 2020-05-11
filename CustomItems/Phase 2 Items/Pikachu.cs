using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

using MonoMod.RuntimeDetour;
using Dungeonator;
using Random = UnityEngine.Random;
namespace CustomItems
{
    class Pikachu : ActiveSummonItem
    {
        public static GameObject pikaPrefab;
        private static readonly string guid = "pikachu";
        private static tk2dSpriteCollectionData pikaCollection;

        private GameObject companion;
        public static void Init()
        {
            string itemName = "Capture Sphere";
            string resourceName = "CustomItems/Resources/P2/capture_sphere";

            GameObject obj = new GameObject();
            var item = obj.AddComponent<Pikachu>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "I choose you!";
            string longDesc = "Summon a shockingly adorable mouse. \n\n" +
                "Try to let it out often, it doesn't seem to like being kept in captivity.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            item.quality = PickupObject.ItemQuality.B;
            item.CompanionGuid = guid;
            item.IsTimed = true;
            item.Lifespan = 60f;

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 350);

            BuildPrefab();

            new Hook(
                typeof(ActiveSummonItem).GetMethod("DestroyCompanion", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(Pikachu).GetMethod("DestroyCompanion")
            );
        }

        public static void DestroyCompanion(Action<ActiveSummonItem> orig, ActiveSummonItem self)
        {
            if (self is Pikachu)
                (self as Pikachu).DoWarpEffect();
            orig(self);
        }

        protected override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_teleport_arrive_01", this.LastOwner.gameObject);
            base.DoEffect(user);
            FieldInfo m_extantCompanion = typeof(ActiveSummonItem).GetField("m_extantCompanion", BindingFlags.Instance | BindingFlags.NonPublic);

            companion = (GameObject)m_extantCompanion.GetValue(this);
            if (!companion)
                return;

            DoWarpEffect(false);

        }

        private void DoWarpEffect(bool playSound = true)
        {
            if (!companion) return;

            var position = companion.GetComponent<tk2dSprite>().WorldBottomCenter;
            GameObject warpFXPrefab = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Teleport_Beam");
            if (position != null)
            {
                if (playSound)
                    AkSoundEngine.PostEvent("Play_OBJ_teleport_arrive_01", this.LastOwner.gameObject);
                GameObject warpFX = GameObject.Instantiate<GameObject>(warpFXPrefab);
                warpFX.GetComponent<tk2dBaseSprite>().PlaceAtLocalPositionByAnchor(position, tk2dBaseSprite.Anchor.LowerCenter);
                warpFX.transform.position = warpFX.transform.position.Quantize(0.0625f);
                warpFX.GetComponent<tk2dBaseSprite>().UpdateZDepth();
            }
        }

        private static string[] spritePaths = new string[]
        {
            "CustomItems/Resources/P2/pikachu/pika_idle_left_001", //0-3
            "CustomItems/Resources/P2/pikachu/pika_idle_left_002",
            "CustomItems/Resources/P2/pikachu/pika_idle_left_003",
            "CustomItems/Resources/P2/pikachu/pika_idle_left_004",

            "CustomItems/Resources/P2/pikachu/pika_idle_right_001", //4-7
            "CustomItems/Resources/P2/pikachu/pika_idle_right_002",
            "CustomItems/Resources/P2/pikachu/pika_idle_right_003",
            "CustomItems/Resources/P2/pikachu/pika_idle_right_004",

            "CustomItems/Resources/P2/pikachu/pika_run_left_001", //8-13
            "CustomItems/Resources/P2/pikachu/pika_run_left_002",
            "CustomItems/Resources/P2/pikachu/pika_run_left_003",
            "CustomItems/Resources/P2/pikachu/pika_run_left_004",
            "CustomItems/Resources/P2/pikachu/pika_run_left_005",
            "CustomItems/Resources/P2/pikachu/pika_run_left_006",

            "CustomItems/Resources/P2/pikachu/pika_run_right_001", //14-19
            "CustomItems/Resources/P2/pikachu/pika_run_right_002",
            "CustomItems/Resources/P2/pikachu/pika_run_right_003",
            "CustomItems/Resources/P2/pikachu/pika_run_right_004",
            "CustomItems/Resources/P2/pikachu/pika_run_right_005",
            "CustomItems/Resources/P2/pikachu/pika_run_right_006",

            "CustomItems/Resources/P2/pikachu/pika_attack_left_001", //20-21
            "CustomItems/Resources/P2/pikachu/pika_attack_left_002",

            "CustomItems/Resources/P2/pikachu/pika_attack_right_001", //22-23
            "CustomItems/Resources/P2/pikachu/pika_attack_right_002", 
        };

        public static void BuildPrefab()
        {
            if (pikaPrefab != null || CompanionBuilder.companionDictionary.ContainsKey(guid))
            {
                Tools.PrintError("Tried to make the same Pikachu prefab twice!");
                return;
            }

            pikaPrefab = CompanionBuilder.BuildPrefab("Pikachu", guid, spritePaths[0], new IntVector2(3, 2), new IntVector2(8, 9));
            var pika = pikaPrefab.AddComponent<PikaBehavior>();

            var aiAnimator = pika.aiAnimator;
            aiAnimator.MoveAnimation = new DirectionalAnimation()
            { //animation
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                Flipped = new DirectionalAnimation.FlipType[] { DirectionalAnimation.FlipType.None, DirectionalAnimation.FlipType.None },
                AnimNames = new string[] { "run_right", "run_left" },
            };

            aiAnimator.IdleAnimation = new DirectionalAnimation()
            {
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                Flipped = new DirectionalAnimation.FlipType[] { DirectionalAnimation.FlipType.None, DirectionalAnimation.FlipType.None },
                AnimNames = new string[] { "idle_right", "idle_left" },
            };


            var attackAnim = new DirectionalAnimation()
            {
                Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                Flipped = new DirectionalAnimation.FlipType[] { DirectionalAnimation.FlipType.None, DirectionalAnimation.FlipType.None },
                AnimNames = new string[] { "attack_right", "attack_left" },
            };
            aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>()
            {
                new AIAnimator.NamedDirectionalAnimation()
                {
                    name = "attack",
                    anim = attackAnim
                }
            };

            if (pikaCollection == null)
            {
                pikaCollection = SpriteBuilder.ConstructCollection(pikaPrefab, "Pikachu_Collection");
                GameObject.DontDestroyOnLoad(pikaCollection);
                for (int i = 0; i < spritePaths.Length; i++)
                {
                    SpriteBuilder.AddSpriteToCollection(spritePaths[i], pikaCollection);
                }
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 0, 1, 2, 3 }, "idle_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 5;
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 4, 5, 6, 7 }, "idle_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 5;
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 8, 9, 10, 11, 12, 13 }, "run_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 10;
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 14, 15, 16, 17, 18, 19 }, "run_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 10;
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 20, 21 }, "attack_left", tk2dSpriteAnimationClip.WrapMode.RandomLoop).fps = 20;
                SpriteBuilder.AddAnimation(pika.spriteAnimator, pikaCollection, new List<int>() { 22, 23 }, "attack_right", tk2dSpriteAnimationClip.WrapMode.RandomLoop).fps = 20;

            }


            pika.aiActor.MovementSpeed = 7f;
            pika.specRigidbody.Reinitialize();

            var bs = pika.behaviorSpeculator;
            bs.AttackBehaviors.Add(new ElectricityAttackBehavior());
            bs.MovementBehaviors.Add(new ApproachEnemiesBehavior());
            bs.MovementBehaviors.Add(new CompanionFollowPlayerBehavior() { IdleAnimations = new string[] { "idle" } });

            GameObject.DontDestroyOnLoad(pikaPrefab);
            FakePrefab.MarkAsFakePrefab(pikaPrefab);
            pikaPrefab.SetActive(false);
        }

        //--------------------------------------Companion Controller--------------------------------------------

        public class PikaBehavior : CompanionController
        {
            public PlayerController Owner;
            void Start()
            {
                this.Owner = m_owner;
            }
        }

        public class ElectricityAttackBehavior : AttackBehaviorBase
        {
            public GameObject LinkVFXPrefab;
            public float damagePerHit = 3.5f;
            public string attackAnimation = "attack";

            private bool isAttacking;
            private float attackCooldown = 2f;
            private float attackDuration = 2f;
            private float electricDamageCooldown = .25f;
            private float attackTimer;
            private float attackCooldownTimer;
            private Dictionary<AIActor, tk2dTiledSprite> m_extantLinks = new Dictionary<AIActor, tk2dTiledSprite>();
            private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();
            private PlayerController Owner;
            private List<AIActor> roomEnemies = new List<AIActor>();

            public override void Destroy()
            {
                ClearLinks();
                base.Destroy();
            }

            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
            {
                base.Init(gameObject, aiActor, aiShooter);
                LinkVFXPrefab = Gungeon.Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX;
                Owner = this.m_aiActor.GetComponent<PikaBehavior>().Owner;
            }

            public override BehaviorResult Update()
            {
                if (attackTimer > 0 && isAttacking)
                    base.DecrementTimer(ref attackTimer, false);
                else if (attackCooldownTimer > 0 && !isAttacking)
                    base.DecrementTimer(ref attackCooldownTimer, false);

                bool inRange = IsReady();
                if ((!inRange || attackCooldownTimer > 0 || attackTimer == 0 || m_aiActor.TargetRigidbody == null) && isAttacking) //Break link if not in range or time is up
                {
                    StopAttacking();
                    return BehaviorResult.Continue;
                }

                if (inRange && attackCooldownTimer == 0 && !isAttacking) //Start Attacking
                {
                    AkSoundEngine.PostEvent("Play_ENM_electric_charge_01", m_aiActor.gameObject);
                    attackTimer = attackDuration;
                    m_aiActor.SetOverrideOutlineColor(Color.yellow);
                    m_aiAnimator.PlayForDuration(attackAnimation, attackDuration);
                    isAttacking = true;
                }

                if (attackTimer > 0 && inRange) //Continue Attacking
                {
                    this.HandleLinks();
                    return BehaviorResult.SkipAllRemainingBehaviors;
                }

                return BehaviorResult.Continue;
            }

            private void StopAttacking()
            {
                isAttacking = false;
                attackTimer = 0;
                attackCooldownTimer = attackCooldown;
                m_aiActor.ClearOverrideOutlineColor();
                m_aiAnimator.spriteAnimator.Stop();
                m_aiAnimator.spriteAnimator.Play("idle");

                this.ClearLinks();
            }

            private void HandleLinks()
            {
                if (Owner == null)
                    Owner = this.m_aiActor.GetComponent<PikaBehavior>().Owner;
                Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref roomEnemies);
                foreach (var enemy in roomEnemies)
                {
                    if (!m_extantLinks.ContainsKey(enemy) && IsInRange(enemy))
                    {
                        if (!enemy.IsHarmlessEnemy && enemy.IsNormalEnemy && !enemy.healthHaver.IsDead && enemy != this.m_aiActor)
                        {
                            var link = SpawnManager.SpawnVFX(this.LinkVFXPrefab, false).GetComponent<tk2dTiledSprite>();
                            m_extantLinks.Add(enemy, link);
                        }
                    }
                }

                for (int i = 0; i < m_extantLinks.Count; i++)
                {
                    var enemy = m_extantLinks.Keys.ElementAt(i);
                    if (!enemy || !IsInRange(enemy))
                    {
                        SpawnManager.Despawn(m_extantLinks[enemy].gameObject);
                        m_extantLinks.Remove(enemy);
                    }
                    else
                    {
                        UpdateLink(enemy, m_extantLinks[enemy]);
                    }
                }
            }

            private void UpdateLink(AIActor target, tk2dTiledSprite m_extantLink)
            {
                Vector2 unitCenter = m_aiActor.specRigidbody.UnitCenter;
                Vector2 unitCenter2 = target.specRigidbody.HitboxPixelCollider.UnitCenter;

                m_extantLink.transform.position = unitCenter;

                Vector2 vector = unitCenter2 - unitCenter;
                float z = BraveMathCollege.Atan2Degrees(vector.normalized);
                int dimensionsX = Mathf.RoundToInt(vector.magnitude / 0.0625f);

                m_extantLink.dimensions = new Vector2((float)dimensionsX, m_extantLink.dimensions.y);
                m_extantLink.transform.rotation = Quaternion.Euler(0f, 0f, z);
                m_extantLink.UpdateZDepth();

                this.ApplyLinearDamage(unitCenter, unitCenter2);
            }

            private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
            {
                float damage = damagePerHit;
                if (PassiveItem.IsFlagSetForCharacter(Owner, typeof(BattleStandardItem)))
                    damage *= BattleStandardItem.BattleStandardCompanionDamageMultiplier;

                for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
                {
                    AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                    if (!this.m_damagedEnemies.Contains(aiactor))
                    {
                        if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody)
                        {
                            Vector2 zero = Vector2.zero;
                            bool flag = BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero);
                            if (flag)
                            {
                                aiactor.healthHaver.ApplyDamage(this.damagePerHit, Vector2.zero, "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                                GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
                            }
                        }
                    }
                }
            }

            private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
            {
                this.m_damagedEnemies.Add(damagedTarget);
                yield return new WaitForSeconds(electricDamageCooldown);
                this.m_damagedEnemies.Remove(damagedTarget);
                yield break;
            }
            private void ClearLinks()
            {
                List<AIActor> toRemove = new List<AIActor>();
                for (int i = 0; i < m_extantLinks.Count; i++)
                {
                    var enemy = m_extantLinks.Keys.ElementAt(i);
                    SpawnManager.Despawn(m_extantLinks[enemy].gameObject);
                    toRemove.Add(enemy);
                }
                foreach (var enemy in toRemove)
                {
                    m_extantLinks.Remove(enemy);
                }
            }

            public override float GetMaxRange()
            {
                return 5f;
            }

            public override float GetMinReadyRange()
            {
                return 5f;
            }

            public override bool IsReady()
            {
                if (m_aiActor?.TargetRigidbody?.UnitCenter == null) return false;
                return Vector2.Distance(m_aiActor.specRigidbody.UnitCenter, m_aiActor.TargetRigidbody.UnitCenter) <= GetMinReadyRange();
            }

            public bool IsInRange(AIActor enemy)
            {
                if (enemy?.specRigidbody?.UnitCenter == null) return false;
                return Vector2.Distance(m_aiActor.specRigidbody.UnitCenter, enemy.specRigidbody.UnitCenter) <= GetMinReadyRange();
            }
        }

        public class ApproachEnemiesBehavior : MovementBehaviorBase
        {
            public float PathInterval = .25f;
            public float DesiredDistance = 5f;

            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;


            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
            {
                base.Init(gameObject, aiActor, aiShooter);
            }

            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
            }

            public override BehaviorResult Update()
            {
                var target = this.m_aiActor.OverrideTarget;

                if (repathTimer > 0f)
                    return (target == null) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors;

                if (target == null)
                {
                    PickNewTarget();
                    return BehaviorResult.Continue;
                }

                isInRange = Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, target.UnitCenter) <= DesiredDistance;
                if (target != null && !isInRange)
                {
                    this.m_aiActor.PathfindToPosition(target.UnitCenter, smooth: true);
                    this.repathTimer = this.PathInterval;
                    return BehaviorResult.SkipRemainingClassBehaviors;
                }
                else if (target != null && repathTimer >= 0)
                {
                    m_aiActor.ClearPath();
                    repathTimer = -1;
                }



                return BehaviorResult.Continue;
            }

            private void PickNewTarget()
            {
                if (this.m_aiActor == null)
                {
                    Tools.PrintError("PikachuBehavior: Null actor");
                    return;
                }
                if (Owner == null)
                    Owner = this.m_aiActor.GetComponent<PikaBehavior>().Owner;
                Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref this.roomEnemies);
                for (int i = 0; i < roomEnemies.Count; i++)
                {
                    var enemy = roomEnemies[i];
                    if (enemy.IsHarmlessEnemy || !enemy.IsNormalEnemy || enemy.healthHaver.IsDead || enemy == this.m_aiActor)
                        roomEnemies.Remove(enemy);
                }

                if (roomEnemies.Count == 0)
                    this.m_aiActor.OverrideTarget = null;
                else
                    this.m_aiActor.OverrideTarget = roomEnemies[Random.Range(0, roomEnemies.Count)]?.specRigidbody;
            }
        }

    }
}
