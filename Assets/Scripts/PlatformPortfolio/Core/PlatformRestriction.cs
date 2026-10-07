using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public abstract class PlatformRestriction
{
    public abstract UniTask<bool> CheckLocalUserCommunicatableAsync(bool showMessage = false, CancellationToken cancellationToken = default);
    public abstract UniTask<bool> CheckInteractAllowedAsync(CancellationToken cancellationToken = default);
    public abstract UniTask<bool> CheckHasPlusAsync(bool showPlusUpsellDialog, CancellationToken cancellationToken = default);

    public async UniTask<bool> CheckAndSetUGCAvailable(bool showDialog)
    {
        var _ok = await CheckLocalUserCommunicatableAsync(showDialog);
        //set if needed
        return _ok;
    }
    public async UniTask<bool> CheckAndSetInteractAllowed()
    {
        var _ok = await CheckInteractAllowedAsync();
        //set if needed
        return _ok;
    }
    public async UniTask<bool> CheckAndSetLocalUserCommunicatable(bool showMessage = false)
    {
        var _ok = await CheckLocalUserCommunicatableAsync(showMessage);
        //set if needed
        return _ok;
    }
    public async UniTask<bool> CheckAndSetHasPlus(bool showPlusUpsellDialog)
    {
        var _ok = await CheckHasPlusAsync(showPlusUpsellDialog, default);
        //set if needed
        return _ok;
    }
}
