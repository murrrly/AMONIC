using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AMONIC
{
	public partial class SchedulesManagement : Page
	{
		private List<Schedules> _allSchedules;
		private List<Routes> _allRoutes;
		private List<Airports> _allAirports;
		private List<Aircrafts> _allAircrafts;

		public SchedulesManagement()
		{
			InitializeComponent();
			_allSchedules = new List<Schedules>();
			LoadReferenceData();
			LoadSchedules();
		}

		private void LoadSchedules()
		{
			using (var db = new AMONICEntities3())
			{
				_allSchedules = db.Schedules.ToList();

				System.Diagnostics.Debug.WriteLine($"Загружено рейсов: {_allSchedules.Count}");

				if (_allSchedules == null)
					_allSchedules = new List<Schedules>();

				ApplyFiltersAndSort();
			}
		}

		private void LoadReferenceData()
		{
			using (var db = new AMONICEntities3())
			{
				_allAirports = db.Airports.ToList();
				_allRoutes = db.Routes.ToList();
				_allAircrafts = db.Aircrafts.ToList();

				cmbDepartureAirport.ItemsSource = _allAirports;
				cmbArrivalAirport.ItemsSource = _allAirports;
				cmbDepartureAirport.SelectedIndex = -1;
				cmbArrivalAirport.SelectedIndex = -1;
			}
		}

		private string GetAirportName(int airportId)
		{
			var airport = _allAirports.FirstOrDefault(a => a.ID == airportId);
			return airport?.IATACode ?? "Неизвестно";
		}

		private string GetAircraftName(int aircraftId)
		{
			var aircraft = _allAircrafts.FirstOrDefault(a => a.ID == aircraftId);
			return aircraft?.Name ?? "Неизвестно";
		}

		private int? GetDepartureAirportId(int routeId)
		{
			var route = _allRoutes.FirstOrDefault(r => r.ID == routeId);
			return route?.DepartureAirportID;
		}

		private int? GetArrivalAirportId(int routeId)
		{
			var route = _allRoutes.FirstOrDefault(r => r.ID == routeId);
			return route?.ArrivalAirportID;
		}

		private decimal CalculatePrice(decimal basePrice, decimal multiplier)
		{
			decimal price = basePrice * multiplier;
			return Math.Floor(price);
		}

		private void ApplyFiltersAndSort()
		{
			if (_allSchedules == null || _allSchedules.Count == 0)
			{
				dgSchedules.ItemsSource = null;
				return;
			}

			var filtered = _allSchedules.AsEnumerable();

			if (cmbDepartureAirport.SelectedValue != null)
			{
				int depId = (int)cmbDepartureAirport.SelectedValue;
				filtered = filtered.Where(s => GetDepartureAirportId(s.RouteID) == depId);
			}

			if (cmbArrivalAirport.SelectedValue != null)
			{
				int arrId = (int)cmbArrivalAirport.SelectedValue;
				filtered = filtered.Where(s => GetArrivalAirportId(s.RouteID) == arrId);
			}

			if (dpDateFrom.SelectedDate.HasValue)
			{
				DateTime from = dpDateFrom.SelectedDate.Value;
				filtered = filtered.Where(s => s.Date >= from);
			}

			if (dpDateTo.SelectedDate.HasValue)
			{
				DateTime to = dpDateTo.SelectedDate.Value;
				filtered = filtered.Where(s => s.Date <= to);
			}

			if (!string.IsNullOrWhiteSpace(txtFlightNumber.Text))
			{
				string flightNum = txtFlightNumber.Text.Trim();
				filtered = filtered.Where(s => s.FlightNumber != null && s.FlightNumber.Contains(flightNum));
			}

			var filteredList = filtered.ToList();

			string sortBy = (cmbSortBy.SelectedItem as ComboBoxItem)?.Tag as string ?? "DateTime";
			switch (sortBy)
			{
				case "EconomyPrice":
					filteredList = filteredList.OrderBy(s => s.EconomyPrice).ToList();
					break;
				case "Confirmed":
					filteredList = filteredList.OrderBy(s => s.Confirmed).ToList();
					break;
				default:
					filteredList = filteredList.OrderBy(s => s.Date).ThenBy(s => s.Time).ToList();
					break;
			}

			var result = filteredList.Select(s =>
			{
				int? depId = GetDepartureAirportId(s.RouteID);
				int? arrId = GetArrivalAirportId(s.RouteID);
				return new
				{
					s.ID,
					Date = s.Date.ToString("dd.MM.yyyy"),
					Time = s.Time.ToString(@"hh\:mm"),
					DepartureAirport = depId.HasValue ? GetAirportName(depId.Value) : "Неизвестно",
					ArrivalAirport = arrId.HasValue ? GetAirportName(arrId.Value) : "Неизвестно",
					s.FlightNumber,
					AircraftName = GetAircraftName(s.AircraftID),
					EconomyPrice = s.EconomyPrice.ToString("N2"),
					BusinessPrice = CalculatePrice(s.EconomyPrice, 1.35m).ToString("N2"),
					FirstClassPrice = CalculatePrice(CalculatePrice(s.EconomyPrice, 1.35m), 1.30m).ToString("N2"),
					IsCancelled = s.Confirmed == false,
					OriginalSchedule = s
				};
			}).ToList();

			dgSchedules.ItemsSource = result;
		}

		private void BtnSearch_Click(object sender, RoutedEventArgs e)
		{
			if (cmbDepartureAirport.SelectedValue != null &&
				cmbArrivalAirport.SelectedValue != null &&
				(int)cmbDepartureAirport.SelectedValue == (int)cmbArrivalAirport.SelectedValue)
			{
				MessageBox.Show("Аэропорт отправления и прибытия не могут совпадать",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			ApplyFiltersAndSort();
		}

		// Полноценный глобальный сброс всех фильтров
		private void BtnResetSearch_Click(object sender, RoutedEventArgs e)
		{
			cmbDepartureAirport.SelectedIndex = -1;
			cmbArrivalAirport.SelectedIndex = -1;
			dpDateFrom.SelectedDate = null;
			dpDateTo.SelectedDate = null;
			txtFlightNumber.Text = "";
			ApplyFiltersAndSort();
		}

		private void CmbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dgSchedules != null)
				ApplyFiltersAndSort();
		}

		private void BtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			LoadReferenceData();
			LoadSchedules();
		}

		private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
		{
			dynamic selected = dgSchedules.SelectedItem;
			if (selected == null)
			{
				MessageBox.Show("Выберите рейс", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var schedule = selected.OriginalSchedule as Schedules;
			if (schedule != null)
			{
				using (var db = new AMONICEntities3())
				{
					var dbSchedule = db.Schedules.Find(schedule.ID);
					if (dbSchedule != null)
					{
						dbSchedule.Confirmed = !dbSchedule.Confirmed;
						db.SaveChanges();
						LoadSchedules();
						MessageBox.Show(dbSchedule.Confirmed == true ? "Рейс подтверждён" : "Рейс отменён",
										"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
		}

		private void BtnEditSchedule_Click(object sender, RoutedEventArgs e)
		{
			dynamic selected = dgSchedules.SelectedItem;
			if (selected == null)
			{
				MessageBox.Show("Выберите рейс", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var schedule = selected.OriginalSchedule as Schedules;
			if (schedule != null)
			{
				var dialog = new EditScheduleWindow(schedule.ID);
				dialog.Owner = Window.GetWindow(this); // Выравнивание дочернего окна строго по центру
				if (dialog.ShowDialog() == true)
					LoadSchedules();
			}
		}
	}
}