using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    private AudioClip currentClip;

    private bool soundShouldPlay = false;

    public void PlaySoundClip(string soundClipName)
    {
        soundShouldPlay = true;
        if (audioClips.ContainsKey(soundClipName))
        {
            audioSource.clip = audioClips[soundClipName];
            audioSource.Play();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(LoadAllStreamingAudio());
    }

    IEnumerator LoadAllStreamingAudio()
    {
        string soundsPath = Path.Combine(Application.streamingAssetsPath, "Sounds");

        if (!Directory.Exists(soundsPath))
        {
            if (!Directory.CreateDirectory(soundsPath).Exists)
            {
                UnityEngine.Debug.LogError("Sounds folder not found: " + soundsPath);
                yield break;
            }
        }

        string[] files = Directory.GetFiles(soundsPath);
        foreach (var filePath in files)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            AudioType audioType;

            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".mp3":
                    audioType = AudioType.MPEG;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                case ".meta":
                    continue;
                default:
                    UnityEngine.Debug.LogWarning("Unsupported file format: " + filePath);
                    continue;
            }

            string url = "file:///" + filePath.Replace("\\", "/"); // Windows-safe path

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load " + filePath + ": " + www.error);
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                string clipName = Path.GetFileNameWithoutExtension(filePath);
                audioClips[clipName] = clip;

                Debug.Log("Loaded audio: " + clipName);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (soundShouldPlay)
            {
                audioSource.PlayOneShot(currentClip);
                soundShouldPlay = false;
            }
        }
    }
}
