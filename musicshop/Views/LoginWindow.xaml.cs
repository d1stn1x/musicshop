using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;

namespace musicshop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                txtStatus.Text = "Заполните все поля!";
                return;
            }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();

                const string sql = @"SELECT UserID, Role, ISNULL(FullName, Username) AS FullName
                                     FROM Users
                                     WHERE Username = @u
                                       AND PasswordHash = @p
                                       AND IsActive = 1";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@u", login);
                cmd.Parameters.AddWithValue("@p", pass);

                using SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    int userId = (int)r["UserID"];
                    string role = r["Role"].ToString()!;
                    string fullName = r["FullName"].ToString()!;

                    if (role == "Клиент")
                        new ClientMainWindow(fullName, userId).Show();
                    else if (role == "Продавец")
                        new SellerMainWindow(fullName, userId).Show();
                    else
                        new MainWindow(role, fullName, userId).Show();

                    Close();
                }
                else
                {
                    txtStatus.Text = "Неверный логин или пароль!";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}