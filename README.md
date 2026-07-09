# SO Singleton (com.actionfit.sosingleton)

`Resources/SO/{타입명}` 에서 자동 로드되는 ScriptableObject 싱글톤 베이스 클래스입니다.

```csharp
public class MySettings_SO : SO_Singleton<MySettings_SO>
{
    [SerializeField] private int value;
    public int Value => value;
}

// 사용
var v = MySettings_SO.Instance.Value; // Resources.Load<MySettings_SO>("SO/MySettings_SO")
```

- 에셋은 어딘가의 `Resources/SO/{클래스명}.asset` 에 두면 됩니다(Unity가 모든 Resources 폴더를 가상 통합).
- global namespace로 제공되어 기존 `SO_Singleton<T>` 사용 코드와 호환됩니다.

## 설치 (manifest.json)

```json
"com.actionfit.sosingleton": "https://github.com/ActionFit-Editor/SO_Singleton.git#1.0.3"
```

## Unity Menu

- Package root: `Tools > Package > SO Singleton`.
- README: `Tools > Package > SO Singleton > README`.
- Package commands stay under the same package root and appear above the separated README/Setting SO entries when those entries exist.
