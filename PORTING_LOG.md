# POE1 → POE2 포팅 작업 로그

## 프로젝트 정보
- **원본**: phiDelPark의 PoeTradeSearch (POE1용)
- **라이선스**: GPL 3.0 (소스 공개, 동일 라이선스 유지, 원저작자 표시 필수)
- **포팅 목표**: POE2 한국 거래소 전용, 경량 유지, .NET Framework 4.8
- **프로젝트명**: Poe2TradeSearch
- **회사**: POE2TOOLS
- **웹사이트**: https://poe2tools.net/
- **현재 버전**: 0.5.0.0

---

## 빌드 방법

### 빌드 명령
```
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" PoeTradeSearch.sln /p:Configuration=Release /p:Platform="Any CPU"
```

### 주요 경로
| 경로 | 설명 |
|------|------|
| `PoeTradeSearch.sln` | 솔루션 파일 (프로젝트 루트) |
| `_POE_Data\` | 데이터 파일 원본 (빌드 시 자동 복사됨) |
| `bin\Release\PoeTradeSearch.exe` | 실행 파일 |
| `bin\Release\PoeTradeSearch.log` | 런타임 에러 로그 (예외 발생 시 생성) |
| `bin\Release\` (데이터파일) | PostBuildEvent로 `_POE_Data\*` 자동 복사 |

### 주의사항
- 빌드 전 앱이 실행 중이면 `.exe` 잠금으로 빌드 실패 → **반드시 앱 종료 후 빌드**
- 데이터 파일은 PostBuildEvent(`xcopy /Y /E`)로 자동 복사되므로 별도 작업 불필요
- 실행 시 관리자 권한 필요 (핫키 등록)

---

## 데이터 파일 (`_POE_Data\`)

| 파일 | 내용 | 참조 |
|------|------|------|
| `Config.txt` | JSON 사용자 설정 (리그, 서버, 단축키 등) | `Configure.cs` `LoadData()` |
| `Parser.txt` | 아이템 텍스트 파싱 정의 (희귀도, 카테고리, 소켓 등) | `Methods.cs` `ItemTextParser()` |
| `FiltersKO.txt` | POE2 한글 stat 필터 데이터 | `Methods.cs` `mFilterData[0]` |
| `FiltersEN.txt` | POE2 영문 stat 필터 데이터 | `Methods.cs` `mFilterData[1]` (현재 미사용) |
| `ItemsKO.txt` | POE2 한글 아이템 카테고리 | `Methods.cs` `mItemsData[0]` |
| `StaticKO.txt` | POE2 한글 통화/static 데이터 | `Methods.cs` `mStaticData[0]` |

> FiltersKO.txt 삭제 시 다음 실행 때 POE2 API에서 자동 재다운로드됨.

---

## 소스 파일 구조

| 파일 | 역할 |
|------|------|
| `Configure.cs` | `RS` 정적 클래스 — URL, stat ID 딕셔너리 (`lFilterType`, `lDefaultPosition`, `lDisable`, `lParticular`, `lResistance`, `lPseudo`) |
| `Contracts.cs` | `[DataContract]` 모델 클래스 (`ItemOption`, `ConfigData`, `PoeData` 등) |
| `Functions.cs` | P/Invoke 선언, HTTP/JSON/클립보드 헬퍼 |
| `Methods.cs` | UI 리셋, 아이템 파싱(`ItemTextParser`), 검색 JSON 생성(`CreateJson`), 단축키 처리(`WndProc`) |
| `Updates.cs` | 필터/아이템/스태틱 데이터 자동 다운로드 (`FilterDataUpdate` 등) |
| `WinMain.xaml.cs` | 윈도우 라이프사이클, 핫키 등록, 클립보드 체인 훅 |
| `WinPopup.xaml.cs` | 시세 결과 팝업 |
| `App.xaml.cs` | 트레이 아이콘, 앱 진입점 |

---

## 완료된 작업 전체 이력

### 1. Methods.cs — 파서 수정
**위치**: `ItemTextParser` 내부 루프

**변경 1**: POE2 전용 `{ 접두어 속성 부여 ... }` 줄 스킵
```csharp
if (asOpt[j].Trim().StartsWith("{")) continue;
```

**변경 2**: POE2 수치 범위 표기 제거 (`35(29-44)~51(50-75)` → `35~51`)
```csharp
input = Regex.Replace(input, @"\([0-9]+\.[0-9]+-[0-9]+\.[0-9]+\)|\([0-9]+-[0-9]+\)", "");
```

---

### 2. Configure.cs — API URL 변경
```csharp
TradeUrl    = "https://poe.game.daum.net/trade2/search/poe2/"    // KO (index 0)
TradeApi    = "https://poe.game.daum.net/api/trade2/search/poe2/"
FetchApi    = "https://poe.game.daum.net/api/trade2/fetch/"
ExchangeUrl = "https://poe.game.daum.net/trade2/exchange/poe2/"
ExchangeApi = "https://poe.game.daum.net/api/trade2/exchange/poe2/"
PoeCaption  = "Path of Exile 2"
```

---

### 3. Updates.cs — 데이터 업데이트 URL 변경
```
api/trade/data/stats  → api/trade2/data/stats
api/trade/data/items  → api/trade2/data/items
api/trade/data/static → api/trade2/data/static
```

---

### 4. _POE_Data 파일 교체 (POE2 데이터로)
| 파일 | 교체 내용 |
|------|----------|
| `FiltersKO.txt` | POE2 한글 stat (`trade2/data/stats` KO 응답) |
| `FiltersEN.txt` | POE2 영문 stat (`trade2/data/stats` EN 응답) |
| `ItemsKO.txt` / `ItemsEN.txt` | POE2 아이템 카테고리 |
| `StaticKO.txt` / `StaticEN.txt` | POE2 통화 데이터 |
| `Parser.txt` | `build_parser.py`로 POE2 기준 재생성 |

---

### 5. PoeTradeSearch.csproj — 타겟 프레임워크 업그레이드
```
v4.6 → v4.8
```

---

### 6. WinGrid.xaml / WinGrid.xaml.cs 신규 생성
원본에 누락된 파일. 빈 껍데기로 생성 (POE2 미지원 기능, 참조 오류 방지용)

---

### 7. Config.txt 기본값 수정
```json
"league": "Runes of Aldur",
"server": "ko"
```

---

### 8. API 400 에러 — POE1 전용 필드 제거 (`Contracts.cs`)
POE2 API는 아래 필드를 포함하면 400 반환:
- `sale_type` (trade_filters)
- `gem_alternate_quality`, `shaper_item`, `elder_item`, `crusader_item`, `redeemer_item`, `hunter_item`, `warlord_item`, `synthesised_item` (misc_filters)
- `map_shaped`, `map_elder`, `map_blighted` (map_filters)

`Contracts.cs`에서 해당 필드 삭제, `Methods.cs` CreateJson에서 관련 대입 코드 삭제.

---

### 9. API 400 에러 — stats 빈 배열
POE2는 `"stats":[]` 거부. `[{"type":"and","filters":[]}]` 필수.
```csharp
JQ.Stats = new q_Stats[1] { new q_Stats { Type = "and", Filters = new q_Stats_filters[0] } };
```

---

### 10. API 400 에러 — 빈 category 필드
`"category":{"option":""}` 거부. 빈 경우 필드 자체 생략 필요.
- `Contracts.cs`: `Category`에 `EmitDefaultValue=false`, 기본값 `null`
- `Methods.cs`: category 빈 문자열이면 null 유지

---

### 11. API 400 에러 — disabled 필터 블록
POE2는 disabled 필터 블록 포함 시 400.
- `Contracts.cs` `q_Filters`: 모든 필드 `EmitDefaultValue=false`, 기본값 `null`
- `Methods.cs` CreateJson: `Socket`, `Misc`, `Map`은 조건부 생성, `Weapon/Armour/Req`는 항상 null

---

### 12. lFilterType 레이블 POE2 맞춤 수정 (`Configure.cs`)
POE2 FiltersKO.txt의 result label이 POE1과 다름 → ComboBox 매칭 실패 → 옵션 미표시.
```
explicit   : "일반"   → "비고정"
fractured  : "분열"   → "분열된"
enchant    : "인챈"   → "인챈트"
crafted    : "제작"   → "제작된"
추가: rune="증강물", desecrated="훼손된", sanctum="성역", skill="스킬"
```

---

### 13. 클립보드 줄바꿈 \n 처리 (`Methods.cs`) ← 핵심 버그
**원인**: POE2 클립보드는 `\n`만 사용. 기존 코드는 `\r\n`으로만 Split.

**수정**: ItemTextParser 진입 시 `\r\n` → `\n` 정규화, 모든 Split에 `\n` 추가.

---

### 14. lParticular 기반 로컬(특정) stat 선택 (`Methods.cs`)
**원인**: POE2 FiltersKO.txt에 `part` 필드 없음. 기존 `x.Part == cate_ids[0]` 항상 실패.

**수정**: `lParticular` 딕셔너리 기반으로 로컬 stat 판별. value=1(무기), value=2(방어구).

---

### 15. 룬(rune) 효과 줄 무시 (`Methods.cs`)
**원인**: POE2 아이템에 `화염 저항 +18% (rune)` 줄이 존재. 파싱 시 크래시 또는 잘못된 매칭.

**수정**:
```csharp
if (line.IndexOf("(rune)") > -1) continue;   // foreach 스캔 루프
if (asOpt[j].IndexOf("(rune)") > -1) continue; // for j 루프
```

---

### 16. CreateJson NullReferenceException 수정 (`Methods.cs`)
**원인**: 희귀 방어구 등 category가 없는 아이템에서 `JQ.Filters.Type.Filters.Category`가 null.

**수정**:
```csharp
else if (JQ.Filters.Type?.Filters?.Category?.Option == "monster.sample")
```

---

### 17. 빌드 시 데이터 파일 자동 복사 (`PoeTradeSearch.csproj`)
```xml
<PostBuildEvent>xcopy /Y /E "$(ProjectDir)_POE_Data\*" "$(TargetDir)"</PostBuildEvent>
```

---

### 18. POE2에 없는 UI 요소 숨김 (`WinMain.xaml`)
```xml
Synthesis   Visibility="Collapsed"   <!-- 결합 -->
cbInfluence1 Visibility="Collapsed"  <!-- 영향력 1 -->
cbInfluence2 Visibility="Collapsed"  <!-- 영향력 2 -->
```

---

### 19. CreateJson filterResult null 체크 (`Methods.cs`)
**원인**: `mFilterData`의 Result에 해당 레이블이 없으면 `filterResult`가 null → NullReferenceException.

**수정**: `if (filterResult == null) continue;`

---

### 20. matches1 인덱스 초과 방어 (`Methods.cs`)
**원인**: 필터 텍스트에 고정 숫자 포함 시 `matches2.Count > matches1.Count`가 되어 인덱스 초과.

**수정**:
```csharp
else if (t >= matches1.Count || matches1[t].Value != matches2[t].Value)
```

---

### 21. lPseudo POE2 전용으로 정리 (`Configure.cs`)
**원인**: POE1 기준 pseudo stat으로 변환 시도 → POE2에 없어 빨간색 에러 표시.

**POE2에 존재하는 pseudo만 유지** (12개):
```
stat_4220027924 → pseudo_total_cold_resistance
stat_3372524247 → pseudo_total_fire_resistance
stat_1671376347 → pseudo_total_lightning_resistance
stat_2923486259 → pseudo_total_chaos_resistance
stat_3299347043 → pseudo_total_life
stat_1050105434 → pseudo_total_mana
stat_3489782002 → pseudo_total_energy_shield
stat_2482852589 → pseudo_increased_energy_shield
stat_4080418644 → pseudo_total_strength
stat_3261801346 → pseudo_total_dexterity
stat_328541901  → pseudo_total_intelligence
stat_2250533757 → pseudo_increased_movement_speed
```

---

### 22. 백그라운드 스레드 에러 로깅 (`Methods.cs`)
**배경**: 백그라운드 스레드 예외는 `AppDispatcherUnhandledException`이 잡지 못해 무소음 종료.

**수정**: `priceThread`, `CreateJson`, `WndProc`, `ItemTextParser` catch 블록에 파일 로깅 추가.

로그 파일: `bin\Release\PoeTradeSearch.log`

---

### 23. 리브랜딩 (`AssemblyInfo.cs`)
```csharp
AssemblyTitle   = "POE2 거래소 검색"
AssemblyCompany = "POE2TOOLS"
AssemblyProduct = "Poe2TradeSearch"
AssemblyCopyright = "Copyright © 2019 phiDel | 2026 POE2TOOLS (https://poe2tools.net/)"
AssemblyVersion = "0.5.0.0"
```

---

### 24. 한국 서버 단일화 (`WinMain.xaml.cs`, `Methods.cs`)
**제거**: 서버 자동 감지 로직, 영어 서버 전환 UI
**유지**: `RS.ServerLang = 0` 하드코딩 (한국 서버 고정)

---

### 25. 자동 업데이트 기능 제거 (`Updates.cs`, `WinMain.xaml.cs`)
**제거**: `CheckUpdates()`, `PoeExeUpdates()`, 시작 시 업데이트 확인 블록

**유지**: `FilterDataUpdate()`, `ItemDataUpdate()`, `StaticDataUpdate()` — 리그 변경 시 데이터 갱신용으로 보존

---

### 26. 단축키 액션 로직 정리 (`Methods.cs`)
**제거**: `{Enter}` 채팅 명령어, `{Link}` URL 열기, `{Wiki}` 위키, `{Grid:Stash/Quad}` 창고 그리드, `.jpg` 이미지 출력

**유지**: `{Pause}` 일시중지, `{Close}` 창 닫기, `{Run}` 단축키 변경용 트리거

---

### 27. lDefaultPosition / lDisable POE2 정리 (`Configure.cs`)
**조사**: FiltersKO.txt 대조 결과:
- `lDefaultPosition` 25개 중 21개가 POE2에 없는 stat → 딕셔너리 전체 비움
- `lDisable` 6개 중 4개 누락 → POE2에 존재하는 2개만 유지

```csharp
lDefaultPosition = new Dictionary<string, bool>();  // 빈 딕셔너리

