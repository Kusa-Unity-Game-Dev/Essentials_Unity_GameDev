using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager s_Instance { get; private set; }

    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isDisplaying = false;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            s_Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
    }

    public void ShowNotification(NotificationData data)
    {
        notificationQueue.Enqueue(data);

        if (!isDisplaying)
        {
            DisplayNextNotification();
        }
    }

    private void DisplayNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isDisplaying = false;
            return;
        }

        isDisplaying = true;
        NotificationData data = notificationQueue.Dequeue();
        NotificationUI.ShowUI_s(data, ()=> DisplayNextNotification());

    }

}
