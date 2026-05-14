using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class GroupEditWindow : Window
    {
        private readonly Group? _edit;

        public GroupEditWindow() => InitializeComponent();

        public GroupEditWindow(Group g) : this()
        {
            _edit = g;
            lblTitle.Text = "Редактировать группу";
            Title = "Редактировать группу";
            txtName.Text = g.Name;
            txtGenre.Text = g.Genre;
            txtBio.Text = g.Biography;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            { MessageBox.Show("Введите название!", "Ошибка"); return; }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                string sql = _edit == null
                    ? "INSERT INTO Groupp (Name, Genre, Biography) VALUES (@n,@g,@b)"
                    : "UPDATE Groupp SET Name=@n, Genre=@g, Biography=@b WHERE ID_group=@id";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@g", Null(txtGenre.Text));
                cmd.Parameters.AddWithValue("@b", Null(txtBio.Text));
                if (_edit != null) cmd.Parameters.AddWithValue("@id", _edit.ID_group);
                cmd.ExecuteNonQuery();
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private static object Null(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();
    }
}