using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T m_Instance;
    private static bool isShuttingDown = false;

    public static T Ins
    {
        get
        {
            // ✅ Tránh tạo instance mới khi game đang tắt
            if (isShuttingDown)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} already destroyed. Returning null.");
                return null;
            }

            if (m_Instance == null)
            {
                m_Instance = FindAnyObjectByType<T>();

                if (m_Instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    m_Instance = obj.AddComponent<T>();
                }
            }

            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        if (m_Instance == null)
        {
            m_Instance = this as T;
            OnInit();
        }
        else if (m_Instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} detected! Destroying...");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    protected virtual void OnDestroy()
    {
        if (m_Instance == this)
        {
            m_Instance = null;
        }
    }

    protected virtual void OnInit() { }
}