using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class AlbumEditWindow : Window
    {
        private readonly Album? _edit;

        public AlbumEditWindow()
        {
            InitializeComponent();
            LoadGroups();
        }

        public AlbumEditWindow(Album a) : this()
        {
            _edit = a;
            lblTitle.Text = "Редактировать альбом";
            Title = "Редактировать альбом";
            txtName.Text = a.Name;
            txtYear.Text = a.Year_of_release.ToString();
            txtDesc.Text = a.Description;
            cmbGroup.SelectedValue = a.ID_group;
        }

        private void LoadGroups()
        {
            var list = new List<Group>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new(
                    "SELECT ID_group, Name FROM Groupp ORDER BY Name", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Group
                    {
                        ID_group = (int)r["ID_group"],
                        Name = r["Name"].ToString()!
                    });
            }
            catch { /* не критично */ }
            cmbGroup.ItemsSource = list;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtYear.Text, out int year) ||
                year < 1900 || year > DateTime.Now.Year)
            {
                MessageBox.Show("Введите корректный год выпуска!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();

                string sql = _edit == null
                    ? @"INSERT INTO Album (Name, Year_of_release, ID_group, Description)
                        VALUES (@n, @y, @g, @d)"
                    : @"UPDATE Album
                        SET Name=@n, Year_of_release=@y, ID_group=@g, Description=@d
                        WHERE ID_album=@id";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@y", year);
                cmd.Parameters.AddWithValue("@g",
                    (object?)cmbGroup.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d",
                    string.IsNullOrWhiteSpace(txtDesc.Text)
                        ? DBNull.Value
                        : (object)txtDesc.Text.Trim());

                if (_edit != null)
                    cmd.Parameters.AddWithValue("@id", _edit.ID_album);

                cmd.ExecuteNonQuery();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}