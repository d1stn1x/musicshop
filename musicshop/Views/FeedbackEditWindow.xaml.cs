using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class FeedbackEditWindow : Window
    {
        private readonly string _role;
        private readonly int    _userId;

        public FeedbackEditWindow(string role = "Администратор", int userId = 0)
        {
            InitializeComponent();
            _role   = role;
            _userId = userId;
            LoadAlbums();

            if (role == "Клиент")
            {
                // Клиент не выбирает пользователя — строка скрыта
                rowUser.Visibility = Visibility.Collapsed;
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
                        UserID   = (int)r["UserID"],
                        FullName = r["FullName"].ToString()
                    });
            }
            catch { }
            cmbUser.ItemsSource       = list;
            cmbUser.DisplayMemberPath = "FullName";
            cmbUser.SelectedValuePath = "UserID";
        }

        private void LoadAlbums()
        {
            var list = new List<Album>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("SELECT ID_album, Name FROM Album ORDER BY Name", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Album
                    {
                        ID_album = (int)r["ID_album"],
                        Name     = r["Name"].ToString()!
                    });
            }
            catch { }
            cmbAlbum.ItemsSource       = list;
            cmbAlbum.DisplayMemberPath = "Name";
            cmbAlbum.SelectedValuePath = "ID_album";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            int? userId  = _role == "Клиент" ? _userId : (int?)cmbUser.SelectedValue;
            int? albumId = (int?)cmbAlbum.SelectedValue;
            int  rate    = int.Parse((cmbRate.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "5");
            string comment = txtComment.Text.Trim();

            if (albumId == null)
            {
                MessageBox.Show("Выберите альбом!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"INSERT INTO Feedback (Comment, Rate, ID_user, ID_album)
                                     VALUES (@c, @r, @u, @a)";
                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@c", string.IsNullOrEmpty(comment)
                                                  ? (object)DBNull.Value : comment);
                cmd.Parameters.AddWithValue("@r", rate);
                cmd.Parameters.AddWithValue("@u", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@a", albumId);
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
