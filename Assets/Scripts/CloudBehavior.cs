using DG.Tweening;
using UnityEngine;

public class CloudBehavior : MonoBehaviour
{
    private Vector2 InitialPosition;

    private void Start()
    {
        InitialPosition = transform.position;
        RandomizeScale();
        RandomizePosition();
    }

    void RandomizeScale()
    {
        float randomScale = Random.Range(0.75f, 1.25f);
        transform.DOScale(new Vector3(randomScale, randomScale, 1), Random.Range(7, 10)).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            RandomizeScale();
        });
    }

    void RandomizePosition()
    {
        Vector2 randomDirection = Random.insideUnitCircle * 2;
        Vector2 randomPosition = InitialPosition + randomDirection;
        transform.DOMove(randomPosition, Random.Range(7, 10)).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            RandomizePosition();
        });
    }
}
