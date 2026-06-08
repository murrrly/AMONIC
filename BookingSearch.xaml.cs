using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AMONIC.Helpers;

namespace AMONIC
{
	// Общие модели данных теперь лежат здесь, внутри namespace, и видны всем окнам
	public class BookingData
	{
		public BookingSearch.FlightInfo OutboundFlight { get; set; }
		public BookingSearch.FlightInfo ReturnFlight { get; set; }
		public int PassengerCount { get; set; }
		public bool IsRoundTrip { get; set; }
		public string CabinType { get; set; }
		public List<PassengerInfo> Passengers { get; set; } = new List<PassengerInfo>();
	}

	public class PassengerInfo
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTime Birthdate { get; set; }
		public string PassportNumber { get; set; }
		public int PassportCountryID { get; set; }
		public string Phone { get; set; }
	}

	public partial class BookingSearch : Page
	{
		private List<FlightInfo> _outboundFlights;
		private List<FlightInfo> _returnFlights;
		private FlightInfo _selectedOutbound;
		private FlightInfo _selectedReturn;

		public class FlightInfo
		{
			public int ScheduleID { get; set; }
			public string FromAirport { get; set; }
			public string ToAirport { get; set; }
			public string Date { get; set; }
			public string Time { get; set; }
			public string FlightNumber { get; set; }
			public decimal Price { get; set; }
			public DateTime OriginalDate { get; set; }
			public TimeSpan OriginalTime { get; set; }
			public int FromAirportId { get; set; }
			public int ToAirportId { get; set; }
			public int NumberOfStops { get; set; }
			public string TotalTravelTime { get; set; }
			public List<FlightSegment> Segments { get; set; } = new List<FlightSegment>();
		}

		public class FlightSegment
		{
			public string FromAirport { get; set; }
			public string ToAirport { get; set; }
			public string Date { get; set; }
			public string Time { get; set; }
			public string FlightNumber { get; set; }
		}

		public BookingSearch()
		{
			InitializeComponent();
			LoadAirports();
			ResetForm();
		}

		private void LoadAirports()
		{
			using (var db = new AMONICEntities3())
			{
				var airports = db.Airports.ToList();
				cmbDepartureAirport.ItemsSource = airports;
				cmbArrivalAirport.ItemsSource = airports;
			}
		}

		private void CmbTripType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lblReturnDate == null || dpReturnDate == null || borderReturn == null) return;

			if (cmbTripType.SelectedIndex == 1)
			{
				lblReturnDate.Visibility = Visibility.Visible;
				dpReturnDate.Visibility = Visibility.Visible;
				borderReturn.Visibility = Visibility.Visible;
			}
			else
			{
				lblReturnDate.Visibility = Visibility.Collapsed;
				dpReturnDate.Visibility = Visibility.Collapsed;
				borderReturn.Visibility = Visibility.Collapsed;
				dgReturnFlights.ItemsSource = null;
				_selectedReturn = null;
			}
			UpdateProceedButtonState();
		}

		private void BtnSearch_Click(object sender, RoutedEventArgs e)
		{
			if (cmbDepartureAirport.SelectedValue == null || cmbArrivalAirport.SelectedValue == null)
			{
				MessageBox.Show("Выберите аэропорты отправления и прибытия.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if ((int)cmbDepartureAirport.SelectedValue == (int)cmbArrivalAirport.SelectedValue)
			{
				MessageBox.Show("Аэропорты отправления и прибытия не могут совпадать.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (dpDepartureDate.SelectedDate == null)
			{
				MessageBox.Show("Выберите дату отправления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (cmbTripType.SelectedIndex == 1 && dpReturnDate.SelectedDate == null)
			{
				MessageBox.Show("Выберите дату обратного вылета.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (cmbTripType.SelectedIndex == 1 && dpReturnDate.SelectedDate < dpDepartureDate.SelectedDate)
			{
				MessageBox.Show("Дата обратного вылета не может быть раньше даты отправления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			int passengerCount;
			if (!int.TryParse(txtPassengerCount.Text, out passengerCount) || passengerCount <= 0)
			{
				MessageBox.Show("Введите корректное число пассажиров (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			_selectedOutbound = null;
			_selectedReturn = null;
			btnProceed.IsEnabled = false;

			PerformSearch();
		}

		private void PerformSearch()
		{
			int fromId = (int)cmbDepartureAirport.SelectedValue;
			int toId = (int)cmbArrivalAirport.SelectedValue;
			DateTime outDate = dpDepartureDate.SelectedDate.Value;
			string cabinType = (cmbCabinType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Economy";

			using (var db = new AMONICEntities3())
			{
				var schedules = db.Schedules.Include("Routes").Include("Aircrafts").Where(s => s.Confirmed).ToList();
				var airports = db.Airports.ToList();

				_outboundFlights = FindFlights(schedules, airports, fromId, toId, outDate, chkPlusMinus3Days.IsChecked == true, cabinType);
				dgOutboundFlights.ItemsSource = _outboundFlights;

				if (cmbTripType.SelectedIndex == 1)
				{
					DateTime retDate = dpReturnDate.SelectedDate.Value;
					_returnFlights = FindFlights(schedules, airports, toId, fromId, retDate, chkPlusMinus3Days.IsChecked == true, cabinType);
					dgReturnFlights.ItemsSource = _returnFlights;
				}
			}
		}

		private List<FlightInfo> FindFlights(List<Schedules> allSchedules, List<Airports> airports, int fromId, int toId, DateTime targetDate, bool plusMinus3, string cabinType)
		{
			var result = new List<FlightInfo>();
			DateTime startDate = plusMinus3 ? targetDate.AddDays(-3) : targetDate;
			DateTime endDate = plusMinus3 ? targetDate.AddDays(3) : targetDate;

			// 1. Прямые рейсы
			var directSchedules = allSchedules.Where(s => s.Routes.DepartureAirportID == fromId &&
														  s.Routes.ArrivalAirportID == toId &&
														  s.Date >= startDate && s.Date <= endDate).ToList();

			foreach (var s in directSchedules)
			{
				decimal calculatedPrice = CalculateCabinPrice(s.EconomyPrice, cabinType);
				var info = new FlightInfo
				{
					ScheduleID = s.ID,
					FromAirport = s.Routes.Airports.IATACode,
					ToAirport = airports.FirstOrDefault(a => a.ID == s.Routes.ArrivalAirportID)?.IATACode ?? "",
					Date = s.Date.ToString("dd.MM.yyyy"),
					Time = s.Time.ToString(@"hh\:mm"),
					FlightNumber = s.FlightNumber,
					Price = calculatedPrice,
					OriginalDate = s.Date,
					OriginalTime = s.Time,
					FromAirportId = fromId,
					ToAirportId = toId,
					NumberOfStops = 0,
					TotalTravelTime = $"{s.Routes.FlightTime} мин."
				};
				info.Segments.Add(new FlightSegment { FromAirport = info.FromAirport, ToAirport = info.ToAirport, Date = info.Date, Time = info.Time, FlightNumber = info.FlightNumber });
				result.Add(info);
			}

			// 2. Исправленный и безопасный поиск транзитных рейсов через список airports
			var firstLegs = allSchedules.Where(s => s.Routes.DepartureAirportID == fromId && s.Date >= startDate && s.Date <= endDate).ToList();
			foreach (var first in firstLegs)
			{
				int transitId = first.Routes.ArrivalAirportID;
				if (transitId == toId) continue;

				DateTime firstArrivalDateTime = first.Date.Add(first.Time).AddMinutes(first.Routes.FlightTime);

				var secondLegs = allSchedules.Where(s => s.Routes.DepartureAirportID == transitId &&
														   s.Routes.ArrivalAirportID == toId &&
														   s.Date >= first.Date).ToList();

				foreach (var second in secondLegs)
				{
					DateTime secondDepartureDateTime = second.Date.Add(second.Time);
					if (secondDepartureDateTime > firstArrivalDateTime)
					{
						decimal totalPrice = CalculateCabinPrice(first.EconomyPrice, cabinType) + CalculateCabinPrice(second.EconomyPrice, cabinType);
						TimeSpan totalDuration = secondDepartureDateTime.AddMinutes(second.Routes.FlightTime) - first.Date.Add(first.Time);

						string fromCode = airports.FirstOrDefault(a => a.ID == fromId)?.IATACode ?? "";
						string transitCode = airports.FirstOrDefault(a => a.ID == transitId)?.IATACode ?? "";
						string toCode = airports.FirstOrDefault(a => a.ID == toId)?.IATACode ?? "";

						var info = new FlightInfo
						{
							ScheduleID = first.ID,
							FromAirport = fromCode,
							ToAirport = toCode,
							Date = first.Date.ToString("dd.MM.yyyy"),
							Time = first.Time.ToString(@"hh\:mm"),
							FlightNumber = $"{first.FlightNumber} / {second.FlightNumber}",
							Price = totalPrice,
							OriginalDate = first.Date,
							OriginalTime = first.Time,
							FromAirportId = fromId,
							ToAirportId = toId,
							NumberOfStops = 1,
							TotalTravelTime = $"{(int)totalDuration.TotalHours}ч {totalDuration.Minutes}м"
						};
						info.Segments.Add(new FlightSegment { FromAirport = fromCode, ToAirport = transitCode, Date = first.Date.ToString("dd.MM.yyyy"), Time = first.Time.ToString(@"hh\:mm"), FlightNumber = first.FlightNumber });
						info.Segments.Add(new FlightSegment { FromAirport = transitCode, ToAirport = toCode, Date = second.Date.ToString("dd.MM.yyyy"), Time = second.Time.ToString(@"hh\:mm"), FlightNumber = second.FlightNumber });

						result.Add(info);
					}
				}
			}

			return result.OrderBy(r => r.OriginalDate).ThenBy(r => r.OriginalTime).ToList();
		}

		private decimal CalculateCabinPrice(decimal basePrice, string cabinType)
		{
			if (cabinType.Contains("Business") || cabinType.Contains("Бизнес"))
			{
				return Math.Floor(basePrice * 1.35m);
			}
			if (cabinType.Contains("First") || cabinType.Contains("Первый"))
			{
				decimal businessPrice = Math.Floor(basePrice * 1.35m);
				return Math.Floor(businessPrice * 1.30m);
			}
			return Math.Floor(basePrice);
		}

		private void DgOutboundFlights_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_selectedOutbound = dgOutboundFlights.SelectedItem as FlightInfo;
			UpdateProceedButtonState();
		}

		private void DgReturnFlights_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_selectedReturn = dgReturnFlights.SelectedItem as FlightInfo;
			UpdateProceedButtonState();
		}

		private void UpdateProceedButtonState()
		{
			if (cmbTripType.SelectedIndex == 0)
			{
				btnProceed.IsEnabled = (_selectedOutbound != null);
			}
			else
			{
				btnProceed.IsEnabled = (_selectedOutbound != null && _selectedReturn != null);
			}
		}

		private void BtnProceed_Click(object sender, RoutedEventArgs e)
		{
			int passCount = int.Parse(txtPassengerCount.Text);
			var bookingData = new BookingData
			{
				OutboundFlight = _selectedOutbound,
				ReturnFlight = (cmbTripType.SelectedIndex == 1) ? _selectedReturn : null,
				PassengerCount = passCount,
				IsRoundTrip = (cmbTripType.SelectedIndex == 1),
				CabinType = (cmbCabinType.SelectedItem as ComboBoxItem)?.Content.ToString()
			};

			NavigationService?.Navigate(new BookingConfirmation(bookingData));
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			ResetForm();
		}

		private void ResetForm()
		{
			cmbDepartureAirport.SelectedIndex = -1;
			cmbArrivalAirport.SelectedIndex = -1;
			cmbTripType.SelectedIndex = 0;
			cmbCabinType.SelectedIndex = 0;
			dpDepartureDate.SelectedDate = null;
			dpReturnDate.SelectedDate = null;
			chkPlusMinus3Days.IsChecked = false;
			txtPassengerCount.Text = "1";
			dgOutboundFlights.ItemsSource = null;
			dgReturnFlights.ItemsSource = null;
			_outboundFlights = null;
			_returnFlights = null;
			_selectedOutbound = null;
			_selectedReturn = null;
			btnProceed.IsEnabled = false;
		}
	}
}