lDisable = new Dictionary<string, bool>()
{
    { "stat_57434274", true },   // 경험치 획득 #% 증가
    { "stat_3666934677", true }  // 경험치 획득 #% 증가 (중복)
};
```

---

### 28. "변경이 불가능한 값" 접미사 제거 (`Methods.cs`)
**원인**: POE2 일부 고유 옵션에 `— 변경이 불가능한 값` 접미사가 붙어 FiltersKO.txt 텍스트와 불일치 → 매칭 실패.

예: `최대 권능 충전에 도달 시 모든 권능 충전을 상실 — 변경이 불가능한 값`
→ FiltersKO.txt에는 `최대 권능 충전에 도달 시 모든 권능 충전을 상실`

**수정**: `optLine` 변수 도입, stat 줄 파싱 전 접미사 제거:
```csharp
string optLine = Regex.Replace(asOpt[j], @"\s*—\s*변경이 불가능한 값$", "");
```
이후 `asOpt[j]` 대신 `optLine` 사용 (input 생성, matches1 추출 등).

---

### 29. 로컬(특정) stat 카테고리 기반 자동 교체 (`Methods.cs`)
**원인**: 클립보드 텍스트에는 `(특정)` 접미사가 없어 글로벌 버전만 1개 매칭됨.
예: `회피 57% 증가` → `회피 #% 증가`(글로벌)만 매칭, `회피 #% 증가(특정)`(로컬)은 미매칭.

