using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Poe2TradeSearch
{
    internal static class RS
    {
        internal static string PoeClass = "POEWindowClass";
        internal static string PoeCaption = "Path of Exile 2";

        internal static string[] TradeUrl = { "https://poe.kakaogames.com/trade2/search/poe2/", "https://www.pathofexile.com/trade2/search/poe2/" };
        internal static string[] TradeApi = { "https://poe.kakaogames.com/api/trade2/search/poe2/", "https://www.pathofexile.com/api/trade2/search/poe2/" };
        internal static string[] FetchApi = { "https://poe.kakaogames.com/api/trade2/fetch/", "https://www.pathofexile.com/api/trade2/fetch/" };
        internal static string[] ExchangeUrl = { "https://poe.kakaogames.com/trade2/exchange/poe2/", "https://www.pathofexile.com/trade2/exchange/poe2/" };
        internal static string[] ExchangeApi = { "https://poe.kakaogames.com/api/trade2/exchange/poe2/", "https://www.pathofexile.com/api/trade2/exchange/poe2/" };

        internal static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

        internal static byte ServerLang = 0;
        internal static string ServerType = "";

        internal static Dictionary<string, string> lFilterType = new Dictionary<string, string>()
        {
            { "pseudo", "유사"}, { "explicit", "비고정"}, { "implicit", "고정"}, { "fractured", "분열된"},
            { "enchant", "인챈트"},  { "crafted", "제작된"}, { "rune", "증강물"}, { "desecrated", "훼손된"}, { "sanctum", "성역"},
            { "veiled", "장막"}, { "monster", "야수"}, { "delve", "탐광"}, { "skill", "스킬"}
        };
        internal static Dictionary<string, bool> lDefaultPosition = new Dictionary<string, bool>();

        internal static Dictionary<string, bool> lDisable = new Dictionary<string, bool>()
        {
            { "stat_57434274", true}, { "stat_3666934677", true}
        };

        internal static Dictionary<string, byte> lParticular = new Dictionary<string, byte>()
        {
            { "stat_210067635", 1}, { "stat_691932474", 1}, { "stat_3885634897", 1}, { "stat_2223678961", 1},
            { "stat_1940865751", 1}, { "stat_3336890334", 1}, { "stat_709508406", 1}, { "stat_1037193709", 1}, { "stat_821021828", 1 },
            { "stat_55876295", 1 },
            { "stat_4052037485", 2}, { "stat_4015621042", 2}, { "stat_124859000", 2}, { "stat_53045048", 2},
            { "stat_1062208444", 2}, { "stat_3484657501", 2}, { "stat_3321629045", 2}, { "stat_1999113824", 2}, { "stat_2451402625", 2}, { "stat_3523867985", 2 }
        };

        internal static Dictionary<string, bool> lResistance = new Dictionary<string, bool>()
        {
            { "stat_4220027924", true }, { "stat_3372524247", true }, { "stat_1671376347", true }, { "stat_2923486259", true },
            { "stat_2915988346", true }, { "stat_4277795662", true }, { "stat_3441501978", true }
        };

        internal static Dictionary<string, string> lPseudo = new Dictionary<string, string>()
        {
            // POE2에 실제로 존재하는 pseudo stat만 등록
            { "stat_4220027924", "pseudo_total_cold_resistance" }, { "stat_3372524247", "pseudo_total_fire_resistance" }, { "stat_1671376347", "pseudo_total_lightning_resistance" }, { "stat_2923486259", "pseudo_total_chaos_resistance" },
            { "stat_3299347043", "pseudo_total_life" }, { "stat_1050105434", "pseudo_total_mana" }, { "stat_3489782002", "pseudo_total_energy_shield" }, { "stat_2482852589", "pseudo_increased_energy_shield" },
            { "stat_4080418644", "pseudo_total_strength" }, { "stat_3261801346", "pseudo_total_dexterity" }, { "stat_328541901", "pseudo_total_intelligence" },
            { "stat_2250533757", "pseudo_increased_movement_speed" }
        };
    }

    public partial class WinMain : Window
    {
        private ConfigData mConfigData;
        private ParserData mParserData;
        private ItemBaseName mItemBaseName;

        private PoeData[] mFilterData = new PoeData[2];
        private PoeData[] mItemsData = new PoeData[2];
        private PoeData[] mStaticData = new PoeData[2];

        private bool mDisableClip = false;
        private bool mAdministrator = false;

        // 시세 표시 후 일정 시간 뒤 시세 창 자동 숨김용
        // (hover 보류는 상태변수 대신 WinMain의 실시간 this.IsMouseOver로 판정 — stuck 방지)
        private System.Windows.Threading.DispatcherTimer mHideTimer;

        private static int closeKeyCode = 0;

        private void SaveConfig()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data\\";
#endif
            try
            {
                string json = Json.Serialize<ConfigData>(mConfigData);
                System.IO.File.WriteAllText(path + "Config.txt", json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "설정 저장 실패");
            }
        }

        private bool Setting()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data\\";
#endif
            FileStream fs = null;
            try
            {
                fs = new FileStream(path + "Config.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mConfigData = Json.Deserialize<ConfigData>(json);
                }

                if (mConfigData.Options.SearchPriceCount > 80)
                    mConfigData.Options.SearchPriceCount = 80;

                // 업데이트 오류시 Parser.txt가 지워질수 있어 존재여부 체크
                if (File.Exists(path + "Parser.txt"))
                {
                    fs = new FileStream(path + "Parser.txt", FileMode.Open);
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        fs = null;
                        string json = reader.ReadToEnd();
                        mParserData = Json.Deserialize<ParserData>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "에러");
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            return true;
        }

        private bool LoadData(out string outString)
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data\\";
#endif
            FileStream fs = null;
            string s = "";
            try
            {
                string[] required = { "FiltersKO", "FiltersEN", "ItemsKO", "ItemsEN", "StaticKO", "StaticEN" };
                bool needDownload = false;
                foreach (string item in required)
                    if (!File.Exists(path + item + ".txt")) { needDownload = true; break; }

                if (needDownload)
                {
                    if (!FilterDataUpdate(path) || !ItemDataUpdate(path) || !StaticDataUpdate(path))
                    {
                        s = "생성 실패";
                        throw new UnauthorizedAccessException("failed to create database");
                    }
                }

                s = "FiltersKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[0] = Json.Deserialize<PoeData>(json);
                }

                s = "FiltersEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[1] = Json.Deserialize<PoeData>(json);
                }

                s = "ItemsKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mItemsData[0] = Json.Deserialize<PoeData>(json);
                }

                s = "ItemsEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mItemsData[1] = Json.Deserialize<PoeData>(json);
                }

                s = "StaticKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mStaticData[0] = Json.Deserialize<PoeData>(json);
                }

                s = "StaticEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mStaticData[1] = Json.Deserialize<PoeData>(json);
                }
            }
            catch (Exception ex)
            {
                outString = s;
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "에러");
                return false;
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }

            outString = s;
            return true;
        }
    }
}