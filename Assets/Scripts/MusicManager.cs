using DG.Tweening;
using UnityEngine;

public class MusicManager : MonoBehaviour
{

    private AudioSource musicSource;

    public static MusicManager Instance;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        musicSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    public void EnterBarn()
    {
        musicSource.DOFade(0.03f, 0.5f);
    }

    public void ExitBarn()
    {
        musicSource.DOFade(0.1f, 0.5f);
    }
}
