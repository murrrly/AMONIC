using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;

namespace AMONIC
{
    public partial class ReportsWindow : Page
    {
        private List<SurveyResponse> _allResponses;
        private List<string> _availableMonths;

        public ReportsWindow()
        {
            InitializeComponent();
            LoadSurveyData();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cmbMonth.Items.Clear();
            cmbMonth.Items.Add(new ComboBoxItem { Content = "Все месяцы", Tag = null });
            foreach (var month in _availableMonths)
            {
                cmbMonth.Items.Add(new ComboBoxItem { Content = month, Tag = month });
            }
            cmbMonth.SelectedIndex = 0;

            chkGender.Checked += (s, ev) => cmbGender.IsEnabled = true;
            chkGender.Unchecked += (s, ev) => cmbGender.IsEnabled = false;
            chkAge.Checked += (s, ev) => cmbAgeGroup.IsEnabled = true;
            chkAge.Unchecked += (s, ev) => cmbAgeGroup.IsEnabled = false;
            chkCabinType.Checked += (s, ev) => cmbCabinType.IsEnabled = true;
            chkCabinType.Unchecked += (s, ev) => cmbCabinType.IsEnabled = false;

            UpdateReport();
        }

        private void LoadSurveyData()
        {
            _allResponses = new List<SurveyResponse>();
            _availableMonths = new List<string>();

            string[] months = { "05", "06", "07" };
            string[] years = { "2017" };

            foreach (var year in years)
            {
                foreach (var month in months)
                {
                    string fileName = $"survey_{month}.csv";
                    if (File.Exists(fileName))
                    {
                        LoadCsvFile(fileName, month, year);
                        string monthName = GetMonthName(month);
                        _availableMonths.Add($"{monthName} {year}");
                    }
                }
            }
        }

        private void LoadCsvFile(string fileName, string month, string year)
        {
            var lines = File.ReadAllLines(fileName);
            bool isFirstLine = true;

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

                if (parts.Length < 12)
                    continue;

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

                string ageGroup = GetAgeGroup(age);
                DateTime surveyDate = new DateTime(int.Parse(year), int.Parse(month), 1);

                var response = new SurveyResponse
                {
                    SurveyMonth = surveyDate,
                    Gender = gender,
                    Age = age,
                    AgeGroup = ageGroup,
                    DepartureAirport = depAirport,
                    ArrivalAirport = arrAirport,
                    CabinTypeID = cabinType,
                    Q1 = q1,
                    Q2 = q2,
                    Q3 = q3,
                    Q4 = q4
                };

                _allResponses.Add(response);
            }
        }

        private string GetAgeGroup(int age)
        {
            if (age >= 18 && age <= 24) return "18-24";
            if (age >= 25 && age <= 39) return "25-39";
            if (age >= 40 && age <= 59) return "40-59";
            if (age >= 60) return "60+";
            return "Не указан";
        }

        private string GetMonthName(string month)
        {
            switch (month)
            {
                case "05": return "Май";
                case "06": return "Июнь";
                case "07": return "Июль";
                default: return month;
            }
        }

        private void UpdateReport()
        {
            var filtered = _allResponses.AsQueryable();

            string selectedMonth = (cmbMonth.SelectedItem as ComboBoxItem)?.Tag as string;
            if (!string.IsNullOrEmpty(selectedMonth))
            {
                filtered = filtered.Where(r => r.SurveyMonth.ToString("MMMM yyyy") == selectedMonth);
            }

            if (chkGender.IsChecked == true && cmbGender.SelectedItem != null)
            {
                string gender = (cmbGender.SelectedItem as ComboBoxItem)?.Tag as string;
                if (!string.IsNullOrEmpty(gender))
                {
                    filtered = filtered.Where(r => r.Gender == gender);
                }
            }

            if (chkAge.IsChecked == true && cmbAgeGroup.SelectedItem != null)
            {
                string ageGroup = (cmbAgeGroup.SelectedItem as ComboBoxItem)?.Tag as string;
                if (!string.IsNullOrEmpty(ageGroup))
                {
                    filtered = filtered.Where(r => r.AgeGroup == ageGroup);
                }
            }

            if (chkCabinType.IsChecked == true && cmbCabinType.SelectedItem != null)
            {
                string cabinTag = (cmbCabinType.SelectedItem as ComboBoxItem)?.Tag as string;
                if (!string.IsNullOrEmpty(cabinTag))
                {
                    int cabinId = int.Parse(cabinTag);
                    filtered = filtered.Where(r => r.CabinTypeID == cabinId);
                }
            }

            var result = filtered.ToList();
            txtSampleSize.Text = result.Count.ToString();

            txtAvailableMonths.Text = string.Join(", ", _availableMonths);

            var summary = result.GroupBy(r => r.AgeGroup)
                .Select(g => new
                {
                    Возрастная_группа = g.Key,
                    Количество = g.Count(),
                    Q1_среднее = g.Average(x => x.Q1).ToString("F2"),
                    Q2_среднее = g.Average(x => x.Q2).ToString("F2"),
                    Q3_среднее = g.Average(x => x.Q3).ToString("F2"),
                    Q4_среднее = g.Average(x => x.Q4).ToString("F2")
                }).ToList();

            dgSummary.ItemsSource = summary;
        }

        private void CmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReport();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateReport();
        }
    }

    public class SurveyResponse
    {
        public DateTime SurveyMonth { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string AgeGroup { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public int CabinTypeID { get; set; }
        public int Q1 { get; set; }
        public int Q2 { get; set; }
        public int Q3 { get; set; }
        public int Q4 { get; set; }
    }
}