# 코드 점검 보고서 (배포 전)

> 작성: 2026-06-12 (9차 세션). security-reviewer + performance-optimizer 에이전트 점검.
> 대상: Poe2TradeSearch (C# WPF / .NET Framework 4.8). 약 4,500줄.
> 목적: GitHub 오픈소스 배포 전 보안 취약점 / 메모리 누출 / 성능 병목 식별.
>
> **진행 상태**: 8건 수정 완료 (✅) — 안전 7건 + 보안 S-H1. Windows 빌드 후 실측 대기.
> 나머지(자료구조 변경/로직 영향/운영자 판단)는 미착수 (⬜).

---

## 요약

| 영역 | CRITICAL | HIGH | MEDIUM | LOW |
|------|----------|------|--------|-----|
| 보안 | 0 | 2 | 4 | 2 |
| 메모리/병목/동시성 | 0 | 6 | 5 | 2 |

- **CRITICAL 없음.** 하드코딩된 비밀/토큰 없음. 인증서 검증 우회 없음 (.NET 기본 검증 유지).
- 8차 세션에서 일부 이미 수정됨 (mNinjaCache volatile, 게임패드 Sleep 백그라운드, WebException Response using, TLS 명시). 아래는 그 위에서 추가 발견된 항목.

---

## 1. 보안 (security-reviewer)

### HIGH

#### ✅ S-H1. API 응답값(`resultData.ID`) 비검증 URL/Process.Start 삽입 — **수정 완료**
- **위치**: WinMain.xaml.cs:264, Methods.cs:1301
- **문제**: 거래소 API 반환 `resultData.ID` / `resultData.Result[]`를 검증 없이 URL 경로 + `Process.Start`에 삽입. 공식 API 신뢰 전제면 실현성 낮으나 MITM/캐시오염/서버침해 시 임의 URL 실행·비정상 요청 가능.
- **적용**: `Functions.cs`에 `IsSafeTradeId(string)` static 헬퍼 추가. **화이트리스트 패턴 대신 인젝션 문자만 거부** 방식 채택 — ID 형식(실제 샘플 미확보)을 좁히지 않아 정상 검색 오탐 0. 제어문자 + URL 조작문자(`/ \ : ? # & % 공백 " ' < > @`) + 길이>100 거부.
  - WinMain.xaml.cs:265 — `Process.Start` 전 ID 검증.
  - Methods.cs:1300~ — listing id 각각 검증 후 `tmp` 채움(`tmpLen`), 검색 id 검증해 query 조건부 생성, 유효 id 없으면 페이지 스킵.
  - **부수 개선**: 기존 `string.Join(",", tmp)`가 마지막 페이지 null 슬롯을 `,,,`로 넣던 버그도 `string.Join(",", tmp, 0, tmpLen)`로 해결.
- **빌드 후 확인**: 정상 아이템 검색 여전히 동작하는지(오탐 없는지) + Process.Start 브라우저 열기 정상.
- ✅ **실측 완료 (2026-06-13, Windows)**: 무작위 매물 다수 검색·브라우저 열기 정상, 오탐 0. **실제 listing ID = 영문 대소문자+숫자 10자리** (예: `2KX2kzVeUk`, `KlRjXm76s5`, `X3p9XQ8KfP`) — 인젝션 문자 없어 `IsSafeTradeId`와 충돌 없음 확인.

#### S-H2. TLS 1.1 활성화
- **위치**: WinMain.xaml.cs:37-38 (`Tls12 | Tls11`)
- **문제**: TLS 1.1 = 2021 deprecated (POODLE/BEAST). 대상 서버는 TLS1.2+만 지원하므로 무의미하나 다운그레이드 시도 위험.
- **수정**: `Tls11` 제거 → `Tls12 | Tls13` (.NET 4.8 Tls13 지원).

### MEDIUM

#### S-M3. 에러 로그에 HTTP 요청 바디 전문 + 무제한 누적
- **위치**: Functions.cs:444-445, 450-451
- **문제**: `entity`(검색 JSON) 전문을 `.exe` 옆 로그에 무제한 append. 현재 민감정보 없으나 디스크 소진 + 타 프로세스 가독.
- **수정**: 로그 크기 상한(1MB) 로테이션. entity는 길이만 기록.

#### S-M4. 데드코드 WebClient 경로 (timeout=0)
- **위치**: Functions.cs:383-396
- **문제**: "테스트용" WebClient 분기 — CookieContainer/UserAgent/RateLimit 헤더 전부 누락. 혼란 유발.
- **수정**: `timeout == 0` 분기 전체 제거.

#### S-M5. Config 값(`League`)의 URL 삽입 검증 부재
- **위치**: Configure.cs:114/128 (역직렬화), WinMain.xaml.cs:83/258/264 (사용)
- **문제**: 조작된 Config.txt `League`가 `RS.ServerType`으로 URL에 그대로 삽입. 로컬 파일이라 벡터 제한적이나 사용자 수동 편집 시 비정상 URL 생성.
- **수정**: `RS.ServerType` 설정 시 `[a-zA-Z0-9 +%-]+` 패턴 검증.

#### S-M6. 클립보드 체인(SetClipboardViewer) 동시성
- **위치**: WinMain.xaml.cs:557 (ApplyShortcutSetting)
- **문제**: 모드 전환 시 `mNextClipBoardViewerHWnd` 접근에 동시성 보호 없음. WM_CHANGECBCHAIN 처리와 타이밍 경합 가능. (보안보다 안정성)
- **수정**: 해당 핸들 UI 스레드 전용 접근 보장.

### LOW

#### S-L7. UserAgent Chrome 사칭
- **위치**: Configure.cs:19 (`Mozilla/5.0 ... Chrome/136...`)
- **문제**: 봇 탐지 우회 의도로 보이나 오픈소스 공개 시 API ToS 리스크.
- **수정(검토)**: `Poe2TradeSearch/버전` 식별형 UA 권장. **단, 의도적 선택일 수 있어 운영자 판단 필요.**

#### ✅ S-L8. 예외 무조건 삼킴 (catch{}) — **수정 완료 (총 7곳)**
- **문제**: 빈 catch가 예외를 완전히 삼켜 "조용한 실패" → 사용자는 원인 모름, 개발자는 진단 불가. 실사용 디버그성에 가장 큰 영향.
- **적용**: `catch (Exception ex) { System.Diagnostics.Debug.WriteLine(...) }`. **`Debug.WriteLine`은 Release 빌드에서 컴파일러가 자동 제거 → 배포본 파일/성능 영향 0, 개발(Debug 빌드)에서만 VS 출력창에 표시.** 별도 파일 로그 생성 안 함.
  - Functions.cs:323 FetchNinjaPrices (ninja 타입별 파싱)
  - Functions.cs:371 StrToDouble (값 추출 실패, 입력값 포함)
  - Functions.cs:390 DamageToDPS (DPS 파싱)
  - Methods.cs:717 디테일 텍스트 Regex 처리
  - WinMain.xaml.cs:269 브라우저 열기(Process.Start) — 중요
  - WinMain.xaml.cs:434 가격 정보 색상 변경
  - WinMain.xaml.cs:685 클립보드 파싱(핵심 경로) — 중요
- **의도적 유지 (로그 불필요)**:
  - App.xaml.cs:33 — 로그 쓰기 자체의 실패 (로그에 로그 = 무한루프 위험)
  - Functions.cs:561 — 클립보드 SetText 10회 재시도 루프 (실패는 정상 흐름)
  - Methods.cs:1402, 1490 — `OperationCanceledException` (취소는 정상 흐름)

---

## 2. 메모리 누출 / 성능 / 동시성 (performance-optimizer)

### HIGH

#### ✅ P-H1. CancellationTokenSource Dispose 누락 — **수정 완료**
- **위치**: Methods.cs:1437, 1452
- **문제**: `mPriceCts` 매 검색마다 new로 교체, 이전 것 Dispose 안 함. 내부 WaitHandle(커널) 누적. Ctrl+C 반복 시 누수.
- **적용**: 교체 전 `Cancel()` → `Dispose()`. 워커는 `ThrowIfCancellationRequested`만 사용(WaitHandle 직접 접근 없음)이라 Dispose 후 ObjectDisposedException 없음. **빌드 후 취소 타이밍 실측 권장.**

#### ✅ P-H2. SetClipText 포그라운드 스레드 누적 — **수정 완료**
- **위치**: Functions.cs:532-548
- **문제**: 호출마다 `new Thread()` + `IsBackground=false`. 앱 종료 방해 + 재시도 루프 중 스레드 누적.
- **적용**: `IsBackground=true` (STA 유지).

#### P-H3. 파싱 루프마다 Regex 재컴파일
- **위치**: Methods.cs:228, 263-264, 305, 311-314, 320, 373, 380
- **문제**: `ItemTextParser`(Ctrl+C마다 호출) 내부 루프에서 `new Regex` 반복 생성. 특히 320줄 동적 패턴 stat 줄 수만큼 매번 컴파일.
- **수정**: 고정 패턴 `static readonly Regex`. 동적 패턴은 `RegexOptions.Compiled` 제거 또는 input 단위 캐시.

#### P-H4. GetExchangeItem O(n) 선형스캔 반복
- **위치**: Methods.cs:1570-1612, 호출 1345-1353
- **문제**: 두 오버로드 모두 Currency/Exchange/StaticData 전체 선형 탐색. fetch 결과(수십 건) × O(n)이 매 검색마다.
- **수정**: 데이터 로드 후 `Dictionary<string,ParserDictionary>` 인덱싱 (id→item, text[0]→item 1회 구성).

#### ✅ P-H5. mLockUpdatePrice volatile 미지정 (경합) — **수정 완료**
- **위치**: WinMain.xaml.cs:28 (선언), Methods.cs:1440-1441 (읽기), 1408 (쓰기)
- **문제**: 단순 bool. UI 스레드 읽기 / 백그라운드 쓰기. volatile 없어 캐시된 값 읽힐 수 있음.
- **적용**: `private volatile bool mLockUpdatePrice`.

#### P-H6. priceThread 중첩 실행 가능
- **위치**: Methods.cs:1436, 1455, WaitForRateLimit:1516
- **문제**: 이전 스레드가 Sleep 루프에 살아있는 채 새 스레드 시작 가능. Sleep 중 취소 반응 1초 지연 → 두 스레드 동시 UpdatePrice 위험.
- **수정**: 취소 후 짧은 Join(타임아웃) 또는 Sleep 단위 200ms 이하로 분할.

### MEDIUM

#### P-M7. mNinjaExaltedRate double 경합
- **위치**: Functions.cs:291-292, 329
- **문제**: `mNinjaCache`는 volatile이나 `mNinjaExaltedRate`(double)는 volatile 불가. 캐시 교체와 rate 쓰기 사이 UI가 신캐시+구rate 조합 읽기 가능.
- **수정**: 두 값을 불변 레코드로 묶어 단일 volatile 참조 교체.

#### P-M8. 핫키 {run} 처리 중 UI 스레드 Sleep(300)
- **위치**: WinMain.xaml.cs:1767 (WndProc)
- **문제**: WndProc(UI 스레드)에서 `Thread.Sleep(300)` → 300ms UI 멈춤. **게임패드 경로(664)는 8차에 백그라운드 이동했으나 핫키 경로는 미수정.**
- **수정**: 게임패드와 동일하게 Task.Run/별도 스레드. Ctrl+C 전송→백그라운드 Sleep→UI에서 클립보드 읽기.

#### ✅ P-M9. HwndSource Hook RemoveHook 누락 — **수정 완료**
- **위치**: WinMain.xaml.cs:129-130 (AddHook), Window_Closed
- **문제**: 라이프사이클상 실질 누출은 없으나 상주 앱 방어적으로 RemoveHook 권장.
- **적용**: 지역변수 `source` → 필드 `mHwndSource` 승격. Window_Closed에서 `RemoveHook` + null 처리.

#### P-M10. mFilterData 이중 선형 탐색
- **위치**: Methods.cs:266-271
- **문제**: 멀티라인 mod 병합마다 `foreach Result` + `Array.Exists(Entries)` 이중 루프 × 줄수 × 4회. 옵션 많은 아이템에서 지연.
- **수정**: 로드 시 stat 텍스트 `HashSet`/`Dictionary` 인덱싱 → O(1) 존재 확인.

#### ✅ P-M11. DispatcherTimer Tick 내 불필요한 BeginInvoke — **수정 완료**
- **위치**: Methods.cs:1523 (AutoSearchTimer_Tick)
- **문제**: 이미 UI 스레드인데 다시 `Dispatcher.BeginInvoke`로 감쌈 → 불필요 큐.
- **적용**: BeginInvoke 래퍼 제거, 직접 조작.

### LOW

#### P-L12. Button_Click Thread + Join (동기와 실효 동일)
- **위치**: WinMain.xaml.cs:256-282
- **문제**: `new Thread()` 후 즉시 `Join()` → 스레드 생성비 지불하며 UI 블로킹. 동기 호출과 동일.
- **수정**: Task.Run+await(async 핸들러) 또는 완료 콜백.

#### ✅ P-L13. Json.Deserialize MemoryStream using 미사용 — **수정 완료**
- **위치**: Functions.cs:264-267
- **문제**: 명시 Dispose하나 예외 시 미실행. MemoryStream은 비관리 리소스 없어 실해 미미.
- **적용**: `using (MemoryStream ...)` 블록으로 전환.

---

## 배포 전 권장 우선순위

### 반드시 (HIGH — 실위험·체감)
1. ✅ **P-H1** CancellationTokenSource Dispose (상주앱 핸들 누수) — 완료
2. ✅ **P-H2** SetClipText IsBackground=true (스레드 누적+종료 방해) — 완료
3. ✅ **S-H1** resultData.ID 검증 (보안) — 완료 (인젝션 문자 거부 방식, 오탐 0)
4. ✅ **P-H5** mLockUpdatePrice volatile — 완료 / ⬜ **P-M8** 핫키 Sleep 백그라운드 (멈춤) — 미착수
5. ⬜ **P-H4** GetExchangeItem Dictionary 인덱싱 (매 검색 병목) — 미착수 (자료구조 변경)
6. ⬜ **P-H3** Regex static화, ⬜ **P-H6** priceThread 취소 반응 — 미착수

### 권장 (MEDIUM)
- ⬜ S-H2 TLS1.1 제거, ⬜ S-M3 로그 크기 제한, ⬜ P-M7 ninja rate 묶기, ✅ P-M9 RemoveHook(완료), ⬜ P-M10 filter 인덱싱, ✅ P-M11 BeginInvoke 제거(완료)
- ✅ S-L8 예외 로깅(완료), ✅ P-L13 MemoryStream using(완료)

### 이번 세션 수정 완료 (9건)
✅ P-H1 · P-H2 · P-H5 · P-M9 · P-M11 · P-L13 (안전 6건) · **S-H1**(보안 ID 검증) · **S-L8**(catch{} 7곳 진단로그). Windows 빌드 후 실측 대기.

### 남은 HIGH (배포 전 검토 필요)
⬜ P-H3(Regex) · P-H4(인덱싱) · P-H6(priceThread) · P-M8(핫키 멈춤)
→ 자료구조/로직 영향 있어 신중히. **P-H6 priceThread 중첩은 동시성 실위험이라 우선 검토 권장.**

### 운영자 판단 필요
- **S-L7 UserAgent**: 봇탐지 우회 의도일 수 있음. 오픈소스 공개 시 ToS 리스크 ↔ 기능 동작. 결정 보류.
- S-M4 데드코드 WebClient 제거, S-L8 예외 로깅.

---

## 비고
- 8차 세션 "수용" 결정과 겹치는 항목(UserAgent, 로그 바디) 재확인 필요.
- 점검은 정적 분석 — 실제 Windows 런타임 프로파일링 미수행. 누수 의심 항목은 빌드 후 장시간 실행 + 핸들/메모리 모니터로 실측 권장.
