using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ItemAPI;
using UnityEngine;
using Dungeonator;
using System.Collections;

using MonoMod.RuntimeDetour;
namespace CustomItems
{
    class HologramItem : PlayerItem
    {
        private string holoShader = "Brave/Internal/HologramShader";
        private const float rollDamageMultiplier = 5f;
        private static CombineEvaporateEffect effect;
        private static FieldInfo rolledDamagedEnemies = typeof(PlayerController).GetField("m_rollDamagedEnemies", BindingFlags.NonPublic | BindingFlags.Instance);
        public static HologramItem Instance;
        private List<AIActor> zappedEnemies = new List<AIActor>();
        private Hook preDeathHook;

        public static void Init()
        {
            //The name of the item
            string itemName = "Material Emancipation Grill";

            //Refers to an embedded png in the project. Make sure to embed your resources!
            string resourceName = "CustomItems/Resources/P1/MEG";

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<HologramItem>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "For Science";
            string longDesc = "Creates a field around the user that vaporizes unauthorized materials.\n\n" +
                "This handy piece of technology found its way to the Gungeon in the hands of a " +
                "mysteriously silent woman. Some say she possessed a form of serious brain damage.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 1000);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;

            effect = GetCombineEffect();
        }

        public static void HookedActorPreDeath(Action<AIActor, Vector2> orig, AIActor self, Vector2 finalDamageDirection)
        {
            Instance.ActorPreDeath(self);
            orig(self, finalDamageDirection);
        }

        private static CombineEvaporateEffect GetCombineEffect()
        {
            var gun = Gungeon.Game.Items["combined_rifle"].GetComponent<Gun>();
            return gun.alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>();
        }

        public override void Pickup(PlayerController player)
        {
            if (this.m_pickedUp)
            {
                return;
            }
            Instance = this;

            base.Pickup(player);
        }

        protected override void DoEffect(PlayerController user)
        {
            ApplyDodgeRollDamageModifier(user, true);
            StartCoroutine(this.HandleDuration(user));
            preDeathHook = new Hook(
                typeof(AIActor).GetMethod("PreDeath", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(HologramItem).GetMethod("HookedActorPreDeath")
            );
        }

        public void ActorPreDeath(AIActor aiActor)
        {
            var player = this.LastOwner;
            if (player.IsDodgeRolling)
            {
                var rolledEnemies = (List<AIActor>)rolledDamagedEnemies.GetValue(player);
                if (!zappedEnemies.Contains(aiActor) && aiActor.IsNormalEnemy && (!aiActor.healthHaver || !aiActor.healthHaver.IsBoss))
                {
                    zappedEnemies.Add(aiActor);
                    GameManager.Instance.Dungeon.StartCoroutine(this.HandleEnemyDeath(aiActor));
                }
            }
        }

        private void ApplyDodgeRollDamageModifier(PlayerController user, bool apply)
        {
            if (apply && this.IsCurrentlyActive || !apply && !this.IsCurrentlyActive) return;
            float newDamage;
            if (apply)
            {
                newDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.DodgeRollDamage) * rollDamageMultiplier;
                user.stats.SetBaseStatValue(PlayerStats.StatType.DodgeRollDamage, newDamage, user);
            }
            else
            {
                newDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.DodgeRollDamage) / rollDamageMultiplier;
                user.stats.SetBaseStatValue(PlayerStats.StatType.DodgeRollDamage, newDamage, user);
            }
        }

        protected override void OnPreDrop(PlayerController user)
        {
            HandleInvulnerability(user, false);
            ApplyDodgeRollDamageModifier(user, false);
            this.IsCurrentlyActive = false;
            base.OnPreDrop(user);
        }

        //Disables the item if the player's health is less than or equal to 1 heart
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }

        float duration = 8f;
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
                HandleInvulnerability(user, true);
                yield return null;
            }
            HandleInvulnerability(user, false);
            ApplyDodgeRollDamageModifier(user, false);
            this.IsCurrentlyActive = false;
            preDeathHook.Dispose();
            yield break;
        }

        private void HandleInvulnerability(PlayerController user, bool active)
        {
            user.healthHaver.IsVulnerable = !active;
            if (active)
            {
                user.SetOverrideShader(ShaderCache.Acquire(holoShader));
            }
            else
            {
                user.ClearOverrideShader();
            }
        }

        private IEnumerator HandleEnemyDeath(AIActor target)
        {
            target.EraseFromExistenceWithRewards(false);
            Transform copyTransform = this.CreateEmptySprite(target);
            tk2dSprite copySprite = copyTransform.GetComponentInChildren<tk2dSprite>();
            GameObject gameObject = Instantiate<GameObject>(effect.ParticleSystemToSpawn, copySprite.WorldCenter.ToVector3ZisY(0f), Quaternion.identity);
            gameObject.transform.parent = copyTransform;
            /*
            ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
            if (copySprite)
            {
                gameObject.transform.position = copySprite.WorldCenter;
                Bounds bounds = copySprite.GetBounds();
                component.shape.scale = new Vector3(bounds.extents.x * 2f, bounds.extents.y * 2f, 0.125f);
            }
            */
            float elapsed = 0f;
            float duration = 2.5f;
            copySprite.renderer.material.DisableKeyword("TINTING_OFF");
            copySprite.renderer.material.EnableKeyword("TINTING_ON");
            copySprite.renderer.material.DisableKeyword("EMISSIVE_OFF");
            copySprite.renderer.material.EnableKeyword("EMISSIVE_ON");
            copySprite.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_ON");
            copySprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
            copySprite.renderer.material.SetFloat("_EmissiveThresholdSensitivity", 5f);
            copySprite.renderer.material.SetFloat("_EmissiveColorPower", 1f);
            int emId = Shader.PropertyToID("_EmissivePower");
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                float t = elapsed / duration;
                copySprite.renderer.material.SetFloat(emId, Mathf.Lerp(1f, 10f, t));
                copySprite.renderer.material.SetFloat("_BurnAmount", t);
                copyTransform.position += Vector3.up * BraveTime.DeltaTime * 1f;
                yield return null;
            }
            Destroy(copyTransform.gameObject);
            yield break;
        }


        private Transform CreateEmptySprite(AIActor target)
        {
            GameObject gameObject = new GameObject("suck image");
            gameObject.layer = target.gameObject.layer;
            tk2dSprite tk2dSprite = gameObject.AddComponent<tk2dSprite>();
            gameObject.transform.parent = SpawnManager.Instance.VFX;
            tk2dSprite.SetSprite(target.sprite.Collection, target.sprite.spriteId);
            tk2dSprite.transform.position = target.sprite.transform.position;
            GameObject gameObject2 = new GameObject("image parent");
            gameObject2.transform.position = tk2dSprite.WorldCenter;
            tk2dSprite.transform.parent = gameObject2.transform;
            tk2dSprite.usesOverrideMaterial = true;
            if (target.optionalPalette != null)
            {
                tk2dSprite.renderer.material.SetTexture("_PaletteTex", target.optionalPalette);
            }
            if (tk2dSprite.renderer.material.shader.name.Contains("ColorEmissive"))
            {
            }
            return gameObject2.transform;
        }
    }
}