**수정**: 1개 매칭 시, 글로벌 stat이고 같은 그룹에 `텍스트+(특정)` 버전이 `lParticular`에 등록되어 있으면 카테고리 조건에 맞게 교체:

```csharp
// lParticular value=1(weapon) → 무기일 때만 (특정) 교체
// lParticular value=2(armour) → 무기가 아닐 때 (투구 포함) (특정) 교체
return (partVal == 1 && cate_ids[0] == "weapon") || (partVal == 2 && cate_ids[0] != "weapon");
```

**category 매칭 수정**: `item_category`("투구") 대신 `item_type`(베이스명 "은밀한 두건")으로 Parser.txt category 매칭:
```csharp
ParserDictionary category = Array.Find(PS.Category.Entries, x => x.Text[z] == item_type)
                         ?? Array.Find(PS.Category.Entries, x => x.Text[z] == item_name);
```

---

### 30. 타락 여부 자동 설정 (`Methods.cs`)
**동작**: 클립보드 아이템 파싱 후 타락 여부에 따라 `cbCorrupt` 자동 선택.
- 타락 감지 → `cbCorrupt` 강조 표시 (기존 동작 유지)
- 타락 없음 → `cbCorrupt.SelectedIndex = 2` ("아니오") 자동 설정 (신규)

