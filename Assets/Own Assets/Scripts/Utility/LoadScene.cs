using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Game.Data;

public class LoadScene : MonoBehaviour
{
    [SerializeField] SceneReference _loadingScene;
    [SerializeField] MapSelectionData _mapSelectionData;

    public static UnityEvent<SceneReference> E_LoadScene = new();
    public static UnityEvent<string> E_LoadSceneWithPath = new();
    public static UnityEvent<int> E_LoadSceneWithMapIndex = new();

    private void Awake()
    {
        E_LoadSceneWithMapIndex.AddListener(LoadMap);
        E_LoadScene.AddListener((scene) => StartCoroutine(LoadNextScene(scene)));
        E_LoadSceneWithPath.AddListener((scenePath) => StartCoroutine(LoadNextScene(scenePath)));
        DontDestroyOnLoad(gameObject);
    }

    private void LoadMap(int index)
    {
        StartCoroutine(LoadNextScene(_mapSelectionData.Maps[index].SceneReference));
    }

    private IEnumerator LoadNextScene(string scenePath)
    {
        SceneManager.LoadSceneAsync(_loadingScene);
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadSceneAsync(scenePath);
    }

    private void OnDestroy()
    {
        E_LoadScene.RemoveListener((scene) => StartCoroutine(LoadNextScene(scene)));
        E_LoadSceneWithPath.RemoveListener((scenePath) => StartCoroutine(LoadNextScene(scenePath)));
        E_LoadSceneWithMapIndex.RemoveListener(LoadMap);
    }
}
