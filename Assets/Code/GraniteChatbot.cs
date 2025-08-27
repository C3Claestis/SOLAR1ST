using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Humanizer;

public class GraniteChatbot : MonoBehaviour
{
    [Header("3D Model")]
    [SerializeField] Animator[] characters;

    [Header("UI References")]
    [SerializeField] InputField inputField;
    [SerializeField] Button sendButton;
    [SerializeField] Toggle toggleTokenEksternal;
    [SerializeField] InputField tokenEntry;
    [SerializeField] ScrollRect scrollRect;

    [Header("Prefab Object")]
    [SerializeField] Transform container;
    [SerializeField] GameObject bubbleAI;
    [SerializeField] GameObject bubbleUser;

    [Header("API Settings")]
    private string replicateToken = "r8_Qn3rFnmFEvpPbJhc4F7v6ENvkrObXDb1uu5u9";
    private string modelUrl = "https://api.replicate.com/v1/predictions";   

    private int currentIndex = 0;

    private void Start()
    {
        sendButton.onClick.AddListener(OnSendClick);
        Instantiate(bubbleAI, container);

        // 🔹 Pasang listener toggle untuk show/hide input token
        toggleTokenEksternal.onValueChanged.AddListener(OnToggleTokenChanged);

        // 🔹 Set awal sesuai kondisi toggle
        tokenEntry.gameObject.SetActive(toggleTokenEksternal.isOn);
    }

    private void OnToggleTokenChanged(bool isOn)
    {
        tokenEntry.gameObject.SetActive(isOn);
    }

    void OnSendClick()
    {
        string prompt = inputField.text;
        string nameAi = "";
        if (string.IsNullOrWhiteSpace(prompt)) return;

        // 🔹 Cari karakter yang sedang aktif
        Animator activeCharacter = null;
        foreach (var character in characters)
        {
            if (character.gameObject.activeSelf)
            {
                activeCharacter = character;
                break;
            }
        }

        // 🔹 Kalau ada karakter aktif, set trigger random
        if (activeCharacter != null)
        {
            string[] randomTriggers = { "2", "3", "4" }; // ganti sesuai animator
            string chosenTrigger = randomTriggers[Random.Range(0, randomTriggers.Length)];
            activeCharacter.SetTrigger(chosenTrigger);
        }

        if (currentIndex == 0)
        {
            nameAi = "Phoebe";
        }
        else if (currentIndex == 1)
        {
            nameAi = "Cantarella";
        }
        else
        {
            nameAi = "Baizhi";
        }

        // Spawn bubble untuk user
        GameObject userBubble = Instantiate(bubbleUser, container);
        Text userText = userBubble.transform.GetChild(0).GetChild(1).GetComponent<Text>();
        if (userText != null)
            userText.text = prompt;

        // Spawn bubble untuk placeholder AI
        GameObject aiBubble = Instantiate(bubbleAI, container);
        Text aiText = aiBubble.transform.GetChild(0).GetChild(1).GetComponent<Text>();
        Text textNameAi = aiBubble.transform.GetChild(0).GetChild(0).GetComponent<Text>();
        if (aiText != null)
        {
            textNameAi.text = nameAi;
            aiText.text = nameAi + " is thinking...";
        }

        // 🔹 Scroll otomatis ke bawah
        StartCoroutine(ScrollToBottomNextFrame());

        // Panggil API
        StartCoroutine(CallGraniteAPI(prompt, aiText));

        inputField.text = "";
    }

    IEnumerator CallGraniteAPI(string prompt, Text aiText)
    {
        // 🔹 Tentukan token yang dipakai
        string tokenToUse = replicateToken;
        if (toggleTokenEksternal.isOn)
        {
            string externalToken = tokenEntry.text.Trim();
            if (!string.IsNullOrEmpty(externalToken) && externalToken.StartsWith("r"))
            {
                tokenToUse = externalToken;
            }
            else
            {
                Debug.LogWarning("Token eksternal tidak valid, kembali pakai replicateToken default.");
            }
        }

        var request = new ReplicateRequest
        {
            version = "ibm-granite/granite-3.3-8b-instruct",
            input = new Input
            {
                prompt = prompt,
                max_tokens = 512,
                temperature = 0.7f,
                top_p = 0.9f
            }
        };

        string jsonBody = JsonConvert.SerializeObject(request);

        using (UnityWebRequest www = new UnityWebRequest(modelUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Token {tokenToUse}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseJson = www.downloadHandler.text;
                var prediction = JsonConvert.DeserializeObject<ReplicateResponse>(responseJson);

                StartCoroutine(PollForResult(prediction.urls.get, 0, aiText, tokenToUse));
            }
            else
            {
                aiText.text = $"Error: {www.error}\n{www.downloadHandler.text}";
            }
        }
    }


    IEnumerator PollForResult(string getUrl, int attempt, Text aiText, string tokenToUse)
    {
        if (attempt > 15)
        {
            aiText.text = "Maaf, timeout menunggu jawaban.";
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(getUrl))
        {
            www.SetRequestHeader("Authorization", $"Token {tokenToUse}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string resultJson = www.downloadHandler.text;
                var result = JsonConvert.DeserializeObject<ReplicateResponse>(resultJson);

                if (result.status == "succeeded")
                {
                    if (result.output != null)
                    {
                        var listOutput = JsonConvert.DeserializeObject<List<string>>(result.output.ToString());
                        string rawAnswer = string.Join(" ", listOutput);
                        string cleanedAnswer = SortirTeks(rawAnswer);

                        aiText.text = cleanedAnswer;
                    }
                    else
                    {
                        aiText.text = "Tidak ada jawaban yang dihasilkan.";
                    }
                }
                else if (result.status == "failed")
                {
                    aiText.text = "Gagal menghasilkan jawaban.";
                }
                else
                {
                    yield return new WaitForSeconds(2);
                    StartCoroutine(PollForResult(getUrl, attempt + 1, aiText, tokenToUse));
                }
            }
            else
            {
                aiText.text = $"Polling error: {www.error}";
            }
        }
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        // Tunggu 1 frame supaya layout selesai update
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // 0 = bawah, 1 = atas
    }

    string SortirTeks(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        text = text.Replace("\"", "")
                   .Replace("\'", "")
                   .Replace("\n", " ")
                   .Replace("\r", " ");

        while (text.Contains("  "))
            text = text.Replace("  ", " ");

        text = text.Replace(" ,", ",")
                   .Replace(" .", ".")
                   .Replace(" ?", "?")
                   .Replace(" !", "!")
                   .Replace(" - ", "-");

        text = Regex.Replace(text, @"\b(\w)\s(?=\w)", "$1");

        text = text.Transform(To.SentenceCase);

        return text.Trim();
    }

    public void SwitchCharacter(int index)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].gameObject.SetActive(i == index);
            currentIndex = index;
        }
    }
}

// Classes untuk serialisasi JSON
[System.Serializable]
public class Input
{
    public string prompt;
    public int max_tokens;
    public float temperature;
    public float top_p;
}

[System.Serializable]
public class ReplicateRequest
{
    public string version;
    public Input input;
}

[System.Serializable]
public class Urls
{
    public string get;
    public string cancel;
}

[System.Serializable]
public class ReplicateResponse
{
    public string id;
    public string status;
    public object output;
    public Urls urls;
}
