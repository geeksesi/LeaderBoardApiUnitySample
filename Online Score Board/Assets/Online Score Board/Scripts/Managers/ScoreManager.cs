using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ScoreManager : Singleton<ScoreManager> {

    public Text scoreText;
    public InputField nameInputField;
    public GameObject scoresPnl;
    public ScrollRect scoresScrollView;
    public GameObject scoreItemPrefab;

    [Space(20)]
    public string serverSetURL = "http://api.geeksesi.tk/set";
    public string serverGetURL = "http://api.geeksesi.tk/get";
    public string serverToken = "fd40c20e30d7c258f6bacfe892a5c48a3f7b954d";
    public string hashKey = "2fa4231a009e1482";
    public string hashIV = "a874a935c9680esd";
    public bool RepeatMode;

    int _score = 0;
    public int score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;

            scoreText.text = value.ToString();
        }
    }

	// Use this for initialization
	void Start () {
        score = 0;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void AddScore()
    {
        score++;
    }

    public void SubmitScore()
    {
        SendScoresToServer();
        //Debug.Log("Score Submitted!!!");
    }

    public void OpenScoresPnl()
    {
        ReceiveScoresFromServerAndSetUI();
        scoresPnl.SetActive(true);
    }

    public void CloseScoresPnl()
    {
        scoresPnl.SetActive(false);
        scoresScrollView.content.DestroyAllChilds();
    }

    public void SendScoresToServer()
    {
        string name = nameInputField.text;
        if (name != string.Empty && score > 0)
        {
            LeaderboardOnlineRequest leaderboardRequest = new LeaderboardOnlineRequest(name, score);
            StartCoroutine(leaderboardRequest.IESendRequest());
        }
        else
        {
            Debug.LogWarning("Request not sent to server. name must be fill and score must be greater than 0.");
        }
    }

    public void ReceiveScoresFromServerAndSetUI()
    {
        LeaderboardOnlineResponse leaderboardResponse = new LeaderboardOnlineResponse();
        StartCoroutine(leaderboardResponse.IEReceiveResponse());
    }
}

[Serializable]
public class LeaderboardOnlineRequest
{
    public string token;
    public string name;
    public int score;
    public long unix_time;

    public LeaderboardOnlineRequest(string name, int score)
    {
        this.token = ScoreManager.Instance.serverToken;
        this.name = name;
        this.score = score;
        this.unix_time = DateTime.UtcNow.ToUnixTimestamp();
    }

    public string Get_AES_128_CBC_HashString()
    {
        return OpensslAES.EncryptString(JsonUtility.ToJson(this, false), ScoreManager.Instance.hashKey, ScoreManager.Instance.hashIV);
    }

    public IEnumerator IESendRequest()
    {
        // Create a form object for sending score data to the server
        WWWForm form = new WWWForm();
        if(ScoreManager.Instance.RepeatMode){

        form.AddField("repeat","on");
        }else{
        form.AddField("repeat","off");
        }
        form.AddField("token", token);
        form.AddField("name", name);
        form.AddField("score", score);
        form.AddField("unix_time", unix_time.ToString());
        form.AddField("hash", Get_AES_128_CBC_HashString());

        // Create a download object
        UnityWebRequest webRequest = UnityWebRequest.Post(ScoreManager.Instance.serverSetURL, form);

        // Set headers
        webRequest.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
        webRequest.SetRequestHeader("Pragma", "no-cache");

        // Wait until the download is done
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogWarning("Error in sending data: " + webRequest.error);
        }
        else
        {
            // data was successfully send
            LeaderboardOnlineRequestResult requestResult = JsonUtility.FromJson<LeaderboardOnlineRequestResult>(webRequest.downloadHandler.text);
            if (requestResult.status == "ok")
            {
                // data was successfully submitted
                Debug.Log("Result of submitting data: " + requestResult.data);
            }
            else //rsult == false
            {
                Debug.LogWarning("Error in submitting data: " + requestResult.data);
            }
        }

        ScoreManager.Instance.score = 0;
        ScoreManager.Instance.nameInputField.text = string.Empty;
    }
}

[Serializable]
public class LeaderboardOnlineRequestResult
{
    public string status = "false";
    public string data = string.Empty;
}

[Serializable]
public class LeaderboardOnlineResponse
{
    public string status = "false";
    public List<ScoreData> data = new List<ScoreData>();
    public string message = string.Empty;

