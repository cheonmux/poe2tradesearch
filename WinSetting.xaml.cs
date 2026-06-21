using System.Windows;
using System.Windows.Input;

namespace Poe2TradeSearch
{
    public partial class WinSetting : Window
    {
        public bool UseAutoClip { get; private set; }
        public int CapturedKeycode { get; private set; }
        public bool UseCtrl { get; private set; }
        public int HideDelay { get; private set; }
        public string League { get; private set; }
        public bool UseCtrlWheel { get; private set; }
        public double UiScale { get; private set; }
        public bool GamePadEnabled { get; private set; }
        public string GamePadButton { get; private set; }
        public int HideoutKeycode { get; private set; }
        public int RemainingKeycode { get; private set; }
        public string BackgroundColor { get; private set; }

        private int _pendingKeycode = 0;
        private bool _capturing = false;

        private static readonly (string Label, int Keycode)[] FKeyOptions = new[]
        {
            ("없음", 0),
            ("F1",  112), ("F2",  113), ("F3",  114), ("F4",  115),
            ("F5",  116), ("F6",  117), ("F7",  118), ("F8",  119),
            ("F9",  120), ("F10", 121), ("F11", 122), ("F12", 123),
        };

        public WinSetting(bool useAutoClip, int currentKeycode, bool currentCtrl, int hideDelay, string currentLeague, bool useCtrlWheel, double uiScale, bool gamePadEnabled, string gamePadButton, int hideoutKeycode, int remainingKeycode, string backgroundColor)
        {
            InitializeComponent();
            UseAutoClip = useAutoClip;
            CapturedKeycode = currentKeycode;
            UseCtrl = currentCtrl;
            HideDelay = hideDelay;
            League = currentLeague ?? "Runes of Aldur";
            UseCtrlWheel = useCtrlWheel;
            UiScale = uiScale <= 0 ? 1.0 : uiScale;
            GamePadEnabled = gamePadEnabled;
            GamePadButton = string.IsNullOrEmpty(gamePadButton) ? "A" : gamePadButton;
            HideoutKeycode = hideoutKeycode;
            RemainingKeycode = remainingKeycode;
            BackgroundColor = string.IsNullOrEmpty(backgroundColor) ? "#F0F0F0" : backgroundColor;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (UseAutoClip)
            {
                rbAuto.IsChecked = true;
            }
            else
            {
                rbManual.IsChecked = true;
                ckCtrl.IsChecked = UseCtrl;
                _pendingKeycode = CapturedKeycode;
                UpdateCaptureButton(CapturedKeycode);
            }

            tbHideDelay.Text = HideDelay.ToString();
            ckUseCtrlWheel.IsChecked = UseCtrlWheel;

            foreach (System.Windows.Controls.ComboBoxItem item in cbLeague.Items)
            {
                if (item.Content.ToString() == League)
                {
                    cbLeague.SelectedItem = item;
                    break;
                }
            }
            if (cbLeague.SelectedIndex < 0) cbLeague.SelectedIndex = 0;

            // 글자 크기(UI 배율): Tag(문자열 실수)를 현재 값과 매칭해 선택
            foreach (System.Windows.Controls.ComboBoxItem item in cbUiScale.Items)
            {
                if (double.TryParse((item.Tag ?? "").ToString(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double tagVal)
                    && System.Math.Abs(tagVal - UiScale) < 0.001)
                {
                    cbUiScale.SelectedItem = item;
                    break;
                }
            }
            if (cbUiScale.SelectedIndex < 0) cbUiScale.SelectedIndex = 0;

            ckGamePadEnabled.IsChecked = GamePadEnabled;
            cbGamePadButton.IsEnabled = GamePadEnabled;
            ckGamePadEnabled.Checked += (s, ev) => cbGamePadButton.IsEnabled = true;
            ckGamePadEnabled.Unchecked += (s, ev) => cbGamePadButton.IsEnabled = false;

            foreach (System.Windows.Controls.ComboBoxItem item in cbGamePadButton.Items)
            {
                if (item.Content.ToString() == GamePadButton)
                {
                    cbGamePadButton.SelectedItem = item;
                    break;
                }
            }
            if (cbGamePadButton.SelectedIndex < 0) cbGamePadButton.SelectedIndex = 0;

            PopulateFKeyCombo(cbHideoutKey, HideoutKeycode);
            PopulateFKeyCombo(cbRemainingKey, RemainingKeycode);

            tbBgColor.Text = BackgroundColor;
            ApplyColorPreview(BackgroundColor);
        }

        private void btnPickColor_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.ColorDialog();
            dlg.Color = HexToDrawingColor(tbBgColor.Text);
            dlg.FullOpen = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string hex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
                tbBgColor.Text = hex;
                ApplyColorPreview(hex);
            }
        }

