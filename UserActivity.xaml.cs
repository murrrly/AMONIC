using System;
using System.Linq;
using System.Windows.Controls;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class UserActivity : Page
    {
        public UserActivity()
        {
            InitializeComponent();
            LoadSessions();
        }

        private void LoadSessions()
        {
            using (var db = new AMONICEntities3())
            {
                var sessions = db.UserSessions
                    .Where(s => s.UserID == SessionManager.CurrentUserID)
                    .ToList()
                    .Select(s => new
                    {
                        LoginTime = s.LoginTime.ToString("dd.MM.yyyy HH:mm:ss"),
                        LogoutTime = s.LogoutTime.HasValue ? s.LogoutTime.Value.ToString("dd.MM.yyyy HH:mm:ss") : "Активен",
                        Duration = s.LogoutTime.HasValue
                            ? $"{(int)(s.LogoutTime.Value - s.LoginTime).TotalHours:D2}:{(s.LogoutTime.Value - s.LoginTime).Minutes:D2}:{(s.LogoutTime.Value - s.LoginTime).Seconds:D2}"
                            : "В процессе",
                        s.CrashReason,
                        HasCrash = s.CrashReason != null
                    });

                dgSessions.ItemsSource = sessions.ToList();
            }
        }
    }
}