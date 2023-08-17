using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField] SceneReference _loadingScene;

    public static UnityEvent<SceneReference> E_LoadScene = new();

    private void Awake()
    {
        E_LoadScene.AddListener((scene) => StartCoroutine(LoadNextScene(scene)));
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator LoadNextScene(SceneReference scene)
    {
        SceneManager.LoadSceneAsync(_loadingScene);
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadSceneAsync(scene);
    }

    private void OnDestroy()
    {
        E_LoadScene.RemoveListener((scene) => StartCoroutine(LoadNextScene(scene)));
    }
}
