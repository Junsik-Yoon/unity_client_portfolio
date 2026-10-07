using System.Globalization;
using System.Threading;
using Portfolio.Platforms;
using UnityEngine;

public class PortfolioDemoSetting : ScriptableObject
{
    [Header("시작 플랫폼")]
    [Tooltip("게임 실행 시 초기화할 플랫폼을 선택합니다.")]
    [SerializeField] private PlatformProvider platform = PlatformProvider.MockPlatform1;

    public static PlatformProvider SelectedPlatform { get; private set; } = PlatformProvider.MockPlatform1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Application.runInBackground = true;

        var _setting = Resources.Load<PortfolioDemoSetting>("PortfolioDemoSetting");
        SelectedPlatform = _setting != null ? _setting.platform : PlatformProvider.MockPlatform1;

        GameManager.StartGame();
    }
}