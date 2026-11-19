using Microsoft.Maui.Controls.Shapes;

namespace AutoLog.Views;

public static class BasePage
{
    public static void AttachMenu(ContentPage page)
    {
        bool isWrapped = false;
        Border? drawer = null;
        BoxView? backdrop = null;

        page.Appearing += (s, e) =>
        {
            if (isWrapped || page.Content == null) return;
            isWrapped = true;

            var originalContent = page.Content;

            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#1E2433")
            };

            // 1. ВЕРХНЯ ПАНЕЛЬ (Логотип зверху зліва, три риски під ним, заголовок по центру)
            var headerBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#252D40"),
                Stroke = Color.FromArgb("#38435E"),
                StrokeThickness = 1,
                Padding = new Thickness(24, 12)
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            var leftStack = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Start };

            var logoRow = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
            logoRow.Children.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#3B82F6"),
                WidthRequest = 30,
                HeightRequest = 30,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Label
                {
                    Text = "◈",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
            logoRow.Children.Add(new Label
            {
                Text = "AutoLog",
                FontSize = 19,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F8FAFC"),
                VerticalOptions = LayoutOptions.Center
            });

            var menuBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#313B54"),
                Stroke = Color.FromArgb("#475577"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 6),
                HorizontalOptions = LayoutOptions.Start,
                StrokeShape = new RoundRectangle { CornerRadius = 8 }
            };

            var menuBtnRow = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
            menuBtnRow.Children.Add(new Label { Text = "☰", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#60A5FA"), VerticalOptions = LayoutOptions.Center });
            menuBtnRow.Children.Add(new Label { Text = "Меню", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E2E8F0"), VerticalOptions = LayoutOptions.Center });
            menuBtn.Content = menuBtnRow;

            leftStack.Children.Add(logoRow);
            leftStack.Children.Add(menuBtn);

            var centerTitleStack = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 2,
                Margin = new Thickness(-110, 0, 0, 0)
            };
            centerTitleStack.Children.Add(new Label
            {
                Text = "INTELLIGENT FLEET SYSTEM",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#60A5FA"),
                CharacterSpacing = 1.8,
                HorizontalOptions = LayoutOptions.Center
            });
            centerTitleStack.Children.Add(new Label
            {
                Text = page.Title ?? "Керування автопарком",
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F8FAFC"),
                HorizontalOptions = LayoutOptions.Center
            });

            headerGrid.Add(leftStack, 0, 0);
            headerGrid.Add(centerTitleStack, 1, 0);
            headerBorder.Content = headerGrid;

            // 2. ПРОЗОРА ЗОНА ЗАКРИТТЯ МЕНЮ
            backdrop = new BoxView
            {
                Color = Color.FromArgb("#55090D16"),
                BackgroundColor = Colors.Transparent,
                IsVisible = false
            };

            // 3. ВИСУВНЕ МЕНЮ ЗЛІВА (їздить прямо поверх сторінки!)
            drawer = BuildLeftDrawer(async () => await ToggleDrawerAsync(drawer, backdrop, false));

            var openTap = new TapGestureRecognizer();
            openTap.Tapped += async (sender, args) => await ToggleDrawerAsync(drawer, backdrop, true);
            menuBtn.GestureRecognizers.Add(openTap);

            var closeTap = new TapGestureRecognizer();
            closeTap.Tapped += async (sender, args) => await ToggleDrawerAsync(drawer, backdrop, false);
            backdrop.GestureRecognizers.Add(closeTap);

            rootGrid.Add(headerBorder, 0, 0);
            rootGrid.Add(originalContent, 0, 1);

            rootGrid.Add(backdrop, 0, 0);
            Grid.SetRowSpan(backdrop, 2);

            rootGrid.Add(drawer, 0, 0);
            Grid.SetRowSpan(drawer, 2);

            page.Content = rootGrid;
        };
    }

    private static async Task ToggleDrawerAsync(Border? drawer, BoxView? backdrop, bool open)
    {
        if (drawer == null || backdrop == null) return;
        if (open)
        {
            backdrop.IsVisible = true;
            await drawer.TranslateTo(0, 0, 250, Easing.CubicOut);
        }
        else
        {
            await drawer.TranslateTo(-310, 0, 200, Easing.CubicIn);
            backdrop.IsVisible = false;
        }
    }

    private static Border BuildLeftDrawer(Func<Task> closeAction)
    {
        var shell = AppShell.Instance;
        string userName = shell?.UserFullName ?? Preferences.Get("UserName", "Користувач");
        string userEmail = shell?.UserEmail ?? Preferences.Get("UserEmail", "user@autolog.ua");
        string initials = shell?.UserInitials ?? "U";

        var drawerGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var topRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Margin = new Thickness(0, 0, 0, 28)
        };

        var brandRow = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
        brandRow.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#3B82F6"),
            WidthRequest = 36,
            HeightRequest = 36,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Label { Text = "◈", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        });
        var brandText = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
        brandText.Children.Add(new Label { Text = "AutoLog", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
        brandText.Children.Add(new Label { Text = "Smart Fleet & Fuel", FontSize = 11, TextColor = Color.FromArgb("#94A3B8") });
        brandRow.Children.Add(brandText);

        var closeBtn = new Button
        {
            Text = "✕",
            BackgroundColor = Color.FromArgb("#313B54"),
            TextColor = Color.FromArgb("#CBD5E1"),
            WidthRequest = 34,
            HeightRequest = 34,
            CornerRadius = 9,
            Padding = 0
        };
        closeBtn.Clicked += async (s, e) => await closeAction();

        topRow.Add(brandRow, 0, 0);
        topRow.Add(closeBtn, 1, 0);

        var navStack = new VerticalStackLayout { Spacing = 10 };
        navStack.Children.Add(new Label { Text = "НАВІГАЦІЯ", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#94A3B8"), CharacterSpacing = 1.4, Margin = new Thickness(6, 0, 0, 6) });

        navStack.Children.Add(CreateNavButton("❖    Мій автопарк", "VehiclesPage", shell?.ActiveRoute == "VehiclesPage", closeAction));
        navStack.Children.Add(CreateNavButton("◈    Калькулятор АЗС", "OptimizerPage", shell?.ActiveRoute == "OptimizerPage", closeAction));
        navStack.Children.Add(CreateNavButton("⚙    Параметри профілю", "SettingsPage", shell?.ActiveRoute == "SettingsPage", closeAction));

        var profileCard = new Border
        {
            BackgroundColor = Color.FromArgb("#2C364E"),
            Stroke = Color.FromArgb("#435170"),
            StrokeThickness = 1,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 16 }
        };

        var profileStack = new VerticalStackLayout { Spacing = 12 };
        var userInfoGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12,
            VerticalOptions = LayoutOptions.Center
        };

        userInfoGrid.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#3B82F6"),
            WidthRequest = 42,
            HeightRequest = 42,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 21 },
            Content = new Label { Text = initials, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        }, 0, 0);

