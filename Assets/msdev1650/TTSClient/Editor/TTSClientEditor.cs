using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

[CustomEditor(typeof(msdev1650.TTSClient.TTSClient))]
public class TTSClientEditor : Editor
{
    //----------| Private Fields |----------
    private string apiKey = "";
    private msdev1650.TTSClient.TTSClient ttsClient;

    //----------| Custom Inspector GUI |----------
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ttsClient = (msdev1650.TTSClient.TTSClient)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("API Key Encryption", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.TextField("API Key", apiKey);

        EditorGUILayout.Space();
        if (GUILayout.Button("1-Click Encryption"))
        {
            OneClickEncryption();
        }

        if (GUILayout.Button("Encrypt and Set API Key"))
        {
            EncryptAndSetApiKey();
        }
    }

    //----------| Encryption Methods |----------
    private void EncryptAndSetApiKey()
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(ttsClient.AesKey) || string.IsNullOrEmpty(ttsClient.AesIV))
        {
            ShowError("Please enter API Key, AES Key, and AES IV.");
            return;
        }

        try
        {
            ttsClient.EncryptedApiKey = EncryptApiKey(apiKey, ttsClient.AesKey, ttsClient.AesIV);
            SaveChangesAndClearApiKey();
            ShowSuccess("API Key encrypted and set successfully!");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to encrypt API Key: {ex.Message}");
        }
    }

    private void OneClickEncryption()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            ShowError("Please enter the API Key.");
            return;
        }

        try
        {
            GenerateAesKeyAndIV();
            ttsClient.EncryptedApiKey = EncryptApiKey(apiKey, ttsClient.AesKey, ttsClient.AesIV);
            SaveChangesAndClearApiKey();
            ShowSuccess("API Key encrypted and set successfully with generated AES Key and IV!");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to encrypt API Key: {ex.Message}");
        }
    }

    //----------| Helper Methods |----------
    private string EncryptApiKey(string apiKey, string key, string iv)
    {
        byte[] keyBytes = Convert.FromBase64String(key);
        byte[] ivBytes = Convert.FromBase64String(iv);
        byte[] plainBytes = Encoding.UTF8.GetBytes(apiKey);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (MemoryStream ms = new MemoryStream())
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private void GenerateAesKeyAndIV()
    {
        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();
            ttsClient.AesKey = Convert.ToBase64String(aes.Key);
            ttsClient.AesIV = Convert.ToBase64String(aes.IV);
        }
    }

    private void SaveChangesAndClearApiKey()
    {
        EditorUtility.SetDirty(ttsClient);
        AssetDatabase.SaveAssets();
        apiKey = "";
    }

    private void ShowError(string message)
    {
        EditorUtility.DisplayDialog("Error", message, "OK");
    }

    private void ShowSuccess(string message)
    {
        EditorUtility.DisplayDialog("Success", message, "OK");
    }
}
