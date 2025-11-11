using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Collection of utility methods for common game development tasks.
    /// Provides helper functions for timing, coroutines, transforms, and more.
    /// </summary>
    public static class GameUtilities
    {
        #region Timing Utilities

        /// <summary>
        /// Invokes an action after a specified delay using scaled time.
        /// </summary>
        /// <param name="monoBehaviour">MonoBehaviour to run the coroutine on</param>
        /// <param name="action">Action to invoke</param>
        /// <param name="delay">Delay in seconds</param>
        /// <returns>Coroutine instance that can be stopped</returns>
        public static Coroutine DelayedCall(this MonoBehaviour monoBehaviour, Action action, float delay)
        {
            return monoBehaviour.StartCoroutine(DelayedCallCoroutine(action, delay, false));
        }

        /// <summary>
        /// Invokes an action after a specified delay using unscaled time (not affected by Time.timeScale).
        /// </summary>
        /// <param name="monoBehaviour">MonoBehaviour to run the coroutine on</param>
        /// <param name="action">Action to invoke</param>
        /// <param name="delay">Delay in seconds</param>
        /// <returns>Coroutine instance that can be stopped</returns>
        public static Coroutine DelayedCallUnscaled(this MonoBehaviour monoBehaviour, Action action, float delay)
        {
            return monoBehaviour.StartCoroutine(DelayedCallCoroutine(action, delay, true));
        }

        private static IEnumerator DelayedCallCoroutine(Action action, float delay, bool useUnscaledTime)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }

            action?.Invoke();
        }

        /// <summary>
        /// Interpolates a value over time using a specified easing function.
        /// </summary>
        public static IEnumerator Interpolate(float duration, Action<float> onUpdate, Action onComplete = null, Func<float, float> easingFunction = null)
        {
            float elapsed = 0f;
            easingFunction ??= Easing.Linear;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(easingFunction(t));
                yield return null;
            }

            onUpdate?.Invoke(1f);
            onComplete?.Invoke();
        }

        #endregion

        #region Transform Utilities

        /// <summary>
        /// Destroys all children of a transform.
        /// </summary>
        public static void DestroyChildren(this Transform transform, bool immediate = false)
        {
            if (transform == null)
            {
                Debug.LogWarning("[GameUtilities] Cannot destroy children of null transform");
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (immediate)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Resets a transform to default values (position, rotation, scale).
        /// </summary>
        public static void Reset(this Transform transform)
        {
            if (transform == null)
            {
                Debug.LogWarning("[GameUtilities] Cannot reset null transform");
                return;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Gets all children of a transform as a list.
        /// </summary>
        public static List<Transform> GetChildren(this Transform transform)
        {
            List<Transform> children = new List<Transform>();
            if (transform == null) return children;

            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            return children;
        }

        #endregion

        #region Collection Utilities

        /// <summary>
        /// Returns a random element from a list.
        /// </summary>
        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("[GameUtilities] Cannot get random element from null or empty list");
                return default;
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Returns a random element from an array.
        /// </summary>
        public static T GetRandom<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
            {
                Debug.LogWarning("[GameUtilities] Cannot get random element from null or empty array");
                return default;
            }

            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Shuffles a list in place using Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this List<T> list)
        {
            if (list == null || list.Count <= 1) return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        #endregion

        #region Math Utilities

        /// <summary>
        /// Remaps a value from one range to another.
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        /// <summary>
        /// Clamps an angle to the range -180 to 180.
        /// </summary>
        public static float ClampAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        #endregion

        #region String Utilities

        /// <summary>
        /// Formats a time value in seconds to MM:SS format.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        /// <summary>
        /// Formats a large number with K, M, B suffixes.
        /// </summary>
        public static string FormatNumber(long number)
        {
            if (number >= 1000000000)
                return (number / 1000000000f).ToString("0.#") + "B";
            if (number >= 1000000)
                return (number / 1000000f).ToString("0.#") + "M";
            if (number >= 1000)
                return (number / 1000f).ToString("0.#") + "K";
            return number.ToString();
        }

        #endregion

        #region Layer Utilities

        /// <summary>
        /// Checks if a GameObject is in the specified layer.
        /// </summary>
        public static bool IsInLayer(GameObject gameObject, LayerMask layerMask)
        {
            return ((1 << gameObject.layer) & layerMask) != 0;
        }

        /// <summary>
        /// Sets the layer for a GameObject and all its children.
        /// </summary>
        public static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (gameObject == null) return;

            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Common easing functions for smooth animations.
    /// </summary>
    public static class Easing
    {
        public static float Linear(float t) => t;

        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => t * (2 - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t) => (--t) * t * t + 1;
        public static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;

        public static float EaseInQuart(float t) => t * t * t * t;
        public static float EaseOutQuart(float t) => 1 - (--t) * t * t * t;
        public static float EaseInOutQuart(float t) => t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;

        public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);
        public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);
        public static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

        public static float EaseInExpo(float t) => t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
        public static float EaseOutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        public static float EaseInOutExpo(float t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
        }

        public static float EaseInBack(float t) => t * t * (2.70158f * t - 1.70158f);
        public static float EaseOutBack(float t) => 1 + (--t) * t * (2.70158f * t + 1.70158f);
        public static float EaseInOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
                : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }

        public static float EaseOutElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }
    }
}
