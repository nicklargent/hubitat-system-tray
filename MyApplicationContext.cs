using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Collections.Specialized;
using System.Threading;

namespace HomeConnectTray
{
    public class MyApplicationContext : ApplicationContext
	{
		NotifyIcon notifyIcon;
		System.Timers.Timer idleTimer;

		public MyApplicationContext()
		{
			MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
			MenuItem settingsMenuItem = new MenuItem("Settings", new EventHandler(Settings));

			notifyIcon = new NotifyIcon();
			updateTrayIcon();
			notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { settingsMenuItem, exitMenuItem });
			notifyIcon.Visible = true;

			idleTimer = new System.Timers.Timer(1000 * 1);
			idleTimer.Elapsed += idleTimer_Elapsed;
			idleTimer.Start();

			if (Properties.Settings.Default.HubitatIpAddress == "" || Properties.Settings.Default.PresenceDeviceId == "")
            {
				Settings(null, null);
            }
		}

		private void updateTrayIcon()
		{
			bool connected = false;
			if (_lastNotifySuccess.HasValue && _lastNotifySuccess.Value)
				connected = true;

			string filename = "Resources\\home_" + (connected ? "connected" : "disconnected") + ((_idleSent.HasValue && _idleSent.Value) ? "_idle" : "") + ".ico";
			notifyIcon.Icon = new Icon(filename);
		}

		private async Task<bool> Post(string id, string method)
		{
			using (var client = new WebClient())
			{
				try
				{
					var values = new NameValueCollection();
					values["id"] = id;
					values["method"] = method;
					System.Diagnostics.Debug.WriteLine("Begin POST (id: " + id + ", method: " + method + ")");
                    await client.UploadValuesTaskAsync("http://" + Properties.Settings.Default.HubitatIpAddress + "/device/runmethod", values);
					System.Diagnostics.Debug.WriteLine("End POST");
					_lastNotifySuccess = true;
					return true;
				}
				catch (Exception ex)
				{
					_lastNotifySuccess = false;
					System.Diagnostics.Debug.WriteLine(ex.ToString());
					return false;
				}
			}
		}

		private bool? _idleSent = null;
		private bool? _lastNotifySuccess = null;
		private static AutoResetEvent _timerShouldProcess = new AutoResetEvent(true);

		private bool IsIdle()
		{
			long idleTime = IdleTimeFinder.GetIdleTime();
			return (idleTime > 1000 * Properties.Settings.Default.IdleSeconds);
		}

		async void idleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Properties.Settings.Default.HubitatIpAddress == "" || Properties.Settings.Default.PresenceDeviceId == "")
				return;

			if (_timerShouldProcess.WaitOne(0))
            {
				//System.Diagnostics.Debug.WriteLine("Begin idleTimer_Elapsed");

				bool isIdle = IsIdle();
				if (_idleSent == null || isIdle != _idleSent)
				{
					System.Diagnostics.Debug.WriteLine("Idle: " + isIdle);
					if (await Post(Properties.Settings.Default.PresenceDeviceId, isIdle ? "inactive" : "active"))
						_idleSent = isIdle;
					updateTrayIcon();
				}
				//System.Diagnostics.Debug.WriteLine("End idleTimer_Elapsed");
				_timerShouldProcess.Set();
			}
			else
            {
				//System.Diagnostics.Debug.WriteLine("Skipping idleTimer_Elapsed");
			}
		}

		void Settings(object sender, EventArgs e)
        {
			SettingsForm form = new SettingsForm();
			form.ShowDialog();
        }

		async void Exit(object sender, EventArgs e)
		{
			// We must manually tidy up and remove the icon before we exit.
			// Otherwise it will be left behind until the user mouses over.
			notifyIcon.Visible = false;

			// Send an inactive message so we don't leave the sensor in a triggered state.
			await Post(Properties.Settings.Default.PresenceDeviceId, "inactive");
			Application.Exit();
		}
	}
}