        var nameStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        nameStack.Children.Add(new Label { Text = userName, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, LineBreakMode = LineBreakMode.TailTruncation });
        nameStack.Children.Add(new Label { Text = userEmail, FontSize = 12, TextColor = Color.FromArgb("#CBD5E1"), LineBreakMode = LineBreakMode.TailTruncation });
        userInfoGrid.Add(nameStack, 1, 0);

        var logoutBtn = new Button
        {
            Text = "Вийти з облікового запису",
            BackgroundColor = Color.FromArgb("#33EF4444"),
            TextColor = Color.FromArgb("#FCA5A5"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 38,
            CornerRadius = 10
        };
        logoutBtn.Clicked += async (s, e) =>
        {
            await closeAction();
            shell?.LogoutCommand.Execute(null);
        };

        profileStack.Children.Add(userInfoGrid);
        profileStack.Children.Add(logoutBtn);
        profileCard.Content = profileStack;

        drawerGrid.Add(topRow, 0, 0);
        drawerGrid.Add(navStack, 0, 1);
        drawerGrid.Add(profileCard, 0, 2);

        return new Border
        {
            WidthRequest = 290,
            HorizontalOptions = LayoutOptions.Start,
            BackgroundColor = Color.FromArgb("#232B3E"),
            Stroke = Color.FromArgb("#3E4C6D"),
            StrokeThickness = 1,
            TranslationX = -310,
            Padding = new Thickness(22, 26),
            Content = drawerGrid
        };
    }

    private static Button CreateNavButton(string title, string route, bool isActive, Func<Task> closeAction)
    {
        var btn = new Button
        {
            Text = title,
            BackgroundColor = Color.FromArgb(isActive ? "#3B82F6" : "#313B54"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            HeightRequest = 48,
            CornerRadius = 12
        };
        btn.Clicked += async (s, e) =>
        {
            await closeAction();
            if (AppShell.Instance != null)
            {
                AppShell.Instance.ActiveRoute = route;
                await Shell.Current.GoToAsync($"///{route}");
            }
        };
        return btn;
    }
}