using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Poe2TradeSearch
{
    /// <summary>
    /// WinMain.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinMain : Window
    {
        private static IntPtr mMainHwnd;
        private IntPtr mNextClipBoardViewerHWnd = IntPtr.Zero;
        private static bool mInstalledHotKey = false;
        public static bool mPausedHotKey = false;
        private Thread mGamePadThread;
        private bool mGamePadThreadRunning = false;

        private bool mHotkeyProcBlock = false;
        private bool mClipboardBlock = false;
        private volatile bool mLockUpdatePrice = false;
        private HwndSource mHwndSource = null;

        DispatcherTimer mAutoSearchTimer;

        public WinMain()
        {
            InitializeComponent();

            // 거래소/ninja API는 TLS 1.2+ 요구. .NET 4.6 기본값은 구버전 TLS를 포함할 수 있어 명시.
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

            Clipboard.Clear();
            mAdministrator = IsAdministrator();
            mAutoSearchTimer = new DispatcherTimer();
            mAutoSearchTimer.Interval = TimeSpan.FromSeconds(1);
            mAutoSearchTimer.Tick += new EventHandler(AutoSearchTimer_Tick);
            tkPriceInfo.Tag = tkPriceInfo.Text = "시세를 검색하려면 클릭해주세요";

            // 시세 창에 마우스가 올라가 있으면 자동 숨김 보류
            this.MouseEnter += (s, ev) => mMouseOverWindow = true;
            this.MouseLeave += (s, ev) => { mMouseOverWindow = false; RestartHideTimer(); };

            // 입력 필드 편집(키보드 포커스) 중에는 숨김 보류. 포커스가 창 밖으로 나가면 타이머 재시작.
            this.IsKeyboardFocusWithinChanged += (s, ev) =>
            {
                if (this.IsKeyboardFocusWithin)
                    mHideTimer?.Stop();
                else
                    RestartHideTimer();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Setting())
            {
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            // 저장된 창 위치 복원 (없으면 XAML 기본값 30,30 유지)
            RestoreWindowPosition();

            string outString = "";

            if (!LoadData(out outString))
            {
                this.Visibility = Visibility.Hidden;
                Application.Current.Shutdown(0xD); //ERROR_INVALID_DATA
                return;
            }

            /////////////
            RS.ServerType = RS.ServerType == "" ? mConfigData.Options.League : RS.ServerType;
            RS.ServerType = RS.ServerType.Replace(" ", "%20");
            RS.ServerLang = 0; // 한국 서버 고정

            ComboBox[] cbs = { cbOrbs, cbSplinters, cbCorrupt, cbInfluence1, cbInfluence2 };
            foreach (ComboBox cb in cbs)
            {
                ControlTemplate ct = cb.Template;
                Popup popup = ct.FindName("PART_Popup", cb) as Popup;
                if (popup != null)
                    popup.Placement = PlacementMode.Top;
            }

            Grid input = cbName.Template.FindName("templateRoot", cbName) as Grid;
            if (input != null)
            {
                ToggleButton toggleButton = input.FindName("toggleButton") as ToggleButton;
                if (toggleButton != null)
                {
                    Border border = toggleButton.Template.FindName("templateRoot", toggleButton) as Border;
                    if (border != null)
                    {
                        border.BorderThickness = new Thickness(0, 0, 0, 1);
                        border.Background = System.Windows.Media.Brushes.Transparent;
                    }
                }
            }

            cbName.FontSize = cbOrbs.FontSize + 2;

            foreach (ParserDictionary item in mParserData.Currency.Entries)
            {
                if (item.Hidden == false)
                    cbOrbs.Items.Add(item.Text[0]);
            }
            // 기본값: 엑잘티드 오브
            int exaltedIdx = cbOrbs.Items.IndexOf("엑잘티드 오브");
            cbOrbs.SelectedIndex = exaltedIdx >= 0 ? exaltedIdx : 0;

            this.Title += " - " + RS.ServerType;
            this.Visibility = Visibility.Hidden;

            // poe.ninja 시세 백그라운드 프리로드
            System.Threading.Tasks.Task.Run(() => FetchNinjaPrices());

            /////////////////
            mMainHwnd = new WindowInteropHelper(this).Handle;
            mHwndSource = HwndSource.FromHwnd(mMainHwnd);
            mHwndSource.AddHook(new HwndSourceHook(WndProc));

            if (mAdministrator)
            {
                foreach (var item in mConfigData.Shortcuts)
                {
                    if (item.Keycode > 0 && (item.Value ?? "") != "")
                    {
                        if (!mDisableClip && item.Value.ToLower() == "{run}")
                            mDisableClip = true;
                        else if (item.Value.ToLower() == "{close}")
                            closeKeyCode = item.Keycode;
                    }
                }

                // 창 활성화 후킹 사용시 가끔 꼬여서 타이머로 교체
                //InstallRegisterHotKey();
                //EventHook.EventAction += new EventHandler(WinEvent);
                //EventHook.Start();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();
            }

            // 시세 표시 후 N초 뒤 시세 창 자동 숨김 (권한 무관하게 동작). 시간은 설정값(HideDelay).
            mHideTimer = new DispatcherTimer();
            mHideTimer.Tick += new EventHandler(HideTimer_Tick);
            ApplyHideDelay();

            if (!mDisableClip)
            {
                mNextClipBoardViewerHWnd = Native.SetClipboardViewer(mMainHwnd);
            }

            if (GamePad.IsConnected())
            {
                mGamePadThreadRunning = true;
                mGamePadThread = new Thread(GamePadPollingLoop);
                mGamePadThread.IsBackground = true;
                mGamePadThread.Start();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            long ip = Native.SetWindowLong(
                helper.Handle,
                Native.GWL_EXSTYLE,
                Native.GetWindowLong(helper.Handle, Native.GWL_EXSTYLE) | Native.WS_EX_NOACTIVATE
            );
            btnClose.Background = btnSearch.Background;
            btnClose.Foreground = btnSearch.Foreground;
        }

        private void TbOpt0_0_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsFocused)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                long ip = Native.SetWindowLong(
                    helper.Handle,
                    Native.GWL_EXSTYLE,
                    Native.GetWindowLong(helper.Handle, Native.GWL_EXSTYLE) & ~Native.WS_EX_NOACTIVATE
                );
                Native.SetForegroundWindow(helper.Handle);
                btnClose.Background = System.Windows.SystemColors.HighlightBrush;
                btnClose.Foreground = System.Windows.SystemColors.HighlightTextBrush;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sEntity;
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible && cbOrbs.SelectedIndex >= 0)
            {
                exchange = new string[2];

                ParserDictionary exchange_item1 = (!string.IsNullOrEmpty(mItemBaseName.NameKR) ? GetExchangeItem(0, mItemBaseName.NameKR) : null)
                                                  ?? GetExchangeItem(0, mItemBaseName.TypeKR);
                ParserDictionary exchange_item2 = GetExchangeItem(0, (string)cbOrbs.SelectedValue);

                if (exchange_item1 == null || exchange_item2 == null)
                {
                    ForegroundMessage("선택한 교환 아이템 코드가 잘못되었습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                exchange[0] = exchange_item1.Id;
                exchange[1] = exchange_item2.Id;

                Process.Start(
                        RS.ExchangeUrl[RS.ServerLang] + RS.ServerType + "/?q="
                        + Uri.EscapeDataString(
                            "{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + exchange[0] + "\"],\"want\":[\"" + exchange[1] + "\"]}}"
                        )
                    );
            }
            else
            {
                sEntity = CreateJson(GetItemOptions(), false);

                if (sEntity == null || sEntity == "")
                {
                    ForegroundMessage("Json 생성을 실패했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (mConfigData.Options.ServerRedirect)
                {
                    Process.Start(RS.TradeApi[RS.ServerLang] + RS.ServerType + "/?redirect&source=" + Uri.EscapeDataString(sEntity));
                }
                else
                {
                    string request_result = null;

                    // 마우스 훜시 프로그램에 딜레이가 생겨 쓰레드 처리
                    Thread thread = new Thread(() =>
                    {
                        request_result = SendHTTP(sEntity, RS.TradeApi[RS.ServerLang] + RS.ServerType, mConfigData.Options.ServerTimeout);
                        if ((request_result ?? "") != "")
                        {
                            try
                            {
                                ResultData resultData = Json.Deserialize<ResultData>(request_result);
                                // API 응답 id를 브라우저로 열기 전 검증 (URL 조작/명령 탈취 방지)
                                if (IsSafeTradeId(resultData.ID))
                                    Process.Start(RS.TradeUrl[RS.ServerLang] + RS.ServerType + "/" + resultData.ID);
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("브라우저 열기 실패: " + ex.Message); }
                        }
                    });

                    thread.Start();
                    thread.Join();

                    if ((request_result ?? "") == "")
                    {
                        ForegroundMessage(
                            "현재 거래소 접속이 원활하지 않을 수 있습니다." + '\n'
                            + "한/영 서버의 거래소 접속을 확인 하신후 다시 시도하세요.",
                            "검색 실패",
                            MessageBoxButton.OK, MessageBoxImage.Information
                        );
                        return;
                    }
                }
            }

            Hide();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // "최소화" 버튼: 트레이로 숨김 (완전 종료는 창 오른쪽 위 X 버튼에서만).
            SaveWindowPosition(); // 숨김 전 위치 저장
            Hide();
        }

        // 현재 창 위치를 Config(Position="Left,Top")에 저장.
        private void SaveWindowPosition()
        {
            try
            {
                if (mConfigData?.Options == null) return;
                // 정상 좌표일 때만 저장 (최소화/숨김 상태의 NaN·음수 비정상값 무시)
                if (double.IsNaN(this.Left) || double.IsNaN(this.Top)) return;
                mConfigData.Options.Position =
                    ((int)this.Left).ToString() + "," + ((int)this.Top).ToString();
                SaveConfig();
            }
            catch { /* 위치 저장 실패는 무시 (UX 비핵심) */ }
        }

        // 저장된 Position을 창 위치로 복원. 화면 밖이면 보정.
        private void RestoreWindowPosition()
        {
            try
            {
                string pos = mConfigData?.Options?.Position;
                if (string.IsNullOrWhiteSpace(pos)) return;

                string[] parts = pos.Split(',');
                if (parts.Length != 2) return;
                if (!int.TryParse(parts[0].Trim(), out int left)) return;
                if (!int.TryParse(parts[1].Trim(), out int top)) return;

                // 가상 화면 경계 안으로 클램프 (모니터 분리/해상도 변경 대비)
                double vLeft = SystemParameters.VirtualScreenLeft;
                double vTop = SystemParameters.VirtualScreenTop;
                double vRight = vLeft + SystemParameters.VirtualScreenWidth;
                double vBottom = vTop + SystemParameters.VirtualScreenHeight;

                // 최소 100px는 화면 안에 보이도록 (완전 화면 밖 방지)
                if (left < vLeft) left = (int)vLeft;
                if (top < vTop) top = (int)vTop;
                if (left > vRight - 100) left = (int)(vRight - 100);
                if (top > vBottom - 100) top = (int)(vBottom - 100);

                this.Left = left;
                this.Top = top;
            }
            catch { /* 복원 실패 시 XAML 기본 위치 사용 */ }
        }

        private void cbAiiCheck_Checked(object sender, RoutedEventArgs e)
        {
            bool is_checked = e.RoutedEvent.Name == "Checked";

            for (int i = 0; i < 10; i++)
            {
                if (((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled == true)
                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = is_checked;
            }
        }

        private void CbOrbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Name == "cbOrbs")
            {
                cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
                cbSplinters.SelectedIndex = 0;
                cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;
                cbSplinters.FontWeight = FontWeights.Normal;
            }
            else
            {
                cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
                cbOrbs.SelectedIndex = 0;
                cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
                cbOrbs.FontWeight = FontWeights.Normal;
            }

            ((ComboBox)sender).FontWeight = ((ComboBox)sender).SelectedIndex == 0 ? FontWeights.Normal : FontWeights.SemiBold;

            SetSearchButtonText(RS.ServerLang == 0);
            TkPrice_MouseLeftButtonDown(null, null);
        }

        private void TkPrice_Mouse_EnterOrLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            tkPriceInfo.Foreground = tkPriceCount.Foreground =
                e.RoutedEvent.Name == "MouseEnter" ? System.Windows.SystemColors.HighlightBrush : System.Windows.SystemColors.WindowTextBrush;
        }

        private void TkPrice_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string[] exchange = null;

            if (bdExchange.Visibility == Visibility.Visible && cbOrbs.SelectedIndex >= 0)
            {
                return; // 화폐 시세는 ninja 캐시에서 자동 표시, 클릭 새로고침 불필요
                exchange = new string[2];

                if (mItemBaseName == null)
                {
                    tkPriceInfo.Text = "아이템을 먼저 복사해주세요.";
                    return;
                }

                ParserDictionary exchange_item1 = (!string.IsNullOrEmpty(mItemBaseName.NameKR) ? GetExchangeItem(0, mItemBaseName.NameKR) : null)
                                                  ?? GetExchangeItem(0, mItemBaseName.TypeKR);
                ParserDictionary exchange_item2 = GetExchangeItem(0, (string)cbOrbs.SelectedValue);

                if (exchange_item1 == null || exchange_item2 == null)
                {
                    liPrice.Items.Clear();
                    tkPriceCount.Text = "";
                    tkPriceInfo.Text = "선택한 교환 아이템 코드가 잘못되었습니다.";
                    cbPriceListTotal.Text = "0/0 검색";
                    return;
                }

                exchange[0] = exchange_item1.Id;
                exchange[1] = exchange_item2.Id;
            }

            UpdatePriceThreadWorker(exchange != null ? null : GetItemOptions(), exchange);
        }

        private void tkPriceInfo_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            tabControl1.SelectedIndex = tabControl1.SelectedIndex == 0 ? 1 : 0;
        }

        private void tkPrice_ReSet(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tkPriceCount != null)
                {
                    tkPriceInfo.Foreground = tkPriceCount.Foreground = System.Windows.Media.Brushes.DeepPink;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("가격 정보 색상 변경 실패: " + ex.Message); }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                cbPriceListTotal.Visibility = Visibility.Visible;
                if (tkPriceInfo != null && ((Grid)tkPriceInfo.Parent).Name == "gdTabItem1")
                {
                    gdTabItem1.Children.Remove(tkPriceInfo);
                    gdTabItem1.Children.Remove(tkPriceCount);
                    gdTabItem2.Children.Add(tkPriceInfo);
                    gdTabItem2.Children.Add(tkPriceCount);
                }
                tbHelpText.Text = "최소 값 단위는 카오스 오브";
            }
            else
            {
                cbPriceListTotal.Visibility = Visibility.Hidden;
                if (tkPriceInfo != null && ((Grid)tkPriceInfo.Parent).Name == "gdTabItem2")
                {
                    gdTabItem2.Children.Remove(tkPriceInfo);
                    gdTabItem2.Children.Remove(tkPriceCount);
                    gdTabItem1.Children.Add(tkPriceInfo);
                    gdTabItem1.Children.Add(tkPriceCount);
                }
                Random r = new Random();
                if (r.Next(2) == 1)
                {
                    tbHelpText.Text = "저항 옆 체크시 합산 검색";
                }
                else
                {
                    tbHelpText.Text = "시세 좌클릭은 재검색";
                }
            }
        }

        private void cbName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSearchButtonText(true);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://buymeacoffee.com/poe2tools?utm_source=poe2tools_app&utm_medium=button&utm_campaign=coffee_support&utm_content=ko_KR");
            }
            catch (Exception)
            {
                ForegroundMessage("링크 열기에 실패했습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Application.Current.MainWindow,
                "버전: " + GetFileVersion() + " (DATA." + mFilterData[0].Upddate + ")" + '\n' + '\n'
                + "원작: https://github.com/phiDelPark/PoeTradeSearch" + '\n'
                + "POE2 포팅: https://poe2tools.net/" + '\n' + '\n'
                + "시세 보는법: 검색수[.+] 최소값 ~ 최대값 = 많은[수] 1 ~ 2위" + '\n' + '\n'
                + "   F11) 일시 중지" + '\n'
                + "   ESC) 창 닫기" + '\n' + '\n'
                + "리그/검색 옵션은 ⚙ 버튼 또는 data\\Config.txt 에서 설정 가능합니다." + '\n'
                + "참고: FiltersKO.txt를 삭제 후 실행하면 최신 데이터로 자동 업데이트합니다.",
                "POE2 거래소 검색"
                );

            Native.SetForegroundWindow(Native.FindWindow(RS.PoeClass, RS.PoeCaption));
        }

        private void Button_Click_Setting(object sender, RoutedEventArgs e)
        {
            // 현재 {Run} 단축키 항목 찾기
            ConfigShortcut runShortcut = null;
            if (mConfigData.Shortcuts != null)
                runShortcut = System.Array.Find(mConfigData.Shortcuts, x => (x.Value ?? "").ToLower() == "{run}");

            bool useAutoClip = !mDisableClip;
            int currentKeycode = runShortcut?.Keycode ?? 0;
            bool currentCtrl = runShortcut?.Ctrl ?? false;

            int currentHideDelay = mConfigData.Options.HideDelay;

            WinSetting dlg = new WinSetting(useAutoClip, currentKeycode, currentCtrl, currentHideDelay);
            dlg.Owner = this;

            if (dlg.ShowDialog() == true)
            {
                mConfigData.Options.HideDelay = dlg.HideDelay;
                ApplyHideDelay();
                ApplyShortcutSetting(dlg.UseAutoClip, dlg.CapturedKeycode, dlg.UseCtrl);
            }
        }

        private void ApplyShortcutSetting(bool useAutoClip, int keycode, bool useCtrl)
        {
            // 핫키 해제 (재등록을 위해 강제 해제)
            if (mAdministrator)
            {
                mInstalledHotKey = true;
                RemoveRegisterHotKey();
            }

            // {Run} 항목 업데이트
            if (mConfigData.Shortcuts != null)
            {
                ConfigShortcut runShortcut = System.Array.Find(mConfigData.Shortcuts, x => (x.Value ?? "").ToLower() == "{run}");
                if (runShortcut != null)
                {
                    runShortcut.Keycode = useAutoClip ? 0 : keycode;
                    runShortcut.Ctrl = useAutoClip ? false : useCtrl;
                }
            }

            // 클립보드 감지 모드 전환
            bool wasAutoClip = !mDisableClip;
            mDisableClip = !useAutoClip;

            if (useAutoClip && wasAutoClip == false)
            {
                // 수동 → 자동: 클립보드 감시 등록
                if (mNextClipBoardViewerHWnd != IntPtr.Zero)
                    Native.ChangeClipboardChain(mMainHwnd, mNextClipBoardViewerHWnd);
                mNextClipBoardViewerHWnd = Native.SetClipboardViewer(mMainHwnd);
            }
            else if (!useAutoClip && wasAutoClip)
            {
                // 자동 → 수동: 클립보드 감시 해제
                if (mNextClipBoardViewerHWnd != IntPtr.Zero)
                    Native.ChangeClipboardChain(mMainHwnd, mNextClipBoardViewerHWnd);
                mNextClipBoardViewerHWnd = IntPtr.Zero;
            }

            // Config.txt 저장
            SaveConfig();

            // 핫키 재등록
            if (mAdministrator)
                InstallRegisterHotKey();

            string msg;
            if (useAutoClip)
                msg = "Ctrl+C 자동 감지 모드로 변경되었습니다.";
            else if (mAdministrator)
                msg = "단축키가 설정되었습니다.";
            else
                msg = "단축키가 설정되었지만, 관리자 권한이 없어 작동하지 않습니다.\n프로그램을 관리자 권한으로 다시 실행해주세요.";
            MessageBox.Show(this, msg, "설정 완료");
        }

        private void cbPriceListCount_DropDownOpened(object sender, EventArgs e)
        {
            // 탭 컨트로 뒤에 있어서 Window_Loaded 에서 작동안해 여기서 처리
            if (cbPriceListCount.Tag == null)
            {
                ControlTemplate ct = cbPriceListCount.Template;
                Popup popup = ct.FindName("PART_Popup", cbPriceListCount) as Popup;
                if (popup != null)
                    popup.Placement = PlacementMode.Top;
                cbPriceListCount.Tag = 1;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (closeKeyCode > 0 && KeyInterop.VirtualKeyFromKey(e.Key) == closeKeyCode)
                Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 오른쪽 위 X 버튼: 종료 확인 후 완전 종료. (최소화 버튼은 Hide()만 함)
            MessageBoxResult result = MessageBox.Show(this, "프로그램을 종료하시겠습니까?", "종료 확인",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SaveWindowPosition(); // 종료 전 현재 창 위치 저장
                e.Cancel = false;
                Application.Current.Shutdown();
                return;
            }

            e.Cancel = true;
            SaveWindowPosition(); // 숨김 시에도 위치 저장
            Keyboard.ClearFocus();
            this.Visibility = Visibility.Hidden;
            // 자동 시세 검색으로 바뀐 텍스트 닫을때 초기화
            tkPriceCount.Text = "";
            tkPriceInfo.Text = (string)tkPriceInfo.Tag;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mGamePadThreadRunning = false;

            // 메시지 훅 해제 (상주 앱 자원 정리)
            if (mHwndSource != null)
            {
                mHwndSource.RemoveHook(new HwndSourceHook(WndProc));
                mHwndSource = null;
            }

            if (mNextClipBoardViewerHWnd != IntPtr.Zero)
                Native.ChangeClipboardChain(mMainHwnd, mNextClipBoardViewerHWnd);

            if (mAdministrator && mConfigData != null)
            {
                if (mInstalledHotKey)
                    RemoveRegisterHotKey();

            }
        }

        private bool mGamePadLTAprevPressed = false;

        private void GamePadPollingLoop()
        {
            while (mGamePadThreadRunning)
            {
                try
                {
                    bool pressed = GamePad.IsLTplusAPressed();

                    if (pressed && !mGamePadLTAprevPressed)
                    {
                        mGamePadLTAprevPressed = true;

                        if (!mPausedHotKey && Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
                        {
                            // 1) UI 스레드에서 Ctrl+C 전송만 (블로킹 없음)
                            Dispatcher.Invoke(() =>
                            {
                                mClipboardBlock = true;
                                System.Windows.Forms.SendKeys.SendWait("^{c}");
                            });

                            // 2) 클립보드가 채워질 때까지 대기는 백그라운드 스레드에서 (UI 안 멈춤)
                            Thread.Sleep(300);

                            // 3) UI 스레드에서 클립보드 읽기 + 파싱
                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) ||
                                        System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text))
                                        ItemTextParser(GetClipText(System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText)));
                                }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("클립보드 파싱 실패: " + ex.Message); }
                                mClipboardBlock = false;
                            });
                        }
                    }
                    else if (!pressed)
                    {
                        mGamePadLTAprevPressed = false;
                    }
                }
                catch { }

                Thread.Sleep(100);
            }
        }
    }
}