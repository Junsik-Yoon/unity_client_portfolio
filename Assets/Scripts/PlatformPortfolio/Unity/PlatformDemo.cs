using Portfolio.Login;
using Portfolio.Backend;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Portfolio.Platforms;
using UnityEngine;

// 플랫폼 통합 데모
public sealed class PlatformDemo : MonoBehaviour
{
    private PlatformManager platformManager;
    private readonly MockBaas someBaas = new MockBaas();
    private LoginFlow loginFlow;
    private BaasSession BackendSession => loginFlow?.Session;
    private BaasLoginState LoginState => loginFlow?.State ?? BaasLoginState.SignedOut;
    private readonly List<string> logs = new List<string>();
    private CancellationToken token;
    private PlatformUser user;
    private int providerIndex;
    private bool busy;
    private bool failFriends;
    private bool failAuth;
    private bool failBackend;
    private bool failApi;
    private bool timeoutApi;
    private string inventoryNextToken;
    private bool inventoryLoaded;
    private string apiStatus = "백엔드 로그인 후 조회할 수 있습니다.";
    private readonly List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private IReadOnlyList<LeaderboardEntry> leaderboard;
    private string status = "종료됨";
    private string statInput = "10";
    private Vector2 scroll;
    private GUISkin guiSkin;
    private bool showRestrictionDialog;
    private string restrictionStatus = "검사 대기";
    private Vector2 controlsScroll;
    private PlatformTaskManager.Task restrictionTask;
    private string restrictionTaskLabel;
    private static readonly string[] ProviderNames = { "Mock 플랫폼 1", "Mock 플랫폼 2", "Mock 플랫폼 3", "플랫폼 없음" };
    private static readonly PlatformProvider[] Providers =
    {
        PlatformProvider.MockPlatform1,
        PlatformProvider.MockPlatform2,
        PlatformProvider.MockPlatform3,
        PlatformProvider.NoPlatform
    };

    private void Start()
    {
        token = this.GetCancellationTokenOnDestroy();

        providerIndex = Array.IndexOf(Providers, PortfolioDemoSetting.SelectedPlatform);
        if (providerIndex < 0)
        {
            providerIndex = 0;
        }

        platformManager = PlatformManager.Instance;
        platformManager.PlatformReleased += LogoutBackend;
        RunAsync("초기화", InitializeAsync).Forget();
    }

    private async UniTask InitializeAsync()
    {
        user = null;
        status = "초기화 중";
        restrictionStatus = "검사 대기";
        platformManager.ReleasePlatform();
        platformManager.SelectProvider(Providers[providerIndex]);
        await platformManager.InitializeAsync(token);

        Log("플랫폼 초기화 완료: " + ProviderNames[providerIndex]);

        user = await PlatformManager.Platform.Social.GetLocalUserAsync(token);
        Log("플랫폼 사용자: " + user.DisplayName + " (" + user.Id + ")");
        await LoginBackendAsync();
    }

    private async UniTask LoginBackendAsync()
    {
        var platform = PlatformManager.Platform;
        if (platform is MockPlatform mock) mock.SimulateAuthFailure = failAuth;
        someBaas.SimulateLoginFailure = failBackend;
        status = "플랫폼 인증 중";
        Log("인증 시작: 플랫폼 초기화 확인 → 인증 코드 발급 → MockBaas 로그인");
        try
        {
            // 상위 흐름에서 플랫폼과 백엔드를 조합한다.
            if (loginFlow == null) loginFlow = new LoginFlow(platform, someBaas);
            var _session = await loginFlow.LoginAsync(token);
            status = _session == null ? "오프라인 준비 완료" : "백엔드 로그인 완료";
            Log(_session == null ? "오프라인: 인증 코드 발급과 백엔드 로그인을 생략합니다."
                : "MockBaas 로그인 완료 / PlayerId: " + _session.PlayerId);
        }
        catch (OperationCanceledException) { status = "인증 취소됨"; throw; }
        catch { status = "인증 실패 — 옵션을 해제하고 다시 시도하세요"; throw; }
    }

