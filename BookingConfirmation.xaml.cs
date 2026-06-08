using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AMONIC.Helpers;

namespace AMONIC
{
	public partial class BookingConfirmation : Page
	{
		private BookingData _bookingData;
		private ObservableCollection<PassengerDisplay> _passengers;

		public class PassengerDisplay
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string Birthdate { get; set; }
			public DateTime BirthdateValue { get; set; }
			public string PassportNumber { get; set; }
			public int CountryID { get; set; }
			public string CountryName { get; set; }
			public string Phone { get; set; }
		}

		public BookingConfirmation(BookingData bookingData)
		{
			InitializeComponent();
			_bookingData = bookingData;
			_passengers = new ObservableCollection<PassengerDisplay>();
			dgPassengers.ItemsSource = _passengers;
			LoadCountries();
			DisplayFlightInfo();
			UpdatePassengerCount();
		}

		private void LoadCountries()
		{
			using (var db = new AMONICEntities3())
			{
				cmbCountry.ItemsSource = db.Countries.ToList();
				if (cmbCountry.Items.Count > 0)
					cmbCountry.SelectedIndex = 0;
			}
		}

		private void DisplayFlightInfo()
		{
			if (_bookingData == null) return;

			// Отображение прямого пути
			var outFlight = _bookingData.OutboundFlight;
			txtOutboundInfo.Text = $"Рейс: {outFlight.FlightNumber} | Откуда: {outFlight.FromAirport} -> Куда: {outFlight.ToAirport} | Дата: {outFlight.Date} в {outFlight.Time} | Цена сегмента: {outFlight.Price:N2}$";

			// Отображение обратного пути (если выбран)
			if (_bookingData.IsRoundTrip && _bookingData.ReturnFlight != null)
			{
				var retFlight = _bookingData.ReturnFlight;
				txtReturnInfo.Text = $"Обратный рейс: {retFlight.FlightNumber} | Откуда: {retFlight.FromAirport} -> Куда: {retFlight.ToAirport} | Дата: {retFlight.Date} в {retFlight.Time} | Цена сегмента: {retFlight.Price:N2}$";
				txtReturnInfo.Visibility = Visibility.Visible;
			}
			else
			{
				txtReturnInfo.Visibility = Visibility.Collapsed;
			}
		}

		private void BtnAddPassenger_Click(object sender, RoutedEventArgs e)
		{
			if (_passengers.Count >= _bookingData.PassengerCount)
			{
				ShowError($"Вы уже добавили максимальное количество пассажиров ({_bookingData.PassengerCount}) для этого бронирования.");
				return;
			}

			string firstName = txtFirstName.Text.Trim();
			string lastName = txtLastName.Text.Trim();
			string passport = txtPassportNumber.Text.Trim();
			string phone = txtPhone.Text.Trim();

			if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(passport) || string.IsNullOrEmpty(phone))
			{
				ShowError("Все поля обязательны для заполнения.");
				return;
			}

			if (dpBirthdate.SelectedDate == null)
			{
				ShowError("Укажите дату рождения пассажира.");
				return;
			}

			if (cmbCountry.SelectedValue == null)
			{
				ShowError("Выберите страну выдачи паспорта.");
				return;
			}

			// ЖЕСТКАЯ ВАЛИДАЦИЯ ИЗ КРИТЕРИЕВ: Проверка на дублирование паспортов в рамках одного бронирования
			if (_passengers.Any(p => p.PassportNumber.Equals(passport, StringComparison.OrdinalIgnoreCase)))
			{
				ShowError("Пассажир с таким номером паспорта уже добавлен в текущее бронирование!");
				return;
			}

			var country = cmbCountry.SelectedItem as Countries;

			_passengers.Add(new PassengerDisplay
			{
				FirstName = firstName,
				LastName = lastName,
				Birthdate = dpBirthdate.SelectedDate.Value.ToString("dd.MM.yyyy"),
				BirthdateValue = dpBirthdate.SelectedDate.Value,
				PassportNumber = passport,
				CountryID = country.ID,
				CountryName = country.Name,
				Phone = phone
			});

			ClearPassengerInputs();
			UpdatePassengerCount();
		}

		private void ClearPassengerInputs()
		{
			txtFirstName.Text = "";
			txtLastName.Text = "";
			txtPassportNumber.Text = "";
			txtPhone.Text = "";
			dpBirthdate.SelectedDate = null;
			if (cmbCountry.Items.Count > 0) cmbCountry.SelectedIndex = 0;
		}

		private void UpdatePassengerCount()
		{
			txtPassengerCount.Text = $"{_passengers.Count} из {_bookingData.PassengerCount}";
		}

		private void ShowError(string msg)
		{
			MessageBox.Show(msg, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private void BtnConfirm_Click(object sender, RoutedEventArgs e)
		{
			if (_passengers.Count != _bookingData.PassengerCount)
			{
				ShowError($"Необходимо добавить всех пассажиров перед переходом к оплате. Ожидается: {_bookingData.PassengerCount}, добавлено: {_passengers.Count}.");
				return;
			}

			// Сохраняем проверенных и собранных пассажиров в сессионную структуру данных
			_bookingData.Passengers = _passengers.Select(p => new PassengerInfo
			{
				FirstName = p.FirstName,
				LastName = p.LastName,
				Birthdate = p.BirthdateValue,
				PassportNumber = p.PassportNumber,
				PassportCountryID = p.CountryID,
				Phone = p.Phone
			}).ToList();

			// Перенаправляем на окно оплаты PaymentWindow (где генерируется 6-значный PNR код)
			var paymentWindow = new PaymentWindow(_bookingData);
			paymentWindow.Owner = Window.GetWindow(this);
			paymentWindow.ShowDialog();

			// Возврат на начальный экран поиска после проведения транзакции
			var mainWindow = Application.Current.Windows.OfType<UserMainWindow>().FirstOrDefault();
			if (mainWindow != null)
			{
				mainWindow.MainFrame.Navigate(new BookingSearch());
			}
			else
			{
				NavigationService?.GoBack();
			}
		}

		private void BtnBack_Click(object sender, RoutedEventArgs e)
		{
			var mainWindow = Application.Current.Windows.OfType<UserMainWindow>().FirstOrDefault();
			if (mainWindow != null)
			{
				mainWindow.MainFrame.Navigate(new BookingSearch());
			}
			else
			{
				NavigationService?.GoBack();
			}
		}


	}
}