using Cysharp.Threading.Tasks;
using Portfolio.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class PlatformTaskManager : MonoBehaviour
{
    public class Task
    {
        public Task(int id, bool hasSubsequentAction)
        {
            this.id = id;
            this.isWork = false;
            this.isDone = false;
            this.hasSubsequentAction = hasSubsequentAction;
        }
        public bool? IsAllowed { get; private set; }
        public Exception Error { get; private set; }
        public void SetResult(bool allowed) => IsAllowed = allowed;
        public void SetError(Exception error) => Error = error;
        public int id { get; private set; }
        public bool isWork { get; private set; }
        public bool isDone { get; private set; }
        public bool hasSubsequentAction { get; private set; }

        public void SetWork()
        {
            isWork = true;
            isDone = false;
        }
        public void SetDone()
        {
            isWork = false;
            isDone = true;
        }
    }
    public enum COMMAND
    {
        UGC,
        Interact,
        HasPlus,
        LocalCommunicate,
    }

    private readonly Queue<KeyValuePair<COMMAND, Task>> commandQueue = new();
    private int handlerIndex = 0;
    private readonly double timeout = 10d;

    public bool IsEmpty => commandQueue.Count <= 0;

    private void Update()
    {
        if (IsEmpty)
        {
            return;
        }

        var _peekCommand = commandQueue.Peek();

        if (true == _peekCommand.Value.isWork)
        {
            return;
        }
        if (true == _peekCommand.Value.isDone)
        {
            commandQueue.Dequeue();
            return;
        }

        _peekCommand.Value.SetWork();

        ExecuteCommand(_peekCommand.Key, _peekCommand.Value);
    }
    private async void ExecuteCommand(COMMAND command, Task task)
    {
        try
        {
            await ExecuteCommandAsync(command, task).Timeout(TimeSpan.FromSeconds(timeout));
        }
        catch (TimeoutException e)
        {
            task.SetError(e);
            Debug.LogError($"timeout : {e}");
        }
        catch (Exception e)
        {
            task.SetError(e);
            Debug.LogError(e);
        }
        finally
        {
            task.SetDone();
        }
    }
    private async UniTask ExecuteCommandAsync(COMMAND command, Task task)
    {
        switch (command)
        {
            case COMMAND.UGC:
                task.SetResult(await PlatformManager.Platform.Restriction.CheckAndSetUGCAvailable(task.hasSubsequentAction));
                break;
            case COMMAND.Interact:
                task.SetResult(await PlatformManager.Platform.Restriction.CheckAndSetInteractAllowed());
                break;
            case COMMAND.HasPlus:
                task.SetResult(await PlatformManager.Platform.Restriction.CheckAndSetHasPlus(task.hasSubsequentAction));
                break;
            case COMMAND.LocalCommunicate:
                task.SetResult(await PlatformManager.Platform.Restriction.CheckAndSetLocalUserCommunicatable(task.hasSubsequentAction));
                break;
            default:
                Debug.LogError("Unknown Command");
                break;
        }
        task.SetDone();
    }
    public Task Register(COMMAND command, bool hasSubsequentAction = false, bool checkBeforeRegister = true, PlayerInfo.AccountCheck accountCheck = default)
    {
        if (checkBeforeRegister)
        {
            var _okToRegister = IsOkToRegister(command, accountCheck);
            if (!_okToRegister)
            {
                return default;
            }
        }

        ++handlerIndex;

        var _task = new Task(handlerIndex, hasSubsequentAction);
        commandQueue.Enqueue(new KeyValuePair<COMMAND, Task>(command, _task));
        return _task;
    }
    private bool IsOkToRegister(COMMAND command, PlayerInfo.AccountCheck accountCheck)
    {
        if (accountCheck != null &&
            accountCheck.IsNeedCheck &&
            false == IsContainWithCheckComplete(command))
        {
            return true;
        }
        return false;
    }
    /// <summary>중복 커맨드 허용 시</summary>
    public bool IsContainAll(COMMAND command)
    {
        return commandQueue.Any(x => x.Key == command);
    }
    /// <summary>중복 커맨드 비허용 시</summary>
    public bool IsContainWithCheckComplete(COMMAND command)
    {
        return commandQueue.Any(x => x.Key == command && false == x.Value.isDone);
    }
    public Task GetTaskById(int id)
    {
        var _target = commandQueue.LastOrDefault(x => x.Value.id == id);
        if (_target.Equals(default(KeyValuePair<COMMAND, Task>)))
        {
            return default;
        }
        return _target.Value;
    }
    public List<Task> GetTasksByCommand(COMMAND command)
    {
        return commandQueue.Where(x=>x.Key == command)
                            .Select(x=>x.Value)
                            .ToList();
    }
    public void ForceCompleteTask(Task task)
    {
        task.SetDone();
    }
    public void Clear()
    {
        commandQueue.Clear();
    }

    public async UniTask<bool> WaitForQueueEmpty(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.WaitUntil(() => IsEmpty, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            Debug.LogError(e);
            return false;
        }
        return true;
    }
}
