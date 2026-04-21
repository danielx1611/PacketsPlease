using UnityEngine;

[DisallowMultipleComponent]
public class PersistentMusicPlayer : MonoBehaviour
{
    private static PersistentMusicPlayer instance;

    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] private bool playOnAwake = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;

        if (musicClip != null && audioSource.clip != musicClip)
        {
            audioSource.clip = musicClip;
        }

        if (playOnAwake && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
