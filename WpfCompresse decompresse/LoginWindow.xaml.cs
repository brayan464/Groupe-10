using System.Windows;
using System.Windows.Controls;

namespace WpfCompresse_decompresse
{
    public partial class LoginWindow : Window
    {
        public string Username => UsernameBox.Text;
        public string Password => PasswordBox.Password;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
