using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Poe2TradeSearch
{
    // 거래소 rate limit 강제 관리자.
    // 거래소가 응답 헤더로 주는 규칙(x-rate-limit-ip)과 현재상태(x-rate-limit-ip-state)를
    // 그대로 믿고, 다음 요청을 언제 보내야 안전한지 계산한다.
    // 헤더 형식: "최대요청:기간초:차단초" (규칙), "현재요청:기간초:차단남은초" (상태)
    internal static class RateLimit
    {
        // 한도의 몇 %까지 채우면 미리 한 박자 쉬는지 (강제 여유).
        private const double SafetyRatio = 0.8;
        // 차단당하지 않으려 추가로 더 기다리는 여유초.
        private const double SafetyPadSeconds = 0.5;

        private class Policy
        {
            public DateTime BlockedUntil = DateTime.MinValue; // 거래소가 차단한 해제 시각
            public DateTime NextSafe = DateTime.MinValue;     // 자체 throttle: 이 시각 후에 보내야 안전
            public string LastRule = "";                      // 디버그/표시용 원본 규칙 문자열
        }

        private static readonly object mLock = new object();
        private static readonly Dictionary<string, Policy> mPolicies = new Dictionary<string, Policy>();

        private static Policy Get(string policyName)
        {
            if (string.IsNullOrEmpty(policyName)) policyName = "default";
            Policy p;
            if (!mPolicies.TryGetValue(policyName, out p))
            {
                p = new Policy();
                mPolicies[policyName] = p;
            }
            return p;
        }

        // 요청 보내기 전 호출. 안전해질 때까지 필요한 대기초를 반환(0이면 바로 가능).
        // 이 메서드 자체는 대기하지 않는다 — 호출측이 대기/표시 방식을 정한다.
        public static double SecondsToWait(string policyName)
        {
            lock (mLock)
            {
                Policy p = Get(policyName);
                DateTime now = DateTime.UtcNow;
                DateTime until = p.BlockedUntil > p.NextSafe ? p.BlockedUntil : p.NextSafe;
                double wait = (until - now).TotalSeconds;
                return wait > 0 ? wait : 0;
            }
        }

        // 현재 차단 중이면 남은 초, 아니면 0.
        public static double BlockedSeconds(string policyName)
        {
            lock (mLock)
            {
                Policy p = Get(policyName);
                double wait = (p.BlockedUntil - DateTime.UtcNow).TotalSeconds;
                return wait > 0 ? wait : 0;
            }
        }

        // 응답 헤더를 먹여 상태 갱신. SendHTTP / fetch 응답 직후 호출.
        // ruleHeader  = x-rate-limit-ip      (예: "5:10:60,15:60:300,30:300:1800")
        // stateHeader = x-rate-limit-ip-state(예: "4:10:0,1:60:0,1:300:0")
        // retryAfter  = 429 응답의 Retry-After 초 (없으면 null)
        public static void Update(string policyName, string ruleHeader, string stateHeader, string retryAfter)
        {
            lock (mLock)
            {
                Policy p = Get(policyName);
                DateTime now = DateTime.UtcNow;

                // 429: 거래소가 명시한 Retry-After를 그대로 차단 시각으로.
                if (!string.IsNullOrEmpty(retryAfter))
                {
                    double ra;
                    if (double.TryParse(retryAfter.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out ra) && ra > 0)
                    {
                        DateTime blockEnd = now.AddSeconds(ra + SafetyPadSeconds);
                        if (blockEnd > p.BlockedUntil) p.BlockedUntil = blockEnd;
                    }
                }

                if (string.IsNullOrEmpty(ruleHeader) || string.IsNullOrEmpty(stateHeader))
                    return;

                p.LastRule = ruleHeader;
                string[] rules = ruleHeader.Split(',');
                string[] states = stateHeader.Split(',');
                int n = Math.Min(rules.Length, states.Length);

                for (int i = 0; i < n; i++)
                {
                    int rMax, rPeriod, rBlock;
                    int sCur, sPeriod, sBlocked;
                    if (!ParseTriple(rules[i], out rMax, out rPeriod, out rBlock)) continue;
                    if (!ParseTriple(states[i], out sCur, out sPeriod, out sBlocked)) continue;
                    if (rMax <= 0 || rPeriod <= 0) continue;

                    // 이미 차단당한 규칙: 차단남은초 만큼 대기.
                    if (sBlocked > 0)
                    {
                        DateTime blockEnd = now.AddSeconds(sBlocked + SafetyPadSeconds);
                        if (blockEnd > p.BlockedUntil) p.BlockedUntil = blockEnd;
                    }

                    // 한도 근접: 이번 창에서 안전선(80%) 넘었으면 창이 끝날 때까지 한 박자 쉰다.
                    // 창이 풀리는 시점을 정확히 알 수 없으므로 기간만큼 보수적으로 미룬다.
                    if (sCur >= Math.Max(1, (int)Math.Floor(rMax * SafetyRatio)))
                    {
                        DateTime safeAt = now.AddSeconds((double)rPeriod / rMax + SafetyPadSeconds);
                        if (safeAt > p.NextSafe) p.NextSafe = safeAt;
                    }
                }
            }
        }

        private static bool ParseTriple(string s, out int a, out int b, out int c)
        {
            a = b = c = 0;
            if (string.IsNullOrEmpty(s)) return false;
            string[] parts = s.Trim().Split(':');
            if (parts.Length < 3) return false;
            return int.TryParse(parts[0], out a)
                && int.TryParse(parts[1], out b)
                && int.TryParse(parts[2], out c);
        }
    }

    internal static class Native
    {
        [DllImport("user32.dll")] internal static extern IntPtr SetClipboardViewer(IntPtr hWnd);

        [DllImport("user32.dll")] internal static extern bool ChangeClipboardChain(IntPtr hWnd, IntPtr hWndNext);

        internal const int WM_DRAWCLIPBOARD = 0x0308;
        internal const int WM_CHANGECBCHAIN = 0x030D;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr parenthWnd, IntPtr childAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /*
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
        [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
        */

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string lpModuleName);

        internal const int WH_MOUSE_LL = 14;

        internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] internal static extern short GetKeyState(int nVirtKey);
    }

    internal static class GamePad
    {
        private const uint XINPUT_GAMEPAD_A = 0x1000;
        private const byte XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30;

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "#100")]
        private static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);

        internal static bool IsLTplusAPressed(uint controllerIndex = 0)
        {
            XINPUT_STATE state;
            if (XInputGetState(controllerIndex, out state) != 0)
                return false;

            bool ltPressed = state.Gamepad.bLeftTrigger > XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
            bool aPressed = (state.Gamepad.wButtons & XINPUT_GAMEPAD_A) != 0;
            return ltPressed && aPressed;
        }

        internal static bool IsConnected(uint controllerIndex = 0)
        {
            XINPUT_STATE state;
            return XInputGetState(controllerIndex, out state) == 0;
        }
    }

    internal static class Json
    {
        internal static string Serialize<T>(object obj) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream mS = new MemoryStream())
            {
                dcsJson.WriteObject(mS, obj);
                return Encoding.UTF8.GetString(mS.ToArray());
            }
        }

        internal static T Deserialize<T>(string strData) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            byte[] byteArray = Encoding.UTF8.GetBytes(strData);
            MemoryStream mS = new MemoryStream(byteArray);
            T tRet = dcsJson.ReadObject(mS) as T;
            mS.Dispose();
            return (tRet);
        }
    }

    public partial class WinMain : Window
    {
        internal bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string GetFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        // poe.ninja 시세 캐시 (화폐 id → divine 가치)
        // volatile: 백그라운드 FetchNinjaPrices의 참조 교체와 UI 스레드 읽기 사이 가시성 보장.
        // (Dictionary는 교체 후 변경하지 않으므로 참조만 원자적으로 바꾸면 안전. double은 volatile 불가라 제외)
        private volatile Dictionary<string, double> mNinjaCache = new Dictionary<string, double>();
        private double mNinjaExaltedRate = 1.0;
        private DateTime mNinjaLastFetch = DateTime.MinValue;
        private static readonly string[] NinjaTypes = { "Currency", "Runes", "Expedition", "Verisium", "Essences", "SoulCores", "Fragments", "Breach", "UncutGems", "Idols", "LineageSupportGems", "Delirium", "Abyss", "Ritual" };

        private void FetchNinjaPrices()
        {
            string league = mConfigData?.Options?.League ?? "Runes of Aldur";
            string encodedLeague = Uri.EscapeDataString(league);
            var newCache = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            double exaltedRate = 1.0;

            foreach (string type in NinjaTypes)
            {
                try
                {
                    string url = "https://poe.ninja/poe2/api/economy/exchange/current/overview?league=" + encodedLeague + "&type=" + type;
                    string json = SendHTTP(null, url, 10);
                    if (json == null) continue;

                    NinjaOverviewData data = Json.Deserialize<NinjaOverviewData>(json);
                    if (data == null) continue;

                    if (data.Core?.Rates != null && data.Core.Rates.Exalted > 0)
                        exaltedRate = data.Core.Rates.Exalted;

                    if (data.Lines == null) continue;
                    foreach (var line in data.Lines)
                    {
                        if (line.Id != null && line.PrimaryValue > 0)
                            newCache[line.Id] = line.PrimaryValue;
                    }
                }
                catch { }
            }

            if (newCache.Count > 0)
            {
                mNinjaCache = newCache;
                mNinjaExaltedRate = exaltedRate > 0 ? exaltedRate : 1.0;
                mNinjaLastFetch = DateTime.Now;
            }
        }

        // id로 ninja 시세 조회 (divine 기준), 없으면 -1
        private double GetNinjaDivineValue(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            if (mNinjaCache.TryGetValue(id, out double val)) return val;
            return -1;
        }

        private double StrToDouble(string s, double def = 0)
        {
            if ((s ?? "") != "")
            {
                try
                {
                    // 소수점은 항상 '.' (InvariantCulture). 한국 로케일에서 ',' 로 오해되는 것 방지.
                    def = double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
                catch { }
            }

            return def;
        }

        private double DamageToDPS(string damage)
        {
            double dps = 0;
            try
            {
                string[] stmps = Regex.Replace(damage, @"\([a-zA-Z]+\)", "").Split(',');
                for (int t = 0; t < stmps.Length; t++)
                {
                    string[] maidps = (stmps[t] ?? "").Trim().Split('-');
                    if (maidps.Length == 2)
                        dps += double.Parse(maidps[0].Trim()) + double.Parse(maidps[1].Trim());
                }
            }
            catch { }
            return dps;
        }

        private string SendHTTP(string entity, string urlString, int timeout = 5)
        {
            string result = "";

            try
            {
                // WebClient 코드는 테스트할게 있어 만들어둔 코드...
                if (timeout == 0)
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Encoding = UTF8Encoding.UTF8;

                        if (entity == null)
                        {
                            result = webClient.DownloadString(urlString);
                        }
                        else
                        {
                            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                            result = webClient.UploadString(urlString, entity);
                        }
                    }
                }
                else
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(urlString));
                    request.CookieContainer = new CookieContainer();
                    request.UserAgent = RS.UserAgent;
                    request.Timeout = timeout * 1000;

                    if (entity == null)
                    {
                        request.Method = WebRequestMethods.Http.Get;
                    }
                    else
                    {
                        request.Accept = "application/json";
                        request.ContentType = "application/json; charset=UTF-8";
                        request.Method = WebRequestMethods.Http.Post;

                        byte[] data = Encoding.UTF8.GetBytes(entity);
                        request.ContentLength = data.Length;
                        using (Stream reqStream = request.GetRequestStream())
                            reqStream.Write(data, 0, data.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        CaptureRateLimit(response);
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException wex)
            {
                // 429(또는 거래소 차단) 응답도 헤더를 읽어 차단 시각을 반영한다.
                // using으로 응답을 닫아 연결 풀 고갈 방지 (429 반복 시 중요).
                string status = "?";
                using (HttpWebResponse resp = wex.Response as HttpWebResponse)
                {
                    if (resp != null)
                    {
                        CaptureRateLimit(resp);
                        status = ((int)resp.StatusCode).ToString();
                    }
                }

                string logPath = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
                System.IO.File.AppendAllText(logPath, string.Format("[SENDHTTP] {0} STATUS:{1} URL:{2}\r\nERR:{3}\r\nBODY:{4}\r\n\r\n",
                    wex.GetType().Name, status, urlString, wex.Message, entity ?? "null"));
                return null;
            }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
                System.IO.File.AppendAllText(logPath, string.Format("[SENDHTTP] {0} URL:{1}\r\nERR:{2}\r\nBODY:{3}\r\n\r\n", ex.GetType().Name, urlString, ex.Message, entity ?? "null"));
                return null;
            }

            return result;
        }

        // 응답에서 rate limit 헤더를 뽑아 RateLimit 관리자에 먹인다.
        // 정책명(x-rate-limit-policy)을 키로 쓰므로 search/fetch가 자동 분리된다.
        internal static void CaptureRateLimit(HttpWebResponse response)
        {
            try
            {
                if (response == null) return;
                string policy = response.Headers["x-rate-limit-policy"];
                string rule = response.Headers["x-rate-limit-ip"];
                string state = response.Headers["x-rate-limit-ip-state"];
                string retry = response.Headers["Retry-After"];
                if (string.IsNullOrEmpty(policy)) policy = "default";
                RateLimit.Update(policy, rule, state, retry);
            }
            catch { /* 헤더 파싱 실패는 무시 — limit 보호만 약해질 뿐 */ }
        }

        private string GetLapsedTime(string utc)
        {
            string timeString = string.Empty;

            DateTime dateTime = DateTime.ParseExact(utc,
                                           new string[] { "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:sszzz" },
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.AdjustToUniversal);

            //   dateTime = Convert.ToDateTime(dateTime, new CultureInfo("ko-KR"));
            TimeSpan ts = DateTime.UtcNow.Subtract(dateTime);

            int DayPeriod = Math.Abs(ts.Days);

            if (DayPeriod < 1)
            {
                int HourPeriod = Math.Abs(ts.Hours);

                if (HourPeriod < 1)
                {
                    int MinutePeriod = Math.Abs(ts.Minutes);
                    if (MinutePeriod < 1)
                    {
                        int SecondPeriod = Math.Abs(ts.Seconds);
                        return " * " + SecondPeriod.ToString().PadLeft(2, '\u2000') + "초전";
                    }
                    else
                    {
                        return " * " + MinutePeriod.ToString().PadLeft(2, '\u2000')  + "분전";
                    }
                }
                else
                {
                    return " - " + HourPeriod.ToString().PadLeft(2, '\u2000') + "시전";
                }
            }
            else if ((DayPeriod > 0) && (DayPeriod < 7))
            {
                return " ? " + DayPeriod.ToString().PadLeft(2, '\u2000') + "일전";
            }
            else if (DayPeriod == 7)
            {
                return " ? " + "1".PadLeft(2, '\u2000') + "주전";
            }
            else
            {
                return dateTime.ToString("yyyy년 MM월 dd일");
            }
        }

        private string GetClipText(bool isUnicode)
        {
            return Clipboard.GetText(isUnicode ? TextDataFormat.UnicodeText : TextDataFormat.Text);
        }

        private void SetClipText(string text, TextDataFormat textDataFormat)
        {
            var ClipboardThread = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetText(text, textDataFormat);
                        return;
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            });
            ClipboardThread.SetApartmentState(ApartmentState.STA);
            ClipboardThread.IsBackground = false;
            ClipboardThread.Start();
        }

        private void ForegroundMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBox.Show(Application.Current.MainWindow, message, caption, button, icon);
            Native.SetForegroundWindow(Native.FindWindow(RS.PoeClass, RS.PoeCaption));
        }
    }
}