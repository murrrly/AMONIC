using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AMONIC
{
    public partial class ImportSchedules : Page
    {
        private int _addedCount = 0;
        private int _editedCount = 0;
        private int _skippedCount = 0;

        public ImportSchedules()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Выберите файл с изменениями расписания"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtFilePath.Text = openFileDialog.FileName;
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFilePath.Text))
            {
                MessageBox.Show("Выберите CSV файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _addedCount = 0;
            _editedCount = 0;
            _skippedCount = 0;

            try
            {
                var lines = File.ReadAllLines(txtFilePath.Text);

                // Пропускаем заголовок, если он есть
                int startIndex = 0;
                if (lines[0].ToLower().Contains("action"))
                    startIndex = 1;

                for (int i = startIndex; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    ProcessLine(lines[i]);
                }

                txtResult.Text = $"Импорт завершён!\n" +
                                 $"Добавлено рейсов: {_addedCount}\n" +
                                 $"Изменено рейсов: {_editedCount}\n" +
                                 $"Пропущено (ошибки или дубликаты): {_skippedCount}";

                MessageBox.Show($"Импорт завершён!\nДобавлено: {_addedCount}\nИзменено: {_editedCount}\nПропущено: {_skippedCount}",
                                "Результат импорта", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessLine(string line)
        {
            var parts = line.Split(',');

            // Проверка на минимальное количество полей
            if (parts.Length < 9)
            {
                _skippedCount++;
                return;
            }

            string action = parts[0].Trim().ToUpper();
            string dateStr = parts[1].Trim();
            string timeStr = parts[2].Trim();
            string flightNumber = parts[3].Trim();
            string depIATA = parts[4].Trim();
            string arrIATA = parts[5].Trim();
            string aircraftCode = parts[6].Trim();
            string economyPriceStr = parts[7].Trim();
            string confirmedStr = parts[8].Trim();

            // Проверка обязательных полей
            if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(timeStr) ||
                string.IsNullOrEmpty(flightNumber) || string.IsNullOrEmpty(depIATA) ||
                string.IsNullOrEmpty(arrIATA) || string.IsNullOrEmpty(aircraftCode) ||
                string.IsNullOrEmpty(economyPriceStr))
            {
                _skippedCount++;
                return;
            }

            if (!DateTime.TryParse(dateStr, out DateTime date) ||
                !TimeSpan.TryParse(timeStr, out TimeSpan time) ||
                !decimal.TryParse(economyPriceStr, out decimal economyPrice))
            {
                _skippedCount++;
                return;
            }

            bool confirmed = confirmedStr.ToUpper() == "OK";

            using (var db = new AMONICEntities3())
            {
                // Получаем ID аэропортов, маршрута, самолета
                var depAirport = db.Airports.FirstOrDefault(a => a.IATACode == depIATA);
                var arrAirport = db.Airports.FirstOrDefault(a => a.IATACode == arrIATA);
                var aircraft = db.Aircrafts.FirstOrDefault(a => a.MakeModel == aircraftCode);

                if (depAirport == null || arrAirport == null || aircraft == null)
                {
                    _skippedCount++;
                    return;
                }

                // Проверяем существующий маршрут
                var route = db.Routes.FirstOrDefault(r => r.DepartureAirportID == depAirport.ID && r.ArrivalAirportID == arrAirport.ID);
                if (route == null)
                {
                    route = new Routes
                    {
                        DepartureAirportID = depAirport.ID,
                        ArrivalAirportID = arrAirport.ID,
                        Distance = 0,
                        FlightTime = 0
                    };
                    db.Routes.Add(route);
                    db.SaveChanges();
                }

                // Проверка на дублирование по номеру рейса и дате
                var existing = db.Schedules.FirstOrDefault(s => s.FlightNumber == flightNumber && s.Date == date);

                if (action == "ADD")
                {
                    if (existing != null)
                    {
                        // Дубликат - пропускаем
                        _skippedCount++;
                        return;
                    }

                    var newSchedule = new Schedules
                    {
                        Date = date,
                        Time = time,
                        FlightNumber = flightNumber,
                        RouteID = route.ID,
                        AircraftID = aircraft.ID,
                        EconomyPrice = economyPrice,
                        Confirmed = confirmed
                    };
                    db.Schedules.Add(newSchedule);
                    _addedCount++;
                }
                else if (action == "EDIT")
                {
                    if (existing == null)
                    {
                        _skippedCount++;
                        return;
                    }

                    existing.Date = date;
                    existing.Time = time;
                    existing.FlightNumber = flightNumber;
                    existing.RouteID = route.ID;
                    existing.AircraftID = aircraft.ID;
                    existing.EconomyPrice = economyPrice;
                    existing.Confirmed = confirmed;
                    _editedCount++;
                }
                else
                {
                    _skippedCount++;
                    return;
                }

                db.SaveChanges();
            }
        }
    }
}