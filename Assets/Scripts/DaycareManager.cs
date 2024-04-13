using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaycareManager : MonoBehaviour
{
    public void GotoDom()
    {
        SceneTransitionManager.Instance.ChangeScene("Dom");
    }
}
