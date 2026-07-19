using System;
using UnityEngine;

internal static class SO_SingletonRuntimeReset
{
    internal static event Action Resetting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        Resetting?.Invoke();
    }

    internal static void ResetAllForTests()
    {
        ResetAll();
    }
}

/// <summary>
/// ScriptableObject 싱글톤 베이스 클래스.
/// Resources/SO/{클래스명} 경로에서 자동 로드됩니다 (Unity가 모든 Resources/ 폴더를 가상 통합).
/// 등록된 RuntimeSingleton 설정은 Assets/_Data/_{Owner}/Resources/SO/{클래스명}.asset을 사용합니다.
/// 기존 직접 사용 에셋은 어느 Resources/SO 폴더에 있어도 같은 경로로 로드됩니다.
/// </summary>
public class SO_Singleton<T> : ScriptableObject where T : SO_Singleton<T>
{
    private static T _instance;
    private static bool _isResolved;
    private static Func<string, T> _loader = Resources.Load<T>;

    static SO_Singleton()
    {
        SO_SingletonRuntimeReset.Resetting += ResetState;
    }

    public static T Instance
    {
        get
        {
            if (!_isResolved)
            {
                _instance = _loader($"SO/{typeof(T).Name}");
                _isResolved = true;
            }

            return _instance;
        }
    }

    private static void ResetState()
    {
        _instance = null;
        _isResolved = false;
    }

    internal static void SetLoaderForTests(Func<string, T> loader)
    {
        _loader = loader ?? Resources.Load<T>;
        ResetState();
    }

    internal static void RestoreLoaderForTests()
    {
        _loader = Resources.Load<T>;
        ResetState();
    }
}
