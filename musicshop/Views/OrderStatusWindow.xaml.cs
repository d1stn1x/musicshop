using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using musicshop.Helpers;

namespace musicshop
{
    public partial class OrderStatusWindow : Window
    {
        private readonly int _orderId;

        public OrderStatusWindow(int orderId, string? currentStatus)
        {
            InitializeComponent();
            _orderId = orderId;
            tbOrderId.Text = $"#{orderId}";

            // Выбрать текущий статус в ComboBox
            foreach (ComboBoxItem item in cmbStatus.Items)
            {
                if (item.Content?.ToString() == currentStatus)
                {
                    cmbStatus.SelectedItem = item;
                    break;
                }
            }

            if (cmbStatus.SelectedItem == null)
                cmbStatus.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Новый";

            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new(
                    "UPDATE Orderr SET Status_order = @s WHERE ID_order = @id", conn);
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@id", _orderId);
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