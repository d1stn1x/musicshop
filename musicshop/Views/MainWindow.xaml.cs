using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using musicshop.Models;
using ClosedXML.Excel;
using Microsoft.Win32;
using musicshop.Helpers;

namespace musicshop
{
    public partial class MainWindow : Window
    {
        private readonly string _role;
        private readonly int _userId;
        private List<Album> _allAlbums = new();
        private List<Artist> _allArtists = new();
        private List<Group> _allGroups = new();

        private const string RoleAdmin = "Администратор";
        private const string RoleSeller = "Продавец";
        private const string RoleClient = "Клиент";

        public MainWindow(string role, string fullName, int userId)
        {
            InitializeComponent();
            _role = role;
            _userId = userId;
            txtUserInfo.Text = $"{fullName} ({role})";
            ApplyRolePermissions();
            LoadAll();
        }

        private void ApplyRolePermissions()
        {
            switch (_role)
            {
                case RoleAdmin:
                    roleBadge.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    txtRoleBadge.Text = "ADMIN";
                    txtRoleBadge.Foreground = Brushes.White;
                    break;
                case RoleSeller:
                    roleBadge.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    txtRoleBadge.Text = "ПРОДАВЕЦ";
                    txtRoleBadge.Foreground = Brushes.White;
                    break;
                default:
                    roleBadge.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    txtRoleBadge.Text = "КЛИЕНТ";
                    txtRoleBadge.Foreground = Brushes.White;
                    break;
            }

            bool isAdmin = _role == RoleAdmin;
            bool isSeller = _role == RoleSeller;
            bool isClient = _role == RoleClient;

            tabUsers.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            tabOrdersAdmin.Visibility = isClient ? Visibility.Collapsed : Visibility.Visible;
            tabOrdersClient.Visibility = isClient ? Visibility.Visible : Visibility.Collapsed;

            btnDeleteAlbum.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteArtist.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteGroup.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteFeedback.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            bool canEditCatalog = isAdmin;

            btnAddAlbum.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;
            btnEditAlbum.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;

            btnAddArtist.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;
            btnEditArtist.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;

            btnAddGroup.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;
            btnEditGroup.Visibility = canEditCatalog ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadAll()
        {
            LoadAlbums();
            LoadArtists();
            LoadGroups();
            if (_role == RoleClient) LoadOrdersClient();
            else LoadOrders();
            LoadFeedback();
            if (_role == RoleAdmin) LoadUsers();
        }

        // ══════════════════════════════════════════════════════════════
        //  АЛЬБОМЫ
        // ══════════════════════════════════════════════════════════════
        private void LoadAlbums()
        {
            _allAlbums = new List<Album>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT a.ID_album, a.Name, a.Description, a.Year_of_release,
                           a.ID_group, g.Name AS GroupName,
                           ISNULL((SELECT TOP 1 ao.Cost
                                   FROM Album_Order ao
                                   WHERE ao.ID_album = a.ID_album
                                   ORDER BY ao.ID_album_order DESC), 0) AS Price
                    FROM   Album a
                    LEFT JOIN Groupp g ON a.ID_group = g.ID_group
                    ORDER  BY a.Name";
                using SqlCommand cmd = new(sql, conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    _allAlbums.Add(new Album
                    {
                        ID_album = (int)r["ID_album"],
                        Name = r["Name"].ToString()!,
                        Description = r["Description"]?.ToString(),
                        Year_of_release = (int)r["Year_of_release"],
                        ID_group = r["ID_group"] == DBNull.Value ? null : (int?)r["ID_group"],
                        GroupName = r["GroupName"]?.ToString(),
                        Price = (decimal)r["Price"]
                    });
                dgAlbums.ItemsSource = _allAlbums;
            }
            catch (Exception ex) { Err("Альбомы", ex); }
        }

        private void AddAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (new AlbumEditWindow().ShowDialog() == true) LoadAlbums();
        }

