using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject signupPanel;
    public GameObject loginPanel;

    [Header("Register UI")]
    public TMP_InputField regMail;
    public TMP_InputField regUsername;
    public TMP_InputField regPassword;
    public Button btnRegister;

    [Header("Login UI")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public Button btnLogin;

    private string apiUrl = "https://5wcorlr6ic.execute-api.eu-north-1.amazonaws.com/v2";

    public string UserID { get; private set; }
    public string Username { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        // Register şifre alanını password tipine çevir
        if (regPassword != null)
        {
            regPassword.contentType = TMP_InputField.ContentType.Password;
        }

        btnRegister.onClick.AddListener(() => StartCoroutine(RegisterUser()));
    }

    private IEnumerator RegisterUser()
    {
        string email = regMail.text.Trim();
        string username = regUsername.text.Trim();
        string password = regPassword.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Registration failed: Fields cannot be empty!");
            yield break;
        }

        string jsonData = $"{{\"email\":\"{email}\",\"username\":\"{username}\",\"password\":\"{password}\"}}";
        using (UnityWebRequest request = new UnityWebRequest(apiUrl + "/register", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError($"Register failed: {request.error}\n{request.downloadHandler.text}");
            else
            {
                Debug.Log($"Registration successful: {request.downloadHandler.text}");
                signupPanel.SetActive(false);
            }
        }
    }

    private IEnumerator LoginUser()
    {
        Debug.Log("LOGIN: Başladı");
        string userInput = loginUsername != null ? loginUsername.text.Trim() : null;
        string password = loginPassword != null ? loginPassword.text.Trim() : null;

        if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(password))
        {
            Debug.Log("LOGIN: Alanlar boş, giriş başarısız!");
            yield break;
        }

        string getUrl = $"{apiUrl}/login?user_input={UnityWebRequest.EscapeURL(userInput)}&password={UnityWebRequest.EscapeURL(password)}";

        using (UnityWebRequest request = UnityWebRequest.Get(getUrl))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"LOGIN ERROR: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                Debug.Log("LOGIN: AWS API başarılı, Unity Auth başlatılıyor...");
                try
                {
                    UserDataResponse response = JsonUtility.FromJson<UserDataResponse>(request.downloadHandler.text);
                    if (response.success == 1 && !string.IsNullOrEmpty(response.userId))
                    {
                        Instance.UserID = response.userId;
                        Instance.Username = userInput;

                        PlayerInfo.PlayerName = userInput;

                        Debug.Log("LOGIN: OnLoginSuccess çağrılıyor...");
                        OnLoginSuccess(response.userId);
                    }
                    else
                    {
                        Debug.LogError("LOGIN: Geçersiz kimlik bilgileri.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"LOGIN: JSON Parsing Error: {e.Message}");
                }
            }
        }
        Debug.Log("LOGIN: Bitti");
    }

    public async void OnLoginSuccess(string userId)
    {
        UserID = userId;
        Debug.Log($"Login successful. UserID: {UserID}");

        await InitializeUnityAuthAfterAwsLogin(); // Unity Auth login

        Debug.Log("LOGIN: Unity Auth IsSignedIn: " + AuthenticationService.Instance.IsSignedIn);

        UIManager.Instance.RefreshUI();
        SceneManager.LoadScene("HomeScreen");
    }

    private async Task InitializeUnityAuthAfterAwsLogin()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        // Zaten giriş yapılıyorsa tekrar deneme!
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Unity Auth: Zaten giriş yapılmış.");
            return;
        }

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        PlayerPrefs.SetString("unity_player_id", AuthenticationService.Instance.PlayerId);
        PlayerPrefs.Save();

        Debug.Log("✅ Unity Anonymous oturum açıldı: " + AuthenticationService.Instance.PlayerId);
    }

    public void Logout()
    {
        Debug.Log("LOGOUT: Başladı");
        // Kullanıcı bilgilerini sıfırla
        UserID = null;
        Username = null;

        // Gerekirse PlayerPrefs temizle
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("unity_player_id");
        PlayerPrefs.DeleteKey($"{Application.cloudProjectId}.default.unity.services.authentication.session_token");
        PlayerPrefs.Save();

        // Unity Authentication'dan da çıkış yap (ANA THREAD'DE!)
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("LOGOUT: Unity Auth SignOut çağrılıyor");
            AuthenticationService.Instance.SignOut(true);
        }
        AuthenticationService.Instance.ClearSessionToken();
        Debug.Log("LOGOUT: Unity Auth session token temizlendi");

        // OpeningScreen sahnesine dön
        Debug.Log("LOGOUT: OpeningScreen sahnesine geçiliyor");
        UnityEngine.SceneManagement.SceneManager.LoadScene("OpeningScreen");
        Debug.Log("LOGOUT: Bitti");
    }

    public void OnLoginButtonClicked()
    {
        StartCoroutine(LoginUser());
    }

    public void RebindLoginUI(TMP_InputField username, TMP_InputField password, Button loginBtn)
    {
        loginUsername = username;
        loginPassword = password;
        btnLogin = loginBtn;
        if (btnLogin != null)
        {
            btnLogin.onClick.RemoveAllListeners();
            btnLogin.onClick.AddListener(() => StartCoroutine(LoginUser()));
        }
    }

    public void RebindRegisterUI(TMP_InputField mail, TMP_InputField username, TMP_InputField password, Button registerBtn)
    {
        regMail = mail;
        regUsername = username;
        regPassword = password;
        btnRegister = registerBtn;

        if (regPassword != null)
        {
            regPassword.contentType = TMP_InputField.ContentType.Password;
        }

        if (btnRegister != null)
        {
            btnRegister.onClick.RemoveAllListeners();
            btnRegister.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        }
    }

    [System.Serializable]
    private class UserDataResponse
    {
        public int success;
        public string userId;
    }
}