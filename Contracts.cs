using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace Poe2TradeSearch
{
    public partial class WinMain : Window
    {
        [DataContract]
        public class Itemfilter
        {
            public string id;
            public string text;
            public double max;
            public double min;
            public bool disabled;
            public bool isNull = false;
        }

        [DataContract]
        public class ItemOption
        {
            public byte RarityAt;
            public byte Corrupt;
            public bool ByType;
            public bool ChkLv;
            public bool ChkSocket;
            public bool ChkQuality;
            public bool ChkUnidentify;
            public double SocketMin;
            public double SocketMax;
            public double LinkMin;
            public double LinkMax;
            public double QualityMin;
            public double QualityMax;
            public double LvMin;
            public double LvMax;
            public double PriceMin;
            public List<Itemfilter> itemfilters = new List<Itemfilter>();
        }

        [DataContract]
        public class ItemBaseName
        {
            public string[] Ids;
            public string NameKR;
            public string TypeKR;
            public string NameEN;
            public string TypeEN;
            public byte LangType;
        }

        // GitHub releases/latest 응답 (자동 업데이트 확인용, 필요한 필드만)
        [DataContract]
        internal class GithubRelease
        {
            [DataMember(Name = "tag_name")]
            internal string TagName = null;

            [DataMember(Name = "html_url")]
            internal string HtmlUrl = null;

            [DataMember(Name = "prerelease")]
            internal bool Prerelease = false;
        }

        [DataContract()]
        internal class ConfigData
        {
            [DataMember(Name = "options")]
            internal ConfigOption Options = null;

            [DataMember(Name = "shortcuts")]
            internal ConfigShortcut[] Shortcuts = null;

            [DataMember(Name = "version")]
            internal string[] Version = null;

            // Config.txt가 없을 때 사용할 완전한 기본 설정 생성.
            // Options/Shortcuts의 모든 null 위험 멤버를 빠짐없이 채운다.
            internal static ConfigData CreateDefault()
            {
                return new ConfigData
                {
                    Options = new ConfigOption
                    {
                        League = "Runes of Aldur",
                        ServerTimeout = 5,
                        ServerRedirect = false,
                        SearchBeforeDay = 7,
                        SearchPriceMin = 0,
                        SearchPriceCount = 20,
                        AutoSearchDelay = 30,
                        HideDelay = 5,
                        AutoCheckUnique = true,
                        AutoCheckTotalres = true,
                        AutoSelectPseudo = true,
                        AutoSelectCorrupt = "no",
                        AutoSelectByType = "",
                        UiScale = 1.0,
                        BackgroundColor = "#F0F0F0",
                        TextColor = "#000000",
                        CustomCommands = new CustomCommand[]
                        {
                            new CustomCommand { Keycode = 0, Command = "" },
                            new CustomCommand { Keycode = 0, Command = "" },
                            new CustomCommand { Keycode = 0, Command = "" }
                        }
                    },
                    Shortcuts = new ConfigShortcut[]
                    {
                        new ConfigShortcut { Keycode = 27, Ctrl = false, Value = "{Close}" },
                        new ConfigShortcut { Keycode = 0, Ctrl = true, Value = "{Run}" },
                        new ConfigShortcut { Keycode = 113, Ctrl = false, Value = "{ENTER}/hideout{ENTER}" },
                        new ConfigShortcut { Keycode = 116, Ctrl = false, Value = "{ENTER}/remaining{ENTER}" }
                    },
                    Version = new string[] { "0.5.1", "0.5.1.0" }
                };
            }
        }

        [DataContract(Name = "customCommand")]
        internal class CustomCommand
        {
            [DataMember(Name = "keycode")]
            internal int Keycode;

            [DataMember(Name = "command")]
            internal string Command;
        }

        [DataContract(Name = "options")]
        internal class ConfigOption
        {
            [DataMember(Name = "league")]
            internal string League = null;

            [DataMember(Name = "customCommands")]
            internal CustomCommand[] CustomCommands;

            [DataMember(Name = "server")]
            internal string Server = null;

            [DataMember(Name = "server_timeout")]
            internal int ServerTimeout = 0;

            [DataMember(Name = "server_redirect")]
            internal bool ServerRedirect = false;

            [DataMember(Name = "search_price_min")]
            internal decimal SearchPriceMin = 0;

            [DataMember(Name = "search_price_count")]
            internal decimal SearchPriceCount = 20;

            [DataMember(Name = "search_before_day")]
            internal int SearchBeforeDay = 0;

            [DataMember(Name = "auto_search_delay")]
            internal int AutoSearchDelay = 10;

            // 시세 창 자동 숨김 시간(초). 0이면 자동 숨김 비활성.
            [DataMember(Name = "hide_delay")]
            internal int HideDelay = 5;

            [DataMember(Name = "auto_check_unique")]
            internal bool AutoCheckUnique = false;

            [DataMember(Name = "auto_check_totalres")]
            internal bool AutoCheckTotalres = false;

            [DataMember(Name = "auto_select_pseudo")]
            internal bool AutoSelectPseudo = false;

            [DataMember(Name = "auto_select_corrupt")]
            internal string AutoSelectCorrupt = "";

            [DataMember(Name = "auto_select_bytype")]
            internal string AutoSelectByType = "";

            // 창 위치 저장 ("Left,Top")
            [DataMember(Name = "position")]
            internal string Position = null;

            [DataMember(Name = "use_ctrl_wheel")]
            internal bool UseCtrlWheel = false;

            // UI 배율(글자 크기). 1.0=100%, 1.15, 1.3, 1.5. ScaleTransform으로 전체 확대.
            [DataMember(Name = "ui_scale")]
            internal double UiScale = 1.0;

            // 다크모드 여부. true=다크(POE 스타일), false=라이트.
            [DataMember(Name = "dark_mode")]
            internal bool DarkMode = false;

            [DataMember(Name = "gamepad_enabled")]
            internal bool GamePadEnabled = false;

            [DataMember(Name = "gamepad_button")]
            internal string GamePadButton = "A";

            [DataMember(Name = "background_color")]
            internal string BackgroundColor = "#F0F0F0";

            [DataMember(Name = "text_color")]
            internal string TextColor = "#000000";
        }

        [DataContract(Name = "shortcuts")]
        internal class ConfigShortcut
        {
            [DataMember(Name = "keycode")]
            internal int Keycode = 0;

            [DataMember(Name = "value")]
            internal string Value = null;

            [DataMember(Name = "position")]
            internal string Position = null;

            [DataMember(Name = "ctrl")]
            internal bool Ctrl = false;
        }

        [DataContract()]
        internal class ParserData
        {
            [DataMember(Name = "category")]
            internal ParserEntries Category = null;
            [DataMember(Name = "rarity")]
            internal ParserEntries Rarity = null;
            [DataMember(Name = "quality")]
            internal ParserEntries Quality = null;
            [DataMember(Name = "sockets")]
            internal ParserEntries Sockets = null;
            [DataMember(Name = "unidentified")]
            internal ParserEntries Unidentified = null;
            [DataMember(Name = "max")]
            internal ParserEntries Max = null;
            [DataMember(Name = "level")]
            internal ParserEntries Level = null;
            [DataMember(Name = "item_level")]
            internal ParserEntries ItemLevel = null;
            [DataMember(Name = "talisman_tier")]
            internal ParserEntries TalismanTier = null;
            [DataMember(Name = "map_tier")]
            internal ParserEntries MapTier = null;
            [DataMember(Name = "superior")]
            internal ParserEntries Superior = null;
            [DataMember(Name = "vaal")]
            internal ParserEntries Vaal = null;
            [DataMember(Name = "corrupted")]
            internal ParserEntries Corrupted = null;
            [DataMember(Name = "metamorph")]
            internal ParserEntries Metamorph = null;
            [DataMember(Name = "shaper_item")]
            internal ParserEntries ShaperItem = null;
            [DataMember(Name = "elder_item")]
            internal ParserEntries ElderItem = null;
            [DataMember(Name = "crusader_item")]
            internal ParserEntries CrusaderItem = null;
            [DataMember(Name = "redeemer_item")]
            internal ParserEntries RedeemerItem = null;
            [DataMember(Name = "hunter_item")]
            internal ParserEntries HunterItem = null;
            [DataMember(Name = "warlord_item")]
            internal ParserEntries WarlordItem = null;
            [DataMember(Name = "synthesised_item")]
            internal ParserEntries SynthesisedItem = null;
            [DataMember(Name = "synthesised")]
            internal ParserEntries Synthesised = null;
            [DataMember(Name = "shaped")]
            internal ParserEntries Shaped = null;
            [DataMember(Name = "blighted")]
            internal ParserEntries Blighted = null;
            [DataMember(Name = "monster_genus")]
            internal ParserEntries MonsterGenus = null;
            [DataMember(Name = "monster_group")]
            internal ParserEntries MonsterGroup = null;
            [DataMember(Name = "physical_damage")]
            internal ParserEntries PhysicalDamage = null;
            [DataMember(Name = "elemental_damage")]
            internal ParserEntries ElementalDamage = null;
            [DataMember(Name = "chaos_damage")]
            internal ParserEntries ChaosDamage = null;
            [DataMember(Name = "attacks_per_second")]
            internal ParserEntries AttacksPerSecond = null;
            [DataMember(Name = "attack_speed_incr")]
            internal ParserEntries AttackSpeedIncr = null;
            [DataMember(Name = "physical_damage_incr")]
            internal ParserEntries PhysicalDamageIncr = null;
            [DataMember(Name = "prophecy_item")]
            internal ParserEntries ProphecyItem = null;
            [DataMember(Name = "entrails_item")]
            internal ParserEntries EntrailsItem = null;
            [DataMember(Name = "unstack_items")]
            internal ParserEntries UnstackItems = null;
            [DataMember(Name = "gems")]
            internal ParserEntries Gems = null;
            [DataMember(Name = "currency")]
            internal ParserEntries Currency = null;
            [DataMember(Name = "exchange")]
            internal ParserEntries Exchange = null;
            [DataMember(Name = "checked")]
            internal ParserEntries Checked = null;
        }

        [DataContract]
        internal class ParserEntries
        {
            [DataMember(Name = "text")]
            internal string[] Text = null;

            [DataMember(Name = "entries")]
            internal ParserDictionary[] Entries = null;
        }

        [DataContract]
        internal class ParserDictionary
        {
            [DataMember(Name = "id")]
            internal string Id = null;

            [DataMember(Name = "key")]
            internal string Key = null;

            [DataMember(Name = "text")]
            internal string[] Text = null;

            [DataMember(Name = "hidden")]
            internal bool Hidden = false;
        }

        [DataContract]
        internal class PoeData
        {
            [DataMember(Name = "result")]
            internal DataResult[] Result = null;

            [DataMember(Name = "upddate")]
            internal string Upddate = null;
        }

        [DataContract]
        internal class DataResult
        {
            [DataMember(Name = "id")]
            internal string Id = "";

            [DataMember(Name = "label")]
            internal string Label = "";

            [DataMember(Name = "entries")]
            internal DataEntrie[] Entries = null;
        }

        [DataContract]
        internal class DataEntrie
        {
            [DataMember(Name = "id")]
            internal string Id = "";

            [DataMember(Name = "name")]
            internal string Name = "";

            [DataMember(Name = "text")]
            internal string Text = "";

            [DataMember(Name = "type")]
            internal string Type = "";

            [DataMember(Name = "part")]
            internal string Part = "";

            [DataMember(Name = "flags")]
            internal DataFlags[] Flags = null;
        }

        [DataContract]
        internal class DataFlags
        {
            [DataMember(Name = "unique")]
            internal bool Unique = false;
        }

        [DataContract]
        internal class AccountData
        {
            [DataMember(Name = "name")]
            internal string Name = "";
        }

        [DataContract]
        internal class PriceData
        {
            [DataMember(Name = "type")]
            internal string Type = "";

            [DataMember(Name = "amount")]
            internal double Amount = 0;

            [DataMember(Name = "currency")]
            internal string Currency = "";
        }

        [DataContract]
        internal class FetchDataListing
        {
            [DataMember(Name = "indexed")]
            internal string Indexed = "";

            [DataMember(Name = "account")]
            internal AccountData Account = new AccountData();

            [DataMember(Name = "price")]
            internal PriceData Price = new PriceData();
        }

        [DataContract]
        internal class FetchDataInfo
        {
            [DataMember(Name = "id")]
            internal string ID = "";

            [DataMember(Name = "listing")]
            internal FetchDataListing Listing = new FetchDataListing();
        }

        [DataContract]
        internal class FetchData
        {
            [DataMember(Name = "result")]
            internal FetchDataInfo[] Result;
        }

        [DataContract]
        internal class ResultData
        {
            [DataMember(Name = "result")]
            internal string[] Result = null;

            [DataMember(Name = "id")]
            internal string ID = "";

            [DataMember(Name = "total")]
            internal int Total = 0;
        }

        [DataContract]
        internal class ExchangeOffer
        {
            [DataMember(Name = "currency")]
            internal string Currency = "";

            [DataMember(Name = "amount")]
            internal double Amount = 0;
        }

        [DataContract]
        internal class ExchangeOfferPair
        {
            [DataMember(Name = "exchange")]
            internal ExchangeOffer Exchange = new ExchangeOffer();

            [DataMember(Name = "item")]
            internal ExchangeOffer Item = new ExchangeOffer();
        }

        [DataContract]
        internal class ExchangeListing
        {
            [DataMember(Name = "indexed")]
            internal string Indexed = "";

            [DataMember(Name = "account")]
            internal AccountData Account = new AccountData();

            [DataMember(Name = "offers")]
            internal ExchangeOfferPair[] Offers = null;
        }

        [DataContract]
        internal class ExchangeEntry
        {
            [DataMember(Name = "id")]
            internal string Id = "";

            [DataMember(Name = "listing")]
            internal ExchangeListing Listing = new ExchangeListing();
        }

        [DataContract]
        internal class ExchangeResultData
        {
            [DataMember(Name = "total")]
            internal int Total = 0;
        }

        [DataContract]
        internal class q_Option
        {
            [DataMember(Name = "option")]
            internal string Option;
        }

        [DataContract]
        internal class q_Min_And_Max
        {
            [DataMember(Name = "min")]
            internal double Min;

            [DataMember(Name = "max")]
            internal double Max;
        }

        [DataContract]
        internal class q_Type_filters_filters
        {
            [DataMember(Name = "category", EmitDefaultValue = false)]
            internal q_Option Category = null;

            [DataMember(Name = "rarity")]
            internal q_Option Rarity = new q_Option();
        }

        [DataContract]
        internal class q_Type_filters
        {
            [DataMember(Name = "filters")]
            internal q_Type_filters_filters Filters = new q_Type_filters_filters();
        }

        [DataContract]
        internal class q_Equipment_filters_filters
        {
            [DataMember(Name = "rune_sockets")]
            internal q_Min_And_Max RuneSockets = new q_Min_And_Max();
        }

        [DataContract]
        internal class q_Equipment_filters
        {
            [DataMember(Name = "disabled")]
            internal bool Disabled = false;

            [DataMember(Name = "filters")]
            internal q_Equipment_filters_filters Filters = new q_Equipment_filters_filters();
        }

        [DataContract]
        internal class q_Misc_filters_filters
        {
            [DataMember(Name = "quality")]
            internal q_Min_And_Max Quality = new q_Min_And_Max();

            [DataMember(Name = "ilvl")]
            internal q_Min_And_Max Ilvl = new q_Min_And_Max();

            [DataMember(Name = "gem_level")]
            internal q_Min_And_Max Gem_level = new q_Min_And_Max();

            [DataMember(Name = "corrupted")]
            internal q_Option Corrupted = new q_Option();

            [DataMember(Name = "identified")]
            internal q_Option Identified = new q_Option();
        }

        [DataContract]
        internal class q_Misc_filters
        {
            [DataMember(Name = "disabled")]
            internal bool Disabled = false;

            [DataMember(Name = "filters")]
            internal q_Misc_filters_filters Filters = new q_Misc_filters_filters();
        }

        [DataContract]
        internal class q_Map_filters_filters
        {
            [DataMember(Name = "map_tier")]
            internal q_Min_And_Max Tier = new q_Min_And_Max();
        }

        [DataContract]
        internal class q_Map_filters
        {
            [DataMember(Name = "disabled")]
            internal bool Disabled = false;

            [DataMember(Name = "filters")]
            internal q_Map_filters_filters Filters = new q_Map_filters_filters();
        }

        [DataContract]
        internal class q_Trade_filters_filters
        {
            [DataMember(Name = "indexed")]
            internal q_Option Indexed = new q_Option();

            [DataMember(Name = "price")]
            internal q_Min_And_Max Price = new q_Min_And_Max();
        }

        [DataContract]
        internal class q_Trade_filters
        {
            [DataMember(Name = "disabled")]
            internal bool Disabled = false;

            [DataMember(Name = "filters")]
            internal q_Trade_filters_filters Filters = new q_Trade_filters_filters();
        }

        [DataContract]
        internal class q_Filters
        {
            [DataMember(Name = "type_filters", EmitDefaultValue = false)]
            internal q_Type_filters Type = new q_Type_filters();

            [DataMember(Name = "equipment_filters", EmitDefaultValue = false)]
            internal q_Equipment_filters Equipment = null;

            [DataMember(Name = "map_filters", EmitDefaultValue = false)]
            internal q_Map_filters Map = null;

            [DataMember(Name = "misc_filters", EmitDefaultValue = false)]
            internal q_Misc_filters Misc = null;

            [DataMember(Name = "trade_filters", EmitDefaultValue = false)]
            internal q_Trade_filters Trade = new q_Trade_filters();

            [DataMember(Name = "weapon_filters", EmitDefaultValue = false)]
            internal q_Disabled_filters Weapon = null;
            [DataMember(Name = "armour_filters", EmitDefaultValue = false)]
            internal q_Disabled_filters Armour = null;
            [DataMember(Name = "req_filters", EmitDefaultValue = false)]
            internal q_Disabled_filters Req = null;
        }

        [DataContract]
        internal class q_Disabled_filters
        {
            [DataMember(Name = "disabled")]
            internal bool Disabled = true;
        }

        [DataContract]
        internal class q_Stats_filters
        {
            [DataMember(Name = "id")]
            internal string Id;

            [DataMember(Name = "value")]
            internal q_Min_And_Max Value;

            [DataMember(Name = "disabled")]
            internal bool Disabled;
        }

        [DataContract]
        internal class q_Stats
        {
            [DataMember(Name = "type")]
            internal string Type;

            [DataMember(Name = "filters")]
            internal q_Stats_filters[] Filters;
        }

        [DataContract]
        internal class q_Sort
        {
            [DataMember(Name = "price")]
            internal string Price;
        }

        [DataContract]
        internal class q_Query
        {
            [DataMember(Name = "status", Order = 0)]
            internal q_Option Status = new q_Option();

            [DataMember(Name = "name")]
            internal string Name;

            [DataMember(Name = "type")]
            internal string Type;

            [DataMember(Name = "stats")]
            internal q_Stats[] Stats;

            [DataMember(Name = "filters")]
            internal q_Filters Filters = new q_Filters();
        }

        [DataContract]
        internal class JsonData
        {
            [DataMember(Name = "query")]
            internal q_Query Query;

            [DataMember(Name = "sort")]
            internal q_Sort Sort = new q_Sort();
        }
    }

    // poe.ninja API 응답 모델
    [DataContract]
    internal class NinjaOverviewData
    {
        [DataMember(Name = "lines")]
        internal NinjaLine[] Lines = null;

        [DataMember(Name = "core")]
        internal NinjaCore Core = null;
    }

    [DataContract]
    internal class NinjaCore
    {
        [DataMember(Name = "rates")]
        internal NinjaRates Rates = null;
    }

    [DataContract]
    internal class NinjaRates
    {
        [DataMember(Name = "exalted")]
        internal double Exalted = 1.0;
    }

    [DataContract]
    internal class NinjaLine
    {
        [DataMember(Name = "id")]
        internal string Id = null;

        [DataMember(Name = "primaryValue")]
        internal double PrimaryValue = 0.0;
    }

    public class FilterEntrie
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public FilterEntrie(string id, string name)
        {
            this.ID = id;
            this.Name = name;
        }
    }
}