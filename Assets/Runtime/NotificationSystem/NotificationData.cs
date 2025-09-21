using System;
using UnityEngine;

namespace BB.Framework
{

    [Serializable]
    public class NotificationData
    {
        public string message;
        public NotificationType type;

        public NotificationData(string message, NotificationType type)
        {
            this.message = message;
            this.type = type;
        }
    }
}