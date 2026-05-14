using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;
using musicshop.Models;

namespace musicshop
{
    public partial class ClientEditWindow : Window
    {
        private readonly Client? _edit;

        public ClientEditWindow() => InitializeComponent();

        public ClientEditWindow(Client c) : this()
        {
            _edit = c;
            lblTitle.Text = "Редактировать клиента";
            Title = "Редактировать клиента";
            txtName.Text = c.Name;
            txtSurname.Text = c.Surname;
            txtPhone.Text = c.Telephone;
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
                    ? "INSERT INTO Client (Name, Surname, Telephone) VALUES (@n,@s,@t)"
                    : "UPDATE Client SET Name=@n, Surname=@s, Telephone=@t WHERE ID_client=@id";

                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@s", Null(txtSurname.Text));
                cmd.Parameters.AddWithValue("@t", Null(txtPhone.Text));
                if (_edit != null) cmd.Parameters.AddWithValue("@id", _edit.ID_client);
                cmd.ExecuteNonQuery();
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private static object Null(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();
    }
}