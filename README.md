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

## Agent Skill 안내

Custom Package Manager의 `Install or Refresh Agent Skills`를 실행하면 Codex와 Claude에 read-only `so-singleton-help`가 설치됩니다.

help는 `Resources/SO/{타입명}` 로딩, non-null instance cache, global API와 asset 배치 규칙을 설명합니다. ScriptableObject를 검색·생성·이동·이름 변경하거나 serialized value를 수정하지 않는 help-only 진입점입니다.

## 설치 (manifest.json)

```json
"com.actionfit.sosingleton": "https://github.com/ActionFit-Editor/SO_Singleton.git#1.0.5"
```

## Unity 메뉴

- Package root: `Tools > Package > SO Singleton`.
- README: `Tools > Package > SO Singleton > README`.
- 패키지 명령은 같은 package root 아래에 유지하며 README/Setting SO 항목이 있으면 분리된 해당 항목보다 위에 표시합니다.
