using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
public class NinjaMask : PassiveItem
{
    private float rollDistanceMultiplier = 1.3f;
    protected static Action<PlayerController> OnDodgeRollEnded;

    public static void Init()
    {
        string itemName = "Mask"; 
        string resourceName = "CustomItems/Resources/P2/ninja_mask";

        GameObject obj = new GameObject();
        var item = obj.AddComponent<NinjaMask>();
        ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

        string shortDesc = "Hidden and Forgotten";
        string longDesc = "Improves dodgerolling abilities.\n\n" +
            "This item was found in the back of a desk drawer in a previously inaccessible wing of the Gungeon. " +
            "Next to it was a note that read, \"What a weeb!\"";

        ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
        item.quality = PickupObject.ItemQuality.D;

        Hook rollEnd = new Hook(
            typeof(PlayerController).GetMethod("ClearDodgeRollState", BindingFlags.NonPublic | BindingFlags.Instance),
            typeof(NinjaMask).GetMethod("ClearDodgeRollState")
        );

    }

    public static void ClearDodgeRollState(Action<PlayerController> orig, PlayerController self)
    {
        orig(self);
        OnDodgeRollEnded?.Invoke(self);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);

        player.OnPreDodgeRoll += this.OnPreDodgeRoll;
        NinjaMask.OnDodgeRollEnded += OnPostDodgeRoll;

        player.rollStats.distance *= rollDistanceMultiplier;
    }

    private PlayerController affectedPlayer;
    private void OnPreDodgeRoll(PlayerController user)
    {
        user.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider));
        affectedPlayer = user;
    }

    private void OnPostDodgeRoll(PlayerController user)
    {
        if(user == affectedPlayer)
        {
            user.specRigidbody.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider));
            affectedPlayer = null;
        }
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.specRigidbody.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider));
        player.rollStats.distance /= rollDistanceMultiplier;
        return base.Drop(player);
    }

}