    public IEnumerator IEReceiveResponse()
    {
        string repeat = "";
        if(!ScoreManager.Instance.RepeatMode){
        repeat = "&repeat=off";
        }
        // Create a download object
        UnityWebRequest webRequest = UnityWebRequest.Get(
            string.Format("{0}?token={1}&interval=all&order=DESC"+repeat, ScoreManager.Instance.serverGetURL, ScoreManager.Instance.serverToken)
        );

        // Set headers
        webRequest.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
        webRequest.SetRequestHeader("Pragma", "no-cache");

        // Wait until the download is done
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogWarning("Error in sending data: " + webRequest.error);
        }
        else
        {
            // data was successfully send
            LeaderboardOnlineResponse leaderboardResponse = JsonUtility.FromJson<LeaderboardOnlineResponse>(webRequest.downloadHandler.text);
            if (leaderboardResponse.status == "ok")
            {
                // data was successfully submitted
                Debug.Log("Result of receiving data: ok");
                
                for (int i = 0; i < leaderboardResponse.data.Count; i++)
                {
                    ScoreHolder newScoreHolder = UnityEngine.Object.Instantiate(
                        ScoreManager.Instance.scoreItemPrefab,
                        ScoreManager.Instance.scoresScrollView.content
                    ).GetComponent<ScoreHolder>();
                    newScoreHolder.SetScoreData(i + 1, leaderboardResponse.data[i].name, leaderboardResponse.data[i].score);
                }
            }
            else //rsult == false
            {
                Debug.LogWarning("Error in receiving data: " + leaderboardResponse.message);
            }
        }
    }

    [Serializable]
    public class ScoreData
    {
        public int id;
        public string name;
        public string date_shamsy;
        public string date_milady;
        public long date_unix;
        public int score;
        public string timestamp;
    }
}

public static class OpensslAES
{
    public static string EncryptString(string plainText, string keyString, string ivString, int keySize = 128, CipherMode cipherMode = CipherMode.CBC)
    {
        // Instantiate a new Aes object to perform string symmetric encryption
        Aes encryptor = Aes.Create();

        encryptor.Mode = cipherMode;

        // Set key and IV
        byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);//16-24-32 length of key for 128-192-256 keySize
        byte[] ivBytes = Encoding.UTF8.GetBytes(ivString);//16 length of iv for AES
        encryptor.KeySize = keySize;
        encryptor.Padding = PaddingMode.PKCS7;
        encryptor.Key = keyBytes;
        encryptor.IV = ivBytes;

        // Instantiate a new MemoryStream object to contain the encrypted bytes
        MemoryStream memoryStream = new MemoryStream();

        // Instantiate a new encryptor from our Aes object
        ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

        // Instantiate a new CryptoStream object to process the data and write it to the 
        // memory stream
        CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

        // Convert the plainText string into a byte array
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Encrypt the input plaintext string
        cryptoStream.Write(plainBytes, 0, plainBytes.Length);

        // Complete the encryption process
        cryptoStream.FlushFinalBlock();

        // Convert the encrypted data from a MemoryStream to a byte array
        byte[] cipherBytes = memoryStream.ToArray();

        // Close both the MemoryStream and the CryptoStream
        memoryStream.Close();
        cryptoStream.Close();

        // Convert the encrypted byte array to a base64 encoded string
        string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length, Base64FormattingOptions.None);
        //string cipherText = Encoding.UTF8.GetString(cipherBytes);

        // Return the encrypted data as a string
        return cipherText;
    }

    public static string DecryptString(string cipherText, string keyString, string ivString, int keySize = 128, CipherMode cipherMode = CipherMode.CBC)
    {
        // Instantiate a new Aes object to perform string symmetric encryption
        Aes decryptor = Aes.Create();

        decryptor.Mode = cipherMode;

        // Set key and IV
        byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);//16-24-32 length of key for 128-192-256 keySize
        byte[] ivBytes = Encoding.UTF8.GetBytes(ivString);//16 length of iv for AES
        decryptor.KeySize = keySize;
        decryptor.Padding = PaddingMode.PKCS7;
        decryptor.Key = keyBytes;
        decryptor.IV = ivBytes;

        // Instantiate a new MemoryStream object to contain the encrypted bytes
        MemoryStream memoryStream = new MemoryStream();

        // Instantiate a new encryptor from our Aes object
        ICryptoTransform aesDecryptor = decryptor.CreateDecryptor();

        // Instantiate a new CryptoStream object to process the data and write it to the 
        // memory stream
        CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

        // Will contain decrypted plaintext
        string plainText = string.Empty;

        try
        {
            // Convert the ciphertext string into a byte array
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            //byte[] cipherBytes = Encoding.UTF8.GetBytes(cipherText);

            // Decrypt the input ciphertext string
            cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

            // Complete the decryption process
            cryptoStream.FlushFinalBlock();

            // Convert the decrypted data from a MemoryStream to a byte array
            byte[] plainBytes = memoryStream.ToArray();

            // Convert the decrypted byte array to string
            plainText = Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);
        }
        finally
        {
            // Close both the MemoryStream and the CryptoStream
            memoryStream.Close();
            cryptoStream.Close();
        }

        // Return the decrypted data as a string
        return plainText;
    }
}
