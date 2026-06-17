# 유지보수 가이드

Poe2TradeSearch 개발/릴리스 실무 메모. (포팅 이력은 `PORTING_LOG.md`, 사용법은 `README.md`.)

## 버전 관리

### 버전 표기 규칙
- **표시/관리용 버전은 3단위** (`0.5.1`). 사용자에게 보이는 버전, GitHub 태그, 비교 기준 전부 3단위.
- `AssemblyVersion` / `AssemblyFileVersion`은 .NET이 **4파트를 강제**하므로 내부적으로만 `0.5.1.0` 형태로 둔다.
- 코드에서 버전을 읽을 땐 `GetFileVersion()`(Functions.cs) 사용 → `FileVersionInfo.ProductVersion`(= `AssemblyInformationalVersion`, 3단위)을 반환한다.

### 버전 올릴 때 고칠 곳 (예: 0.5.1 → 0.5.2)
1. `Properties/AssemblyInfo.cs`
   - `AssemblyInformationalVersion("0.5.2")` ← **표시/관리용 (3단위)**
   - `AssemblyVersion("0.5.2.0")` / `AssemblyFileVersion("0.5.2.0")` ← 내부 (4단위 강제)
2. `_POE_Data/Config.txt` — `"version":[ "0.5.2", "0.5.2.0" ]`
3. `bin/Release/data/Config.txt` — `"version":["0.5.2","0.5.2.0"]` (런타임에 덮어쓰이지만 일관성 위해 같이 수정)
4. 버전 태그 푸시 → GitHub Actions가 자동 릴리스 (아래 참조)

> Config.txt는 데이터 2벌 규칙(`_POE_Data` Debug + `bin/Release/data` Release)을 따른다.

## 릴리스 절차

- 릴리스는 `.github/workflows/release.yml`이 **`v*` 태그 푸시**에 트리거되어 자동 처리.
  태그 push → windows-latest에서 MSBuild Release 빌드 → `Poe2TradeSearch.zip` 생성 →
  `softprops/action-gh-release`가 릴리스 생성 + zip 첨부.
- 명령:
  ```
  git tag v0.5.2
  git push origin v0.5.2
  ```
- **변경 내역(릴리스 노트)은 수동 입력.** 워크플로우는 노트를 자동 생성하지 않는다(빈 본문으로 릴리스 생성).
  - 방법: 액션 완료 후 GitHub Releases 페이지에서 해당 릴리스 **Edit** → 변경 내역 작성 → Update.
  - 또는 태그 푸시 전에 같은 태그명으로 **Draft 릴리스를 먼저 작성**해두면, 액션이 그 릴리스에 zip만 붙이고 노트는 유지된다.
- 자동 변경내역을 원하면 `release.yml`의 `action-gh-release` step에 `generate_release_notes: true`를 추가하면 직전 태그 이후 커밋/PR이 자동으로 채워진다(현재는 미사용).

## 자동 업데이트 확인

- 시작 시 `CheckUpdate()`(Updates.cs)가 백그라운드 스레드로 GitHub 최신 릴리스를 확인한다.
- API: `https://api.github.com/repos/cheonmux/poe2tradesearch/releases/latest`
- 비교: 최신 `tag_name`(`v0.5.2`) vs `GetFileVersion()`(`0.5.1`). `TryParseVersion`이 `v` 접두·`-beta` 접미를 떼고 `System.Version`으로 정규화 비교 → 3/4단위 혼용도 안전.
- 신버전이 있으면 예/아니오 MessageBox → "예" 시 릴리스 페이지를 브라우저로 연다.
- prerelease는 건너뛴다. 네트워크 오류 등 실패는 조용히 무시(앱 사용 안 막음).
- 알림이 뜨려면 GitHub 릴리스 태그가 현재 코드 버전보다 높아야 한다.

## 거래소 URL

- 한국 서버 거래소 도메인: `poe.kakaogames.com` (구 `poe.game.daum.net`, 2026-06-17 변경됨).
- 정의 위치: `Configure.cs`의 `RS.TradeUrl`/`TradeApi`/`FetchApi`/`ExchangeUrl`/`ExchangeApi`, `Updates.cs`의 데이터 다운로드 URL. 도메인 바뀌면 이 두 파일만 수정.

## 화폐명 한글 표시

- 거래소 fetch 요약 시세도 `GetExchangeItem()`(Methods.cs)으로 영문 화폐 id → 한글명 변환 후 표시.
- 매칭 순서: `Currency.Entries` → `Exchange.Entries` → `Static` 데이터. 셋 다 실패 시에만 영문 폴백.
- 신규 화폐가 영문으로 뜨면 `Parser.txt`의 `currency.entries`에 id→한글 매핑 추가.
