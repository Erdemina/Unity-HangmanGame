using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    private float volume = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            volume = PlayerPrefs.GetFloat("musicVolume", 1f);
            AudioListener.volume = volume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    public float GetVolume()
    {
        return volume;
    }
}
