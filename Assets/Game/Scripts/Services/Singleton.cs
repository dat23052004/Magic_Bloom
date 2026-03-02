using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T m_Instance;
    private static bool isShuttingDown = false;
    public static bool IsShuttingDown => isShuttingDown;
    public static T Ins
    {
        get
        {
            // ✅ Tránh tạo instance mới khi game đang tắt
            if (isShuttingDown)
            {
                return null;
            }

            if (m_Instance == null)
            {
                m_Instance = FindAnyObjectByType<T>();

                if (m_Instance == null)
                {
                    // Không tạo mới nếu đang quit
                    if (!Application.isPlaying) return null;
                    GameObject obj = new GameObject(typeof(T).Name);
                    m_Instance = obj.AddComponent<T>();
                }
            }

            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        isShuttingDown = false;
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