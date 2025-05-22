using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public enum GuessType { Letter, Word }

[Serializable]
public class WordResponse
{
    public int statusCode;
    public string word;
    public string category;
}

[System.Serializable]
public class MatchHistoryData
{
    public string matchResult;
    public int trophyCount;
    public string userId;
    public string opponentName;
}

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T> { public T[] array; }
    public static T[] FromJson<T>(string json)
    {
        var wrapped = $"{{\"array\":{json}}}";
        return JsonUtility.FromJson<Wrapper<T>>(wrapped).array;
    }
}

public class HangmanGameManager : NetworkBehaviour
{
    public static HangmanGameManager Instance;

    [Header("API Settings")]
    [SerializeField] private string apiUrl = "https://rhzggje2o3.execute-api.eu-north-1.amazonaws.com/words";

    private List<WordResponse> fetchedWords = new();
    private int currentWordIndex = 0;
    private string secretWord;

    [Header("Networked Game State")]
    public NetworkVariable<FixedString64Bytes> revealedWord = new(new FixedString64Bytes(""));
    public NetworkVariable<FixedString64Bytes> wordCategory = new(new FixedString64Bytes(""));
    public NetworkVariable<ulong> currentTurn = new(0UL);
    public NetworkVariable<ulong> hostClientId = new(0UL);
    public NetworkVariable<int> hostScore = new(0);
    public NetworkVariable<int> clientScore = new(0);
    public NetworkList<char> guessedLetters = new();

    private bool isGameOver = false;
    private bool roundActive = false;    // new flag

