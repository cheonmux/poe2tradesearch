using System.Windows;
using System.Windows.Input;

namespace Poe2TradeSearch
{
    public partial class WinSetting : Window
    {
        public bool UseAutoClip { get; private set; }
        public int CapturedKeycode { get; private set; }
        public bool UseCtrl { get; private set; }

        private int _pendingKeycode = 0;
        private bool _capturing = false;

        public WinSetting(bool useAutoClip, int currentKeycode, bool currentCtrl)
        {
            InitializeComponent();
            UseAutoClip = useAutoClip;
            CapturedKeycode = currentKeycode;
            UseCtrl = currentCtrl;
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

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
