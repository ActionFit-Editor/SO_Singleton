using UnityEngine;

/// <summary>
/// ScriptableObject 싱글톤 베이스 클래스.
/// Resources/SO/{클래스명} 경로에서 자동 로드됩니다 (Unity가 모든 Resources/ 폴더를 가상 통합).
/// 에셋 위치 규칙: Assets/_Project/{Core|Content|_Common|_Shared|_Infra}/{콘텐츠}/Resources/SO/{클래스명}.asset
/// 또는 콘텐츠 매칭 어려운 경우: Assets/_Data/Resources/SO/{클래스명}.asset
/// </summary>
public class SO_Singleton<T> : ScriptableObject where T : SO_Singleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<T>($"SO/{typeof(T).Name}");
            return _instance;
        }
    }
}
