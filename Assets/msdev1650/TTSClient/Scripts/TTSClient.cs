using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

namespace msdev1650.TTSClient
{
    public class TTSClient : MonoBehaviour
    {
        //----------| Private Fields |----------
        private readonly string url = "https://texttospeech.googleapis.com/v1/text:synthesize";
        [SerializeField] private string encryptedApiKey;
        [SerializeField] private string aesKey;
        [SerializeField] private string aesIV;

        //----------| Public Properties |----------
        // Properties for accessing and setting encryption-related fields
        public string EncryptedApiKey
        {
            get => encryptedApiKey;
            set => encryptedApiKey = value;
        }

        public string AesKey
        {
            get => aesKey;
            set => aesKey = value; // Stored as Base64 encoded string
        }

        public string AesIV
        {
            get => aesIV;
            set => aesIV = value; // Stored as Base64 encoded string
        }

        //----------| Serialized Fields |----------
        [SerializeField] private string languageCode = "en-US";
        [SerializeField] private string voiceName = "en-US-Standard-A";
        [SerializeField] private string audioEncoding = "LINEAR16";
        [SerializeField] private string ssmlGender = "MALE";
        [SerializeField] private int sampleRate = 24000;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] public string TTSText = "This is a TTS test with text from the Editors 'TTS Text' field.";

        [SerializeField] public bool playWhenGenerated = false;
        [SerializeField] public bool trimEnds = false;
        [SerializeField] public float trimStart = 0.1f;
        [SerializeField] public float trimEnd = 0.1f;
        [SerializeField] public bool startAutomatic = false;
        [SerializeField] private AudioSource audioSource;

        //----------| Private Fields |----------
        private Queue<AudioClipRequest> audioClipQueue = new Queue<AudioClipRequest>();
        private bool isProcessingAudioClip = false;

        //----------| Structs |----------
        private struct AudioClipRequest
        {
            public string text;
            public string ssmlGender;
            public Action<AudioClip> callback;
        }

        //----------| Unity Lifecycle Methods |----------
        private void Awake()
        {
            // Ensure we have an AudioSource component
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        public void Start()
        {
            if (startAutomatic)
            {
                SynthesizeTextFromInput();
            }
        }

        private void Update()
        {
            if (!isProcessingAudioClip && audioClipQueue.Count > 0)
            {
                ProcessNextAudioClipRequest();
            }
        }

        //----------| Public Methods |----------
        // Synthesize text from the input field
        public void SynthesizeTextFromInput()
        {
            string text = inputField.text;
            SynthesizeText(text, ssmlGender);
        }

        // Synthesize text from the configuration
        public void SynthesizeTextFromConfig()
        {
            string text = TTSText;
            SynthesizeText(text, ssmlGender);
        }

        // Main method to synthesize text
        public void SynthesizeText(string text, string ssmlGender = null, Action<AudioClip> callback = null)
        {
            Debug.Log("Synthesize method called.");
            Debug.Log("Output: " + text);
            if (string.IsNullOrEmpty(text))
            {
                text = "You have not entered any text.";
            }

            audioClipQueue.Enqueue(new AudioClipRequest
            {
                text = text,
                ssmlGender = ssmlGender,
                callback = callback
            });
        }

        // Set the language for TTS
        public void SetLanguage(int language)
        {
            switch (language)
            {
                case 0:
                    languageCode = "en-US";
                    voiceName = "en-US-Standard-A";
                    ssmlGender = "MALE";
                    break;
                case 1:
                    languageCode = "de-DE";
                    voiceName = "de-DE-Standard-A";
                    ssmlGender = "FEMALE";
                    break;
                default:
                    Debug.Log("Invalid language selection. 0 for English, 1 for German.");
                    break;
            }
        }

        //----------| Private Methods |----------
        // Process the next audio clip request in the queue
        private void ProcessNextAudioClipRequest()
        {
            if (audioClipQueue.Count > 0)
            {
                isProcessingAudioClip = true;
                AudioClipRequest request = audioClipQueue.Dequeue();
                StartCoroutine(SynthesizeCoroutine(request.text, request.ssmlGender, request.callback));
            }
        }

        // Coroutine to handle the synthesis process
        private IEnumerator SynthesizeCoroutine(string text, string ssmlGender = null, Action<AudioClip> callback = null)
        {
            Debug.Log($"Synthesize method called with text: {text} and ssmlGender: {ssmlGender}");

            string voicePart = GenerateVoicePart(ssmlGender);
            string jsonBody = GenerateJsonBody(text, voicePart);
            Debug.Log($"JSON body being sent to Google Cloud TTS: {jsonBody}");

            string decryptedApiKey = DecryptApiKey();
            if (string.IsNullOrEmpty(decryptedApiKey))
            {
                Debug.LogError("API key decryption failed. Please ensure the API key, AES Key, and AES IV are set correctly.");
                yield break;
            }

            UnityWebRequest www = SetupWebRequest(jsonBody, decryptedApiKey);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"Received response from Google Cloud TTS: {response}");

                AudioContent audioResponse = JsonUtility.FromJson<AudioContent>(response);

                if (audioResponse != null && !string.IsNullOrEmpty(audioResponse.audioContent))
                {
                    byte[] audioBytes = Convert.FromBase64String(audioResponse.audioContent);
                    Debug.Log($"Audio bytes length: {audioBytes.Length}");
                    AudioClip audioClip = CreateAudioClipFromBytes(audioBytes);
                    
                    callback?.Invoke(audioClip);

                    if (playWhenGenerated)
                    {
                        PlayAudioClip(audioClip);
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse audio content from response.");
                }
            }
            else
            {
                Debug.LogError($"UnityWebRequest error: {www.error}");
                Debug.LogError($"Response Body: {www.downloadHandler.text}");
            }

            isProcessingAudioClip = false;
        }

