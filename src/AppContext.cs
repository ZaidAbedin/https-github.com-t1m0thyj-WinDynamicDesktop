﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace WinDynamicDesktop
{
    class AppContext : ApplicationContext
    {
        private Mutex _mutex;

        public static NotifyIcon notifyIcon;
        public static WallpaperChangeScheduler wcsService;

        public AppContext()
        {
            EnforceSingleInstance();

            JsonConfig.LoadConfig();
            ThemeManager.Initialize();

            InitializeGui();
            wcsService = new WallpaperChangeScheduler();

            ThemeManager.DownloadMissingImages();
            LocationManager.Initialize();

            if (LocationManager.isReady && ThemeManager.isReady)
            {
                wcsService.RunScheduler();
            }

            if (!UwpDesktop.IsRunningAsUwp())
            {
                UpdateChecker.Initialize();
            }
        }

        private void EnforceSingleInstance()
        {
            _mutex = new Mutex(true, @"Global\WinDynamicDesktop", out bool isFirstInstance);
            GC.KeepAlive(_mutex);

            if (!isFirstInstance)
            {
                MessageBox.Show("Another instance of WinDynamicDesktop is already running. " +
                    "You can access it by right-clicking the icon in the system tray.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                Environment.Exit(0);
            }
        }

        private void InitializeGui()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            notifyIcon = new NotifyIcon
            {
                Visible = !JsonConfig.settings.hideTrayIcon,
                Icon = Properties.Resources.AppIcon,
                Text = "WinDynamicDesktop",
            };
            notifyIcon.ContextMenu = MainMenu.GetMenu();
            notifyIcon.MouseUp += new MouseEventHandler(OnNotifyIconMouseUp);
        }

        public static void BackgroundNotify()
        {
            if (!JsonConfig.firstRun || !LocationManager.isReady || !ThemeManager.isReady)
            {
                return;
            }

            notifyIcon.BalloonTipTitle = "WinDynamicDesktop";
            notifyIcon.BalloonTipText = "The app is still running in the background. " +
                "You can access it at any time by right-clicking on this icon.";
            notifyIcon.ShowBalloonTip(10000);

            JsonConfig.firstRun = false;    // Don't show this message again
        }

        private void OnNotifyIconMouseUp(object sender, MouseEventArgs e)
        {
            // Hack to show menu on left click from https://stackoverflow.com/a/2208910/5504760
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            foreach (Form form in Application.OpenForms)
            {
                form.Close();
            }

            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
            }
        }
    }
}