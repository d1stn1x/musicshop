using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class BuyAlbumWindow : Window
    {
        private readonly int _userId;
        private List<Album> _albums = new();

        public BuyAlbumWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadAlbums();
        }

        private void LoadAlbums()
        {
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT a.ID_album, a.Name, a.Description, a.Year_of_release,
                           g.Name AS GroupName,
                           ISNULL((SELECT TOP 1 ao.Cost FROM Album_Order ao
                                   WHERE ao.ID_album = a.ID_album
                                   ORDER BY ao.ID_album_order DESC), 0) AS Price
                    FROM Album a
                    LEFT JOIN Groupp g ON g.ID_group = a.ID_group
                    ORDER BY a.Name";
                using SqlCommand cmd = new(sql, conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    _albums.Add(new Album
                    {
                        ID_album = (int)r["ID_album"],
                        Name = r["Name"].ToString()!,
                        Description = r["Description"]?.ToString(),
                        Year_of_release = (int)r["Year_of_release"],
                        GroupName = r["GroupName"]?.ToString(),
                        Price = (decimal)r["Price"]
                    });
            }
            catch { }

            cmbAlbum.ItemsSource = _albums;
            cmbAlbum.DisplayMemberPath = "Name";
            cmbAlbum.SelectedValuePath = "ID_album";
        }

        private void cmbAlbum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAlbum.SelectedItem is Album a)
            {
                txtPrice.Text = a.Price > 0 ? $"{a.Price:N2} руб." : "Цена не указана";
                txtDesc.Text = string.IsNullOrEmpty(a.Description)
                                ? "Нет описания"
                                : $"{a.Description} ({a.Year_of_release}, {a.GroupName})";
            }
        }

        private void Buy_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAlbum.SelectedItem is not Album album)
            {
                MessageBox.Show("Выберите альбом!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();

                // Создать новый заказ
                int orderId;
                using (SqlCommand cmd = new(
                    @"INSERT INTO Orderr (Status_order, ID_user, Date_of_execution)
                      VALUES (N'Новый', @uid, GETDATE());
                      SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", _userId);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Добавить альбом в заказ
                using (SqlCommand cmd = new(
                    @"INSERT INTO Album_Order (ID_album, ID_order, Cost)
                      VALUES (@aid, @oid, @cost)", conn))
                {
                    cmd.Parameters.AddWithValue("@aid", album.ID_album);
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.Parameters.AddWithValue("@cost", album.Price);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show(
                    $"Альбом «{album.Name}» успешно заказан!\nСумма: {album.Price:N2} руб.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}