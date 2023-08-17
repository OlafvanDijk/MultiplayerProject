using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                T[] instances = FindObjectsOfType<T>();
                if (instances.Length > 0)
                {
                    _instance = instances[0];
                }
                else
                {
                    GameObject gameObject = new();
                    gameObject.name = typeof(T).Name;
                    _instance = gameObject.AddComponent<T>();
                    DontDestroyOnLoad(gameObject);
                }
            }
            return _instance;
        }
    }
}
