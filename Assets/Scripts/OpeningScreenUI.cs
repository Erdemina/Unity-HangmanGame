using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OpeningScreenUI : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject signupPanel;
    public Button loginButton;
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register UI")]
    public TMP_InputField regMailInput;
    public TMP_InputField regUsernameInput;
    public TMP_InputField regPasswordInput;
    public Button registerButton;

    private void Start()
    {
        // Şifre alanlarını password tipine çevir
        if (loginPasswordInput != null)
        {
            loginPasswordInput.contentType = TMP_InputField.ContentType.Password;
        }

        // AuthManager'a UI referanslarını bağla
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.signupPanel = signupPanel;
            AuthManager.Instance.loginPanel = loginPanel;
            
            if (loginUsernameInput != null && loginPasswordInput != null && loginButton != null)
            {
                AuthManager.Instance.RebindLoginUI(loginUsernameInput, loginPasswordInput, loginButton);
            }

            if (regMailInput != null && regUsernameInput != null && regPasswordInput != null && registerButton != null)
            {
                AuthManager.Instance.RebindRegisterUI(regMailInput, regUsernameInput, regPasswordInput, registerButton);
            }
        }
    }

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false); // Diğeri açıksa kapat
    }

    public void OpenSignupPanel()
    {
        signupPanel.SetActive(true);
        loginPanel.SetActive(false); // Diğeri açıksa kapat
    }

    public void CloseAllPanels()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
    }
}
