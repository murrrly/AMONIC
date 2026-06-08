using System;
using System.Linq;
using System.Windows;
using AMONIC.Helpers;

namespace AMONIC
{
	public partial class PaymentWindow : Window
	{
		private BookingData _bookingData;
		private decimal _totalAmount;

		public PaymentWindow(BookingData bookingData)
		{
			InitializeComponent();
			_bookingData = bookingData;
			CalculateTotal();
		}

		private void CalculateTotal()
		{
			_totalAmount = _bookingData.OutboundFlight.Price * _bookingData.PassengerCount;

			if (_bookingData.IsRoundTrip && _bookingData.ReturnFlight != null)
			{
				_totalAmount += _bookingData.ReturnFlight.Price * _bookingData.PassengerCount;
			}

			txtTotalAmount.Text = $"{_totalAmount:N2} USD";
		}

		private string GenerateBookingReference()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var random = new Random();
			string reference;

			using (var db = new AMONICEntities3())
			{
				do
				{
					reference = new string(Enumerable.Repeat(chars, 6)
						.Select(s => s[random.Next(s.Length)]).ToArray());
				}
				while (db.Tickets.Any(t => t.BookingReference == reference));
			}

			return reference;
		}

		private void BtnIssueTicket_Click(object sender, RoutedEventArgs e)
		{
			string bookingReference = GenerateBookingReference();

			using (var db = new AMONICEntities3())
			{
				string cabinTypeTag = _bookingData.CabinType ?? "Economy";
				int cabinTypeId = 1;

				if (cabinTypeTag.Contains("Business") || cabinTypeTag.Contains("Бизнес"))
					cabinTypeId = 2;
				else if (cabinTypeTag.Contains("First") || cabinTypeTag.Contains("Первый"))
					cabinTypeId = 3;

				int currentUserId = 1;
				try
				{
					currentUserId = SessionManager.CurrentUserID;
				}
				catch { }

				foreach (var passenger in _bookingData.Passengers)
				{
					var outboundTicket = new Tickets
					{
						UserID = currentUserId,
						ScheduleID = _bookingData.OutboundFlight.ScheduleID,
						CabinTypeID = cabinTypeId,
						Firstname = passenger.FirstName,
						Lastname = passenger.LastName,
						Phone = passenger.Phone,
						PassportNumber = passenger.PassportNumber,
						PassportCountryID = passenger.PassportCountryID,
						BookingReference = bookingReference,
						Confirmed = true
					};
					db.Tickets.Add(outboundTicket);

					if (_bookingData.IsRoundTrip && _bookingData.ReturnFlight != null)
					{
						var returnTicket = new Tickets
						{
							UserID = currentUserId,
							ScheduleID = _bookingData.ReturnFlight.ScheduleID,
							CabinTypeID = cabinTypeId,
							Firstname = passenger.FirstName,
							Lastname = passenger.LastName,
							Phone = passenger.Phone,
							PassportNumber = passenger.PassportNumber,
							PassportCountryID = passenger.PassportCountryID,
							BookingReference = bookingReference,
							Confirmed = true
						};
						db.Tickets.Add(returnTicket);
					}
				}

				db.SaveChanges();
			}

			MessageBox.Show($"Билеты успешно оформлены!\nНомер бронирования: {bookingReference}\nСумма к оплате: {_totalAmount:N2} USD",
							"Успех", MessageBoxButton.OK, MessageBoxImage.Information);

			DialogResult = true;
			Close();
		}

		private void BtnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}