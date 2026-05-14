using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class UserEditWindow : Window
    {
        private readonly User? _user;
        private readonly bool  _isEdit;

        public UserEditWindow(User? user = null)
        {
            InitializeComponent();
            _user   = user;
            _isEdit = user != null;

            if (_isEdit)
            {
                txtTitle.Text        = "Редактирование пользователя";
                lblPass.Text         = "Новый пароль";
                lblPassHint.Visibility = Visibility.Visible;
                txtLogin.Text        = user!.Username;
                txtFullName.Text     = user.FullName ?? "";
                txtEmail.Text        = user.Email    ?? "";
                SetRole(user.Role);
            }
        }

        private void SetRole(string role)
        {
            foreach (ComboBoxItem item in cmbRole.Items)
                if (item.Content.ToString() == role)
                { cmbRole.SelectedItem = item; break; }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";
            string login    = txtLogin.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string email    = txtEmail.Text.Trim();
            string role     = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Клиент";
            string pass     = txtPassword.Password;

            if (string.IsNullOrEmpty(login))
            { txtError.Text = "Введите логин!"; return; }

            if (!_isEdit && string.IsNullOrEmpty(pass))
            { txtError.Text = "Введите пароль!"; return; }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();

                if (_isEdit)
                {
                    // Проверить уникальность логина (кроме текущего)
                    using (SqlCommand chk = new(
                        "SELECT COUNT(1) FROM Users WHERE Username=@u AND UserID<>@id", conn))
                    {
                        chk.Parameters.AddWithValue("@u",  login);
                        chk.Parameters.AddWithValue("@id", _user!.UserID);
                        if ((int)chk.ExecuteScalar() > 0)
                        { txtError.Text = "Такой логин уже занят!"; return; }
                    }

                    string sql = string.IsNullOrEmpty(pass)
                        ? @"UPDATE Users SET Username=@u, FullName=@fn, Email=@em, Role=@r
                            WHERE UserID=@id"
                        : @"UPDATE Users SET Username=@u, FullName=@fn, Email=@em, Role=@r,
                                           PasswordHash=@p
                            WHERE UserID=@id";

                    using SqlCommand cmd = new(sql, conn);
                    cmd.Parameters.AddWithValue("@u",  login);
                    cmd.Parameters.AddWithValue("@fn", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName);
                    cmd.Parameters.AddWithValue("@em", string.IsNullOrEmpty(email)    ? (object)DBNull.Value : email);
                    cmd.Parameters.AddWithValue("@r",  role);
                    cmd.Parameters.AddWithValue("@id", _user!.UserID);
                    if (!string.IsNullOrEmpty(pass))
                        cmd.Parameters.AddWithValue("@p", pass);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    // Проверить уникальность логина
                    using (SqlCommand chk = new("SELECT COUNT(1) FROM Users WHERE Username=@u", conn))
                    {
                        chk.Parameters.AddWithValue("@u", login);
                        if ((int)chk.ExecuteScalar() > 0)
                        { txtError.Text = "Такой логин уже занят!"; return; }
                    }

                    const string sql = @"INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt)
                                        VALUES (@u, @p, @fn, @em, @r, 1, GETDATE())";
                    using SqlCommand cmd = new(sql, conn);
                    cmd.Parameters.AddWithValue("@u",  login);
                    cmd.Parameters.AddWithValue("@p",  pass);
                    cmd.Parameters.AddWithValue("@fn", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName);
                    cmd.Parameters.AddWithValue("@em", string.IsNullOrEmpty(email)    ? (object)DBNull.Value : email);
                    cmd.Parameters.AddWithValue("@r",  role);
                    cmd.ExecuteNonQuery();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                txtError.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
