using UnityEngine;
using UnityEngine.UI;

public enum ProgressType
{
    Locked, // uncleared
    Active, // focused, uncleared
    Cleared,
    Replay // focused, cleared
}

// progress type transition table
// locked -> active
// active -> locked
// active -> cleared
// cleared -> replay
// replay -> cleared

public class UIProgress : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Animator animator;
    [SerializeField] private string locked2Active = "ProgressLocked2Active";
    [SerializeField] private string active2locked = "ProgressActive2Locked";
    [SerializeField] private string active2Cleared = "ProgressActive2Cleared";
    [SerializeField] private string cleared2Replay = "ProgressCleared2Replay";
    [SerializeField] private string replay2Cleared = "ProgressReplay2Cleared";

    public ProgressType Type { get; private set; } = ProgressType.Locked;
    public void SetType(ProgressType type)
    {
        ProgressType oldType = Type;

        if (oldType == type) return;
        else if (oldType == ProgressType.Active && type == ProgressType.Locked)
            animator.Play(active2locked);
        else if (oldType == ProgressType.Locked && type == ProgressType.Active)
            animator.Play(locked2Active);
        else if (oldType == ProgressType.Active && type == ProgressType.Cleared)
            animator.Play(active2Cleared);
        else if (oldType == ProgressType.Cleared && type == ProgressType.Replay)
            animator.Play(cleared2Replay);
        else if (oldType == ProgressType.Replay && type == ProgressType.Cleared)
            animator.Play(replay2Cleared);
        else
        {
            Debug.LogError($"invalid progress type transition, old: {oldType}, new: {type}");
            return;
        }

        Type = type;
    }
}
