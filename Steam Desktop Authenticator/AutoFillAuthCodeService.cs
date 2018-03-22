using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamMemLib;
using SteamAuth;

namespace Steam_Desktop_Authenticator
{
    class AutoFillAuthCodeService
    {
        #region "Singleton"
        private static AutoFillAuthCodeService instance = null;

        public static AutoFillAuthCodeService GetInstance()
        {
            if (instance == null)
                instance = new AutoFillAuthCodeService();

            return instance;
        }
        #endregion

        private AutoFillAuthCodeService()
        {
        }

        public void Start()
        {
            // create another thread for handling auto fill functionality
            Task.Factory.StartNew(Main, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async void Main()
        {
            while (true)
            {
                await Task.Delay(1000);

                if (!Manifest.GetManifest().AutoFillAuthCode)
                    continue;

                // Find Steam Guard window
                var steam = new Steam();
                var hWnd = await steam.WaitForSteamGuardWindow();

                if (!Manifest.GetManifest().AutoFillAuthCode || hWnd == IntPtr.Zero)
                    continue;

                // perform auto fill
                await AutoFill(steam, hWnd);
            }
        }

        private async Task AutoFill(Steam steam, IntPtr hWnd)
        {
            var mainForm = MainForm.GetInstance();
            if (mainForm == null)
                return;

            var account = mainForm.allAccounts.Where(acc => acc.AccountName.ToLower() == steam.UserNameField.ToLower()).FirstOrDefault();
            if (account == null)
                return;

            // enable the confirm button
            while (steam.AuthCodeField.Length == 0)
            {
                Win32.SetForegroundWindow(hWnd);
                Win32.SendMessage(hWnd, Win32.WM_CHAR, 0x41, IntPtr.Zero);

                await Task.Delay(100);
            }

            steam.AuthCodeField = account.GenerateSteamGuardCodeForTime(mainForm.steamTime);

            // get it confirmed
            Win32.SendMessage(hWnd, Win32.WM_KEYDOWN, Win32.VK_RETURN, IntPtr.Zero);
            Win32.SendMessage(hWnd, Win32.WM_KEYUP, Win32.VK_RETURN, IntPtr.Zero);
        }
    }
}
