using musicshop.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using musicshop.Models;

namespace musicshop
{
    public partial class ClientMainWindow : Window
    {
        private readonly int _userId;
        private readonly string _fullName;
        private List<Album> _allAlbums = new();
        private List<Artist> _allArtists = new();
        private List<Group> _allGroups = new();

        public ClientMainWindow(string fullName, int userId)
        {
            InitializeComponent();
            _userId = userId;
            _fullName = fullName;
            txtUserInfo.Text = $"{fullName} (Клиент)";
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
            {
                var card = MakeCard(
                    a.Name,
                    $"{a.GroupName} · {a.Year_of_release}",
                    a.Description ?? "",
                    a.Price > 0 ? $"{a.Price:N2} руб." : "—",
                    "#FFFCD6"
                );
                var btn = new Button
                {
                    Content = "Купить",
                    Height = 30,
                    Margin = new Thickness(0, 8, 0, 0),
                    Tag = a
                };
                btn.SetResourceReference(StyleProperty, "SuccessButton");
                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is Album alb)
                        BuySingleAlbum(alb);
                };
                (card.Child as StackPanel)?.Children.Add(btn);
                wrapAlbums.Children.Add(card);
            }
        }

        private void BuySingleAlbum(Album album)
        {
            if (MessageBox.Show($"Купить «{album.Name}» за {album.Price:N2} руб.?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                int orderId;
                using (SqlCommand cmd = new(@"
                    INSERT INTO Orderr (Status_order, Date_of_execution, ID_user)
                    VALUES (N'Новый', GETDATE(), @uid);
                    SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", _userId);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (SqlCommand cmd = new(@"
                    INSERT INTO Album_Order (ID_album, ID_order, Cost)
                    VALUES (@a, @o, @c)", conn))
                {
                    cmd.Parameters.AddWithValue("@a", album.ID_album);
                    cmd.Parameters.AddWithValue("@o", orderId);
                    cmd.Parameters.AddWithValue("@c", album.Price);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show($"Альбом «{album.Name}» успешно заказан!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrders();
            }
            catch (Exception ex) { Err("Покупка", ex); }
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
        //  МОИ ЗАКАЗЫ
        // ══════════════════════════════════════════════════════
        private void LoadOrders()
        {
            wrapOrders.Children.Clear();
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
                {
                    var order = new Order
                    {
                        ID_order = (int)r["ID_order"],
                        Status_order = r["Status_order"]?.ToString(),
                        Date_of_execution = r["Date_of_execution"] == DBNull.Value
                                            ? null : (DateTime?)r["Date_of_execution"],
                        TotalCost = (decimal)r["TotalCost"],
                        AlbumName = r["AlbumName"]?.ToString()
                    };

                    string bg = order.Status_order switch
                    {
                        "Новый" => "#FFFCD6",
                        "В обработке" => "#D1EEFC",
                        "Выполнен" => "#C8E8DF",
                        "Доставлен" => "#B5D5CA",
                        "Отменён" => "#F5D0D3",
                        "Отменен" => "#F5D0D3",
                        _ => "#FFFFFF"
                    };

                    var card = MakeCard(
                        $"Заказ №{order.ID_order}",
                        order.Status_order ?? "",
                        order.AlbumName ?? "Альбом не указан",
                        $"{order.TotalCost:N2} руб.",
                        bg
                    );

                    if (order.Status_order == "Новый")
                    {
                        var btnCancel = new Button
                        {
                            Content = "Отменить",
                            Height = 30,
                            Margin = new Thickness(0, 8, 0, 0),
                            Tag = order.ID_order
                        };
                        btnCancel.SetResourceReference(StyleProperty, "DangerButton");
                        btnCancel.Click += (s, e) =>
                        {
                            if (s is Button b && b.Tag is int ordId)
                                CancelOrder(ordId);
                        };
                        (card.Child as StackPanel)?.Children.Add(btnCancel);
                    }
                    wrapOrders.Children.Add(card);
                }
            }
            catch (Exception ex) { Err("Заказы", ex); }
        }

        private void CancelOrder(int orderId)
        {
            if (MessageBox.Show($"Отменить заказ №{orderId}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                using SqlConnection conn = new(AppConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new(
                    "UPDATE Orderr SET Status_order = N'Отменён' WHERE ID_order = @id AND ID_user = @uid",
                    conn);
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.Parameters.AddWithValue("@uid", _userId);
                cmd.ExecuteNonQuery();
                LoadOrders();
            }
            catch (Exception ex) { Err("Отмена заказа", ex); }
        }

        private void BuyAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (new BuyAlbumWindow(_userId).ShowDialog() == true) LoadOrders();
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
                    SELECT f.ID_feedback, f.Comment, f.Rate, f.ID_user, f.ID_album,
                           u.FullName AS ClientName, a.Name AS AlbumName
                    FROM Feedback f
                    LEFT JOIN Users u ON u.UserID = f.ID_user
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

        private void AddFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (new FeedbackEditWindow("Клиент", _userId).ShowDialog() == true)
                LoadFeedback();
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
                    Text = body.Length > 80 ? body[..80] + "…" : body,
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