```csharp
if (lItemOption[PS.Corrupted.Text[z]] == "_TRUE_")
{
    cbCorrupt.BorderThickness = new Thickness(2);
    cbCorrupt.FontWeight = FontWeights.Bold;
    cbCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
}
else
{
    cbCorrupt.SelectedIndex = 2; // 타락 없음 → "아니오"
}
```

---

### 31. 희귀 아이템 빈 접두어/접미어 자동 추가 (`Methods.cs`)
**배경**: POE2 희귀 아이템은 접두어 최대 3개, 접미어 최대 3개. 거래소에서 `# 빈 접두어/접미어 속성 부여` 필터로 크래프팅 가능 아이템 검색 가능.

**stat ID**:
```
pseudo.pseudo_number_of_empty_prefix_mods  → # 빈 접두어 속성 부여
pseudo.pseudo_number_of_empty_suffix_mods  → # 빈 접미어 속성 부여
```

**로직**: 희귀 아이템 파싱 완료 후, `{ 접두어 속성 부여 }` / `{ 접미어 속성 부여 }` 줄 카운트:
```csharp
if (line.Trim().StartsWith("{ 접두어")) prefixCount++;
else if (line.Trim().StartsWith("{ 접미어")) suffixCount++;
```
빈 슬롯 계산: `emptyPrefix = 3 - prefixCount`, `emptySuffix = 3 - suffixCount`

