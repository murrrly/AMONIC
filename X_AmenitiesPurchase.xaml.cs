using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AMONIC.Helpers;

namespace AMONIC
{
    public partial class AmenitiesPurchase : Page
    {
        private List<Tickets> _userTickets;
        private List<AmenityItem> _amenities;
        private int _selectedScheduleId;
        private int _selectedTicketId;
        private decimal _previousTotal = 0;

        public class AmenityItem
        {
            public int ID { get; set; }
            public string Service { get; set; }
            public decimal Price { get; set; }
            public bool IsSelected { get; set; }
        }

        public class FlightDisplay
        {
            public int ScheduleID { get; set; }
            public int TicketID { get; set; }
            public string FlightNumber { get; set; }
            public string Date { get; set; }
            public string FromAirport { get; set; }
            public string ToAirport { get; set; }
            public string Status { get; set; }
            public bool IsAvailable { get; set; }
        }

        public AmenitiesPurchase()
        {
            InitializeComponent();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs ev)
        {
            string pnr = txtPNR.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(pnr) || pnr.Length != 6)
            {
                ShowError("Введите корректный номер бронирования (6 символов)");
                return;
            }

            using (var db = new AMONICEntities3())
            {
                _userTickets = db.Tickets.Where(t => t.BookingReference == pnr && t.Confirmed == true).ToList();

                if (!_userTickets.Any())
                {
                    ShowError("Бронирование не найдено");
                    return;
                }

                HideError();
                var flights = new List<FlightDisplay>();
                var schedules = db.Schedules.ToList();
                var routes = db.Routes.ToList();
                var airports = db.Airports.ToList();

                foreach (var ticket in _userTickets)
                {
                    var schedule = schedules.FirstOrDefault(s => s.ID == ticket.ScheduleID);
                    if (schedule != null)
                    {
                        var route = routes.FirstOrDefault(r => r.ID == schedule.RouteID);
                        var depAirport = airports.FirstOrDefault(a => a.ID == route.DepartureAirportID);
                        var arrAirport = airports.FirstOrDefault(a => a.ID == route.ArrivalAirportID);

                        DateTime flightDateTime = schedule.Date.Add(schedule.Time);
                        bool isAvailable = flightDateTime > DateTime.Now.AddHours(24);

                        flights.Add(new FlightDisplay
                        {
                            ScheduleID = schedule.ID,
                            TicketID = ticket.ID,
                            FlightNumber = schedule.FlightNumber,
                            Date = schedule.Date.ToString("dd.MM.yyyy"),
                            FromAirport = depAirport?.IATACode ?? "???",
                            ToAirport = arrAirport?.IATACode ?? "???",
                            Status = isAvailable ? "Доступно" : "Недоступно",
                            IsAvailable = isAvailable
                        });
                    }
                }

                dgFlights.ItemsSource = flights;
                borderFlights.Visibility = Visibility.Visible;
                borderPassenger.Visibility = Visibility.Collapsed;
                borderAmenities.Visibility = Visibility.Collapsed;
                borderButtons.Visibility = Visibility.Collapsed;
                _selectedScheduleId = 0;
            }
        }

        private void DgFlights_SelectionChanged(object sender, SelectionChangedEventArgs ev)
        {
            var selected = dgFlights.SelectedItem as FlightDisplay;
            if (selected == null) return;

            if (!selected.IsAvailable)
            {
                MessageBox.Show("Услуги для этого рейса недоступны (менее 24 часов до вылета или рейс уже выполнен)",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedScheduleId = selected.ScheduleID;
            _selectedTicketId = selected.TicketID;

            using (var db = new AMONICEntities3())
            {
                var ticket = db.Tickets.FirstOrDefault(t => t.ID == _selectedTicketId);
                if (ticket != null)
                {
                    string cabinName = "Эконом";
                    var cabin = db.CabinTypes.FirstOrDefault(c => c.ID == ticket.CabinTypeID);
                    if (cabin != null) cabinName = cabin.Name;

                    txtPassengerInfo.Text = $"Класс: {cabinName}\n" +
                                            $"Пассажир: {ticket.Firstname} {ticket.Lastname}\n" +
                                            $"Паспорт: {ticket.PassportNumber}";
                    borderPassenger.Visibility = Visibility.Visible;
                }

                var allAmenities = db.Amenities.ToList();
                var existingAmenities = db.AmenitiesTickets.Where(a => a.TicketID == _selectedTicketId).ToList();

                _amenities = new List<AmenityItem>();
                foreach (var amenity in allAmenities)
                {
                    _amenities.Add(new AmenityItem
                    {
                        ID = amenity.ID,
                        Service = amenity.Service,
                        Price = (decimal)amenity.Price,
                        IsSelected = existingAmenities.Any(e => e.AmenityID == amenity.ID)
                    });
                }

                _previousTotal = _amenities.Where(a => a.IsSelected).Sum(a => a.Price);
                dgAmenities.ItemsSource = _amenities;
                borderAmenities.Visibility = Visibility.Visible;
                borderButtons.Visibility = Visibility.Visible;

                UpdateTotals();
            }

            dgAmenities.CellEditEnding += (s, ev2) => UpdateTotals();
        }

        private void UpdateTotals()
        {
            if (_amenities == null) return;

            decimal subtotal = _amenities.Where(a => a.IsSelected).Sum(a => a.Price);
            decimal tax = subtotal * 0.05m;
            decimal total = subtotal + tax;

            txtSubtotal.Text = $"{subtotal:N2} USD";
            txtTax.Text = $"{tax:N2} USD";
            txtTotal.Text = $"{total:N2} USD";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs ev)
        {
            var selectedAmenities = _amenities.Where(a => a.IsSelected).Select(a => a.ID).ToList();
            decimal newSubtotal = _amenities.Where(a => a.IsSelected).Sum(a => a.Price);
            decimal difference = newSubtotal - _previousTotal;
            decimal tax = difference * 0.05m;
            decimal totalDifference = difference + tax;

            using (var db = new AMONICEntities3())
            {
                var existing = db.AmenitiesTickets.Where(a => a.TicketID == _selectedTicketId).ToList();
                foreach (var ex in existing)
                {
                    db.AmenitiesTickets.Remove(ex);
                }

                foreach (var amenityId in selectedAmenities)
                {
                    db.AmenitiesTickets.Add(new AmenitiesTickets
                    {
                        AmenityID = amenityId,
                        TicketID = _selectedTicketId,
                        Price = _amenities.First(a => a.ID == amenityId).Price
                    });
                }

                db.SaveChanges();
            }

            string message = totalDifference >= 0
                ? $"Доплата: {Math.Abs(totalDifference):N2} USD"
                : $"Возврат: {Math.Abs(totalDifference):N2} USD";

            MessageBox.Show($"Изменения сохранены!\n{message}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            _previousTotal = newSubtotal;
            UpdateTotals();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs ev)
        {
            borderFlights.Visibility = Visibility.Collapsed;
            borderPassenger.Visibility = Visibility.Collapsed;
            borderAmenities.Visibility = Visibility.Collapsed;
            borderButtons.Visibility = Visibility.Collapsed;
            txtPNR.Clear();
            _selectedScheduleId = 0;
            _selectedTicketId = 0;
        }

        private void ShowError(string message)
        {
            txtSearchError.Text = message;
            txtSearchError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            txtSearchError.Visibility = Visibility.Collapsed;
        }
    }
}