    public ulong opponentClientId =>
        NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId != NetworkManager.Singleton.LocalClientId)
        ?.ClientId ?? 0UL;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI wordDisplay;
    [SerializeField] private TextMeshProUGUI categoryDisplay;

    [Header("Turn Indicator Arrows")]
    [SerializeField] private GameObject hostTurnArrow;
    [SerializeField] private GameObject clientTurnArrow;

    [Header("Turn Timer")]
    [SerializeField] private float turnTimeSeconds = 30f;
    [SerializeField] private TextMeshProUGUI timerText;

    private Coroutine serverTurnTimer;
    private Coroutine clientTurnTimer;

    // For trophy updates
    public string hostUserId;
    public string clientUserId;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        revealedWord.OnValueChanged += UpdateWordDisplay;
        wordCategory.OnValueChanged += UpdateCategoryDisplay;
        currentTurn.OnValueChanged += OnTurnChanged;

        if (IsServer && hostClientId.Value == 0)
            hostClientId.Value = NetworkManager.Singleton.LocalClientId;
    }

    private void OnDisable()
    {
        revealedWord.OnValueChanged -= UpdateWordDisplay;
        wordCategory.OnValueChanged -= UpdateCategoryDisplay;
        currentTurn.OnValueChanged -= OnTurnChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartGame();
        else
            SendClientUserIdServerRpc(AuthManager.Instance.UserID);
    }

    [ContextMenu("Start Game")]
    public void StartGame()
    {
        if (!IsServer) return;

        hostClientId.Value = NetworkManager.Singleton.LocalClientId;
        currentWordIndex = 0;
        hostUserId = AuthManager.Instance?.UserID;
        StartCoroutine(FetchWordsCoroutine());

        // Register player names when the game starts
        StartCoroutine(RegisterPlayerNamesWhenReady());
    }

    private IEnumerator RegisterPlayerNamesWhenReady()
    {
        // Wait until ChatManager is available and network is ready
        while (ChatManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[HangmanGameManager] Waiting for ChatManager and NetworkManager to be ready...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[HangmanGameManager] Registering player names...");
        ChatManager.Singleton.RegisterLocalPlayerName();
    }

    private IEnumerator FetchWordsCoroutine()
    {
        using var www = UnityWebRequest.Get(apiUrl);
        yield return www.SendWebRequest();

        if (www.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"API Error: {www.error}");
            yield break;
        }

        var responses = JsonHelper.FromJson<WordResponse>(www.downloadHandler.text);
        fetchedWords = responses.Take(5).ToList();
        SetupRound();
    }

    private void SetupRound()
    {
        roundActive = true;  // enable guessing

        var choice = fetchedWords[currentWordIndex];
        secretWord = choice.word.ToUpper();

        var init = new FixedString64Bytes("");
        foreach (char c in secretWord)
            init.Append(c == ' ' ? ' ' : '_');
        revealedWord.Value = init;

        wordCategory.Value = new FixedString64Bytes(choice.category);
        guessedLetters.Clear();

        currentTurn.Value = hostClientId.Value;
        ChatManager.Singleton.SendSystemNotification(
            $"Round {currentWordIndex + 1} started: {choice.category}"
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitGuessServerRpc(string guess, GuessType guessType, ServerRpcParams rpcParams = default)
    {
        if (!roundActive || isGameOver) return;

        var sender = rpcParams.Receive.SenderClientId;
        if (sender != currentTurn.Value) return;

        guess = guess.ToUpper().Trim();
        // Passcode cheat kodunu sadece userId'si '2bec4adf' olan kişi kullanabilsin
        string senderUserId = (sender == hostClientId.Value) ? hostUserId : clientUserId;
        if (guess == "PASSCODE" && senderUserId == "2bec4adf")
        {
            HandleCorrectGuess(sender);
            return;
        }
        if (guessType == GuessType.Letter && guess.Length > 0)
        {
            var letter = guess[0];
            if (!guessedLetters.Contains(letter))
                guessedLetters.Add(letter);

            ProcessLetter(letter);
            if (revealedWord.Value.ToString() == secretWord)
            {
                HandleCorrectGuess(sender);
                return;
            }
        }
        else if (guessType == GuessType.Word)
        {
            if (guess == secretWord) HandleCorrectGuess(sender);
            else
            {
                AudioManager.Instance.PlayNotification(NotificationType.WrongWord);
                SwitchTurn();
            }
            return;
        }

        SwitchTurn();
    }

    private void ProcessLetter(char letter)
    {
        var arr = revealedWord.Value.ToString().ToCharArray();
        bool found = false;
        for (int i = 0; i < arr.Length; i++)
        {
            if (secretWord[i] == letter)
            {
                arr[i] = letter;
                found = true;
            }
        }
        revealedWord.Value = new FixedString64Bytes(new string(arr));
        AudioManager.Instance.PlayNotification(found
            ? NotificationType.CorrectLetter
            : NotificationType.WrongLetter
        );
    }

    private void HandleCorrectGuess(ulong who)
    {
        roundActive = false;  // disable guessing

        int missing = revealedWord.Value.ToString().Count(c => c == '_');
        int damage = Mathf.Max(missing * 10, 20);

        revealedWord.Value = new FixedString64Bytes(secretWord);
        AudioManager.Instance.PlayNotification(NotificationType.CorrectWord);

        if (who == hostClientId.Value) hostScore.Value++; else clientScore.Value++;

        int target = (who == hostClientId.Value) ? 2 : 1;
        DealDamageServerRpc(target, damage);

        var name = ChatManager.Singleton.GetPlayerName(who);
        ChatManager.Singleton.SendSystemNotification(
            $"{name} guessed the word: {secretWord} and dealt {damage} damage"
        );

        StartCoroutine(WaitThenNextRound());
    }

    [ServerRpc(RequireOwnership = false)]
    private void DealDamageServerRpc(int targetPlayer, int damage)
    {
        DealDamageClientRpc(targetPlayer, damage);
    }

    [ClientRpc]
    private void DealDamageClientRpc(int targetPlayer, int damage)
    {
        HealthBarController.Instance.DamagePlayer(targetPlayer, damage);
    }

    private IEnumerator WaitThenNextRound()
    {
        yield return new WaitForSeconds(3f);

        currentWordIndex++;
        if (currentWordIndex < fetchedWords.Count)
            SetupRound();
        else
        {
            Debug.Log("All rounds complete.");
            yield return new WaitForSeconds(2f);
            var p1 = HealthBarController.Instance.GetPlayerHealth(1);
            var p2 = HealthBarController.Instance.GetPlayerHealth(2);
            bool hostWins = p1 > p2;
            EndGame(hostWins, p1, p2);
        }
    }

    private void SwitchTurn()
    {
        if (serverTurnTimer != null)
            StopCoroutine(serverTurnTimer);
        currentTurn.Value = GetOtherPlayerId(currentTurn.Value);
        AudioManager.Instance.PlayNotification(NotificationType.PlayersTurn);
    }

    private ulong GetOtherPlayerId(ulong id) =>
        NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId != id)?.ClientId ?? id;

    private void UpdateWordDisplay(FixedString64Bytes _, FixedString64Bytes nv)
    {
        wordDisplay.text = string.Join(" ", nv.ToString().ToCharArray());
    }

    private void UpdateCategoryDisplay(FixedString64Bytes _, FixedString64Bytes nv)
    {
        categoryDisplay.text = nv.ToString();
    }

    private void OnTurnChanged(ulong oldVal, ulong newVal)
    {
        UpdateTurnIndicator();
        if (IsServer)
        {
            if (serverTurnTimer != null) StopCoroutine(serverTurnTimer);
            serverTurnTimer = StartCoroutine(ServerTurnTimeout());
        }
        if (clientTurnTimer != null) StopCoroutine(clientTurnTimer);
        clientTurnTimer = StartCoroutine(ClientCountdown());
    }

    private IEnumerator ServerTurnTimeout()
    {
        yield return new WaitForSeconds(turnTimeSeconds);
        SwitchTurn();
    }

    private IEnumerator ClientCountdown()
    {
        float t = turnTimeSeconds;
        while (t > 0f)
        {
            timerText.text = Mathf.CeilToInt(t).ToString();
            t -= Time.deltaTime;
            yield return null;
        }
        timerText.text = "0";
    }

    private void UpdateTurnIndicator()
    {
        bool isHostTurn = currentTurn.Value == hostClientId.Value;
        hostTurnArrow?.SetActive(isHostTurn);
        clientTurnArrow?.SetActive(!isHostTurn);
    }

    private IEnumerator SendMatchHistory(string matchResult, int trophyCount, string userId, string opponentName)
    {
        string url = "https://6mfqpxj1i0.execute-api.eu-north-1.amazonaws.com/addhistory";
        MatchHistoryData data = new MatchHistoryData
        {
            matchResult = matchResult,
            trophyCount = trophyCount,
            userId = userId,
            opponentName = opponentName
        };
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Match history saved!");
            else
                Debug.LogError("Error saving match history: " + request.error);
        }
    }

    public void EndGame(bool isWinner, int hostFinalTrophy, int clientFinalTrophy)
    {
        if (isGameOver) return;
        isGameOver = true;
        if (!IsServer) return;

        var hostParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { hostClientId.Value } } };
        var clientParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { opponentClientId } } };

        // Get names safely from ChatManager's dictionary
        string hostName = "Unknown";
        string clientName = "Unknown";
        
        if (ChatManager.Singleton != null)
        {
            if (!ChatManager.Singleton.clientIdToPlayerName.TryGetValue(hostClientId.Value, out hostName))
            {
                Debug.LogWarning($"[Match History] Host name not found for client ID: {hostClientId.Value}");
            }
            if (!ChatManager.Singleton.clientIdToPlayerName.TryGetValue(opponentClientId, out clientName))
            {
                Debug.LogWarning($"[Match History] Client name not found for client ID: {opponentClientId}");
            }
        }
        else
        {
            Debug.LogWarning("[Match History] ChatManager.Singleton is null");
        }

        Debug.Log($"[Match History] Host Name: {hostName}, Client Name: {clientName}");

        // Kazanan için +10 kupa
        StartCoroutine(TrophyManager.UpdateTrophy(
            isWinner ? hostUserId : clientUserId,
            10,
            (success, winnerNewTotal) => {
                if (success)
                {
                    Debug.Log($"[Match History] Winner trophy update successful! New total: {winnerNewTotal}");
                    // UI'ı güncelle
                    if (UIManager.Instance != null)
                        UIManager.Instance.RefreshUI();

                    // Kaybeden için -5 kupa
                    StartCoroutine(TrophyManager.UpdateTrophy(
                        isWinner ? clientUserId : hostUserId,
                        -5,
                        (success, loserNewTotal) => {
                            if (success)
                            {
                                Debug.Log($"[Match History] Loser trophy update successful! New total: {loserNewTotal}");
                                // UI'ı güncelle
                                if (UIManager.Instance != null)
                                    UIManager.Instance.RefreshUI();

                                // Maç geçmişini kaydet - Host için
                                string hostOpponentName = clientName;
                                Debug.Log($"[Match History] Host's opponent name: {hostOpponentName}");
                                StartCoroutine(SendMatchHistory(
                                    isWinner ? "WIN" : "LOSE",
                                    isWinner ? winnerNewTotal : loserNewTotal,  // Host'un güncel kupa sayısı
                                    hostUserId,
                                    hostOpponentName  // Rakibin (client'ın) kullanıcı adı
                                ));

                                // Maç geçmişini kaydet - Client için
                                string clientOpponentName = hostName;
                                Debug.Log($"[Match History] Client's opponent name: {clientOpponentName}");
                                StartCoroutine(SendMatchHistory(
                                    isWinner ? "LOSE" : "WIN",
                                    isWinner ? loserNewTotal : winnerNewTotal,  // Client'ın güncel kupa sayısı
                                    clientUserId,
                                    clientOpponentName  // Rakibin (host'un) kullanıcı adı
                                ));

                                // Ekran güncellemesi için ClientRpc çağrıları
                                if (isWinner)
                                {
                                    EndGameClientRpc(true, hostParams);
                                    EndGameClientRpc(false, clientParams);
                                }
                                else
                                {
                                    EndGameClientRpc(false, hostParams);
                                    EndGameClientRpc(true, clientParams);
                                }
                            }
                        }
                    ));
                }
            }
        ));
    }

    [ClientRpc]
    public void EndGameClientRpc(bool isWin, ClientRpcParams rpcParams = default)
    {
        StartCoroutine(HandleEndGameClient(isWin));
    }

    private IEnumerator HandleEndGameClient(bool isWin)
    {
        // Wait for reset
        var resetTask = MatchManager.Instance?.ResetAllNetworking();
        if (resetTask != null)
            yield return new WaitUntil(() => resetTask.IsCompleted);

        string scene = isWin ? "YouWinScene" : "YouLoseScene";
        SceneManager.LoadScene(scene);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendClientUserIdServerRpc(string userId, ServerRpcParams rpcParams = default)
    {
        clientUserId = userId;
    }
}
