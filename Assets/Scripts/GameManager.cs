using Portfolio.Platforms;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager instance;
    public static GameManager Instance { get { return instance; } }
    public static void StartGame()
    {
        if (instance != null)
        {
            return;
        }

        instance = Instantiate(Resources.Load<GameManager>(nameof(GameManager)));
        instance.name = instance.name.Replace("(Clone)", "");
        DontDestroyOnLoad(instance.gameObject);

        PlatformManager.Instance.InitializeCoreSystem();

        if (FindFirstObjectByType<PlatformDemo>() == null)
        {
            var _root = new GameObject(nameof(PlatformDemo));
            DontDestroyOnLoad(_root);
            _root.AddComponent<PlatformDemo>();
        }
    }
}
