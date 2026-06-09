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
3. **관리자 모드**로 실행 시 단축키 기능 사용 가능 (`파일 우클릭 → 관리자 권한으로 실행`)

### 종료
트레이 아이콘 우클릭 → **종료**

---

## 설치

1. [Releases](https://github.com/cheonmux/poe2tradesearch/releases)에서 최신 버전 다운로드
2. 압축 해제 후 `Poe2TradeSearch.exe` 실행
3. **.NET Framework 4.8** 필요 ([다운로드](https://dotnet.microsoft.com/download/dotnet-framework/net48))

---

## 옵션 파일 (`data\Config.txt`)

```json
{
  "options": {
    "league": "Runes of Aldur",      // 리그명 (거래소와 정확히 일치해야 함)
    "server_timeout": 5,              // 서버 접속 대기 시간 (초)
    "server_redirect": false,         // 접속 문제 시 true로 변경
    "search_before_day": 7,           // 최근 N일 이내 등록 매물만 검색 [0,1,3,7,14]
    "search_price_count": 20,         // 시세 검색 목록 수 (20 단위, 최대 80)
    "auto_search_delay": 30,          // 시세 자동 갱신 주기 (초, 0=비활성)
    "auto_check_unique": true,        // 유니크 아이템 옵션 전체 선택
    "auto_check_totalres": true,      // 저항 합산 자동 체크
    "auto_select_pseudo": true,       // 유사 스탯 자동 선택
    "auto_select_corrupt": "no",      // 타락 필터 기본값 ["all","no","yes"]
    "auto_select_bytype": "",         // 유형으로 검색 (예: "weapon,armour,accessory")
    "ctrl_wheel": false               // 창고 Ctrl+Wheel 탭 이동
  },
  "shortcuts": [
    {"keycode": 122, "value": "{Pause}"},        // F11: 일시 중지
    {"keycode": 27,  "value": "{Close}"},        // ESC: 창 닫기
    {"keycode": 0,   "ctrl": true, "value": "{Run}"}  // 작동키 변경 (0을 원하는 키코드로)
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
