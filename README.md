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

## 패키지 설정 SO 수명주기

설정 타입에 `ActionFitSettingsAssetAttribute`를 붙이면 공통 provider가 canonical 경로, 등록된 legacy 경로, 프로젝트 안의 단일 타입 에셋 순서로 재사용 대상을 찾고, 없을 때만 `Assets/_Data/_<Owner>/` 아래에 생성합니다. 중복 에셋, 잘못된 등록, 다른 타입이 차지한 경로는 자동 생성하지 않고 audit 오류로 남깁니다.

런타임 설정은 `SO_Singleton<자기타입>`을 상속하고 `Assets/_Data/_<Owner>/Resources/SO/<Type>.asset`에 생성됩니다. `Instance`는 에셋을 찾은 결과뿐 아니라 찾지 못한 결과도 현재 subsystem 동안 캐시하며, Domain Reload가 꺼져 있어도 `SubsystemRegistration`에서 초기화됩니다.

등록 타입과 프로젝트 SO 타입 경로는 캐시하며, 시작 검사와 audit은 기존 에셋을 로드·재직렬화하지 않습니다. 기존 에셋은 이동·이름 변경·재직렬화하지 않고, `IActionFitSettingsAssetInitializer`는 처음 생성되는 에셋에만 안전한 기본값을 채울 때 사용합니다.

## Agent Skill 안내

Custom Package Manager의 `Install or Refresh Agent Skills`를 실행하면 Codex와 Claude에 read-only `so-singleton-help`가 설치됩니다.

help는 `Resources/SO/{타입명}` 로딩, present-or-missing result cache, global API와 asset 배치 규칙을 설명합니다. ScriptableObject를 검색·생성·이동·이름 변경하거나 serialized value를 수정하지 않는 help-only 진입점입니다.

## 설치 (manifest.json)

```json
"com.actionfit.sosingleton": "https://github.com/ActionFit-Editor/SO_Singleton.git#1.0.6"
```

## Unity 메뉴

- Package root: `Tools > Package > SO Singleton`.
- README: `Tools > Package > SO Singleton > README`.
- 등록 설정 검사: `Tools > Package > SO Singleton > Audit Settings SO`.
- editor 탐색 캐시 초기화: `Tools > Package > SO Singleton > Clear Settings Cache`.
- 패키지 명령은 같은 package root 아래에 유지하며 README/Setting SO 항목이 있으면 분리된 해당 항목보다 위에 표시합니다.
