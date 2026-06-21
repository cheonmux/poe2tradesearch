[![release](https://img.shields.io/badge/release-Download-brightgreen.svg)](https://github.com/cheonmux/poe2tradesearch/releases)
[![License](https://img.shields.io/badge/license-GPL%203.0-blue.svg)](https://github.com/cheonmux/poe2tradesearch/blob/main/LICENSE)
[![poe2tools](https://img.shields.io/badge/poe2tools.net-visit-orange.svg)](https://poe2tools.net/)

# POE2 거래소 검색 (Poe2TradeSearch)

**Path of Exile 2** 한국 거래소 전용 아이템 시세 검색 프로그램입니다.

게임 내 아이템 위에서 **Ctrl+C** 한 번이면 거래소 시세가 즉시 팝업으로 뜹니다. 약 200KB로 가볍고, 설치나 회원가입 없이 압축만 풀면 바로 동작하며, 백그라운드 리소스를 거의 점유하지 않습니다.

> 원작: [phiDelPark/PoeTradeSearch](https://github.com/phiDelPark/PoeTradeSearch) (GPL 3.0)
> POE2 한국 서버용으로 포팅 및 개선 — [POE2TOOLS](https://poe2tools.net/)

---

## 주요 기능

### 아이템 검색

- POE2에 새로 생긴 홈(소켓) 검색을 지원합니다.
- POE2에 있는 대부분의 접두어·접미어를 지원합니다.
- 접두어·접미어가 비어 있으면 빈 접두어·접미어 필터가 자동으로 추가됩니다.
- 타락 아이템은 자동으로 타락으로, 그 외에는 타락 '아니오'로 지정되어 검색됩니다.
- 아이템 이름을 클릭해 베이스 검색이 가능합니다.
- 저항·치명타·생명력 최대치 등 주요 모드를 자동으로 하이라이트합니다.

### 화폐 시세 (poe.ninja)

- POE2 거래소의 화폐 대량 검색은 부정확해, 화폐·보조 젬 시세를 **poe.ninja** API로 대체했습니다.
- 거의 모든 화폐의 엑잘티드·신성한 오브 대응 시세를 빠르고 제한 없이 확인합니다.
- 시세는 30분 단위로 갱신됩니다.

### 사용 편의

- 환경 설정에서 검색 단축키 변경과 자동 숨김 시간 설정이 가능합니다.
- 창 위치를 옮기면 그 위치가 기억됩니다.
- 입력 필드를 편집하거나 마우스를 창 위에 올린 동안에는 자동으로 숨겨지지 않습니다.
- 거래소 응답에 맞춰 요청 속도를 자동 조절해 IP 차단을 방지합니다.
- 프로그램 종료와 트레이 최소화를 명확히 구분했습니다.

---

## 사용법

### 1. 설치 및 최초 실행

1. [Releases](https://github.com/cheonmux/poe2tradesearch/releases)에서 최신 버전 다운로드
2. 압축 해제 후 `Poe2TradeSearch.exe` 실행
3. **Windows Defender SmartScreen** 경고가 뜨면:
   - **추가 정보** 클릭 → **실행** 버튼 클릭
   - 코드 서명이 없는 프로그램이라 최초 1회만 표시됩니다. 이후 실행부터는 뜨지 않습니다.

### 2. 시세 검색

1. POE2 실행 후 인게임 아이템 위에서 **Ctrl+C**
2. 검색창이 자동으로 뜨며 시세를 조회합니다

> 기본 Ctrl+C 방식은 관리자 권한 없이도 동작합니다.

### 3. 단축키 변경 (Ctrl+C 대신 다른 키 사용)

Ctrl+C 대신 원하는 키(예: Ctrl+D 등)를 눌러 검색하고 싶을 때 사용합니다.

1. `Poe2TradeSearch.exe` 우클릭 → **관리자 권한으로 실행**
2. 검색창의 **설정** 아이콘 클릭
3. **단축키 모드** 선택 → 원하는 키 입력 → 확인
4. 이후 그 키를 누르면 Ctrl+C 없이 바로 검색됩니다

> 단축키 변경 기능만 관리자 권한이 필요합니다. (앱이 게임에 키 입력을 대신 전달하기 때문)

### 4. 종료

- 트레이 아이콘 우클릭 → **종료**
- 또는 창 오른쪽 위 **X 버튼** 클릭 → 종료 확인 창에서 **예** 선택

> 창 안의 **최소화** 버튼은 종료가 아니라 트레이로 숨기기입니다.

---

## 옵션 파일 (`data\Config.txt`)

```json
{
  "options": {
    "league": "Runes of Aldur", // 리그명 (거래소와 정확히 일치해야 함)
    "server_timeout": 5, // 서버 접속 대기 시간 (초)
    "server_redirect": false, // 접속 문제 시 true로 변경
    "search_before_day": 7, // 최근 N일 이내 등록 매물만 검색 [0,1,3,7,14]
    "search_price_min": 0, // 최소 가격 필터 (0=비활성)
    "search_price_count": 20, // 시세 검색 목록 수 (20 단위, 최대 80)
    "auto_search_delay": 30, // 시세 자동 갱신 주기 (초, 0=비활성)
    "hide_delay": 5, // 시세 창 자동 숨김 시간 (초, 0=비활성)
    "auto_check_unique": true, // 유니크 아이템 옵션 전체 선택
    "auto_check_totalres": true, // 저항 합산 자동 체크
    "auto_select_pseudo": true, // 유사 스탯 자동 선택
    "auto_select_corrupt": "no", // 타락 필터 기본값 ["","no","yes"]
    "auto_select_bytype": "" // 유형으로 검색 (예: "weapon,armour,accessory")
  },
  "shortcuts": [
    { "keycode": 122, "value": "{Pause}" }, // F11: 일시 중지
    { "keycode": 27, "value": "{Close}" }, // ESC: 창 닫기
    { "keycode": 0, "ctrl": true, "value": "{Run}" } // 작동키 변경 (0을 원하는 키코드로)
  ]
}
```

> 옵션 변경 후 프로그램 재시작 필요
> 데이터 갱신: `data\FiltersKO.txt` 삭제 후 실행하면 자동 재다운로드

---

## 개발환경

- Windows 10/11
- .NET Framework 4.8
- Visual Studio 2022

### 빌드 방법

```
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" Poe2TradeSearch.sln /p:Configuration=Release /p:Platform="Any CPU" /t:Build /v:minimal
```

빌드 결과물: `bin\Release\Poe2TradeSearch.exe`

---

## 라이선스

[GPL 3.0](LICENSE) — 원작자 phiDelPark의 라이선스를 따릅니다.
포팅 및 수정: © 2026 [POE2TOOLS](https://poe2tools.net/)
