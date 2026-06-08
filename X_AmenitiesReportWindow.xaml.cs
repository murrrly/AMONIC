using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AMONIC
{
    public partial class AmenitiesReportWindow : Window
    {
        public AmenitiesReportWindow()
        {
            InitializeComponent();
            dpDateFrom.SelectedDate = DateTime.Now;
            dpDateTo.SelectedDate = DateTime.Now;
            dpFlightDate.SelectedDate = DateTime.Now;
        }

        private void RbByDate_Checked(object sender, RoutedEventArgs e)
        {
            panelFlight.Visibility = Visibility.Collapsed;
            dpDateFrom.IsEnabled = true;
            dpDateTo.IsEnabled = true;
        }

        private void RbByFlight_Checked(object sender, RoutedEventArgs e)
        {
            panelFlight.Visibility = Visibility.Visible;
            dpDateFrom.IsEnabled = false;
            dpDateTo.IsEnabled = false;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!dpDateFrom.SelectedDate.HasValue || !dpDateTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Select date range", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime from = dpDateFrom.SelectedDate.Value;
            DateTime to = dpDateTo.SelectedDate.Value.AddDays(1);

            using (var db = new AMONICEntities3())
            {
                var result = db.AmenitiesTickets
                    .Join(db.Tickets, at => at.TicketID, t => t.ID, (at, t) => new { at, t })
                    .Join(db.Schedules, at_t => at_t.t.ScheduleID, s => s.ID, (at_t, s) => new { at_t.at, at_t.t, s })
                    .Join(db.Amenities, at_t_s => at_t_s.at.AmenityID, a => a.ID, (at_t_s, a) => new { at_t_s.at, at_t_s.t, at_t_s.s, a })
                    .Where(x => x.s.Date >= from && x.s.Date <= to)
                    .GroupBy(x => x.a.Service)
                    .Select(g => new { Service = g.Key, Count = g.Count() })
                    .ToList();

                dgReport.ItemsSource = result;
                txtTitle.Text = string.Format("Report for {0:dd.MM.yyyy} - {1:dd.MM.yyyy}",
                    dpDateFrom.SelectedDate.Value, dpDateTo.SelectedDate.Value);
            }
        }

        private void BtnSearchFlight_Click(object sender, RoutedEventArgs e)
        {
            string flightNumber = txtFlightNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                MessageBox.Show("Enter flight number", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpFlightDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Select flight date", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime flightDate = dpFlightDate.SelectedDate.Value;

            using (var db = new AMONICEntities3())
            {
                var result = db.AmenitiesTickets
                    .Join(db.Tickets, at => at.TicketID, t => t.ID, (at, t) => new { at, t })
                    .Join(db.Schedules, at_t => at_t.t.ScheduleID, s => s.ID, (at_t, s) => new { at_t.at, at_t.t, s })
                    .Join(db.Amenities, at_t_s => at_t_s.at.AmenityID, a => a.ID, (at_t_s, a) => new { at_t_s.at, at_t_s.t, at_t_s.s, a })
                    .Where(x => x.s.FlightNumber == flightNumber && x.s.Date == flightDate)
                    .GroupBy(x => x.a.Service)
                    .Select(g => new { Service = g.Key, Count = g.Count() })
                    .ToList();

                dgReport.ItemsSource = result;
                txtTitle.Text = string.Format("Report for flight {0} on {1:dd.MM.yyyy}", flightNumber, flightDate);
            }
        }
    }
}