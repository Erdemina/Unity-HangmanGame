using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Slider volumeSlider;

    private void Start()
    {
        // MusicManager varsa bağlan
        if (MusicManager.instance != null)
        {
            // Slider'ı güncel sesle başlat
            volumeSlider.value = MusicManager.instance.GetVolume();

            // Her değişimde müziği kontrol et
            volumeSlider.onValueChanged.AddListener(MusicManager.instance.SetVolume);
        }
        else
        {
            Debug.LogWarning("⚠️ MusicManager sahnede yok!");
        }
    }

    public void OnLogoutButtonPressed()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.Logout();
        UnityEngine.SceneManagement.SceneManager.LoadScene("OpeningScreen");
    }
}
