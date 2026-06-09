import json

with open("items.json", encoding="utf-8") as f:
    items_data = json.load(f)

with open("static.json", encoding="utf-8") as f:
    static_data = json.load(f)

# ── category 섹션 빌드 ──────────────────────────────────────────
# items.json 의 각 그룹 id → trade2 category id 매핑
CATEGORY_ID_MAP = {
    "weapon":    "weapon",
    "armour":    "armour",
    "accessory": "accessory",
    "gem":       "gem",
    "jewel":     "jewel",
    "flask":     "flask",
    "map":       "map",
    "currency":  "currency",
    "card":      "card",
    "monster":   "monster",
    "heistmission": "heistmission",
    "heistequipment": "heistequipment",
}

# items.json label(한글) → 영문 라벨 매핑 (Parser.txt category text[1] 용)
LABEL_EN_MAP = {
    "무기":     "Weapons",
    "방어구":   "Armour",
    "장신구":   "Accessories",
    "젬":       "Gems",
    "주얼":     "Jewels",
    "플라스크": "Flasks",
    "지도":     "Maps",
    "화폐":     "Currency",
    "점술 카드": "Divination Cards",
    "야수":     "Beasts",
    "강탈":     "Heist",
    "강탈 도구": "Heist Equipment",
    "기타":     "Other",
}

# items.json 의 각 type(한글) → 영문 이름 매핑은 없으므로
# category entries 는 한글만 넣고 영문은 동일하게 처리
# (영문 클라이언트 지원은 나중에 추가)

category_entries = []
seen = set()

for group in items_data.get("result", []):
    group_id = group.get("id", "")
    label_ko = group.get("label", group_id)
    label_en = LABEL_EN_MAP.get(label_ko, label_ko)

    for entry in group.get("entries", []):
        type_ko = entry.get("type", "")
        name_ko = entry.get("name", "")  # 고유 아이템은 name 필드 있음
        flags = entry.get("flags", {})

        if not type_ko:
            continue

        # 고유 아이템은 category 에 넣지 않음 (items 검색으로 따로 처리)
        if flags.get("unique"):
            continue

        key = (group_id, type_ko)
        if key in seen:
            continue
        seen.add(key)

        # trade id 는 group id 로 단순 매핑
        trade_id = CATEGORY_ID_MAP.get(group_id, group_id)

        category_entries.append({
            "id": trade_id,
            "key": group_id,
            "text": [type_ko, type_ko]  # 영문 데이터 없으므로 한글로 동일하게
        })

# ── currency 섹션 빌드 ──────────────────────────────────────────
currency_entries = []
exchange_entries = []

for group in static_data.get("result", []):
    group_id = group.get("id", "")
    for entry in group.get("entries", []):
        item_id = entry.get("id", "")
        text_ko = entry.get("text", "")
        if not item_id or not text_ko:
            continue

        record = {
            "id": item_id,
            "hidden": False,
            "text": [text_ko, text_ko]
        }

        # Currency / Splinters / Shards 는 currency, 나머지는 exchange
        if group_id in ("Currency", "Shards", "Splinters"):
            currency_entries.append(record)
        else:
            exchange_entries.append(record)

# ── rarity / category text 는 POE2 도 동일 ──────────────────────
parser = {
    "checked": {"entries": []},  # 기본 체크 옵션 — 나중에 추가
    "currency": {"entries": currency_entries},
    "exchange": {"entries": exchange_entries},
    "physical_damage":    {"text": ["물리 피해",    "Physical Damage"]},
    "elemental_damage":   {"text": ["원소 피해",    "Elemental Damage"]},
    "chaos_damage":       {"text": ["카오스 피해",  "Chaos Damage"]},
    "attacks_per_second": {"text": ["초당 공격 횟수", "Attacks per Second"]},
    "attack_speed_incr":  {"text": ["공격 속도 #% 증가",  "#% increased Attack Speed"]},
    "physical_damage_incr": {"text": ["물리 피해 #% 증가", "#% increased Physical Damage"]},
    "category": {
        "text": ["아이템 종류", "Item Class"],
        "entries": category_entries
    },
    "rarity": {
        "text": ["아이템 희귀도", "Rarity"],
        "entries": [
            {"id": "normal",   "text": ["일반",      "Normal"]},
            {"id": "magic",    "text": ["마법",      "Magic"]},
            {"id": "rare",     "text": ["희귀",      "Rare"]},
            {"id": "unique",   "text": ["고유",      "Unique"]},
            {"id": "gem",      "text": ["젬",        "Gem"]},
            {"id": "currency", "text": ["화폐",      "Currency"]},
            {"id": "card",     "text": ["점술 카드", "Divination Card"]}
        ]
    },
    "gems": {
        "entries": [
            {"text": ["기묘한", "Anomalous"]},
            {"text": ["발산하는", "Divergent"]},
            {"text": ["유령같은", "Phantasmal"]}
        ]
    },
    "quality":           {"text": ["품질",           "Quality"]},
    "level":             {"text": ["레벨",           "Level"]},
    "item_level":        {"text": ["아이템 레벨",     "Item Level"]},
    "map_tier":          {"text": ["지도 등급",       "Map Tier"]},
    "talisman_tier":     {"text": ["부적 등급",       "Talisman Tier"]},
    "sockets":           {"text": ["소켓",            "Sockets"]},
    "corrupted":         {"text": ["타락",            "Corrupted"]},
    "unidentified":      {"text": ["미감정",          "Unidentified"]},
    "synthesised_item":  {"text": ["결합 아이템",     "Synthesised Item"]},
    "synthesised":       {"text": ["결합",            "Synthesised"]},
    "shaper_item":       {"text": ["쉐이퍼 아이템",   "Shaper Item"]},
    "elder_item":        {"text": ["장로 아이템",     "Elder Item"]},
    "crusader_item":     {"text": ["성전사 아이템",   "Crusader Item"]},
    "redeemer_item":     {"text": ["구원자 아이템",   "Redeemer Item"]},
    "hunter_item":       {"text": ["사냥꾼 아이템",   "Hunter Item"]},
    "warlord_item":      {"text": ["군주 아이템",     "Warlord Item"]},
    "vaal":              {"text": ["바알",            "Vaal"]},
    "prophecy_item":     {"text": ["예언",            "Prophecy"]},
    "monster_genus":     {"text": ["야수 속",         "Monster Genus"]},
    "monster_group":     {"text": ["야수 그룹",       "Monster Group"]},
    "max":               {"text": ["최대",            "Max"]},
    "superior":          {"text": ["고급",            "Superior"]},
    "metamorph":         {"text": ["변형",            "Metamorph"]},
    "shaped":            {"text": ["형성된",          "Shaped"]},
    "blighted":          {"text": ["역병 든",         "Blighted"]},
    "entrails_item":     {"text": ["내장",            "Entrails"]},
    "unstack_items":     {"text": ["분리",            "Unstack"]},
}

out_path = "_POE_Data/Parser.txt"
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(parser, f, ensure_ascii=False, indent="\t")

print(f"완료: {out_path}")
print(f"  category entries : {len(category_entries)}")
print(f"  currency entries : {len(currency_entries)}")
print(f"  exchange entries : {len(exchange_entries)}")
