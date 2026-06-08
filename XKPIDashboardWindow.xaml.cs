using System;
using System.Linq;
using System.Windows;

namespace AMONIC
{
    public partial class KPIDashboardWindow : Window
    {
        private DateTime _reportStartTime;

        public KPIDashboardWindow()
        {
            InitializeComponent();
            _reportStartTime = DateTime.Now;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            TimeSpan elapsed = DateTime.Now - _reportStartTime;
            txtGenerationTime.Text = $"{elapsed.TotalSeconds:F3} сек";

            using (var db = new AMONICEntities3())
            {
                DateTime thirtyDaysStart = DateTime.Now.AddDays(-30);

                var confirmed = db.Schedules.Count(s => s.Date >= thirtyDaysStart && s.Confirmed == true);
                var cancelled = db.Schedules.Count(s => s.Date >= thirtyDaysStart && s.Confirmed == false);
                txtConfirmedFlights.Text = confirmed.ToString();
                txtCancelledFlights.Text = cancelled.ToString();

                var schedulesList = db.Schedules.ToList();
                var ticketsList = db.Tickets.Where(t => t.Confirmed == true).ToList();

                var passengerCounts = ticketsList
                    .Where(t => schedulesList.FirstOrDefault(s => s.ID == t.ScheduleID).Date >= thirtyDaysStart)
                    .GroupBy(t => schedulesList.FirstOrDefault(s => s.ID == t.ScheduleID).Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToList();

                if (passengerCounts.Any())
                {
                    var busiest = passengerCounts.OrderByDescending(p => p.Count).First();
                    var quietest = passengerCounts.OrderBy(p => p.Count).First();
                    txtBusiestDay.Text = $"{busiest.Date:dd.MM.yyyy} ({busiest.Count} пасс.)";
                    txtQuietestDay.Text = $"{quietest.Date:dd.MM.yyyy} ({quietest.Count} пасс.)";
                }

                var topPassengers = ticketsList
                    .Where(t => schedulesList.FirstOrDefault(s => s.ID == t.ScheduleID).Date >= thirtyDaysStart)
                    .GroupBy(t => new { t.Firstname, t.Lastname })
                    .Select(g => new { g.Key.Firstname, g.Key.Lastname, Count = g.Count() })
                    .OrderByDescending(p => p.Count)
                    .Take(3)
                    .ToList();

                for (int i = 0; i < topPassengers.Count && i < 3; i++)
                {
                    var p = topPassengers[i];
                    if (i == 0) txtTop1.Text = $"{p.Firstname} {p.Lastname} - {p.Count} билетов";
                    else if (i == 1) txtTop2.Text = $"{p.Firstname} {p.Lastname} - {p.Count} билетов";
                    else if (i == 2) txtTop3.Text = $"{p.Firstname} {p.Lastname} - {p.Count} билетов";
                }

                var allFlights = db.Schedules.Where(s => s.Date >= thirtyDaysStart && s.Confirmed == true).ToList();
                int totalMinutes = 0;
                foreach (var flight in allFlights)
                {
                    var route = db.Routes.FirstOrDefault(r => r.ID == flight.RouteID);
                    if (route != null) totalMinutes += route.FlightTime;
                }
                int days = allFlights.Select(s => s.Date).Distinct().Count();
                int avg = days > 0 ? totalMinutes / days : 0;
                txtAvgFlightTime.Text = $"{avg / 60} ч {avg % 60} мин";

                DateTime threeWeeksStart = DateTime.Now.Date.AddDays(-21);
                for (int weekIndex = 0; weekIndex < 3; weekIndex++)
                {
                    DateTime start = threeWeeksStart.AddDays(weekIndex * 7);
                    DateTime end = start.AddDays(7);
                    var weekSchedules = db.Schedules.Where(s => s.Date >= start && s.Date < end && s.Confirmed == true).ToList();
                    int totalSeats = 0;
                    int booked = 0;
                    foreach (var schedule in weekSchedules)
                    {
                        var aircraft = db.Aircrafts.FirstOrDefault(a => a.ID == schedule.AircraftID);
                        if (aircraft != null)
                        {
                            totalSeats += aircraft.TotalSeats;
                            booked += db.Tickets.Count(t => t.ScheduleID == schedule.ID && t.Confirmed == true);
                        }
                    }
                    int emptyPercent = totalSeats > 0 ? 100 - (booked * 100 / totalSeats) : 0;
                    if (weekIndex == 0) txtWeek1Empty.Text = $"{emptyPercent}%";
                    else if (weekIndex == 1) txtWeek2Empty.Text = $"{emptyPercent}%";
                    else if (weekIndex == 2) txtWeek3Empty.Text = $"{emptyPercent}%";
                }

                var offices = db.Offices.ToList();
                var users = db.Users.ToList();
                var topOffices = offices
                    .Select(o => new
                    {
                        o.Title,
                        Sales = ticketsList.Count(t =>
                        {
                            var user = users.FirstOrDefault(u => u.ID == t.UserID);
                            return user != null && user.OfficeID == o.ID &&
                                   schedulesList.FirstOrDefault(s => s.ID == t.ScheduleID).Date >= thirtyDaysStart;
                        })
                    })
                    .OrderByDescending(o => o.Sales)
                    .Take(3)
                    .ToList();

                for (int i = 0; i < topOffices.Count && i < 3; i++)
                {
                    var office = topOffices[i];
                    if (i == 0) txtOffice1.Text = $"{office.Title} - {office.Sales} продаж";
                    else if (i == 1) txtOffice2.Text = $"{office.Title} - {office.Sales} продаж";
                    else if (i == 2) txtOffice3.Text = $"{office.Title} - {office.Sales} продаж";
                }

                txtTodayIncome.Text = $"{CalculateIncome(db, DateTime.Now.Date):N2} USD";
                txtYesterdayIncome.Text = $"{CalculateIncome(db, DateTime.Now.Date.AddDays(-1)):N2} USD";
                txtTwoDaysAgoIncome.Text = $"{CalculateIncome(db, DateTime.Now.Date.AddDays(-2)):N2} USD";
            }
        }

        private decimal CalculateIncome(AMONICEntities3 db, DateTime date)
        {
            var schedules = db.Schedules.ToList();
            var tickets = db.Tickets.Where(t => t.Confirmed == true).ToList();

            decimal total = 0;
            foreach (var ticket in tickets)
            {
                var schedule = schedules.FirstOrDefault(s => s.ID == ticket.ScheduleID);
                if (schedule != null && schedule.Date == date)
                {
                    decimal price = schedule.EconomyPrice;
                    if (ticket.CabinTypeID == 2)
                        price = Math.Floor(price * 1.35m);
                    else if (ticket.CabinTypeID == 3)
                        price = Math.Floor(price * 1.35m * 1.30m);
                    total += price;
                }
            }
            return total;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}