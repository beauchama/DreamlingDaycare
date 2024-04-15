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

        if (PlayerManager.Instance.IsExploring)
        {
            PlayerManager.Instance.IsExploring = false;
            var portal = GameObject.Find("ExplorationGate");

            GameManager.Instance.Player.transform.position = portal.transform.position;

            if (Camera.main)
                Camera.main.transform.position = new Vector3(portal.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        }
        SpawnBarnDreamlings();
    }

    public void Explore()
    {
        // Count instances of "DreamlingCharacter" in the scene
        var dreamlingCharacters = FindObjectsByType<DreamlingCharacter>(FindObjectsSortMode.None).Length;

        // Count the total number of Dreamlings in the barns
        var barnDreamlings = GameManager.Instance.Barns.Sum(barn => barn.Dreamlings.Count);

        if (dreamlingCharacters != barnDreamlings)
        {
            GameManager.Instance.errorMessageDisplay.DisplayError("You need to have every Dreamling in the barns to explore!");
            return;
        }

        PlayerManager.Instance.IsExploring = true;

        int index = GameManager.Instance.LastSceneIndex;
        while (index == GameManager.Instance.LastSceneIndex)
        {
            index = UnityEngine.Random.Range(0, Scenes.Length);
        }

        GameManager.Instance.LastSceneIndex = index;
        SceneTransitionManager.Instance.ChangeScene(Scenes[index]);
    }

    public void ExploreFailed()
    {
        GameManager.Instance.errorMessageDisplay.DisplayError("You can't explore while carrying a Dreamling!");
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
                var positionOffset = UnityEngine.Random.Range(-6f, 6f);
                var offsetPosition = new Vector3(barnPosition.Value.x + positionOffset, barnPosition.Value.y, barnPosition.Value.z);
                SpawnDreamling(dreamling, false, offsetPosition);
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
            dreamlingCharObj.GetComponent<Rigidbody2D>().simulated = !overrideCarry;
            dreamlingCharObj.SetActive(true);

            if (position.HasValue)
            {
                dreamlingCharObj.transform.position = position.Value;
            }
        }
    }
}
