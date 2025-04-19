using UnityEngine;

[CreateAssetMenu(fileName = "NotificationType", menuName = "kusa/Notifications/Notification Type", order = 1)]
public class NotificationType : ScriptableObject
{
    public string typeName; // E.g., "Error", "Success"
    public Sprite icon;
    public Color backgroundColor;
    public string soundID; // Optional sound for this type
    public float displayDuration = 3f; // Default duration
}
