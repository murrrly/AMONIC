using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AMONIC.Helpers;

namespace AMONIC
{
	public partial class UsersManagement : Page
	{
		public UsersManagement()
		{
			InitializeComponent();
			LoadOffices();
			LoadUsers();
		}

		private void LoadOffices()
		{
			using (var db = new AMONICEntities3())
			{
				var offices = db.Offices.ToList();
				cmbOfficeFilter.Items.Clear();
				cmbOfficeFilter.Items.Add(new ComboBoxItem { Content = "Все офисы" });
				foreach (var office in offices)
				{
					cmbOfficeFilter.Items.Add(new ComboBoxItem { Content = office.Title, Tag = office.ID });
				}
				cmbOfficeFilter.SelectedIndex = 0;
			}
		}

		private void LoadUsers()
		{
			using (var db = new AMONICEntities3())
			{
				int? selectedOfficeId = null;
				if (cmbOfficeFilter.SelectedIndex > 0)
				{
					var selectedItem = cmbOfficeFilter.SelectedItem as ComboBoxItem;
					selectedOfficeId = selectedItem?.Tag as int?;
				}

				var usersQuery = db.Users.AsQueryable();

				if (selectedOfficeId.HasValue)
				{
					usersQuery = usersQuery.Where(u => u.OfficeID == selectedOfficeId);
				}

				var usersList = usersQuery.ToList();

				// ID и Active остаются внутри анонимного объекта для корректной работы триггеров и кнопок
				var users = usersList.Select(u => new
				{
					u.ID,
					u.FirstName,
					u.LastName,
					Age = u.Birthdate.HasValue ? DateTime.Now.Year - u.Birthdate.Value.Year : 0,
					RoleTitle = GetRoleTitle(u.RoleID),
					u.Email,
					OfficeTitle = GetOfficeTitle(u.OfficeID),
					u.Active
				}).ToList();

				dgUsers.ItemsSource = users;
			}
		}

		private string GetRoleTitle(int roleId)
		{
			using (var db = new AMONICEntities3())
			{
				var role = db.Roles.FirstOrDefault(r => r.ID == roleId);
				return role?.Title ?? "Неизвестно";
			}
		}

		private string GetOfficeTitle(int? officeId)
		{
			if (!officeId.HasValue) return "Не указан";
			using (var db = new AMONICEntities3())
			{
				var office = db.Offices.FirstOrDefault(o => o.ID == officeId);
				return office?.Title ?? "Неизвестно";
			}
		}

		private void CmbOfficeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			LoadUsers();
		}

		private void BtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			LoadUsers();
		}

		private void BtnAddUser_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new AddUserWindow();
			if (dialog.ShowDialog() == true)
				LoadUsers();
		}

		private void BtnBlockUser_Click(object sender, RoutedEventArgs e)
		{
			dynamic selected = dgUsers.SelectedItem;
			if (selected == null)
			{
				MessageBox.Show("Выберите пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			int userId = selected.ID;
			using (var db = new AMONICEntities3())
			{
				var user = db.Users.Find(userId);
				if (user != null)
				{
					user.Active = !user.Active;
					db.SaveChanges();
					LoadUsers();
					MessageBox.Show(user.Active == true ? "Пользователь разблокирован" : "Пользователь заблокирован",
									"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
		}

		private void BtnChangeRole_Click(object sender, RoutedEventArgs e)
		{
			dynamic selected = dgUsers.SelectedItem;
			if (selected == null)
			{
				MessageBox.Show("Выберите пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			int userId = selected.ID;
			var dialog = new ChangeRoleWindow(userId);
			if (dialog.ShowDialog() == true)
				LoadUsers();
		}

		private void BtnExit_Click(object sender, RoutedEventArgs e)
		{
			if (this.NavigationService != null && this.NavigationService.CanGoBack)
			{
				this.NavigationService.GoBack();
			}
			else
			{
				Application.Current.Shutdown();
			}
		}
	}
}