빈 슬롯이 1개 이상이면 옵션 UI에 자동 추가 (최솟값 = 빈 슬롯 수, 체크 해제 상태).

---

### 32. 소켓 → "홈" UI 변경 (`Parser.txt`, `WinMain.xaml`, `Methods.cs`)
**배경**: POE2는 링크 개념 없음. 소켓(홈) 개수만 존재. 클립보드 표기는 `홈: S S` 형식.

**Parser.txt**: `sockets.text[0]` `"소켓"` → `"홈"`

**WinMain.xaml**: 소켓 섹션을 레벨/퀄리티와 같은 형태로 재구성.
- `ckSocket` Content `"소켓"` → `"홈"`, `tbSocketMin/Max` 유지
- 링크 관련 UI 제거: `&` 레이블, `tbLinksMin/Max`(Collapsed 처리, 코드 참조 오류 방지용 유지), `lbSocketBackground` 제거
- `cbCorrupt` 위에 별도 `"타락"` Label 추가 (드롭박스 라벨 잘림 문제 해결), 항목 `"타락"` → `"전체"`로 변경

**Methods.cs**: 소켓 파싱을 POE2 방식으로 교체.
```csharp
int sckcnt = Regex.Matches(socket, @"\bS\b").Count;
if (sckcnt == 0) sckcnt = socket.Trim().Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Length;
tbSocketMin.Text = sckcnt.ToString();
ckSocket.IsChecked = sckcnt > 0;   // 홈 있으면 자동 체크 + 최솟값 설정
```
또한 `lbSocketBackground` 참조 코드 2곳 제거.

---

### 33. Release 데이터 파일 경로 버그 수정 (`Configure.cs`) ← 중요
**증상**: Parser.txt를 `"홈"`으로 수정·빌드·복사했는데도 앱이 여전히 `"소켓"`으로 인식.
디버그 로그 결과 `key=소켓 val=`(빈 값) → 앱이 **수정 전 Parser.txt를 읽고 있었음**.

**원인**: Release 빌드의 데이터 경로 계산이 잘못됨.
```csharp
// 변경 전 — ".exe" 4글자만 제거 → bin\Release\PoeTradeSearch\ (존재하지 않는 폴더)
string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
path = path.Remove(path.Length - 4) + "\\";
```
`PoeTradeSearch.exe` → `PoeTradeSearch\` 로 잘못 해석. 실제 데이터는 `bin\Release\`에 있음.

**수정**: exe가 있는 디렉터리를 직접 사용.
```csharp
string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
```
`Setting()`, `LoadData()` 두 곳 모두 수정.

> **교훈**: 데이터 파일 수정이 반영 안 될 때, 앱이 읽는 실제 경로부터 의심할 것.

---

### 34. 데이터 다운로드 조건 버그 수정 (`Configure.cs`)
**증상**: 경로 버그(#33) 수정 후 `ItemsKO.txt 파일을 찾을 수 없음` 에러.

**원인**: `LoadData()`의 다운로드 조건이 `FiltersKO/EN` 존재 여부만 체크.
`_POE_Data\`(및 `bin\Release\`)에 Filters는 있지만 Items/Static이 없는 상태에서:
- 다운로드 조건 `!FiltersKO || !FiltersEN` = false → 다운로드 건너뜀
- 이후 존재하지 않는 `ItemsKO.txt` 열기 시도 → 예외

(이전엔 경로 버그로 Filters조차 "없음" 판정되어 매번 전체 다운로드 → 문제 가려져 있었음)

**수정**: 6개 파일(`FiltersKO/EN`, `ItemsKO/EN`, `StaticKO/EN`) 중 **하나라도 없으면** 다운로드.
```csharp
string[] required = { "FiltersKO", "FiltersEN", "ItemsKO", "ItemsEN", "StaticKO", "StaticEN" };
bool needDownload = false;
foreach (string item in required)
    if (!File.Exists(path + item + ".txt")) { needDownload = true; break; }
