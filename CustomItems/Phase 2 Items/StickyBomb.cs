using UnityEngine;
using System.Collections;
using System;

using System.Reflection;
using MonoMod.RuntimeDetour;
using Random = UnityEngine.Random;
using ItemAPI;
using CustomItems;
public class StickyBomb : PassiveItem
{
    public static ExplosionData explosionData;
    public static GameObject stickyBombPrefab;
    private static string itemID = "Adhesive Grenade"; //The name of the item
    private static string resourcePath = "CustomItems/Resources/P2/sticky_bomb"; //Refers to an embedded png in the project. Make sure to embed your resources!

    public static void Init()
    {

        GameObject obj = new GameObject();
        var item = obj.AddComponent<StickyBomb>();
        ItemBuilder.AddSpriteToObject(itemID, resourcePath, obj);

        string shortDesc = "Get it off me!";
        string longDesc = "5% chance on hit to attach a bomb to an enemy, detonating for 2000% of your base damage.\n\n" +
            "See the orange end? Don't touch it.";

        ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
        item.quality = PickupObject.ItemQuality.C;

        stickyBombPrefab = SpriteBuilder.SpriteFromResource(resourcePath).GetComponent<tk2dSprite>().gameObject;
        GameObject.DontDestroyOnLoad(stickyBombPrefab);
        FakePrefab.MarkAsFakePrefab(stickyBombPrefab);
        stickyBombPrefab.SetActive(false);

        var explosionTemplate = Gungeon.Game.Items["c4"].GetComponent<RemoteMineItem>().objectToSpawn.GetComponent<RemoteMineController>().explosionData;
        explosionData = new ExplosionData()
        {
            useDefaultExplosion = false,
            doDamage = true,
            forceUseThisRadius = false,
            damageRadius = 2.5f,
            damageToPlayer = 0,
            damage = 60f,
            breakSecretWalls = false,
            secretWallsRadius = 4.5f,
            forcePreventSecretWallDamage = false,
            doDestroyProjectiles = true,
            doForce = true,
            pushRadius = 6f,
            force = 50f,
            debrisForce = 50f,
            preventPlayerForce = false,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            comprehensiveDelay = 0f,
            effect = explosionTemplate.effect,
            doScreenShake = true,
            ss = explosionTemplate.ss,
            doStickyFriction = true,
            doExplosionRing = true,
            isFreezeExplosion = false,
            freezeRadius = 0f,
            freezeEffect = null,
            playDefaultSFX = true,
            IsChandelierExplosion = false,
            rotateEffectToNormal = false,
            ignoreList = explosionTemplate.ignoreList,
            overrideRangeIndicatorEffect = null,
        };
    }

    public static void Detonate(Action<RemoteMineController> orig, RemoteMineController self)
    {
        orig(self);
        foreach (var property in typeof(ExplosionData).GetFields())
        {
            Tools.Print(property.Name + " = " + property.GetValue(self.explosionData));
        }
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += this.PostProcessProjectile;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.PostProcessProjectile -= this.PostProcessProjectile;
        return base.Drop(player);
    }

    private float chanceToSticky = .05f;
    void PostProcessProjectile(Projectile projectile, float effectChanceScalar)
    {
        if (Random.value < (chanceToSticky * effectChanceScalar))
        {
            projectile.OnHitEnemy += (proj, enemy, fatal) =>
            {
                StartCoroutine(AttachBomb(enemy, proj.Owner.GetComponent<PlayerController>(), fatal));
            };
        }
    }

    public static IEnumerator AttachBomb(SpeculativeRigidbody enemy, PlayerController murdler, bool fatal)
    {
        var obj = GameObject.Instantiate(stickyBombPrefab);
        obj.SetActive(true);

        var sprite = obj.GetComponent<tk2dSprite>();

        sprite.PlaceAtPositionByAnchor(enemy.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter);
        if (!fatal)
            sprite.transform.SetParent(enemy.transform);

        yield return new WaitForSeconds(.75f);
        if (murdler)
            AkSoundEngine.PostEvent("Play_OBJ_mine_beep_01", murdler.gameObject);
        yield return new WaitForSeconds(.1f);
        if (murdler)
            AkSoundEngine.PostEvent("Play_OBJ_mine_beep_01", murdler.gameObject);
        yield return new WaitForSeconds(.1f);
        if (murdler)
            AkSoundEngine.PostEvent("Play_OBJ_mine_beep_01", murdler.gameObject);

        if (sprite && murdler)
        {
            explosionData.damage = murdler.stats.GetBaseStatValue(PlayerStats.StatType.Damage) * 20f;
            Exploder.Explode(sprite.WorldCenter, explosionData, Vector2.zero, null, false);
            GameObject.Destroy(sprite);
        }
    }
}
