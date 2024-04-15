using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    public GameObject Wizard;
    public GameObject PortalPrefab;
    public GameObject Boudichon;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WizardStepRight());
    }

    private IEnumerator WizardStepRight()
    {
        var wizardBubble = Wizard.GetComponentInChildren<CanvasGroup>();
        var boudichonBubble = Boudichon.GetComponentInChildren<CanvasGroup>();

        Boudichon.SetActive(false);

        wizardBubble.alpha = 0;
        Camera.main.transform.position = new Vector3(-20, 0, -10);
        Camera.main.transform.DOMove(new Vector3(-2, 0, -10), 7f).SetEase(Ease.OutSine);
        for (int i = 0; i < 9; i++)
        {
            yield return Wizard.transform.DOBlendableMoveBy(new Vector2(1, 1), 0.25f).SetEase(Ease.Linear).WaitForCompletion();
            yield return Wizard.transform.DOBlendableMoveBy(new Vector2(1, -1), 0.25f).SetEase(Ease.Linear).WaitForCompletion();
        }
        yield return new WaitForSeconds(2);
        yield return DisplayBubble(wizardBubble, "I'm going to need some help...", 2);
        Wizard.transform.DOShakePosition(2, 0.25f);
        yield return SpawnBoudichon();
        yield return DisplayBubble(boudichonBubble, "Sup!", 1);
        yield return DisplayBubble(boudichonBubble, "You summoned me?", 2);
        yield return DisplayBubble(wizardBubble, "Yeah...", 1);
        yield return DisplayBubble(wizardBubble, "I need you to kill this boss for me", 3);
        yield return DisplayBubble(boudichonBubble, "...", 1);
        yield return DisplayBubble(boudichonBubble, "Alright", 1);
        Boudichon.GetComponent<Animator>().SetFloat("Move", 1);
        yield return Boudichon.transform.DOBlendableMoveBy(new Vector3(15, 0, 0), 3).SetEase(Ease.Linear).WaitForCompletion();
        yield return Camera.main.transform.DOShakePosition(1, new Vector3(2, 0, 0)).WaitForCompletion();
        Boudichon.GetComponent<SpriteRenderer>().flipX = true;
        yield return Boudichon.transform.DOBlendableMoveBy(new Vector3(-15, 0, 0), 3).SetEase(Ease.Linear).WaitForCompletion();
        Boudichon.GetComponent<Animator>().SetFloat("Move", 0);
        yield return DisplayBubble(wizardBubble, "Thanks!", 1);
        yield return DisplayBubble(boudichonBubble, "Now what?", 1.5f);
        yield return DisplayBubble(wizardBubble, "No idea...", 1);
        yield return DisplayBubble(wizardBubble, "These portals are one-way only...", 3);
        yield return DisplayBubble(wizardBubble, "Anyway... Good luck!", 2);
        Wizard.transform.DOScaleX(-1, 0.25f);
        for (int i = 0; i < 4; i++)
        {
            yield return Wizard.transform.DOBlendableMoveBy(new Vector2(-1, 1), 0.25f).SetEase(Ease.Linear).WaitForCompletion();
            yield return Wizard.transform.DOBlendableMoveBy(new Vector2(-1, -1), 0.25f).SetEase(Ease.Linear).WaitForCompletion();
        }
        yield return DisplayBubble(boudichonBubble, "Someone should really build a place for abandonned summons...", 3f);
        EndIntro();
    }

    public void EndIntro()
    {
        GetComponent<AudioSource>().DOFade(0, 0.4f);
        SceneTransitionManager.Instance.ChangeScene("MainMenu");
    }

    private IEnumerator DisplayBubble(CanvasGroup bubble, string txt, float duration)
    {
        bubble.GetComponentInChildren<TextMeshProUGUI>().text = txt;
        yield return bubble.DOFade(1, 0.2f).WaitForCompletion();
        yield return new WaitForSeconds(duration);
        yield return bubble.DOFade(0, 0.2f).WaitForCompletion();
    }

    private IEnumerator SpawnBoudichon()
    {
        var portal = Instantiate(PortalPrefab, new Vector3(-4, -1.5f, 0), Quaternion.identity);
        portal.GetComponent<Animator>().SetFloat("Speed", 1);
        yield return new WaitForSeconds(1f);
        Boudichon.transform.localScale = Vector3.zero;
        Boudichon.transform.position = portal.transform.position;
        Boudichon.SetActive(true);
        yield return Boudichon.transform.DOScale(Vector3.one, 2).WaitForCompletion();
        yield return Boudichon.transform.DOMoveY(-3.2f, 2).WaitForCompletion();
        portal.GetComponent<Animator>().SetFloat("Speed", -1);
        yield return new WaitForSeconds(1);
        Destroy(portal);
    }
}