if (needDownload) { /* FilterDataUpdate + ItemDataUpdate + StaticDataUpdate */ }
```

> **주의**: 다운로드는 `bin\Release\`에만 생성됨. 누락 파일은 POE2 API에서 자동 수신.
> Parser.txt는 다운로드 대상이 아니므로 우리가 수정한 `"홈"` 등은 안전하게 유지됨.

---

## 4차 세션 작업 이력 (2026-06-09)

### 35. 프로젝트 파일 정리
- `.tmp.*` 임시 파일 전체 삭제 (Methods, csproj, PORTING_LOG, WinMain, Config 등)
- raw JSON 파일 삭제 (`stats.json`, `stats (1).json`, `items.json`, `static.json`)
- 구 자동업데이트 잔재 삭제 (`VERSION`, `VERSIONS`, `_POE_EXE.zip`, `bin\Release\_POE_EXE.zip`)
- `PoeTradeSearch.csproj.user` 삭제
- `bin\Release\PoeTradeSearch\` 폴더 (경로 버그 #33 잔재) — 앱 실행 중으로 미삭제, 수동 삭제 필요

---

### 36. 프로젝트 전체 리네이밍 PoeTradeSearch → Poe2TradeSearch
- 모든 `.cs`, `.xaml` 파일 네임스페이스 일괄 치환
- `PoeTradeSearch.csproj` → `Poe2TradeSearch.csproj`
- `PoeTradeSearch.sln` → `Poe2TradeSearch.sln`
- `<AssemblyName>`, `<RootNamespace>` 모두 `Poe2TradeSearch`
- 출력: `bin\Release\Poe2TradeSearch.exe`
- 단, `WinMain.xaml.cs`의 원작 URL이 오치환됨 → 즉시 수정 (`phiDelPark/PoeTradeSearch`로 복원)

---

### 37. phiDel 서명 잔재 제거 (`Poe2TradeSearch.csproj`)
- `ManifestCertificateThumbprint` PropertyGroup 제거
- `ManifestKeyFile` (phiDelSign.pfx) PropertyGroup 제거
- `SignAssembly=false`, `SignManifests=false`는 이미 설정되어 있었음 (서명은 원래도 비활성)

---

### 38. 데이터 파일 `data\` 서브폴더로 분리
- PostBuildEvent: `$(TargetDir)` → `$(TargetDir)data\`
- `Configure.cs` `Setting()`, `LoadData()` Release 경로에 `data\\` 추가
- 결과: `bin\Release\data\`에 모든 txt 파일 저장

---

### 39. Config.txt 정리
- 삭제된 기능의 shortcuts 제거 (`{Enter}`, `{grid:stash/quad}`, `.jpg`, `{Link}`, `{Wiki}`)
- 유지: `{Pause}`, `{Close}`, `{Run}` 3개
- 죽은 options 제거: `server`(한국 고정으로 불필요), `check_updates`(기능 제거됨)
- 잘못된 값 수정: `auto_select_corrupt` `"no or yes"` → `"no"`, `auto_select_bytype` 설명 텍스트 → `""`
- version `"3.14.1d"` → `"0.5.0"`

---

### 40. 시세 통화명 한글화 (`Methods.cs`)
**원인**: `GetExchangeItem()`이 `mParserData`(Parser.txt)만 검색 → 못 찾으면 API 영문 id 그대로 표시.

**수정**: Parser.txt에서 못 찾으면 `mStaticData[0]`(StaticKO.txt) fallback 검색 추가.
```csharp
if (item == null && mStaticData[0]?.Result != null)
{
    foreach (var group in mStaticData[0].Result)
    {
        var entry = Array.Find(group.Entries, x => x.Id == id);
        if (entry != null)
        {
            item = new ParserDictionary { Text = new string[] { entry.Text, entry.Text } };
            break;
        }
    }
}
```

> **미완**: 통화명 축약 표시 (엑잘티드 오브 → 엑잘 등) 요청됨 — 다음 세션 TODO

---

## 현재 상태 (2026-06-09 4차 세션 종료)

### 동작 확인됨
- [x] 클립보드 Ctrl+C → 한글 아이템 파싱
- [x] API 검색 200 OK
- [x] 고유/희귀/마법 아이템 옵션 파싱 및 UI 표시
- [x] 시세 결과 팝업
- [x] 룬 효과 줄 무시
- [x] "변경이 불가능한 값" 옵션 정상 매칭
- [x] 회피/방어도/에너지보호막 로컬(특정) stat 자동 선택
- [x] 정확도 — 투구면 글로벌, 무기면 로컬 자동 구분
- [x] 타락/비타락 자동 설정
- [x] 희귀 아이템 빈 접두어/접미어 자동 추가
- [x] DPS 계산 (무기) 동작 확인
- [x] Release 데이터 경로 버그 수정
- [x] 프로젝트명 Poe2TradeSearch 완전 통일
- [x] 데이터 파일 data\ 폴더로 분리
- [x] 시세 통화명 한글화 (StaticKO.txt fallback) — 빌드 완료, 테스트 미완

### 다음 세션 최우선 확인 (빌드 완료, 테스트 미완)
- [ ] **소켓(홈) 기능** — `홈: S S` 아이템에서 홈 자동체크 + tbSocketMin 개수 + 타락 라벨
- [ ] **시세 통화명 한글 표시** — 엑잘티드 오브, 신성한 오브 등으로 나오는지 확인

### 다음 세션 TODO (우선순위 순)
1. 소켓(홈) + 통화명 한글화 테스트
2. **통화명 축약 표시** — StaticKO.txt `text` 값 수정 (엑잘티드 오브 → 엑잘 등) 또는 코드 축약 테이블
3. **Wiki 버튼** — https://poe2tools.net/ 으로 연결
4. **README.md 재작성** — 우리 프로그램 내용으로 (GitHub 리포 생성 후)
5. **추가 테스트** — 다양한 아이템 타입(반지, 부적 등) 검색 검증
6. **배포 준비** — 릴리즈 패키지 구성

### 배포 패키지 구성 (확정)
- `Poe2TradeSearch.exe`
- `Poe2TradeSearch.exe.config`
- `data\` 폴더 전체 (Config.txt, Parser.txt, FiltersKO/EN.txt — Items/Static은 첫 실행 시 자동 다운로드)

### 거래소 Rate Limit 주의
POE2 거래소 API rate limit 매우 빡빡함. 연속 검색 테스트 시 IP 차단 위험.
`auto_search_delay` 너무 낮게 설정하지 말 것.

---

## POE2 vs POE1 주요 차이점 정리

| 항목 | POE1 | POE2 |
|------|------|------|
| API 경로 | `/api/trade/search/` | `/api/trade2/search/poe2/` |
| stats 빈 배열 | `[]` 허용 | `[{"type":"and","filters":[]}]` 필수 |
| category 빈값 | `{"option":""}` 허용 | 필드 자체 생략 필요 |
| disabled 필터 | 전송 허용 | 포함하면 400 |
| sale_type 필터 | 있음 | 없음 |
| 영향력 필터 | shaper/elder 등 | 없음 |
| 클립보드 줄바꿈 | `\r\n` | `\n` |
| stat label | explicit="일반" | explicit="비고정" |
| 옵션 표기 | `방어도 87% 증가` | `방어도 87(50-100)% 증가` |
| 속성 부여 헤더 | 없음 | `{ 고유 속성 부여 — 방어도 }` |
| 로컬 stat 구분 | entries[].part 필드 | lParticular 딕셔너리로 판별 |
| 합성/영향력 | 있음 | 없음 |
| 룬 효과 줄 | 없음 | `화염 저항 +18% (rune)` 별도 섹션 |
| pseudo stat | 피해/속도/크리 등 풍부 | 저항/능력치/ES/이동속도 제한적 |
| DataEntrie.Type | id prefix와 일치 | rune 타입은 id="rune.xxx"지만 type="augment" |
| 접두어/접미어 | 클립보드에 표시 안 됨 | `{ 접두어 속성 부여 "..." }` 명시 |
| exchange API result | `string[]` 배열 | listingId→객체 딕셔너리 구조 |

---

## 5차 세션 작업 이력 (2026-06-09)

### 41. `{ 고정 속성 부여 }` 헤더 → "인챈트" 대신 "고정" 표시 (`Methods.cs`)
**원인**: 헤더 타입 추적 없이 `filter.Type`(FiltersKO.txt의 API type 필드)을 그대로 사용 → `implicit` 타입인데 "인챈트"로 표시되거나 반대 케이스 발생.

**수정**: `currentSectionType` 변수 도입. 각 `{ }` 헤더 줄을 만날 때 갱신:
```csharp
string currentSectionType = "explicit";
if (hdr == "{ 향상 }") currentSectionType = "enchant";
else if (hdr.StartsWith("{ 고정 속성 부여")) currentSectionType = "implicit";
else currentSectionType = "explicit";
```
`itemfilter.id`에 `filter.Type` 대신 `currentSectionType` 저장. UI ComboBox 선택 시 `ifilter.id` 기준으로 타입 결정.

---

### 42. 음수 범위 표기 제거 정규식 수정 (`Methods.cs`)
**원인**: `(–1-+2)` 같은 음수 포함 범위 표기 제거 실패 → 매칭 오류.

**수정**: 기존 `\([0-9]+` → `\([+-]?[0-9]+` 로 부호 허용.
```csharp
input = Regex.Replace(input, @"\([+-]?[0-9]+\.?[0-9]*-[+-]?[0-9]+\.?[0-9]*\)", "");
```

---

### 43. 서판(map) 아이템 빈 접두어/접미어 슬롯 제외 (`Methods.cs`)
**원인**: 서판 아이템은 크래프팅 대상이 아니므로 빈 접두/접미 자동 추가 불필요.

**수정**: 빈 접두/접미 추가 조건에 `cate_ids[0] != "map"` 추가.
```csharp
if (rarity_id == "rare" && cate_ids[0] != "map" && k < 10)
```

---

### 44. 화폐 교환 UI 정리 (`WinMain.xaml`, `WinMain.xaml.cs`)
- `cbSplinters` (기폭제/화석/조각 드롭다운) `Visibility="Collapsed"` 처리
- "교환을 원하는 오브 선택" placeholder 제거
- 기본값: 엑잘티드 오브 자동 선택
- 교환 조건: `cbOrbs.SelectedIndex > 0` → `cbOrbs.SelectedIndex >= 0`
- `ResetControls()`에서도 이벤트 해제 후 엑잘티드 오브 재선택

---

### 45. exchange API 응답 파싱 전면 재작성 (`Methods.cs`)
**원인**: POE1 exchange API는 `result: string[]`이지만, POE2는 `result: {listingId: object}` 딕셔너리 구조. 기존 Regex 패턴이 `offers` 안의 `"whisper"` 키에서 조기 종료되어 entry 전체를 못 가져옴. 또한 탑레벨 `"item":null`이 있어 `item.amount` 추출용 Regex가 항상 실패.

**수정**:
1. `ExtractResultEntryBlocks()` 헬퍼 메서드 신규 추가 — 중괄호 깊이 추적으로 각 entry 블록을 정확히 분리 (Regex 대신)
2. entry 내에서 `"offers"` 배열 먼저 추출 후 그 안에서 `exchange.amount` / `item.amount` 검색

```csharp
Match mOffersBlock = Regex.Match(entryJson, @"""offers""\s*:\s*\[(.+?)\]", RegexOptions.Singleline);
string searchIn = mOffersBlock.Success ? mOffersBlock.Groups[1].Value : entryJson;
Match mHave = Regex.Match(searchIn, @"""exchange""\s*:\s*\{[^}]*""amount""\s*:\s*([0-9.]+)");
Match mWant = Regex.Match(searchIn, @"""item""\s*:\s*\{[^}]*""amount""\s*:\s*([0-9.]+)");
```

**디버그 로그**: `bin\Release\exchange_debug.log` — Python으로 실제 응답 구조 분석 후 32개 entry 정상 파싱 확인. 빌드 완료, 동작 테스트 필요.

---

## 현재 상태 (2026-06-09 5차 세션 종료)

### 5차 세션에서 완료한 것
- [x] `{ 고정 속성 부여 }` 헤더 → "고정"으로 정상 표시
- [x] 음수 범위 표기 정규식 수정
- [x] 서판 아이템 빈 접두/접미 슬롯 제외
- [x] cbSplinters 드롭다운 제거, cbOrbs 기본값 엑잘티드 오브
- [x] exchange API 파싱 전면 재작성 (ExtractResultEntryBlocks + offers 내 검색)
- [x] 빌드 성공 (경고만, 오류 없음)

### 다음 세션 최우선 테스트
- [ ] **화폐 시세 검색** — 엑잘티드 오브 선택 후 시세 조회 정상 작동 확인
- [ ] **소켓(홈) 기능** — `홈: S S` 아이템에서 홈 자동체크 + tbSocketMin 개수
- [ ] **시세 통화명 한글 표시** — 엑잘티드 오브, 신성한 오브 등으로 나오는지 확인

### 다음 세션 TODO (우선순위 순)
1. 화폐 시세 검색 동작 확인 (엑잘 ↔ 디바인 비율 등)
2. 소켓(홈) + 통화명 한글화 테스트
3. **통화명 축약 표시** — StaticKO.txt text 수정 (엑잘티드 오브 → 엑잘 등)
4. **Wiki 버튼** → https://poe2tools.net/ 연결
5. **README.md 재작성**
6. **배포 준비**
