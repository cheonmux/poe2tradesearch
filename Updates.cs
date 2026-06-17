using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;

namespace Poe2TradeSearch
{
    public partial class WinMain : Window
    {

        private bool FilterDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.kakaogames.com/api/trade2/data/stats", "https://www.pathofexile.com/api/trade2/data/stats" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;
                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        for (int i = 0; i < rootClass.Result.Length; i++)
                        {
                            if (
                                rootClass.Result[i].Entries.Length > 0
                                && RS.lFilterType.ContainsKey(rootClass.Result[i].Entries[0].Type)
                            )
                            {
                                rootClass.Result[i].Label = RS.lFilterType[rootClass.Result[i].Entries[0].Type];
                            }

                            if (rootClass.Result[i].Entries[0].Type == "monster")
                            {
                                for (int j = 0; j < rootClass.Result[i].Entries.Length; j++)
                                {
                                    rootClass.Result[i].Entries[j].Text = rootClass.Result[i].Entries[j].Text.Replace(" (×#)", "");
                                }
                            }
                        }

                        string local = isKR ? "(특정)" : " (Local)";

                        foreach (KeyValuePair<string, byte> itm in RS.lParticular)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.Id.Substring(x.Id.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = rootClass.Result[i].Entries[index].Text.Replace(local, "");
                                    rootClass.Result[i].Entries[index].Part = itm.Value == 1 ? "weapon" : "armour";
                                }
                            }
                        }

                        foreach (KeyValuePair<string, bool> itm in RS.lDisable)
                        {
                            for (int i = 0; i < rootClass.Result.Length; i++)
                            {
                                int index = Array.FindIndex(rootClass.Result[i].Entries, x => x.Id.Substring(x.Id.IndexOf(".") + 1) == itm.Key);
                                if (index > -1)
                                {
                                    rootClass.Result[i].Entries[index].Text = "__DISABLE__";
                                    rootClass.Result[i].Entries[index].Part = "Disable";
                                }
                            }
                        }

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "FiltersKO.txt" : "FiltersEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        private bool ItemDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.kakaogames.com/api/trade2/data/items", "https://www.pathofexile.com/api/trade2/data/items" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;
                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "ItemsKO.txt" : "ItemsEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        private bool StaticDataUpdate(string path)
        {
            bool success = false;
            string[] urls = { "https://poe.kakaogames.com/api/trade2/data/static", "https://www.pathofexile.com/api/trade2/data/static" };

            // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
            Thread thread = new Thread(() =>
            {
                bool isKR = false;
                foreach (string u in urls)
                {
                    isKR = !isKR;
                    string sResult = SendHTTP(null, u, 5);
                    if ((sResult ?? "") != "")
                    {
                        PoeData rootClass = Json.Deserialize<PoeData>(sResult);

                        rootClass.Upddate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        using (StreamWriter writer = new StreamWriter(path + (isKR ? "StaticKO.txt" : "StaticEN.txt"), false, Encoding.UTF8))
                        {
                            writer.Write(Json.Serialize<PoeData>(rootClass));
                        }

                        success = true;
                    }
                }
            });

            thread.Start();
            thread.Join();

            return success;
        }

        // 시작 시 GitHub 최신 릴리스와 현재 버전을 비교해 새 버전이 있으면 알림.
        // 네트워크 오류 등은 조용히 무시 (업데이트 확인 실패가 앱 사용을 막지 않도록).
        private const string ReleaseLatestApi = "https://api.github.com/repos/cheonmux/poe2tradesearch/releases/latest";

        private void CheckUpdate()
        {
            // 마우스 훅 딜레이 회피 위해 다른 데이터 갱신과 동일하게 쓰레드 처리.
            Thread thread = new Thread(() =>
            {
                try
                {
                    string sResult = SendHTTP(null, ReleaseLatestApi, 5);
                    if (string.IsNullOrEmpty(sResult)) return;

                    GithubRelease release = Json.Deserialize<GithubRelease>(sResult);
                    if (release == null || release.Prerelease || string.IsNullOrEmpty(release.TagName)) return;

                    if (!TryParseVersion(release.TagName, out Version latest)) return;
                    if (!TryParseVersion(GetFileVersion(), out Version current)) return;

                    if (latest <= current) return;

                    string url = string.IsNullOrEmpty(release.HtmlUrl)
                        ? "https://github.com/cheonmux/poe2tradesearch/releases/latest"
                        : release.HtmlUrl;

                    Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        MessageBoxResult r = MessageBox.Show(
                            Application.Current.MainWindow,
                            "새 버전이 있습니다.\n\n현재: " + current + "\n최신: " + release.TagName + "\n\n다운로드 페이지를 여시겠습니까?",
                            "업데이트 확인",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (r == MessageBoxResult.Yes)
                            System.Diagnostics.Process.Start(url);
                    });
                }
                catch
                {
                    // 업데이트 확인 실패는 무시
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        // "v0.5.1", "0.5.1.0" 등에서 숫자 버전만 추출해 Version으로 파싱.
        private bool TryParseVersion(string raw, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(raw)) return false;

            string s = raw.Trim().TrimStart('v', 'V');
            int cut = s.IndexOf('-'); // "0.5.1-beta" 같은 접미 제거
            if (cut > 0) s = s.Substring(0, cut);

            return Version.TryParse(s, out version);
        }
    }
}
