using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Dungeonator;
using CustomItems;
using ItemAPI;

using MonoMod.RuntimeDetour;
public class CloakAndDagger : PlayerItem
{
    private static readonly string[] spritePaths =
    {
        "CustomItems/Resources/P2/cloak_and_dagger_001",
        "CustomItems/Resources/P2/cloak_and_dagger_002",
    };

    private static int[] spriteIDs;
    private const int cooldown = 500;

    public static void Init()
    {
        string itemName = "Cloak and Dagger";
        string resourceName = spritePaths[0];

        GameObject obj = new GameObject(itemName);
        var item = obj.AddComponent<CloakAndDagger>();

        spriteIDs = new int[spritePaths.Length];

        ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
        spriteIDs[0] = item.sprite.spriteId;
        spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(spritePaths[1], item.sprite.Collection);

        string shortDesc = "Blood on my Suit";
        string longDesc = "Allows the user to sneak up on enemies and stab them in the back, " +
            "although \"back\" is very loosely defined.";

        ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, cooldown);
        ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);
        item.quality = ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula, 1f);
    }

    protected override void OnPreDrop(PlayerController user)
    {
        user.DidUnstealthyAction();
    }

    private bool activeStatus = false;
    protected override void DoEffect(PlayerController user)
    {
        //base.DoEffect(user);
        if (activeStatus && user.IsStealthed)
        {
            BackStabNearestEnemy(user);
        }
        else if (!activeStatus)
        {
            this.HandleStealth(user);
        }
    }

    protected override void AfterCooldownApplied(PlayerController user)
    {
        if (activeStatus)
        {
            ClearCooldowns();
        }
        this.CurrentDamageCooldown = Mathf.Min(CurrentDamageCooldown, cooldown);
    }

    private void BackStabNearestEnemy(PlayerController user)
    {
        float dist = 10f;
        var nearestEnemy = user?.CurrentRoom?.GetNearestEnemy(user.sprite.WorldCenter, out dist, false, true);
        if (nearestEnemy && dist < 2.5f)
        {
            AkSoundEngine.PostEvent("Play_CHR_general_death_01", base.gameObject);
            nearestEnemy.healthHaver.ApplyDamage(900, user.LastCommandedDirection, "Backstab");
            //critEffect.SpawnAtPosition(nearestEnemy.sprite.WorldCenter, user.FacingDirection, transform);
            Pixelator.Instance.FadeToColor(.1f, new Color(1, 0, 0, .5f), true, .1f);
        }
        user.DidUnstealthyAction();
    }

    private void HandleStealth(PlayerController user)
    {
        AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        user.ChangeSpecialShaderFlag(1, 1f);
        user.SetIsStealthed(true, "smoke");
        user.SetCapableOfStealing(true, "StealthItem", null);
        user.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider));
        user.OnDidUnstealthyAction += this.BreakStealth;
        user.OnItemStolen += this.BreakStealthOnSteal;
        SetActiveStatus(true);
    }

    private void BreakStealth(PlayerController user)
    {
        user.OnDidUnstealthyAction -= this.BreakStealth;
        user.OnItemStolen -= this.BreakStealthOnSteal;
        user.specRigidbody.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider));
        user.ChangeSpecialShaderFlag(1, 0f);
        user.SetIsStealthed(false, "smoke");
        user.SetCapableOfStealing(false, "StealthItem", null);
        AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        SetActiveStatus(false);
        ForceApplyCooldown(user);
    }

    private void BreakStealthOnSteal(PlayerController arg1, ShopItemController arg2)
    {
        this.BreakStealth(arg1);
    }

    private void SetActiveStatus(bool active)
    {
        this.activeStatus = active;
        //set sprite here
        sprite.SetSprite(active ? spriteIDs[1] : spriteIDs[0]);
    }
}
