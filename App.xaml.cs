using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Poe2TradeSearch
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private string logFilePath;

        private System.Windows.Forms.NotifyIcon TrayIcon;

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            RunException(e.Exception);
            e.Handled = true;
        }

        private void RunException(Exception ex)
        {
            try
            {
                File.AppendAllText(logFilePath, String.Format("{0} Error:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace));
            }
            catch { }

            if (ex.InnerException != null)
                RunException(ex.InnerException);
            else
                Application.Current.Shutdown(ex.HResult);
        }

        private Mutex m_Mutex = null;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && (m_Mutex != null))
            {
                m_Mutex.ReleaseMutex();
                m_Mutex.Close();
                m_Mutex = null;
            }
        }

        public void Dispose()
        {
            TrayIcon.Visible = false;
            TrayIcon.Dispose();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            Assembly assembly = Assembly.GetExecutingAssembly();
            String MutexName = String.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);
            m_Mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("애플리케이션이 이미 시작되었습니다.", "중복 실행", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(-1);
                return;
            }

            logFilePath = Assembly.GetExecutingAssembly().Location;
            logFilePath = logFilePath.Remove(logFilePath.Length - 4) + ".log";

            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            Application.Current.DispatcherUnhandledException += AppDispatcherUnhandledException;

            Uri uri = new Uri("pack://application:,,,/Poe2TradeSearch;component/Icon1.ico");
            using (Stream iconStream = Application.GetResourceStream(uri).Stream)
            {
                TrayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = new Icon(iconStream),
                    Visible = true
                };

                var trayMenu = new System.Windows.Forms.ContextMenuStrip();
                var menuOpen = new System.Windows.Forms.ToolStripMenuItem("창 열기");
                var menuSite = new System.Windows.Forms.ToolStripMenuItem("POE2TOOLS 열기");
                var menuSep  = new System.Windows.Forms.ToolStripSeparator();
                var menuExit = new System.Windows.Forms.ToolStripMenuItem("종료");

                menuOpen.Click += (s, a) =>
                {
                    var win = Application.Current.MainWindow;
                    if (win != null) { win.Show(); win.Activate(); }
                };
                menuSite.Click += (s, a) =>
                {
                    System.Diagnostics.Process.Start("https://poe2tools.net/");
                };
                menuExit.Click += (s, a) =>
                {
                    Application.Current.Shutdown();
                };

                trayMenu.Items.Add(menuOpen);
                trayMenu.Items.Add(menuSite);
                trayMenu.Items.Add(menuSep);
                trayMenu.Items.Add(menuExit);
                TrayIcon.ContextMenuStrip = trayMenu;

                TrayIcon.MouseClick += (sender, args) =>
                {
                    if (args.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        var win = Application.Current.MainWindow;
                        if (win != null)
                        {
                            win.Show();
                            win.Activate();
                            if (win.WindowState == System.Windows.WindowState.Minimized)
                                win.WindowState = System.Windows.WindowState.Normal;
                        }
                    }
                };
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispose();
            base.OnExit(e);
        }
    }
}