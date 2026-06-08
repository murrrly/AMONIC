using System;
using System.Windows;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }
                
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (SessionManager.CurrentSessionID > 0)
            {
                using (var db = new AMONICEntities3())
                {
                    var session = db.UserSessions.Find(SessionManager.CurrentSessionID);
                    if (session != null && session.LogoutTime == null)
                    {
                        session.LogoutType = "CRASH";
                        session.CrashReason = e.Exception.Message.Length > 500
                            ? e.Exception.Message.Substring(0, 500)
                            : e.Exception.Message;
                        session.LogoutTime = DateTime.Now;
                        db.SaveChanges();
                    }
                }
            }

            MessageBox.Show($"Произошла ошибка: {e.Exception.Message}\n\nОшибка зафиксирована.",
                            "Критическая ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }

    }
}