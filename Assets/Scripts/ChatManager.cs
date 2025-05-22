using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;

    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] GameObject LetterButtons;
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] private TMP_Text typingIndicatorText;

    public string playerName;
    public Dictionary<ulong, string> clientIdToPlayerName = new();

    private NetworkVariable<ulong> typingPlayerId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float typingCooldown = 5f;
    private Coroutine typingResetCoroutine;

    public Color systemMessageColor = Color.blue;
    private string hexSystemMessageColor;

    void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            clientIdToPlayerName = new Dictionary<ulong, string>();
            Debug.Log("[ChatManager] Initialized as singleton");
        }
        else
        {
            Debug.LogWarning("[ChatManager] Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        hexSystemMessageColor = ColorUtility.ToHtmlStringRGB(systemMessageColor);
        if (clientIdToPlayerName == null)
        {
            clientIdToPlayerName = new Dictionary<ulong, string>();
        }

        if (UIManager.Instance?.CurrentUserData != null)
        {
            playerName = UIManager.Instance.CurrentUserData.username;
            Debug.Log($"[ChatManager] Set initial player name from UIManager: {playerName}");
        }
        else if (AuthManager.Instance != null)
        {
            playerName = AuthManager.Instance.Username;
            Debug.Log($"[ChatManager] Set initial player name from AuthManager: {playerName}");
        }
        else
        {
            Debug.LogWarning("[ChatManager] Could not set initial player name - UIManager and AuthManager are null");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("[ChatManager] OnNetworkSpawn called");
        RegisterLocalPlayerName();
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
        typingPlayerId.OnValueChanged += UpdateTypingIndicator;

        // Use MultiLineSubmit so Enter/Done always fires onSubmit:
        chatInput.lineType = TMP_InputField.LineType.MultiLineSubmit;
        chatInput.onSubmit.AddListener(OnChatInputSubmit);
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
        typingPlayerId.OnValueChanged -= UpdateTypingIndicator;

        chatInput.onSubmit.RemoveListener(OnChatInputSubmit);
    }

    void Update()
    {
        bool focused = chatInput.isFocused;
        chatContent.alpha = focused ? 1f : 0.0f;
        LetterButtons.SetActive(!focused);

        if (focused)
        {
            SetTypingStatusServerRpc(true);
            if (typingResetCoroutine != null) StopCoroutine(typingResetCoroutine);
            typingResetCoroutine = StartCoroutine(ResetTypingStatusAfterDelay());
        }
    }

    IEnumerator ResetTypingStatusAfterDelay()
    {
        yield return new WaitForSeconds(typingCooldown);
        SetTypingStatusServerRpc(false);
    }

    // Called on both desktop Enter and mobile Done
    private void OnChatInputSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Send
        SendChatMessage(text, playerName);

        // Clear immediately to prevent double-send
        chatInput.text = string.Empty;
        SetTypingStatusServerRpc(false);

        // Keep focus so the keyboard stays up if desired
        chatInput.ActivateInputField();
    }

    // For mobile "Done" key—fires on focus loss
    private void OnChatInputEndEdit(string text)
    {
#if UNITY_IOS || UNITY_ANDROID
        // Only send when keyboard has closed (user pressed Done)
        if (TouchScreenKeyboard.visible == false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // Send
            SendChatMessage(text, playerName);

            // Clear immediately to prevent double-send
            chatInput.text = string.Empty;
            SetTypingStatusServerRpc(false);

            // Keep focus so the keyboard stays up if desired
            chatInput.ActivateInputField();
        }
#endif
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTypingStatusServerRpc(bool isTyping, ServerRpcParams rpcParams = default)
    {
        typingPlayerId.Value = isTyping
            ? rpcParams.Receive.SenderClientId
            : ulong.MaxValue;
    }

    private void UpdateTypingIndicator(ulong oldValue, ulong newValue)
    {
        if (typingIndicatorText == null) return;
        typingIndicatorText.text =
            (newValue != ulong.MaxValue && newValue != NetworkManager.Singleton.LocalClientId)
            ? "Opponent is typing..."
            : "";
    }

    public void SendChatMessage(string message, string from = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        string timestamp = $"<color=#888888>[{System.DateTime.Now:HH:mm}]</color>";
        string formatted = (from == null)
            ? $"{timestamp} {message}"
            : $"{timestamp} {from} > {message}";
        SendChatMessageServerRpc(formatted);
    }

    public void SendSystemNotification(string msg)
    {
        string timestamp = $"<color=#888888>[{System.DateTime.Now:HH:mm}]</color>";
        string sys = $"[System] {msg}";
        sys = $"<color=#{hexSystemMessageColor}>{sys}</color>";
        SendChatMessageServerRpc($"{timestamp} {sys}");
    }

    void AddMessage(string msg)
    {
        var cm = Instantiate(chatMessagePrefab, chatContent.transform);
        cm.SetText(msg);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        ReceiveChatMessageClientRpc(message);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRpc(string message)
    {
        AddMessage(message);
    }

    void HandleClientConnected(ulong clientId)
    {
        if (IsServer)
            RequestPlayerNameClientRpc(clientId);
    }

    void HandleClientDisconnected(ulong clientId)
    {
        if (IsServer && clientIdToPlayerName.TryGetValue(clientId, out var name))
        {
            SendSystemNotification($"{name} has left the game.");
            clientIdToPlayerName.Remove(clientId);
        }
    }

    [ClientRpc]
    void RequestPlayerNameClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
            SubmitPlayerNameServerRpc(playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        try
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (clientIdToPlayerName == null)
            {
                clientIdToPlayerName = new Dictionary<ulong, string>();
            }
            clientIdToPlayerName[clientId] = name;
            SendSystemNotification($"{name} has joined the game.");
            Debug.Log($"[ChatManager] Successfully registered player name: {name} for client ID: {clientId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChatManager] Error registering player name: {e.Message}");
        }
    }

    public string GetPlayerName(ulong clientId)
    {
        if (clientIdToPlayerName.TryGetValue(clientId, out var name) && !string.IsNullOrEmpty(name))
            return name;
        // Fallback: "Bilinmiyor"
        return "Bilinmiyor";
    }

    public void RegisterLocalPlayerName()
    {
        Debug.Log("[ChatManager] RegisterLocalPlayerName called");
        
        // Check if network is ready
        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[ChatManager] Cannot register player name - NetworkManager not ready");
            return;
        }

        string playerNameToRegister = null;
        // UIManager veya AuthManager'dan kullanıcı adı alınabilir
        if (UIManager.Instance != null && UIManager.Instance.CurrentUserData != null)
        {
            playerNameToRegister = UIManager.Instance.CurrentUserData.username;
            Debug.Log($"[ChatManager] Registering player name from UIManager: {playerNameToRegister}");
        }
        else if (AuthManager.Instance != null)
        {
            playerNameToRegister = AuthManager.Instance.Username;
            Debug.Log($"[ChatManager] Registering player name from AuthManager: {playerNameToRegister}");
        }

        if (!string.IsNullOrEmpty(playerNameToRegister))
        {
            playerName = playerNameToRegister; // Set local player name
            if (IsServer)
            {
                clientIdToPlayerName[NetworkManager.Singleton.LocalClientId] = playerNameToRegister;
                Debug.Log($"[ChatManager] Server registered local player name: {playerNameToRegister}");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                SubmitPlayerNameServerRpc(playerNameToRegister);
                Debug.Log($"[ChatManager] Client submitted player name to server: {playerNameToRegister}");
            }
        }
        else
        {
            Debug.LogWarning("[ChatManager] Failed to register player name - no valid name found");
        }
    }
}
