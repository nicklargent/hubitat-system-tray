using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HubitatSystemTray
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            hubitatHostAddress.Text = Properties.Settings.Default.HubitatIpAddress;
            hubitatDeviceId.Text = Properties.Settings.Default.PresenceDeviceId;
            int seconds = Properties.Settings.Default.IdleSeconds;
            /*
            5 seconds (Test Mode)
            1 minute
            5 minutes
            20 minutes
            1 hour
             */

            switch (seconds)
            {
                case 5:
                    idleSeconds.SelectedIndex = 0;
                    break;
                case 60:
                    idleSeconds.SelectedIndex = 1;
                    break;
                case 60 * 5:
                    idleSeconds.SelectedIndex = 2;
                    break;
                case 60 * 20:
                    idleSeconds.SelectedIndex = 3;
                    break;
                case 60 * 60:
                    idleSeconds.SelectedIndex = 4;
                    break;
                default:
                    idleSeconds.Items.Add("Custom (" + seconds + " seconds)");
                    idleSeconds.SelectedIndex = 5;
                    break;

            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.HubitatIpAddress = hubitatHostAddress.Text;
            Properties.Settings.Default.PresenceDeviceId = hubitatDeviceId.Text;

            Match match = Regex.Match(idleSeconds.SelectedItem.ToString(), @"(\d+) (second|minute|hour)");
            if (match.Success)
            {
                int seconds = 0;
                int number = int.Parse(match.Groups[1].Value);
                string unit = match.Groups[2].Value;
                switch (unit)
                {
                    case "second":
                        seconds = number;
                        break;
                    case "minute":
                        seconds = number * 60;
                        break;
                    case "hour":
                        seconds = number * 60 * 60;
                        break;
                }
                Properties.Settings.Default.IdleSeconds = seconds;
            }

            Properties.Settings.Default.Save();
        }
    }
}
