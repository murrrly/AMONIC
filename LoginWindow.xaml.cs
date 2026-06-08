using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class LoginWindow : Window
    {
        private int _failedAttempts = 0;
        private DateTime _blockUntil = DateTime.MinValue;
        private DispatcherTimer _timer;

        public LoginWindow()
        {
            InitializeComponent();

        }

        

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now < _blockUntil)
            {
                TimeSpan remaining = _blockUntil - DateTime.Now;
                ShowTimer($"Подождите {remaining.Seconds} секунд");
                return;
            }

            string email = txtEmail.Text.Trim();
            string passwordHash = Md5Helper.GetMd5Hash(txtPassword.Password);

            using (var db = new AMONICEntities3())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    HandleFailedAttempt();
                    return;
                }

                if (user.Password != passwordHash)
                {
                    HandleFailedAttempt();
                    return;
                }

                if (user.Active == false)
                {
                    ShowError("Учётная запись заблокирована администратором");
                    return;
                }

                _failedAttempts = 0;

                SessionManager.CurrentUserID = user.ID;
                SessionManager.CurrentUserEmail = user.Email;
                SessionManager.CurrentUserFirstName = user.FirstName;
                SessionManager.CurrentUserLastName = user.LastName;

                var role = db.Roles.FirstOrDefault(r => r.ID == user.RoleID);
                SessionManager.CurrentUserRole = role?.Title;

                // ВАЖНО: используем UserSessions (множественное число)
                var newSession = new UserSessions
                {
                    UserID = user.ID,
                    LoginTime = DateTime.Now
                };
                db.UserSessions.Add(newSession);
                db.SaveChanges();
                SessionManager.CurrentSessionID = newSession.ID;

                if (SessionManager.CurrentUserRole?.ToLower() == "administrator")
                {
                    var adminWindow = new AdminMainWindow();
                    adminWindow.Show();
                }
                else
                {
                    var userWindow = new UserMainWindow();
                    userWindow.Show();
                }

                this.Close();
            }
        }

        private void HandleFailedAttempt()
        {
            _failedAttempts++;
            int remainingAttempts = 3 - _failedAttempts;

            if (_failedAttempts >= 3)
            {
                _blockUntil = DateTime.Now.AddSeconds(10);
                ShowTimer("Превышено количество попыток. Блокировка на 10 секунд");
                _failedAttempts = 0;
            }
            else
            {
                ShowError($"Неверный email или пароль. Осталось попыток: {remainingAttempts}");
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
            txtTimer.Visibility = Visibility.Collapsed;
            txtPassword.Password = "";
            txtEmail.Focus();
        }

        private void ShowTimer(string message)
        {
            txtTimer.Text = message;
            txtTimer.Visibility = Visibility.Visible;
            txtError.Visibility = Visibility.Collapsed;
            btnLogin.IsEnabled = false;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (DateTime.Now >= _blockUntil)
                {
                    _timer.Stop();
                    txtTimer.Visibility = Visibility.Collapsed;
                    btnLogin.IsEnabled = true;
                    txtEmail.Clear();
                    txtPassword.Password = "";
                    txtEmail.Focus();
                }
                else
                {
                    TimeSpan remaining = _blockUntil - DateTime.Now;
                    txtTimer.Text = $"Подождите {remaining.Seconds} секунд";
                }
            };
            _timer.Start();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}