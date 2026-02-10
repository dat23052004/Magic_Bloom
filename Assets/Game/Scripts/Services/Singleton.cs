using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T m_Instance;
    private static bool isShuttingDown = false;
    public static T Ins
    {
        get
        {
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

    protected void Awake()
    {
        if (m_Instance == null)
        {
            m_Instance = this as T;
            OnInit();
        }
        else if (m_Instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnInit() { }
}
