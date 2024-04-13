using DG.Tweening;
using UnityEngine;

public class BarnBehavior : MonoBehaviour
{
    public SpriteRenderer Facade;
    public float Range = 10f;
    private bool IsInside = false;

    // Update is called once per frame
    void Update()
    {
        var distance = Mathf.Abs(transform.position.x - GameManager.Instance.Player.transform.position.x);
        bool isInRange = distance < Range;
        Debug.Log("Barn: " + transform.position + " | Player: " + GameManager.Instance.Player.transform.position);
        if (!IsInside && isInRange)
        {
            ToggleFacade(false);
        }
        else if (IsInside && !isInRange)
        {
            ToggleFacade(true);
        }
    }

    private void ToggleFacade(bool isVisible)
    {
        IsInside = !isVisible;
        Facade.DOFade(isVisible ? 1 : 0, 0.3f);
    }
}
