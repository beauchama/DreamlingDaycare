using DG.Tweening;
using Dreamlings.Interfaces;
using UnityEngine;

public class FindableFoodBehavior : MonoBehaviour
{
    public NeededFood FoodType;

    public void PickupFood()
    {
        GameManager.Instance.Inventory.AddFood(FoodType);
        gameObject.GetComponent<SpriteRenderer>().DOFade(0, 0.5f);
        Destroy(gameObject, 0.5f);
    }
}