        private void ApplyColorPreview(string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                bdColorPreview.Background = new System.Windows.Media.SolidColorBrush(color);
            }
            catch { bdColorPreview.Background = System.Windows.Media.Brushes.White; }
        }

        private System.Drawing.Color HexToDrawingColor(string hex)
        {
            try { return System.Drawing.ColorTranslator.FromHtml(hex); }
            catch { return System.Drawing.Color.FromArgb(240, 240, 240); }
        }

        private void PopulateFKeyCombo(System.Windows.Controls.ComboBox cb, int selectedKeycode)
        {
            cb.Items.Clear();
            int selectIdx = 0;
            for (int i = 0; i < FKeyOptions.Length; i++)
            {
                cb.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = FKeyOptions[i].Label, Tag = FKeyOptions[i].Keycode });
                if (FKeyOptions[i].Keycode == selectedKeycode) selectIdx = i;
            }
            cb.SelectedIndex = selectIdx;
        }

        // 숫자만 입력 허용
        private void tbHideDelay_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c)) { e.Handled = true; return; }
            }
        }

        private void rbAuto_Checked(object sender, RoutedEventArgs e)
        {
            if (btnCapture == null) return;
            btnCapture.IsEnabled = false;
            ckCtrl.IsEnabled = false;
            _capturing = false;
        }

        private void rbManual_Checked(object sender, RoutedEventArgs e)
        {
            if (btnCapture == null) return;
            btnCapture.IsEnabled = true;
            ckCtrl.IsEnabled = true;
        }

        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            _capturing = true;
            btnCapture.Content = "▶ 키를 누르세요...";
        }

        // Window 레벨에서 모든 키 입력 캡처
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_capturing) return;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // 단독 수식키는 무시
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                e.Handled = true;
                return;
            }

            // ESC는 캡처 취소
            if (key == Key.Escape)
            {
                _capturing = false;
                UpdateCaptureButton(_pendingKeycode);
                e.Handled = true;
                return;
            }

            _pendingKeycode = KeyInterop.VirtualKeyFromKey(key);
            _capturing = false;
            UpdateCaptureButton(_pendingKeycode);
            e.Handled = true;
        }

        private void UpdateCaptureButton(int keycode)
        {
            if (keycode > 0)
            {
                Key key = KeyInterop.KeyFromVirtualKey(keycode);
                btnCapture.Content = key.ToString();
            }
            else
            {
                btnCapture.Content = "키를 누르세요...";
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (rbManual.IsChecked == true)
            {
                if (_pendingKeycode <= 0)
                {
                    MessageBox.Show(this, "단축키를 지정해주세요.", "알림");
                    return;
                }
                UseAutoClip = false;
                CapturedKeycode = _pendingKeycode;
                UseCtrl = ckCtrl.IsChecked == true;
            }
            else
            {
                UseAutoClip = true;
                CapturedKeycode = 0;
                UseCtrl = false;
            }

            // 자동 숨김 시간 (빈값/파싱 실패 시 기본 5초)
            int hd;
            HideDelay = int.TryParse(tbHideDelay.Text, out hd) ? hd : 5;

            League = (cbLeague.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Runes of Aldur";
            UseCtrlWheel = ckUseCtrlWheel.IsChecked == true;

            // 글자 크기(UI 배율): 선택 항목 Tag 파싱(실패 시 1.0)
            if ((cbUiScale.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag is object scaleTag
                && double.TryParse(scaleTag.ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double scaleVal)
                && scaleVal > 0)
            {
                UiScale = scaleVal;
            }
            else
            {
                UiScale = 1.0;
            }

            GamePadEnabled = ckGamePadEnabled.IsChecked == true;
            GamePadButton = (cbGamePadButton.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "A";

            HideoutKeycode = (int?)((cbHideoutKey.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag) ?? 0;
            RemainingKeycode = (int?)((cbRemainingKey.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag) ?? 0;

            BackgroundColor = tbBgColor.Text;

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
