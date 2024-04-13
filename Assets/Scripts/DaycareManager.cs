using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DaycareManager : MonoBehaviour
{
    public GameObject dreamlingCharacter;

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
                dreamlingCharObj.SetActive(true);
            }
        }
    }

    public void GotoDom()
    {
        SceneTransitionManager.Instance.ChangeScene("Dom");
    }
}
