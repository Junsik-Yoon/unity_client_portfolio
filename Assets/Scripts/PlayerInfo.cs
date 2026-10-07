using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public class AccountCheck
    {
        public bool isAllowed { get; private set; }
        public float checkTime { get; private set; }
        public readonly float checkExpireDuration;
        public AccountCheck(float expireDuration)
        {
            checkExpireDuration = expireDuration;
        }
        public void Set(bool _isAllowed)
        {
            isAllowed = _isAllowed;
            checkTime = Time.time;
        }
        public bool IsNeedCheck => false == isAllowed || (isAllowed && checkTime + checkExpireDuration < Time.time);
    }
}
