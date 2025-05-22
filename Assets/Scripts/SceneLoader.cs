using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadHomeScreen()
    {
        SceneManager.LoadScene("HomeScreen"); // Sahne adını birebir yazmalısın
    }
}
