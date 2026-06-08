using System;
using System.Windows;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class AdminMainWindow : Window
    {
        public AdminMainWindow()
        {
            InitializeComponent();
            this.Closed += AdminMainWindow_Closed;
        }

        private void AdminMainWindow_Closed(object sender, EventArgs e)
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

        private void MnuUsers_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new UsersManagement());
        private void MnuSchedules_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new SchedulesManagement());
        private void MnuImport_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ImportSchedules());
        private void MnuReports_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReportsWindow());
        private void MnuAmenities_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new AmenitiesPurchase());
        private void MnuExit_Click(object sender, RoutedEventArgs e) { this.Close(); Application.Current.Shutdown(); }

        private void MnuImportSurvey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ImportSurveyWindow();
            dialog.ShowDialog();
        }

        private void MnuAmenitiesReport_Click(object sender, RoutedEventArgs e)
        {
            var window = new AmenitiesReportWindow();
            window.ShowDialog();
        }

        private void MnuKPI_Click(object sender, RoutedEventArgs e)
        {
            var window = new KPIDashboardWindow();
            window.ShowDialog();
        }
    }
}