        // Generate the voice part of the JSON body
        private string GenerateVoicePart(string ssmlGender)
        {
            return !string.IsNullOrEmpty(voiceName) 
                ? $@"'voice': {{ 'languageCode': '{languageCode}', 'name': '{voiceName}', 'ssmlGender': '{ssmlGender ?? "FEMALE"}' }}" 
                : !string.IsNullOrEmpty(ssmlGender) 
                ? $@"'voice': {{ 'languageCode': '{languageCode}', 'ssmlGender': '{ssmlGender}' }}" 
                : $@"'voice': {{ 'languageCode': '{languageCode}' }}";
        }

        // Generate the complete JSON body for the API request
        private string GenerateJsonBody(string text, string voicePart)
        {
            return $@"{{ 'input': {{ 'text': '{EscapeJsonString(text)}' }},
                        {voicePart},
                        'audioConfig': {{ 'audioEncoding': '{audioEncoding}', 'sampleRateHertz': {sampleRate} }} }}";
        }

        // Set up the UnityWebRequest for the API call
        private UnityWebRequest SetupWebRequest(string jsonBody, string decryptedApiKey)
        {
            UnityWebRequest www = new UnityWebRequest($"{url}?key={decryptedApiKey}", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            www.SetRequestHeader("Content-Type", "application/json");
            return www;
        }

        // Create an AudioClip from the byte array received from the API
        private AudioClip CreateAudioClipFromBytes(byte[] audioData)
        {
            int sampleCount = audioData.Length / 2;
            float[] audioDataFloat = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                audioDataFloat[i] = BitConverter.ToInt16(audioData, i * 2) / 32768.0f;
            }

            AudioClip audioClip = AudioClip.Create("Synthesized Audio", sampleCount, 1, sampleRate, false);
            audioClip.SetData(audioDataFloat, 0);

            if (trimEnds)
            {
                audioClip = TrimAudioClip(audioClip, trimStart, trimEnd);
            }

            Debug.Log($"Synthesized audio clip length: {audioClip.length} seconds.");

            // Assign the audio clip to the AudioSource
            audioSource.clip = audioClip;

            return audioClip;
        }

        // Play the generated audio clip
        private void PlayAudioClip(AudioClip audioClip)
        {
            if (audioSource != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
                Debug.Log("Playing audio clip.");
            }
            else
            {
                Debug.LogWarning("No AudioSource component found to play the audio.");
            }
        }

        // Escape special characters in the JSON string
        private string EscapeJsonString(string str)
        {
            return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        // Trim the audio clip based on the specified start and end times
        private AudioClip TrimAudioClip(AudioClip originalClip, float startTrim, float endTrim)
        {
            if (startTrim >= originalClip.length || endTrim <= 0 || (startTrim + endTrim) >= originalClip.length)
            {
                Debug.LogWarning("Invalid trim values. Returning original clip.");
                return originalClip;
            }

            float trimStartTime = startTrim;
            float trimEndTime = originalClip.length - endTrim;
            float trimmedLength = trimEndTime - trimStartTime;

            AudioClip trimmedClip = AudioClip.Create(originalClip.name + "_Trimmed",
                (int)(trimmedLength * originalClip.frequency),
                originalClip.channels,
                originalClip.frequency,
                false);

            float[] data = new float[(int)(trimmedLength * originalClip.frequency)];
            originalClip.GetData(data, (int)(trimStartTime * originalClip.frequency));
            trimmedClip.SetData(data, 0);

            return trimmedClip;
        }

        // Decrypt the API key using AES encryption
        private string DecryptApiKey()
        {
            if (string.IsNullOrEmpty(encryptedApiKey) || string.IsNullOrEmpty(aesKey) || string.IsNullOrEmpty(aesIV))
            {
                Debug.LogError("Encryption parameters missing. Please ensure API key, AES Key, and AES IV are set.");
                return null;
            }

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedApiKey);
                byte[] key = Convert.FromBase64String(aesKey);
                byte[] iv = Convert.FromBase64String(aesIV);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(encryptedBytes))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"API key decryption failed: {ex.Message}");
                return null;
            }
        }

        //----------| Serializable Classes |----------
        [Serializable]
        public class AudioContent
        {
            public string audioContent;
        }
    }
}
