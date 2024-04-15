using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject[] Prefabs;
    private Coroutine MenuAnimationRoutine;

    private void Awake()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ResetGame();
        }
    }

    private void Start()
    {
        MenuAnimationRoutine = StartCoroutine(Animate());
    }

    public void StartGame()
    {
        StopCoroutine(MenuAnimationRoutine);
        SceneTransitionManager.Instance.ChangeScene("Daycare");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public IEnumerator Animate()
    {
        var lastIndex = -1;
        while (true)
        {
            int index = lastIndex;

            while (index == lastIndex)
            {
                index = Random.Range(0, Prefabs.Length);
            }

            lastIndex = index;

            bool isGoingRight = Random.Range(0, 2) > 0;

            Vector3 rightPos = new Vector3(13f, -3.3f, 0);
            Vector3 leftPos = new Vector3(-13f, -3.3f, 0);
            var initialPosition = isGoingRight ? leftPos : rightPos;

            var dreamling = Instantiate(Prefabs[index], initialPosition, Quaternion.identity);

            if (index == 3)
            {
                dreamling.transform.DOBlendableRotateBy(new Vector3(0, 0, isGoingRight ? -360 : 360), 1, RotateMode.FastBeyond360).SetEase(Ease.InOutSine).SetLoops(-1);
            }

            if ((!isGoingRight && index > 0) || (isGoingRight && index == 0))
            {
                dreamling.transform.localScale = new Vector3(-1, 1, 1);
            }

            dreamling.transform.DOMove(isGoingRight ? rightPos : leftPos, 6f).OnComplete(() => dreamling.transform.DOKill());
            Destroy(dreamling, 7);
            yield return new WaitForSeconds(Random.Range(3, 6));
        }
    }
}
