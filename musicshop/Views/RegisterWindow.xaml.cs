using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace musicshop
{
    public partial class RegisterWindow : Window
    {
 
        private const string AdminSecretCode = "ADMIN2024";

        public RegisterWindow()
        {
            InitializeComponent();
            cmbRole.SelectionChanged += CmbRole_SelectionChanged;
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            pnlAdminCode.Visibility = role == "Администратор"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";

            string login    = txtLogin.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string email    = txtEmail.Text.Trim();
            string role     = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Клиент";
            string pass     = txtPassword.Password;
            string pass2    = txtPasswordConfirm.Password;

            // Валидация
            if (string.IsNullOrEmpty(login))
            { txtStatus.Text = "Введите логин!"; return; }

            if (login.Length < 3)
            { txtStatus.Text = "Логин должен быть не короче 3 символов!"; return; }

            if (string.IsNullOrEmpty(pass))
            { txtStatus.Text = "Введите пароль!"; return; }
            if (pass.Length < 6)
            {
                txtStatus.Text = "Пароль должен быть не короче 6 символов!";
                return;
            }

            string pattern = @"^(?=.*[A-ZА-Я])(?=.*\d)(?=.*[!@#$%^]).+$";
            if (!Regex.IsMatch(pass, pattern))
            {
                txtStatus.Text = "Пароль должен содержать минимум 1 заглавную букву, 1 цифру и 1 спецсимвол (!@#$%^)!";
                return;
            }

            if (pass != pass2)
            {
                txtStatus.Text = "Пароли не совпадают!";
                return;
            }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();

                // Проверить уникальность логина
                using (SqlCommand chk = new(
                    "SELECT COUNT(1) FROM Users WHERE Username = @u", conn))
                {
                    chk.Parameters.AddWithValue("@u", login);
                    int cnt = (int)chk.ExecuteScalar();
                    if (cnt > 0)
                    { txtStatus.Text = "Такой логин уже занят!"; return; }
                }

                const string sql = @"
                    INSERT INTO Users (Username, PasswordHash, FullName, Role, Email, IsActive, CreatedAt)
                    VALUES (@u, @p, @fn, @r, @em, 1, GETDATE())";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@u",  login);
                cmd.Parameters.AddWithValue("@p",  pass);          // В реальном проекте — хэш!
                cmd.Parameters.AddWithValue("@fn", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName);
                cmd.Parameters.AddWithValue("@r",  role);
                cmd.Parameters.AddWithValue("@em", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    $"Аккаунт «{login}» успешно создан!\nРоль: {role}",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);

                // Открыть LoginWindow
                new LoginWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void btnGoLogin_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}