        private void EditAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (dgAlbums.SelectedItem is Album a &&
                new AlbumEditWindow(a).ShowDialog() == true) LoadAlbums();
        }

        private void DeleteAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (dgAlbums.SelectedItem is not Album a) return;
            if (!Confirm($"Удалить альбом «{a.Name}»?")) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM Album WHERE ID_album = @id", conn);
                cmd.Parameters.AddWithValue("@id", a.ID_album);
                cmd.ExecuteNonQuery();
                LoadAlbums();
            }
            catch (Exception ex) { Err("Удаление альбома", ex); }
        }

        private void dgAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool sel = dgAlbums.SelectedItem != null;
            if (btnEditAlbum.Visibility == Visibility.Visible) btnEditAlbum.IsEnabled = sel;
            if (btnDeleteAlbum.Visibility == Visibility.Visible) btnDeleteAlbum.IsEnabled = sel;
        }

        private void txtSearchAlbum_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchAlbum.Text.ToLower();
            dgAlbums.ItemsSource = string.IsNullOrEmpty(q)
                ? _allAlbums
                : _allAlbums.Where(a =>
                    a.Name.ToLower().Contains(q) ||
                    (a.GroupName?.ToLower().Contains(q) ?? false)).ToList();
        }

        // ══════════════════════════════════════════════════════════════
        //  АРТИСТЫ
        // ══════════════════════════════════════════════════════════════
        private void LoadArtists()
        {
            _allArtists = new List<Artist>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("SELECT * FROM Artist ORDER BY Surname", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    _allArtists.Add(new Artist
                    {
                        ID_artist = (int)r["ID_artist"],
                        Name = r["Name"].ToString()!,
                        Surname = r["Surname"]?.ToString(),
                        Genre = r["Genre"]?.ToString(),
                        Biography = r["Biography"]?.ToString()
                    });
                dgArtists.ItemsSource = _allArtists;
            }
            catch (Exception ex) { Err("Артисты", ex); }
        }

        private void AddArtist_Click(object sender, RoutedEventArgs e)
        {
            if (new ArtistEditWindow().ShowDialog() == true) LoadArtists();
        }

        private void EditArtist_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtists.SelectedItem is Artist a &&
                new ArtistEditWindow(a).ShowDialog() == true) LoadArtists();
        }

        private void DeleteArtist_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtists.SelectedItem is not Artist a) return;
            if (!Confirm($"Удалить артиста «{a.FullName}»?")) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM Artist WHERE ID_artist = @id", conn);
                cmd.Parameters.AddWithValue("@id", a.ID_artist);
                cmd.ExecuteNonQuery();
                LoadArtists();
            }
            catch (Exception ex) { Err("Удаление артиста", ex); }
        }

        private void dgArtists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool sel = dgArtists.SelectedItem != null;
            if (btnEditArtist.Visibility == Visibility.Visible) btnEditArtist.IsEnabled = sel;
            if (btnDeleteArtist.Visibility == Visibility.Visible) btnDeleteArtist.IsEnabled = sel;
        }

        private void txtSearchArtist_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchArtist.Text.ToLower();
            dgArtists.ItemsSource = string.IsNullOrEmpty(q)
                ? _allArtists
                : _allArtists.Where(a =>
                    a.Name.ToLower().Contains(q) ||
                    (a.Surname?.ToLower().Contains(q) ?? false) ||
                    (a.Genre?.ToLower().Contains(q) ?? false)).ToList();
        }

        // ══════════════════════════════════════════════════════════════
        //  ГРУППЫ
        // ══════════════════════════════════════════════════════════════
        private void LoadGroups()
        {
            _allGroups = new List<Group>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("SELECT * FROM Groupp ORDER BY Name", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    _allGroups.Add(new Group
                    {
                        ID_group = (int)r["ID_group"],
                        Name = r["Name"].ToString()!,
                        Genre = r["Genre"]?.ToString(),
                        Biography = r["Biography"]?.ToString()
                    });
                dgGroups.ItemsSource = _allGroups;
            }
            catch (Exception ex) { Err("Группы", ex); }
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            if (new GroupEditWindow().ShowDialog() == true) LoadGroups();
        }

        private void EditGroup_Click(object sender, RoutedEventArgs e)
        {
            if (dgGroups.SelectedItem is Group g &&
                new GroupEditWindow(g).ShowDialog() == true) LoadGroups();
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (dgGroups.SelectedItem is not Group g) return;
            if (!Confirm($"Удалить группу «{g.Name}»?")) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM Groupp WHERE ID_group = @id", conn);
                cmd.Parameters.AddWithValue("@id", g.ID_group);
                cmd.ExecuteNonQuery();
                LoadGroups();
            }
            catch (Exception ex) { Err("Удаление группы", ex); }
        }

        private void dgGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool sel = dgGroups.SelectedItem != null;
            if (btnEditGroup.Visibility == Visibility.Visible) btnEditGroup.IsEnabled = sel;
            if (btnDeleteGroup.Visibility == Visibility.Visible) btnDeleteGroup.IsEnabled = sel;
        }

        private void txtSearchGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchGroup.Text.ToLower();
            dgGroups.ItemsSource = string.IsNullOrEmpty(q)
                ? _allGroups
                : _allGroups.Where(g =>
                    g.Name.ToLower().Contains(q) ||
                    (g.Genre?.ToLower().Contains(q) ?? false)).ToList();
        }

        // ══════════════════════════════════════════════════════════════
        //  ЗАКАЗЫ — Админ/Продавец
        // ══════════════════════════════════════════════════════════════
        private void LoadOrders(string? status = null)
        {
            if (dgOrders == null) return;
            var list = new List<Order>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT o.ID_order, o.Status_order, o.Date_of_execution,
                           u.FullName AS ClientName,
                           ISNULL((SELECT SUM(ao.Cost) FROM Album_Order ao
                                   WHERE ao.ID_order = o.ID_order), 0) AS TotalCost
                    FROM Orderr o
                    LEFT JOIN Users u ON u.UserID = o.ID_user
                    WHERE (@status IS NULL OR o.Status_order = @status)
                    ORDER BY o.ID_order DESC";
                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Order
                    {
                        ID_order = (int)r["ID_order"],
                        Status_order = r["Status_order"]?.ToString(),
                        Date_of_execution = r["Date_of_execution"] == DBNull.Value
                                            ? null : (DateTime?)r["Date_of_execution"],
                        ClientName = r["ClientName"]?.ToString(),
                        TotalCost = (decimal)r["TotalCost"]
                    });
                dgOrders.ItemsSource = list;
            }
            catch (Exception ex) { Err("Заказы", ex); }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (new OrderEditWindow(_role, _userId).ShowDialog() == true) LoadOrders();
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnChangeStatus.IsEnabled = dgOrders.SelectedItem is Order;
        }

        private void ChangeOrderStatus_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem is not Order selected) return;
            var win = new OrderStatusWindow(selected.ID_order, selected.Status_order);
            if (win.ShowDialog() == true) LoadOrders();
        }

        private void cmbOrderStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders == null) return;
            string? st = (cmbOrderStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            LoadOrders(st == "Все" ? null : st);
        }

        // ══════════════════════════════════════════════════════════════
        //  ЗАКАЗЫ — Клиент (только свои)
        // ══════════════════════════════════════════════════════════════
        private void LoadOrdersClient()
        {
            if (dgOrdersClient == null) return;
            var list = new List<Order>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT o.ID_order, o.Status_order, o.Date_of_execution,
                           ISNULL((SELECT SUM(ao.Cost) FROM Album_Order ao
                                   WHERE ao.ID_order = o.ID_order), 0) AS TotalCost,
                           (SELECT TOP 1 a.Name FROM Album_Order ao
                            JOIN Album a ON a.ID_album = ao.ID_album
                            WHERE ao.ID_order = o.ID_order) AS AlbumName
                    FROM Orderr o
                    WHERE o.ID_user = @uid
                    ORDER BY o.ID_order DESC";
                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@uid", _userId);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Order
                    {
                        ID_order = (int)r["ID_order"],
                        Status_order = r["Status_order"]?.ToString(),
                        Date_of_execution = r["Date_of_execution"] == DBNull.Value
                                            ? null : (DateTime?)r["Date_of_execution"],
                        TotalCost = (decimal)r["TotalCost"],
                        AlbumName = r["AlbumName"]?.ToString()
                    });
                dgOrdersClient.ItemsSource = list;
            }
            catch (Exception ex) { Err("Мои заказы", ex); }
        }

        private void BuyAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (new BuyAlbumWindow(_userId).ShowDialog() == true) LoadOrdersClient();
        }

        private void dgOrdersClient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdersClient.SelectedItem is Order o)
                btnCancelOrder.IsEnabled = o.Status_order == "Новый";
            else
                btnCancelOrder.IsEnabled = false;
        }

        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrdersClient.SelectedItem is not Order o) return;
            if (o.Status_order != "Новый")
            {
                MessageBox.Show("Можно отменить только заказ со статусом «Новый».",
                    "Нельзя отменить", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Отменить заказ №{o.ID_order}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new(
                    "UPDATE Orderr SET Status_order = N'Отменён' WHERE ID_order = @id AND ID_user = @uid",
                    conn);
                cmd.Parameters.AddWithValue("@id", o.ID_order);
                cmd.Parameters.AddWithValue("@uid", _userId);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Заказ отменён.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrdersClient();
            }
            catch (Exception ex) { Err("Отмена заказа", ex); }
        }

        // ══════════════════════════════════════════════════════════════
        //  ОТЗЫВЫ
        // ══════════════════════════════════════════════════════════════
        private void LoadFeedback()
        {
            var list = new List<Feedback>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT f.ID_feedback, f.Comment, f.Rate, f.ID_user, f.ID_album,
                           u.FullName AS ClientName,
                           a.Name    AS AlbumName
                    FROM   Feedback f
                    LEFT JOIN Users u ON u.UserID   = f.ID_user
                    LEFT JOIN Album a ON a.ID_album = f.ID_album
                    ORDER  BY f.ID_feedback DESC";
                using SqlCommand cmd = new(sql, conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Feedback
                    {
                        ID_feedback = (int)r["ID_feedback"],
                        Comment = r["Comment"]?.ToString(),
                        Rate = r["Rate"] == DBNull.Value
                                      ? null : (int?)Convert.ToInt32(r["Rate"]),
                        ID_album = r["ID_album"] == DBNull.Value ? null : (int?)r["ID_album"],
                        ClientName = r["ClientName"]?.ToString(),
                        AlbumName = r["AlbumName"]?.ToString()
                    });
                dgFeedback.ItemsSource = list;
            }
            catch (Exception ex) { Err("Отзывы", ex); }
        }

        private void AddFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (new FeedbackEditWindow(_role, _userId).ShowDialog() == true) LoadFeedback();
        }

        private void DeleteFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (dgFeedback.SelectedItem is not Feedback f) return;
            if (!Confirm("Удалить этот отзыв?")) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM Feedback WHERE ID_feedback = @id", conn);
                cmd.Parameters.AddWithValue("@id", f.ID_feedback);
                cmd.ExecuteNonQuery();
                LoadFeedback();
            }
            catch (Exception ex) { Err("Удаление отзыва", ex); }
        }

        private void dgFeedback_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (btnDeleteFeedback.Visibility == Visibility.Visible)
                btnDeleteFeedback.IsEnabled = dgFeedback.SelectedItem != null;
        }

        // ══════════════════════════════════════════════════════════════
        //  ПОЛЬЗОВАТЕЛИ (только Администратор)
        // ══════════════════════════════════════════════════════════════
        private void LoadUsers()
        {
            var list = new List<User>();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("SELECT * FROM Users ORDER BY Role, Username", conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new User
                    {
                        UserID = (int)r["UserID"],
                        Username = r["Username"].ToString()!,
                        FullName = r["FullName"]?.ToString(),
                        Role = r["Role"].ToString()!,
                        Email = r["Email"]?.ToString(),
                        IsActive = (bool)r["IsActive"],
                        CreatedAt = r["CreatedAt"] == DBNull.Value
                                    ? null : (DateTime?)r["CreatedAt"]
                    });
                dgUsers.ItemsSource = list;
            }
            catch (Exception ex) { Err("Пользователи", ex); }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            if (new UserEditWindow().ShowDialog() == true) LoadUsers();
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is User u &&
                new UserEditWindow(u).ShowDialog() == true) LoadUsers();
        }

        private void ToggleUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not User u) return;
            bool newState = !u.IsActive;
            string action = newState ? "активировать" : "заблокировать";
            if (!Confirm($"Вы хотите {action} пользователя «{u.Username}»?")) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("UPDATE Users SET IsActive=@a WHERE UserID=@id", conn);
                cmd.Parameters.AddWithValue("@a", newState);
                cmd.Parameters.AddWithValue("@id", u.UserID);
                cmd.ExecuteNonQuery();
                LoadUsers();
            }
            catch (Exception ex) { Err("Блокировка пользователя", ex); }
        }

        private void dgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool sel = dgUsers.SelectedItem is User;
            btnEditUser.IsEnabled = sel;
            btnToggleUser.IsEnabled = sel;
            if (dgUsers.SelectedItem is User u)
                btnToggleUser.Content = u.IsActive ? "Заблокировать" : "Активировать";
        }

        // ══════════════════════════════════════════════════════════════
        //  ЭКСПОРТ В EXCEL
        // ══════════════════════════════════════════════════════════════
        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var tab = tabMain.SelectedItem as TabItem;
            string header = tab?.Header?.ToString() ?? "";

            var dlg = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"{header}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(header);

                if (header == "Альбомы")
                {
                    ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Название";
                    ws.Cell(1, 3).Value = "Год"; ws.Cell(1, 4).Value = "Группа";
                    ws.Cell(1, 5).Value = "Описание"; ws.Cell(1, 6).Value = "Цена";
                    int row = 2;
                    foreach (Album a in dgAlbums.Items)
                    {
                        ws.Cell(row, 1).Value = a.ID_album;
                        ws.Cell(row, 2).Value = a.Name;
                        ws.Cell(row, 3).Value = a.Year_of_release;
                        ws.Cell(row, 4).Value = a.GroupName;
                        ws.Cell(row, 5).Value = a.Description;
                        ws.Cell(row, 6).Value = (double)a.Price;
                        row++;
                    }
                }
                else if (header == "Артисты")
                {
                    ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Имя";
                    ws.Cell(1, 3).Value = "Фамилия"; ws.Cell(1, 4).Value = "Жанр";
                    ws.Cell(1, 5).Value = "Биография";
                    int row = 2;
                    foreach (Artist a in dgArtists.Items)
                    {
                        ws.Cell(row, 1).Value = a.ID_artist;
                        ws.Cell(row, 2).Value = a.Name;
                        ws.Cell(row, 3).Value = a.Surname;
                        ws.Cell(row, 4).Value = a.Genre;
                        ws.Cell(row, 5).Value = a.Biography;
                        row++;
                    }
                }
                else if (header == "Группы")
                {
                    ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Название";
                    ws.Cell(1, 3).Value = "Жанр"; ws.Cell(1, 4).Value = "Биография";
                    int row = 2;
                    foreach (Group g in dgGroups.Items)
                    {
                        ws.Cell(row, 1).Value = g.ID_group;
                        ws.Cell(row, 2).Value = g.Name;
                        ws.Cell(row, 3).Value = g.Genre;
                        ws.Cell(row, 4).Value = g.Biography;
                        row++;
                    }
                }
                else if (header == "Заказы")
                {
                    ws.Cell(1, 1).Value = "№"; ws.Cell(1, 2).Value = "Статус";
                    ws.Cell(1, 3).Value = "Дата"; ws.Cell(1, 4).Value = "Пользователь";
                    ws.Cell(1, 5).Value = "Сумма";
                    int row = 2;
                    foreach (Order o in dgOrders.Items)
                    {
                        ws.Cell(row, 1).Value = o.ID_order;
                        ws.Cell(row, 2).Value = o.Status_order;
                        ws.Cell(row, 3).Value = o.Date_of_execution?.ToString("dd.MM.yyyy");
                        ws.Cell(row, 4).Value = o.ClientName;
                        ws.Cell(row, 5).Value = (double)o.TotalCost;
                        row++;
                    }
                }
                else if (header == "Мои заказы")
                {
                    ws.Cell(1, 1).Value = "№"; ws.Cell(1, 2).Value = "Статус";
                    ws.Cell(1, 3).Value = "Дата"; ws.Cell(1, 4).Value = "Альбом";
                    ws.Cell(1, 5).Value = "Сумма";
                    int row = 2;
                    foreach (Order o in dgOrdersClient.Items)
                    {
                        ws.Cell(row, 1).Value = o.ID_order;
                        ws.Cell(row, 2).Value = o.Status_order;
                        ws.Cell(row, 3).Value = o.Date_of_execution?.ToString("dd.MM.yyyy");
                        ws.Cell(row, 4).Value = o.AlbumName;
                        ws.Cell(row, 5).Value = (double)o.TotalCost;
                        row++;
                    }
                }
                else if (header == "Отзывы")
                {
                    ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Пользователь";
                    ws.Cell(1, 3).Value = "Альбом"; ws.Cell(1, 4).Value = "Оценка";
                    ws.Cell(1, 5).Value = "Комментарий";
                    int row = 2;
                    foreach (Feedback f in dgFeedback.Items)
                    {
                        ws.Cell(row, 1).Value = f.ID_feedback;
                        ws.Cell(row, 2).Value = f.ClientName;
                        ws.Cell(row, 3).Value = f.AlbumName;
                        ws.Cell(row, 4).Value = f.Rate;
                        ws.Cell(row, 5).Value = f.Comment;
                        row++;
                    }
                }
                else if (header == "Пользователи")
                {
                    ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Логин";
                    ws.Cell(1, 3).Value = "ФИО"; ws.Cell(1, 4).Value = "Роль";
                    ws.Cell(1, 5).Value = "Email"; ws.Cell(1, 6).Value = "Активен";
                    int row = 2;
                    foreach (User u in dgUsers.Items)
                    {
                        ws.Cell(row, 1).Value = u.UserID;
                        ws.Cell(row, 2).Value = u.Username;
                        ws.Cell(row, 3).Value = u.FullName;
                        ws.Cell(row, 4).Value = u.Role;
                        ws.Cell(row, 5).Value = u.Email;
                        ws.Cell(row, 6).Value = u.IsActiveText;
                        row++;
                    }
                }

                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#B5D5CA");
                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);

                MessageBox.Show($"Файл сохранён:\n{dlg.FileName}", "Экспорт завершён",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  ВЫХОД
        // ══════════════════════════════════════════════════════════════
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private static void Err(string where, Exception ex)
            => MessageBox.Show($"Ошибка ({where}):\n{ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);

        private static bool Confirm(string msg)
            => MessageBox.Show(msg, "Подтверждение",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}