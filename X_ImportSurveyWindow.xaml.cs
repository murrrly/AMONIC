using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace AMONIC
{
    public partial class ImportSurveyWindow : Window
    {
        private int _importedCount = 0;
        private int _skippedCount = 0;

        public ImportSurveyWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Выберите файл с опросами"
            };

            if (dialog.ShowDialog() == true)
            {
                txtFilePath.Text = dialog.FileName;
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFilePath.Text))
            {
                MessageBox.Show("Выберите CSV файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _importedCount = 0;
            _skippedCount = 0;

            try
            {
                var lines = File.ReadAllLines(txtFilePath.Text);
                bool isFirstLine = true;

                using (var db = new AMONICEntities3())
                {
                    foreach (var line in lines)
                    {
                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var parts = line.Split(',');

                        if (parts.Length < 9)
                        {
                            _skippedCount++;
                            continue;
                        }

                        string gender = parts[0].Trim();
                        string ageStr = parts[1].Trim();
                        string depAirport = parts[2].Trim();
                        string arrAirport = parts[3].Trim();
                        string cabinTypeStr = parts[4].Trim();
                        string q1Str = parts[5].Trim();
                        string q2Str = parts[6].Trim();
                        string q3Str = parts[7].Trim();
                        string q4Str = parts[8].Trim();

                        int age = string.IsNullOrEmpty(ageStr) ? 0 : int.Parse(ageStr);
                        int cabinType = string.IsNullOrEmpty(cabinTypeStr) ? 0 : int.Parse(cabinTypeStr);
                        int q1 = string.IsNullOrEmpty(q1Str) ? 0 : int.Parse(q1Str);
                        int q2 = string.IsNullOrEmpty(q2Str) ? 0 : int.Parse(q2Str);
                        int q3 = string.IsNullOrEmpty(q3Str) ? 0 : int.Parse(q3Str);
                        int q4 = string.IsNullOrEmpty(q4Str) ? 0 : int.Parse(q4Str);

                        if (q1 < 0 || q1 > 7) q1 = 0;
                        if (q2 < 0 || q2 > 7) q2 = 0;
                        if (q3 < 0 || q3 > 7) q3 = 0;
                        if (q4 < 0 || q4 > 7) q4 = 0;

                        var surveyResponse = new SurveyResponses
                        {
                            SurveyMonth = DateTime.Now,
                            DepartureAirportID = null,
                            ArrivalAirportID = null,
                            Age = age,
                            Gender = gender,
                            CabinTypeID = cabinType == 0 ? (int?)null : cabinType,
                            Q1 = (byte)q1,
                            Q2 = (byte)q2,
                            Q3 = (byte)q3,
                            Q4 = (byte)q4
                        };

                        db.SurveyResponses.Add(surveyResponse);
                        _importedCount++;
                    }

                    db.SaveChanges();
                }

                MessageBox.Show($"Импорт завершён!\nДобавлено: {_importedCount}\nПропущено: {_skippedCount}",
                                "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}