    private void LogoutBackend()
    {
        var previous = loginFlow;
        loginFlow = null;
        previous?.Dispose();
        inventoryNextToken = null;
        inventoryLoaded = false;
        inventoryItems.Clear();
        leaderboard = null;
        apiStatus = "백엔드 로그인 후 조회할 수 있습니다.";
    }

    private void ConfigureApi()
    {
        someBaas.SimulateApiFailure = failApi;
        someBaas.SimulateApiTimeout = timeoutApi;
    }

    private async UniTask GetBackendLeaderboardAsync()
    {
        ConfigureApi();
        apiStatus = "리더보드 요청 중";
        Log("요청 GetLeaderboard { statisticName: sample.score, startPosition: 0, count: 5 }");
        try
        {
            var response = await someBaas.GetLeaderboardAsync(BackendSession, new LeaderboardRequest("sample.score"), token);
            leaderboard = response.Entries;
            apiStatus = "리더보드 응답: " + leaderboard.Count + "명";
            Log(apiStatus);
        }
        catch (BaasApiException error) { ReportApiError(error); throw; }
    }

    private async UniTask GetBackendInventoryAsync(bool nextPage)
    {
        ConfigureApi();
        apiStatus = "인벤토리 요청 중";
        var cursor = nextPage ? inventoryNextToken : null;
        Log("요청 GetInventoryItems { count: 2, continuationToken: " + (cursor == null ? "없음" : "이전 응답 토큰") + " }");
        try
        {
            var response = await someBaas.GetInventoryItemsAsync(BackendSession, new InventoryRequest(2, cursor), token);
            if (!nextPage) inventoryItems.Clear();
            inventoryItems.AddRange(response.Items);
            inventoryNextToken = response.ContinuationToken;
            inventoryLoaded = true;
            apiStatus = "인벤토리 응답: " + response.Items.Count + "개 / 누적 " + inventoryItems.Count
                + (inventoryNextToken == null ? " / 마지막 페이지" : " / 다음 페이지 있음");
            Log(apiStatus);
        }
        catch (BaasApiException error) { ReportApiError(error); throw; }
    }

    private void ReportApiError(BaasApiException error)
    {
        apiStatus = error.ApiName + " 실패 [" + error.Code + "]: " + error.Message;
        Log(apiStatus);
    }

    private void DrawBackendApiControls()
    {
        GUI.enabled = true;
        GUILayout.Space(8);
        GUILayout.Label("G-BaaS API · 리더보드 / 인벤토리");
        GUILayout.Label("고정 샘플 응답입니다. 플랫폼 스탯·클라우드 데이터와 별개입니다.");
        GUI.enabled = !busy;
        failApi = GUILayout.Toggle(failApi, "API 서버 오류 시뮬레이션");
        timeoutApi = GUILayout.Toggle(timeoutApi, "API 응답 시간 초과 시뮬레이션 (2초)");
        GUI.enabled = !busy && BackendSession != null;
        ActionButton("백엔드 리더보드 상위 5명", GetBackendLeaderboardAsync);
        ActionButton("인벤토리 첫 페이지 / 새로고침", () => GetBackendInventoryAsync(false));
        GUI.enabled = !busy && BackendSession != null && inventoryLoaded && inventoryNextToken != null;
        ActionButton("인벤토리 다음 페이지", () => GetBackendInventoryAsync(true));
        GUI.enabled = true;
        GUILayout.Label(apiStatus);
        if (leaderboard != null)
            foreach (var entry in leaderboard)
                GUILayout.Label((entry.Position + 1) + "위 · " + entry.DisplayName + " · " + entry.Value + "점");
        foreach (var item in inventoryItems)
            GUILayout.Label(item.DisplayName + " × " + item.Amount + " (" + item.ItemId + ")");
    }

