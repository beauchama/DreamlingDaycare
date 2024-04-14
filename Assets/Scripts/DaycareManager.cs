using System;
using System.Linq;
using Dreamlings.Characters;
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
            SpawnDreamling(carriedDreamling, true);
            PlayerManager.Instance.CarriedDreamling = null;
        }

        SpawnBarnDreamlings();
    }

    public void Explore()
    {
        var randomSceneIndex = UnityEngine.Random.Range(0, Scenes.Length);

        SceneTransitionManager.Instance.ChangeScene(Scenes[randomSceneIndex]);
    }

    private void SpawnBarnDreamlings()
    {
        for (var barnIndex = 0; barnIndex < GameManager.Instance.Barns.Count; barnIndex++)
        {
            var barn = GameManager.Instance.Barns[barnIndex];
            var barnPosition = GameObject
                .FindGameObjectsWithTag("Barn")
                .FirstOrDefault(x => x.gameObject.GetComponent<BarnBehavior>().BarnIndex == barnIndex)?
                .transform.position;

            foreach (var dreamling in barn.Dreamlings)
            {
                SpawnDreamling(dreamling, false, barnPosition);
            }
        }
    }

    private void SpawnDreamling(Dreamling dreamling, bool overrideCarry, Vector3? position = null)
    {
        var dreamlingCharObj = Instantiate(dreamlingCharacter);
        var dreamlingChar = dreamlingCharObj.GetComponent<DreamlingCharacter>();

        if (dreamlingChar != null)
        {
            dreamlingChar.OverriddenDreamling = dreamling;
            dreamlingChar.OverrideCarry = overrideCarry;
            dreamlingCharObj.GetComponent<SpriteRenderer>().sortingOrder = 11;
            dreamlingCharObj.GetComponent<Rigidbody2D>().simulated = false;
            dreamlingCharObj.SetActive(true);

            if (position.HasValue)
            {
                dreamlingCharObj.transform.position = position.Value;
            }
        }
    }
}
