using System.Linq;
using System.Windows;

namespace AMONIC
{
    public partial class ChangeRoleWindow : Window
    {
        private int _userId;

        public ChangeRoleWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadRoles();
        }

        private void LoadRoles()
        {
            using (var db = new AMONICEntities3())
            {
                cmbRole.ItemsSource = db.Roles.ToList();
                cmbRole.SelectedValuePath = "ID";
                cmbRole.DisplayMemberPath = "Title";
                cmbRole.SelectedIndex = 0;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new AMONICEntities3())
            {
                var user = db.Users.Find(_userId);
                if (user != null)
                {
                    user.RoleID = (int)cmbRole.SelectedValue;
                    db.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}