    private void DrawBackendControls()
    {
        GUILayout.Space(8);
        GUI.enabled = true;
        GUILayout.Label("플랫폼 인증 → MockBaas 로그인");
        GUILayout.Label("인증 단계: " + (platformManager == null ? "대기" : LoginStateLabel(LoginState)));
        GUILayout.Label("백엔드 사용자: " + (BackendSession?.PlayerId ?? "-"));
        GUI.enabled = platformManager != null && !busy;
        failAuth = GUILayout.Toggle(failAuth, "인증 코드 발급 실패 시뮬레이션");
        failBackend = GUILayout.Toggle(failBackend, "MockBaas 로그인 실패 시뮬레이션");
        GUI.enabled = platformManager != null && platformManager.Initialized && !busy && BackendSession == null;
        ActionButton("백엔드 로그인 / 다시 시도", LoginBackendAsync);
        GUI.enabled = platformManager != null && platformManager.Initialized &&
            (BackendSession != null || LoginState == BaasLoginState.GettingAuthCode ||
             LoginState == BaasLoginState.SigningIn);
        if (GUILayout.Button("백엔드 로그아웃 / 진행 중 인증 취소", GUILayout.Height(34)))
        {
            LogoutBackend();
            status = "플랫폼 준비 완료 / 백엔드 로그아웃";
            Log("백엔드 인증 취소 및 로그인 상태 정리");
        }
        GUI.enabled = true;
        GUILayout.Label("실제 연결 없이 인증 흐름만 재현합니다. 인증 코드 원문은 표시하지 않습니다.");
    }

    private static string LoginStateLabel(BaasLoginState state)
    {
        switch (state)
        {
            case BaasLoginState.GettingAuthCode: return "플랫폼 인증 코드 발급 중";
            case BaasLoginState.SigningIn: return "MockBaas 로그인 중";
            case BaasLoginState.SignedIn: return "백엔드 로그인 완료";
            case BaasLoginState.Offline: return "오프라인 (백엔드 인증 생략)";
            case BaasLoginState.Failed: return "실패 (재시도 가능)";
            default: return "로그아웃";
        }
    }

    private async UniTask RunAsync(string op, Func<UniTask> action)
    {
        if (busy)
        {
            return;
        }
        busy = true;

        Log(op + " 시작");

        try
        {
            await action();
            token.ThrowIfCancellationRequested();
            Log(op + " 완료");
        }
        catch (OperationCanceledException)
        {
            if (!token.IsCancellationRequested)
            {
                Log(op + " 취소됨");
            }
        }
        catch (BaasApiException error)
        {
            Log(op + " 실패: " + error.Code);
        }
        catch (Exception error)
        {
            if (!platformManager.Initialized)
            {
                status = "초기화 실패 — 다시 시도할 수 있습니다";
            }
            Log("실패: " + error.Message);
            Debug.LogException(error);
        }
        finally
        {
            busy = false;
        }
    }

    private void Log(string message)
    {
        logs.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        if (logs.Count > 100)
        {
            logs.RemoveAt(0);
        }
        scroll.y = float.MaxValue;
        Debug.Log("[플랫폼 데모] " + message);
    }

    private async UniTask GetUserAsync()
    {
        user = await PlatformManager.Platform.Social.GetLocalUserAsync(token);
        Log(user.DisplayName + " / " + user.Id);
    }

    private async UniTask GetFriendsAsync()
    {
        var platform = PlatformManager.Platform;
        if (platform is MockPlatform mock) mock.SimulateFriendFailure = failFriends;
        await platform.Social.RefreshFriendListAsync(token);
        var friends = platform.Social.Friends;
        Log("친구 수: " + friends.Count);
        foreach (var friend in friends)
            Log(friend.DisplayName + (friend.IsOnline ? " [온라인]" : " [오프라인]"));
    }

    private async UniTask OpenProfileAsync()
    {
        await PlatformManager.Platform.Social.ShowProfileAsync(user.Id, token);
        Log("프로필 열기 요청 완료: " + user.DisplayName);
    }

