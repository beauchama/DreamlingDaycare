using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class InteractableBehaviour : MonoBehaviour
{
    public float InteractableDistance = 1f;
    public bool alwaysInteractable;
    public GameObject HoverText;
    public UnityEvent OnInteract;
    public UnityEvent OnInteractFailed;

    private static readonly List<InteractableBehaviour> Interactables = new();

    private void Awake()
    {
        Interactables.Add(this);
    }

    private void Start()
    {
        ShowInteractText(false);
    }

    private void OnDestroy()
    {
        Interactables.Remove(this);
    }

    private void Update()
    {
        var playerPosition = GameManager.Instance.Player.transform.position;
        var closestInteractable = Interactables
            .Where(i => !i.alwaysInteractable)
            .OrderBy(i => Vector2.Distance(playerPosition, i.transform.position))
            .FirstOrDefault();

        if (Vector2.Distance(playerPosition, transform.position) < InteractableDistance)
        {
            ShowInteractText(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (alwaysInteractable || closestInteractable == this)
                {
                    OnInteract.Invoke();
                }
                else
                {
                    OnInteractFailed.Invoke();
                }
            }
        }
        else
        {
            ShowInteractText(false);
        }
    }

    private void ShowInteractText(bool show)
    {
        if (HoverText)
        {
            HoverText.SetActive(show);
        }
    }
}
