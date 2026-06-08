using System;
using System.Linq;
using System.Windows;

namespace AMONIC
{
    public partial class EditScheduleWindow : Window
    {
        private int _scheduleId;

        public EditScheduleWindow(int scheduleId)
        {
            InitializeComponent();
            _scheduleId = scheduleId;
            LoadSchedule();
        }

        private void LoadSchedule()
        {
            using (var db = new AMONICEntities3())
            {
                var schedule = db.Schedules.Find(_scheduleId);
                if (schedule != null)
                {
                    dpDate.SelectedDate = schedule.Date;
                    txtTime.Text = schedule.Time.ToString(@"hh\:mm");
                    txtEconomyPrice.Text = schedule.EconomyPrice.ToString();
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!dpDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату");
                return;
            }

            if (!TimeSpan.TryParse(txtTime.Text, out TimeSpan newTime))
            {
                ShowError("Введите время в формате ЧЧ:ММ");
                return;
            }

            if (!decimal.TryParse(txtEconomyPrice.Text, out decimal newPrice) || newPrice <= 0)
            {
                ShowError("Введите корректную цену");
                return;
            }

            using (var db = new AMONICEntities3())
            {
                var schedule = db.Schedules.Find(_scheduleId);
                if (schedule != null)
                {
                    schedule.Date = dpDate.SelectedDate.Value;
                    schedule.Time = newTime;
                    schedule.EconomyPrice = newPrice;
                    db.SaveChanges();

                    DialogResult = true;
                    Close();
                }
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}