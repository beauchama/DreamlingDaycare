using System;
using UnityEngine;

public class DaycareManager : MonoBehaviour
{
    public GameObject dreamlingCharacter;
    public string[] Scenes = Array.Empty<string>();

    private void Start()
    {
        var carriedDreamling = PlayerManager.Instance.CarriedDreamling;
        if (carriedDreamling != null)
        {
            var dreamlingCharObj = Instantiate(dreamlingCharacter);
            var dreamlingChar = dreamlingCharObj.GetComponent<DreamlingCharacter>();

            if (dreamlingChar != null)
            {
                dreamlingChar.OverriddenDreamling = carriedDreamling;
                dreamlingChar.OverrideCarry = true;
                dreamlingCharObj.GetComponent<SpriteRenderer>().sortingOrder = 11;
                dreamlingCharObj.SetActive(true);
            }

            PlayerManager.Instance.CarriedDreamling = null;
        }
    }

    public void Explore()
    {
        var randomSceneIndex = UnityEngine.Random.Range(0, Scenes.Length);

        SceneTransitionManager.Instance.ChangeScene(Scenes[randomSceneIndex]);
    }
}
