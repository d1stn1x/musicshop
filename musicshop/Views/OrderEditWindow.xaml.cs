using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class OrderEditWindow : Window
    {
        private readonly string _role;
        private readonly int _userId;

        public OrderEditWindow(string role = "Администратор", int userId = 0)
        {
            InitializeComponent();
            _role = role;
            _userId = userId;

            if (role == "Клиент")
            {
                cmbClient.Visibility = Visibility.Collapsed;
                cmbStatus.IsEnabled = false;
            }
            else
            {
                LoadUsers();
            }
        }

        private void LoadUsers()
        {
            var list = new List<User>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new(
                    "SELECT UserID, ISNULL(FullName, Username) AS FullName FROM Users WHERE IsActive=1 ORDER BY FullName", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new User
                    {
                        UserID = (int)r["UserID"],
                        FullName = r["FullName"].ToString()
                    });
            }
            catch { }
            cmbClient.ItemsSource = list;
            cmbClient.DisplayMemberPath = "FullName";
            cmbClient.SelectedValuePath = "UserID";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Новый";
            int? userId = _role == "Клиент" ? _userId : (int?)cmbClient.SelectedValue;

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"INSERT INTO Orderr (Status_order, ID_user)
                                     VALUES (@s, @u)";
                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}