    private async UniTask ReadScoreAsync()
    {
        var score = await PlatformManager.Platform.Stats.GetStatAsync("sample.score", token);
        Log("스탯: " + score);
    }

    private async UniTask SaveScoreAsync()
    {
        if (!int.TryParse(statInput, out var score))
            throw new ArgumentException("스탯은 정수로 입력해 주세요.");
        await PlatformManager.Platform.Stats.SetStatAsync("sample.score", score, token);
        await ReadScoreAsync();
    }

    private async UniTask UnlockAsync()
    {
        var stats = PlatformManager.Platform.Stats;
        Log("변경 전 업적 상태: " + (await stats.IsAchievementUnlockedAsync("sample.first_run", token) ? "언락됨" : "락됨"));
        await stats.UnlockAchievementAsync("sample.first_run", token);
        Log("변경 후 업적 상태: " + (await stats.IsAchievementUnlockedAsync("sample.first_run", token) ? "언락됨" : "락됨"));
    }

    private async UniTask AddScoreAsync()
    {
        if (!int.TryParse(statInput, out var value)) throw new ArgumentException("스탯은 정수로 입력해 주세요.");
        await PlatformManager.Platform.Stats.AddStatAsync("sample.score", value, token);
        await ReadScoreAsync();
    }
    private async UniTask UploadSaveAsync()
    {
        var score = await PlatformManager.Platform.Stats.GetStatAsync("sample.score", token);
        await PlatformManager.Platform.Cloud.UploadSaveDataAsync(score.ToString(System.Globalization.CultureInfo.InvariantCulture), token);
        Log("클라우드에 스탯을 저장했습니다: " + score);
    }
    private async UniTask DownloadSaveAsync()
    {
        var cloud = PlatformManager.Platform.Cloud;
        if (!await cloud.IsSaveDataExistAsync(token)) { Log("저장된 클라우드 데이터가 없습니다."); return; }
        var data = await cloud.DownloadSaveDataAsync(token);
        if (!int.TryParse(data, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var score))
            throw new InvalidOperationException("저장된 스탯 형식이 올바르지 않습니다.");
        await PlatformManager.Platform.Stats.SetStatAsync("sample.score", score, token);
        Log("클라우드에서 스탯을 복원했습니다: " + score);
    }
    private async UniTask DeleteSaveAsync()
    {
        await PlatformManager.Platform.Cloud.DeleteSaveDataAsync(token);
        Log("클라우드 데이터를 삭제했습니다.");
    }

    private bool CanRunRestriction => platformManager != null && platformManager.Initialized &&
        platformManager.RestrictQueue != null && platformManager.RestrictQueue.isActiveAndEnabled &&
        PlatformManager.Platform.Restriction != null;

