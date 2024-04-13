using DG.Tweening;
using Dreamlings.Interfaces;
using UnityEngine;

public class FindableFoodBehavior : MonoBehaviour
{
    private bool CanBePickedUp = true;
    public NeededFood FoodType;

    public void PickupFood()
    {
        if (!CanBePickedUp) return;

        CanBePickedUp = false;
        GameManager.Instance.Inventory.AddFood(FoodType);
        foreach (SpriteRenderer spriteRenderer in gameObject.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.DOFade(0, 0.5f);
        }
        Destroy(gameObject, 1f);
    }
}
