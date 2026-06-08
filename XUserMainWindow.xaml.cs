using System;
using System.Linq;
using System.Windows;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class UserMainWindow : Window
    {
        public UserMainWindow()
        {
            InitializeComponent();
            LoadUserData();
            this.Closed += UserMainWindow_Closed;
            MainFrame.Navigate(new UserActivity());
        }

        private void LoadUserData()
        {
            using (var db = new AMONICEntities3())
            {
                txtWelcome.Text = $"Hi {SessionManager.CurrentUserFirstName} {SessionManager.CurrentUserLastName}, Welcome to AMONIC Airlines Automation System";

                DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var sessions = db.UserSessions
                    .Where(s => s.UserID == SessionManager.CurrentUserID && s.LoginTime >= thirtyDaysAgo)
                    .ToList();

                int totalSeconds = sessions
                    .Where(s => s.LogoutTime.HasValue)
                    .Sum(s => (int)(s.LogoutTime.Value - s.LoginTime).TotalSeconds);

                TimeSpan timeSpent = TimeSpan.FromSeconds(totalSeconds);
                txtTimeSpent.Text = $"{timeSpent.Hours:D2}:{timeSpent.Minutes:D2}:{timeSpent.Seconds:D2}";

                int crashCount = sessions.Count(s => s.CrashReason != null);
                txtCrashes.Text = crashCount.ToString();
            }
        }

        private void UserMainWindow_Closed(object sender, EventArgs e)
        {
            using (var db = new AMONICEntities3())
            {
                var session = db.UserSessions.Find(SessionManager.CurrentSessionID);
                if (session != null && session.LogoutTime == null)
                {
                    session.LogoutTime = DateTime.Now;
                    session.LogoutType = "MANUAL";
                    db.SaveChanges();
                }
            }
        }

        private void MnuBooking_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BookingSearch());
        }

        private void MnuActivity_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UserActivity());
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }
    }
}