    private async UniTask RunRestrictionAsync(PlatformTaskManager.COMMAND command, string label)
    {
        if (!CanRunRestriction)
            throw new InvalidOperationException("플랫폼 초기화, 작업 큐, Restriction 연결 상태를 확인해 주세요.");
        var queue = platformManager.RestrictQueue;
        if (queue.IsContainWithCheckComplete(command))
        {
            restrictionStatus = label + ": 동일한 검사가 이미 대기 또는 실행 중입니다.";
            Log(restrictionStatus);
            return;
        }
        // 데모 버튼은 매번 검사한다. AccountCheck 저장소는 아직 연결되어 있지 않다.
        restrictionTask = queue.Register(command,
            hasSubsequentAction: showRestrictionDialog, checkBeforeRegister: false);
        if (restrictionTask == null)
            throw new InvalidOperationException("제한 검사 작업을 등록하지 못했습니다.");
        restrictionTaskLabel = label;
        var _task = restrictionTask;
        restrictionStatus = label + ": 대기 중";
        Log(restrictionStatus + " (작업 " + _task.id + ")");
        try
        {
            while (!_task.isDone)
            {
                token.ThrowIfCancellationRequested();
                if (queue == null || !queue.isActiveAndEnabled || platformManager == null || !platformManager.Initialized)
                    throw new InvalidOperationException("검사 도중 플랫폼 또는 작업 큐가 종료되었습니다.");
                if (queue.GetTaskById(_task.id) != _task)
                    throw new InvalidOperationException("완료 전에 작업이 큐에서 제거되었습니다.");
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            if (_task.Error != null)
            {
                restrictionStatus = label + ": " + _task.Error.Message;
                Log(restrictionStatus);
                return;
            }
            restrictionStatus = label + ": " + (_task.IsAllowed.HasValue
                ? (_task.IsAllowed.Value ? "허용" : "거부") : "검사 결과 없음");
            Log(restrictionStatus);
        }
        catch
        {
            restrictionStatus = label + ": 대기 중단";
            throw;
        }
        finally { restrictionTask = null; }
    }

    private void RestrictionButton(string label, PlatformTaskManager.COMMAND command)
    {
        if (GUILayout.Button(label, GUILayout.Height(34)))
            RunAsync(label, () => RunRestrictionAsync(command, label)).Forget();
    }

    private void DrawRestrictionControls()
    {
        GUILayout.Space(8);
        GUILayout.Label("제한 검사");
        GUI.enabled = !busy && CanRunRestriction;
        showRestrictionDialog = GUILayout.Toggle(showRestrictionDialog, "검사 시 안내창 요청");
        GUILayout.BeginHorizontal();
        RestrictionButton("사용자 제작 콘텐츠 검사", PlatformTaskManager.COMMAND.UGC);
        RestrictionButton("유저간 상호작용 검사", PlatformTaskManager.COMMAND.Interact);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RestrictionButton("유료멤버십 검사", PlatformTaskManager.COMMAND.HasPlus);
        RestrictionButton("로컬커뮤니케이션 검사", PlatformTaskManager.COMMAND.LocalCommunicate);
        GUILayout.EndHorizontal();
        GUI.enabled = true;
        if (platformManager == null || !platformManager.Initialized)
            GUILayout.Label("플랫폼 초기화 후 검사할 수 있습니다.");
        else if (PlatformManager.Platform.Restriction == null)
            GUILayout.Label("Restriction 미연결: 선택한 플랫폼에 제한 검사 구현체를 연결해 주세요.");
        else if (platformManager.RestrictQueue == null || !platformManager.RestrictQueue.isActiveAndEnabled)
            GUILayout.Label("제한 검사 작업 큐가 연결되어 있지 않거나 비활성화되어 있습니다.");
        else
            GUILayout.Label("");
        GUILayout.Label(restrictionTask != null && restrictionTask.isWork
            ? restrictionTaskLabel + ": 실행 중" : restrictionStatus);
        GUILayout.Label("플랫폼1 전체 허용 · 플랫폼2 사용자제작콘텐츠/유료멤버십 거부 · 플랫폼3 상호작용/로컬커뮤니케이션 거부");
    }

    private void ActionButton(string label, Func<UniTask> action)
    {
        if (GUILayout.Button(label, GUILayout.Height(34))) RunAsync(label, action).Forget();
    }

    private void OnGUI()
    {
        var previousSkin = GUI.skin;
        var previousMatrix = GUI.matrix;
        var previousEnabled = GUI.enabled;
        if (guiSkin == null)
        {
            guiSkin = Instantiate(GUI.skin);
            guiSkin.label.fontSize = 16;
            guiSkin.label.wordWrap = true;
            guiSkin.label.stretchWidth = true;
            guiSkin.button.fontSize = 16;
            guiSkin.button.wordWrap = true;
            guiSkin.toggle.fontSize = 16;
            guiSkin.textField.fontSize = 16;
        }
        GUI.skin = guiSkin;

        // 화면 전체를 두 개의 같은 너비 패널로 사용한다.
        var scale = Mathf.Max(0.01f, Mathf.Min(1f, Screen.width / 1100f, Screen.height / 650f));
        var width = Screen.width / scale;
        var height = Screen.height / scale;
        const float margin = 16f;
        const float gap = 12f;
        var panelWidth = (width - margin * 2f - gap) * 0.5f;
        var panelHeight = height - margin * 2f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        try
        {
            GUILayout.BeginArea(new Rect(margin, margin, panelWidth, panelHeight), GUI.skin.box);
            GUILayout.Label("플랫폼 인터페이스 데모");
            GUILayout.Label("상태: " + status + (busy ? " (처리 중…)" : ""));
            GUILayout.Label("사용자: " + (user == null ? "-" : user.DisplayName));
            controlsScroll = GUILayout.BeginScrollView(controlsScroll, false, false);

            GUI.enabled = platformManager != null && !busy && !platformManager.Initialized;
            providerIndex = GUILayout.SelectionGrid(providerIndex, ProviderNames, 2, GUILayout.Height(68));
            if (GUILayout.Button("초기화 / 다시 시도", GUILayout.Height(34)))
                RunAsync("초기화", InitializeAsync).Forget();

            DrawBackendControls();
            DrawBackendApiControls();

            GUI.enabled = platformManager != null && !busy && platformManager.Initialized;
            GUILayout.Space(8);
            GUILayout.Label("사용자 · 친구");
            GUILayout.BeginHorizontal();
            ActionButton("내 정보 조회", GetUserAsync);
            ActionButton("친구 목록 조회", GetFriendsAsync);
            GUILayout.EndHorizontal();
            GUI.enabled = GUI.enabled && user != null;
            ActionButton("프로필 열기", OpenProfileAsync);
            failFriends = GUILayout.Toggle(failFriends, "친구 조회 실패 시뮬레이션");

            GUI.enabled = platformManager != null && !busy && platformManager.Initialized;
            GUILayout.Space(8);
            GUILayout.Label("스탯 · 업적");
            GUILayout.BeginHorizontal();
            GUILayout.Label("스탯", GUILayout.Width(50));
            statInput = GUILayout.TextField(statInput);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ActionButton("스탯 저장", SaveScoreAsync);
            ActionButton("스탯 조회", ReadScoreAsync);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ActionButton("스탯 더하기", AddScoreAsync);
            ActionButton("업적 언락", UnlockAsync);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("클라우드");
            GUILayout.BeginHorizontal();
            ActionButton("클라우드 저장", UploadSaveAsync);
            ActionButton("클라우드 복원", DownloadSaveAsync);
            GUILayout.EndHorizontal();
            ActionButton("클라우드 삭제", DeleteSaveAsync);
            DrawRestrictionControls();
            GUI.enabled = platformManager != null && platformManager.Initialized && !busy &&
                (platformManager.RestrictQueue == null || platformManager.RestrictQueue.IsEmpty);
            GUILayout.Space(8);
            if (GUILayout.Button("플랫폼 릴리즈 / 다른 플랫폼 선택", GUILayout.Height(34)))
            {
                platformManager.ReleasePlatform();
                user = null;
                status = "종료됨";
                restrictionStatus = "검사 대기";
                Log("플랫폼 릴리즈 및 데이터 초기화 완료");
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(margin + panelWidth + gap, margin, panelWidth, panelHeight), GUI.skin.box);
            GUILayout.Label("실행 로그 · 최근 " + logs.Count + "개");
            scroll = GUILayout.BeginScrollView(scroll, GUI.skin.box);
            foreach (var line in logs)
                GUILayout.Label(line, GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        finally
        {
            GUI.enabled = previousEnabled;
            GUI.matrix = previousMatrix;
            GUI.skin = previousSkin;
        }
    }

    private void OnDestroy()
    {
        LogoutBackend();
        if (platformManager != null)
        {
            platformManager.PlatformReleased -= LogoutBackend;
            platformManager.ReleasePlatform();
        }
        if (guiSkin != null) Destroy(guiSkin);
    }
}
