using UnityEngine;

public enum ENVInteractionKind
{
    Seat,
    Gem,
    Boat
}

/// <summary>
/// Stable gameplay identity for an art-authored INT_* object.
/// Gameplay, UI and save data reference this value rather than a Transform path.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVInteractionIdentity : MonoBehaviour
{
    [Tooltip("Keep this equal to the approved INT_* name. Do not use hierarchy paths here.")]
    public string interactionId;

    public ENVInteractionKind kind;

    [TextArea]
    public string designerNote;
}
