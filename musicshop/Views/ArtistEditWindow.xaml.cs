using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class ArtistEditWindow : Window
    {
        private readonly Artist? _edit;

        public ArtistEditWindow() => InitializeComponent();

        public ArtistEditWindow(Artist a) : this()
        {
            _edit = a;
            lblTitle.Text = "Редактировать артиста";
            Title = "Редактировать артиста";
            txtName.Text = a.Name;
            txtSurname.Text = a.Surname;
            txtGenre.Text = a.Genre;
            txtBio.Text = a.Biography;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            { MessageBox.Show("Введите имя!", "Ошибка"); return; }

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                string sql = _edit == null
                    ? "INSERT INTO Artist (Name, Surname, Genre, Biography) VALUES (@n,@s,@g,@b)"
                    : "UPDATE Artist SET Name=@n, Surname=@s, Genre=@g, Biography=@b WHERE ID_artist=@id";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@s", Null(txtSurname.Text));
                cmd.Parameters.AddWithValue("@g", Null(txtGenre.Text));
                cmd.Parameters.AddWithValue("@b", Null(txtBio.Text));
                if (_edit != null) cmd.Parameters.AddWithValue("@id", _edit.ID_artist);
                cmd.ExecuteNonQuery();
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private static object Null(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();
    }
}