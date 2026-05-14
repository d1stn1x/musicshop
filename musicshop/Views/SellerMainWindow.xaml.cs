using musicshop.Helpers;
using musicshop.Models;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace musicshop
{
    public partial class SellerMainWindow : Window
    {
        private readonly int _userId;
        private readonly string _fullName;

        private List<Album> _allAlbums = new();
        private List<Artist> _allArtists = new();
        private List<Group> _allGroups = new();
        private List<Order> _allOrders = new();

        public SellerMainWindow(string fullName, int userId)
        {
            InitializeComponent();
            _userId = userId;
            _fullName = fullName;
            txtUserInfo.Text = $"{fullName} (Продавец)";
            LoadAll();
        }

        private void LoadAll()
        {
            LoadAlbums();
            LoadArtists();
            LoadGroups();
            LoadOrders();
            LoadFeedback();
        }

        // ══════════════════════════════════════════════════════
        //  АЛЬБОМЫ
        // ══════════════════════════════════════════════════════
        private void LoadAlbums()
        {
            _allAlbums = new List<Album>();
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
                    _allAlbums.Add(new Album
                    {
                        ID_album = (int)r["ID_album"],
                        Name = r["Name"].ToString()!,
                        Description = r["Description"]?.ToString(),
                        Year_of_release = (int)r["Year_of_release"],
                        GroupName = r["GroupName"]?.ToString(),
                        Price = (decimal)r["Price"]
                    });
            }
            catch (Exception ex) { Err("Альбомы", ex); }
            RenderAlbums(_allAlbums);
        }

        private void RenderAlbums(List<Album> albums)
        {
            wrapAlbums.Children.Clear();
            foreach (var a in albums)
                wrapAlbums.Children.Add(MakeCard(
                    a.Name,
                    $"{a.GroupName} · {a.Year_of_release}",
                    a.Description ?? "",
                    a.Price > 0 ? $"{a.Price:N2} руб." : "—",
                    "#FFFCD6"
                ));
        }

        private void txtSearchAlbum_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchAlbum.Text.ToLower();
            RenderAlbums(string.IsNullOrEmpty(q) ? _allAlbums
                : _allAlbums.Where(a => a.Name.ToLower().Contains(q) ||
                                        (a.GroupName?.ToLower().Contains(q) ?? false)).ToList());
        }

        // ══════════════════════════════════════════════════════
        //  АРТИСТЫ
        // ══════════════════════════════════════════════════════
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
            }
            catch (Exception ex) { Err("Артисты", ex); }
            RenderArtists(_allArtists);
        }

        private void RenderArtists(List<Artist> artists)
        {
            wrapArtists.Children.Clear();
            foreach (var a in artists)
                wrapArtists.Children.Add(MakeCard(
                    a.FullName ?? a.Name,
                    a.Genre ?? "",
                    a.Biography ?? "",
                    "",
                    "#FFF0F2"
                ));
        }

        private void txtSearchArtist_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchArtist.Text.ToLower();
            RenderArtists(string.IsNullOrEmpty(q) ? _allArtists
                : _allArtists.Where(a => (a.FullName?.ToLower().Contains(q) ?? false) ||
                                         (a.Genre?.ToLower().Contains(q) ?? false)).ToList());
        }

        // ══════════════════════════════════════════════════════
        //  ГРУППЫ
        // ══════════════════════════════════════════════════════
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
            }
            catch (Exception ex) { Err("Группы", ex); }
            RenderGroups(_allGroups);
        }

        private void RenderGroups(List<Group> groups)
        {
            wrapGroups.Children.Clear();
            foreach (var g in groups)
                wrapGroups.Children.Add(MakeCard(
                    g.Name,
                    g.Genre ?? "",
                    g.Biography ?? "",
                    "",
                    "#F0F8FF"
                ));
        }

        private void txtSearchGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchGroup.Text.ToLower();
            RenderGroups(string.IsNullOrEmpty(q) ? _allGroups
                : _allGroups.Where(g => g.Name.ToLower().Contains(q) ||
                                        (g.Genre?.ToLower().Contains(q) ?? false)).ToList());
        }

        // ══════════════════════════════════════════════════════
        //  ЗАКАЗЫ
        // ══════════════════════════════════════════════════════
        private void LoadOrders(string? statusFilter = null)
        {
            _allOrders = new List<Order>();
            if (wrapOrders == null) return;
            wrapOrders.Children.Clear();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT o.ID_order, o.Status_order, o.Date_of_execution,
                           u.FullName AS ClientName,
                           ISNULL((SELECT SUM(ao.Cost) FROM Album_Order ao
                                   WHERE ao.ID_order = o.ID_order), 0) AS TotalCost,
                           (SELECT TOP 1 a.Name FROM Album_Order ao
                            JOIN Album a ON a.ID_album = ao.ID_album
                            WHERE ao.ID_order = o.ID_order) AS AlbumName
                    FROM Orderr o
                    LEFT JOIN Users u ON u.UserID = o.ID_user
                    WHERE (@status IS NULL OR o.Status_order = @status)
                    ORDER BY o.ID_order DESC";
                using SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@status", (object?)statusFilter ?? DBNull.Value);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    _allOrders.Add(new Order
                    {
                        ID_order = (int)r["ID_order"],
                        Status_order = r["Status_order"]?.ToString(),
                        Date_of_execution = r["Date_of_execution"] == DBNull.Value
                                            ? null : (DateTime?)r["Date_of_execution"],
                        ClientName = r["ClientName"]?.ToString(),
                        TotalCost = (decimal)r["TotalCost"],
                        AlbumName = r["AlbumName"]?.ToString()
                    });
            }
            catch (Exception ex) { Err("Заказы", ex); }
            RenderOrders(_allOrders);
        }

        private void RenderOrders(List<Order> orders)
        {
            wrapOrders.Children.Clear();
            foreach (var o in orders)
            {
                string bg = o.Status_order switch
                {
                    "Новый" => "#FFFCD6",
                    "В обработке" => "#D1EEFC",
                    "Выполнен" => "#C8E8DF",
                    "Отправлен" => "#D1EEFC",
                    "Доставлен" => "#B5D5CA",
                    "Отменён" => "#F5D0D3",
                    "Отменен" => "#F5D0D3",
                    _ => "#FFFFFF"
                };

                var card = MakeCard(
                    $"Заказ №{o.ID_order}",
                    $"{o.Status_order}  ·  {o.Date_of_execution:dd.MM.yyyy}",
                    $"Клиент: {o.ClientName}\nАльбом: {o.AlbumName ?? "—"}",
                    $"{o.TotalCost:N2} руб.",
                    bg
                );

                // Кнопка изменить статус
                var btnStatus = new Button
                {
                    Content = "Изменить статус",
                    Height = 34,
                    Margin = new Thickness(0, 10, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(181, 213, 202)),
                    Foreground = new SolidColorBrush(Color.FromRgb(26, 58, 48)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(74, 138, 122)),
                    BorderThickness = new Thickness(1),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Tag = o
                };
                btnStatus.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is Order ord)
                    {
                        var win = new OrderStatusWindow(ord.ID_order, ord.Status_order);
                        if (win.ShowDialog() == true)
                        {
                            string? st = (cmbOrderStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
                            LoadOrders(st == "Все" ? null : st);
                        }
                    }
                };
                (card.Child as StackPanel)?.Children.Add(btnStatus);

                wrapOrders.Children.Add(card);
            }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (new OrderEditWindow("Продавец", _userId).ShowDialog() == true) LoadOrders();
        }

        private void cmbOrderStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? st = (cmbOrderStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            LoadOrders(st == "Все" ? null : st);
        }

        // ══════════════════════════════════════════════════════
        //  ОТЗЫВЫ
        // ══════════════════════════════════════════════════════
        private void LoadFeedback()
        {
            wrapFeedback.Children.Clear();
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                const string sql = @"
                    SELECT f.ID_feedback, f.Comment, f.Rate,
                           u.FullName AS ClientName, a.Name AS AlbumName
                    FROM Feedback f
                    LEFT JOIN Users u ON u.UserID   = f.ID_user
                    LEFT JOIN Album a ON a.ID_album = f.ID_album
                    ORDER BY f.ID_feedback DESC";
                using SqlCommand cmd = new(sql, conn);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int rate = r["Rate"] == DBNull.Value ? 0 : Convert.ToInt32(r["Rate"]);
                    var card = MakeCard(
                        r["AlbumName"]?.ToString() ?? "",
                        $"Оценка: {rate}/5  —  {r["ClientName"]}",
                        r["Comment"]?.ToString() ?? "",
                        "",
                        "#FFFFFF"
                    );
                    wrapFeedback.Children.Add(card);
                }
            }
            catch (Exception ex) { Err("Отзывы", ex); }
        }

        // ══════════════════════════════════════════════════════
        //  ЭКСПОРТ В EXCEL
        // ══════════════════════════════════════════════════════
        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var tab = tabMain.SelectedItem as TabItem;
            string header = tab?.Header?.ToString()?.Trim() ?? "";

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
                    ws.Cell(1, 3).Value = "Группа"; ws.Cell(1, 4).Value = "Год";
                    ws.Cell(1, 5).Value = "Описание"; ws.Cell(1, 6).Value = "Цена";
                    int row = 2;
                    foreach (var a in _allAlbums)
                    {
                        ws.Cell(row, 1).Value = a.ID_album;
                        ws.Cell(row, 2).Value = a.Name;
                        ws.Cell(row, 3).Value = a.GroupName;
                        ws.Cell(row, 4).Value = a.Year_of_release;
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
                    foreach (var a in _allArtists)
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
                    foreach (var g in _allGroups)
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
                    ws.Cell(1, 3).Value = "Дата"; ws.Cell(1, 4).Value = "Клиент";
                    ws.Cell(1, 5).Value = "Альбом"; ws.Cell(1, 6).Value = "Сумма";
                    int row = 2;
                    foreach (var o in _allOrders)
                    {
                        ws.Cell(row, 1).Value = o.ID_order;
                        ws.Cell(row, 2).Value = o.Status_order;
                        ws.Cell(row, 3).Value = o.Date_of_execution?.ToString("dd.MM.yyyy");
                        ws.Cell(row, 4).Value = o.ClientName;
                        ws.Cell(row, 5).Value = o.AlbumName;
                        ws.Cell(row, 6).Value = (double)o.TotalCost;
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

        // ══════════════════════════════════════════════════════
        //  СОЗДАНИЕ КАРТОЧКИ
        // ══════════════════════════════════════════════════════
        private Border MakeCard(string title, string subtitle,
                                string body, string badge, string cardBg)
        {
            var sp = new StackPanel { Margin = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Stretch };

            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(26, 58, 48)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            if (!string.IsNullOrEmpty(subtitle))
                sp.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 122, 106)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6)
                });

            if (!string.IsNullOrEmpty(body))
                sp.Children.Add(new TextBlock
                {
                    Text = body.Length > 100 ? body[..100] + "…" : body,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6)
                });

            if (!string.IsNullOrEmpty(badge))
            {
                var badgeBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFCD6")),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                badgeBorder.Child = new TextBlock
                {
                    Text = badge,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(26, 58, 48))
                };
                sp.Children.Add(badgeBorder);
            }

            var card = new Border
            {
                Width = 270,
                Child = sp,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cardBg)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(6),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B5D5CA")),
                BorderThickness = new Thickness(1)
            };
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.10
            };
            card.MouseEnter += (s, e) =>
            {
                if (s is Border b)
                    b.BorderBrush = new SolidColorBrush(Color.FromRgb(74, 138, 122));
            };
            card.MouseLeave += (s, e) =>
            {
                if (s is Border b)
                    b.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#B5D5CA"));
            };
            return card;
        }

        // ══════════════════════════════════════════════════════
        //  ВЫХОД
        // ══════════════════════════════════════════════════════
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private static void Err(string where, Exception ex)
            => MessageBox.Show($"Ошибка ({where}):\n{ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
    }
}