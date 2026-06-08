using System;
using System.Linq;
using System.Windows;
using AMONIC.Helpers;

namespace AMONIC
{
	public partial class AddUserWindow : Window
	{
		public AddUserWindow()
		{
			InitializeComponent();
			LoadOffices();
			LoadRoles();
		}

		private void LoadOffices()
		{
			using (var db = new AMONICEntities3())
			{
				cmbOffice.ItemsSource = db.Offices.ToList();
				cmbOffice.SelectedIndex = 0;
			}
		}

		private void LoadRoles()
		{
			using (var db = new AMONICEntities3())
			{
				// Администратор не может добавлять других администраторов.
				// Исключаем роль по ID (1) и по возможным текстовым вариациям ("admin", "administrator")
				var roles = db.Roles
					.Where(r => r.ID != 1 &&
								!r.Title.ToLower().Contains("admin"))
					.ToList();

				cmbRole.ItemsSource = roles;

				if (roles.Count > 0)
				{
					cmbRole.SelectedIndex = 0;
				}
			}
		}

		private void BtnSave_Click(object sender, RoutedEventArgs e)
		{
			// Проверка обязательных полей
			if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
				string.IsNullOrWhiteSpace(txtLastName.Text) ||
				string.IsNullOrWhiteSpace(txtEmail.Text) ||
				string.IsNullOrWhiteSpace(txtPassword.Password) ||
				dpBirthdate.SelectedDate == null ||
				cmbOffice.SelectedValue == null ||
				cmbRole.SelectedValue == null)
			{
				ShowError("Все поля обязательны для заполнения");
				return;
			}

			using (var db = new AMONICEntities3())
			{
				// Проверка уникальности email
				if (db.Users.Any(u => u.Email == txtEmail.Text.Trim()))
				{
					ShowError("Пользователь с таким Email уже существует");
					return;
				}

				// Создание нового пользователя (используем класс сущности Users)
				var newUser = new Users
				{
					FirstName = txtFirstName.Text.Trim(),
					LastName = txtLastName.Text.Trim(),
					Email = txtEmail.Text.Trim(),
					Password = Md5Helper.GetMd5Hash(txtPassword.Password),
					Birthdate = dpBirthdate.SelectedDate.Value,
					OfficeID = (int)cmbOffice.SelectedValue,
					RoleID = (int)cmbRole.SelectedValue,
					Active = true
				};

				db.Users.Add(newUser);
				db.SaveChanges();

				DialogResult = true;
				Close();
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