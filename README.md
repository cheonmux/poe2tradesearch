[![release](https://img.shields.io/badge/release-Download-brightgreen.svg)](https://github.com/cheonmux/poe2tradesearch/releases)
[![License](https://img.shields.io/badge/license-GPL%203.0-blue.svg)](https://github.com/cheonmux/poe2tradesearch/blob/main/LICENSE)
[![poe2tools](https://img.shields.io/badge/poe2tools.net-visit-orange.svg)](https://poe2tools.net/)

# POE2 거래소 검색

**Path of Exile 2** 한국 거래소 전용 아이템 시세 검색 프로그램입니다.

> 원작: [phiDelPark/PoeTradeSearch](https://github.com/phiDelPark/PoeTradeSearch) (GPL 3.0)
> POE2 한국 서버용으로 포팅 및 개선 — [POE2TOOLS](https://poe2tools.net/)

---

## 사용법

1. POE2 실행 후 인게임 아이템 위에서 **Ctrl+C**
2. 검색창이 자동으로 뜨며 시세를 조회합니다

### 단축키 변경 (Ctrl+C 대신 다른 키 사용)

Ctrl+C 대신 원하는 키(예: Ctrl+D 등)를 눌러 검색하고 싶을 때 사용합니다.

1. 프로그램을 **관리자 권한으로 실행** (`Poe2TradeSearch.exe 우클릭 → 관리자 권한으로 실행`)
2. 검색창의 **설정** 아이콘 클릭
3. **단축키 모드** 선택 → 원하는 키 입력 → 확인
4. 이후 그 키를 누르면 Ctrl+C 없이 바로 검색됩니다

> 단축키 기능은 **반드시 관리자 권한으로 실행**해야 작동합니다.
> (앱이 게임에 키 입력을 대신 전달하기 때문)
> 일반 권한으로 실행하면 단축키를 설정해도 작동하지 않습니다.

### 종료

- 트레이 아이콘 우클릭 → **종료**
- 또는 창 오른쪽 위 **X 버튼** 클릭 → 종료 확인 창에서 **예** 선택

> 창 안의 **최소화** 버튼은 종료가 아니라 트레이로 숨기기입니다.

---

## 설치

1. [Releases](https://github.com/cheonmux/poe2tradesearch/releases)에서 최신 버전 다운로드
2. 압축 해제 후 `Poe2TradeSearch.exe` 실행

### 최초 실행 시 Windows 보안 경고

코드 서명이 없는 프로그램이라, 처음 실행하면 **Windows Defender SmartScreen** 경고가 뜰 수 있습니다. 정상이며, 아래처럼 통과하면 됩니다.

> "Windows의 PC 보호" / "알 수 없는 게시자" 창이 뜨면
> → **추가 정보** 클릭 → **실행** 버튼 클릭

한 번 허용하면 이후 실행부터는 경고가 뜨지 않습니다.
(권한 방식과는 무관합니다. 더블클릭이든 관리자 권한 실행이든 동일하게 최초 1회만 표시됩니다.)

---

## 옵션 파일 (`data\Config.txt`)

```json
{
  "options": {
    "league": "Runes of Aldur", // 리그명 (거래소와 정확히 일치해야 함)
    "server_timeout": 5, // 서버 접속 대기 시간 (초)
    "server_redirect": false, // 접속 문제 시 true로 변경
    "search_before_day": 7, // 최근 N일 이내 등록 매물만 검색 [0,1,3,7,14]
    "search_price_count": 20, // 시세 검색 목록 수 (20 단위, 최대 80)
    "auto_search_delay": 30, // 시세 자동 갱신 주기 (초, 0=비활성)
    "auto_check_unique": true, // 유니크 아이템 옵션 전체 선택
    "auto_check_totalres": true, // 저항 합산 자동 체크
    "auto_select_pseudo": true, // 유사 스탯 자동 선택
    "auto_select_corrupt": "no", // 타락 필터 기본값 ["all","no","yes"]
    "auto_select_bytype": "", // 유형으로 검색 (예: "weapon,armour,accessory")
    "ctrl_wheel": false // 창고 Ctrl+Wheel 탭 이동
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

---

## 라이선스

[GPL 3.0](LICENSE) — 원작자 phiDelPark의 라이선스를 따릅니다.
포팅 및 수정: © 2026 [POE2TOOLS](https://poe2tools.net/)
