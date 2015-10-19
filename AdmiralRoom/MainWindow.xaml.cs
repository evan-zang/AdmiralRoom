﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;
using Huoyaoyuan.AdmiralRoom.Views;

namespace Huoyaoyuan.AdmiralRoom
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            //Browser button handler
            GameHost.WebBrowser.Navigating += (_, e) =>
            {
                BrowserAddr.Text = e.Uri.AbsoluteUri;
                BrowserBack.IsEnabled = GameHost.WebBrowser.CanGoBack;
                BrowserForward.IsEnabled = GameHost.WebBrowser.CanGoForward;
            };
            BrowserBack.Click += (_, __) => GameHost.WebBrowser.GoBack();
            BrowserForward.Click += (_, __) => GameHost.WebBrowser.GoForward();
            BrowserGoto.Click += (_, __) =>
            {
                if (!BrowserAddr.Text.Contains(":"))
                    BrowserAddr.Text = "http://" + BrowserAddr.Text;
                try
                {
                    GameHost.WebBrowser.Navigate(BrowserAddr.Text);
                }
                catch { }
            };
            BrowserRefresh.Click += (_, __) => GameHost.WebBrowser.Refresh();
            BrowserBackToGame.Click += (_, __) => GameHost.WebBrowser.Navigate(Properties.Settings.Default.GameUrl);

            //Language handler
            LanguageBox.ItemsSource = ResourceService.SupportedCultures;
            LanguageBox.SelectionChanged += (s, _) => ResourceService.Current.ChangeCulture((s as ComboBox).SelectedValue.ToString());
            ResourceService.Current.ChangeCulture(LanguageBox.SelectedValue.ToString());

            //Theme button handler
            NoDWM.Click += (s, _) =>this.DontUseDwm = (s as CheckBox).IsChecked.Value;
            NoDWM.IsChecked = this.DontUseDwm = Config.Current.NoDWM;
            Themes.ItemsSource = ThemeService.SupportedThemes;
            Themes.SelectionChanged += (s, _) =>ThemeService.ChangeTheme((s as ComboBox).SelectedValue.ToString());
            ThemeService.ChangeTheme(Themes.SelectedValue.ToString());
            UseAeroControl.Click += (s, _) =>ThemeService.EnableAeroControls((s as CheckBox).IsChecked.Value);
            UseAeroControl.IsChecked = Config.Current.Aero;
            ThemeService.EnableAeroControls(Config.Current.Aero);

            //Proxy button handler
            UpdateProxySetting.Click += (_, __) =>
            {
                Config.Current.Proxy.Host = ProxyHost.Text;
                Config.Current.Proxy.Port = int.Parse(ProxyPort.Text);
                Config.Current.EnableProxy = EnableProxy.IsChecked.Value;
                Config.Current.HTTPSProxy.Host = ProxyHostHTTPS.Text;
                Config.Current.HTTPSProxy.Port = int.Parse(ProxyPortHTTPS.Text);
                Config.Current.EnableProxyHTTPS = EnableProxyHTTPS.IsChecked.Value;
            };
            CancelProxySetting.Click += (_, __) =>
            {
                ProxyHost.Text = Config.Current.Proxy.Host;
                ProxyPort.Text = Config.Current.Proxy.Port.ToString();
                EnableProxy.IsChecked = Config.Current.EnableProxy;
                ProxyHostHTTPS.Text = Config.Current.HTTPSProxy.Host;
                ProxyPortHTTPS.Text = Config.Current.HTTPSProxy.Port.ToString();
                EnableProxyHTTPS.IsChecked = Config.Current.EnableProxyHTTPS;
            };

            //Font handler
            var FontFamilies = (new System.Drawing.Text.InstalledFontCollection()).Families;
            List<string> FontNames = new List<string>();
            foreach (var font in FontFamilies)
            {
                FontNames.Add(font.Name);
            }
            SelectFontFamily.ItemsSource = FontNames;
            SelectFontFamily.SelectionChanged += (s, _) => { try {
                    this.FontFamily = new FontFamily((s as ComboBox).SelectedValue.ToString());
                } catch { } };
            SelectFontFamily.SelectedValue = "等线";
            TextFontSize.DataContext = this;
            TextFontSize.SetBinding(TextBox.TextProperty, new Binding { Source = this.ribbonWindow, Path = new PropertyPath("FontSize"), Mode = BindingMode.TwoWay });
            FontLarge.Click += (_, __) => this.FontSize += 1;
            FontSmall.Click += (_, __) => this.FontSize -= 1;

            this.Loaded += (_, __) => GameHost.Browser.Navigate(Properties.Settings.Default.GameUrl);
        }
        
        private void MakeViewList(ILayoutElement elem)
        {
            if(elem is LayoutAnchorable)
            {
                ViewList.Add((elem as LayoutAnchorable).ContentId, elem as LayoutAnchorable);
                return;
            }
            if (elem is ILayoutContainer)
            {
                foreach (var child in (elem as ILayoutContainer).Children)
                {
                    MakeViewList(child);
                }
            }
        }

        private void LoadLayout(object sender, RoutedEventArgs e)
        {
            var s = new XmlLayoutSerializer(DockMan);
            s.LayoutSerializationCallback += (_, args) => args.Content = args.Content;
            try
            {
                s.Deserialize("layout.xml");
            }
            catch { }
            foreach (var view in DockMan.Layout.Hidden.Where(x => x.PreviousContainerIndex == -1).ToArray())
            {
                DockMan.Layout.Hidden.Remove(view);
            }
            MakeViewList(DockMan.Layout);
        }
        private void SaveLayout(object sender, RoutedEventArgs e)
        {
            var s = new XmlLayoutSerializer(DockMan);
            s.Serialize("layout.xml");
        }

        private Dictionary<string, Type> ViewTypeList = new Dictionary<string, Type>()
        {
            //[nameof(APIView)] = typeof(APIView),
            [nameof(AdmiralView)] = typeof(AdmiralView),
            [nameof(FleetView)] = typeof(FleetView),
            [nameof(MissionView)] = typeof(MissionView),
            [nameof(RepairView)] = typeof(RepairView),
            [nameof(BuildingView)] = typeof(BuildingView)
        };
        private Dictionary<string, LayoutAnchorable> ViewList = new Dictionary<string, LayoutAnchorable>();
        private void SetToggleBinding(object sender, RoutedEventArgs e)
        {
            Binding ToggleBinding = new Binding();
            string ViewName = (sender as Control).Tag as string;
            //var TargetView = FindView(DockMan.Layout, ViewName);
            LayoutAnchorable TargetView;
            Type ViewType;
            if (!ViewTypeList.TryGetValue((sender as Control).Tag as string, out ViewType))
            {
                System.Diagnostics.Debug.WriteLine($"Invalid View name: {ViewName}");
                return;
            }
            if (!ViewType.IsSubclassOf(typeof(Control)))
            {
                System.Diagnostics.Debug.WriteLine($"Invalid View type: {ViewName}");
                return;
            }
            if (!ViewList.TryGetValue(ViewName, out TargetView))
            {
                TargetView = new LayoutAnchorable();
                ViewList.Add(ViewName, TargetView);
                TargetView.AddToLayout(DockMan, AnchorableShowStrategy.Most);
                TargetView.Float();
                TargetView.Hide();
            }
            if(TargetView.Content == null)
            {
                Control content = ViewType.GetConstructor(new Type[0]).Invoke(new object[0]) as Control;
                TargetView.Content = content;
                if (content.DataContext == null)
                    content.DataContext = Officer.Staff.Current;
                TargetView.ContentId = ViewName;
                TargetView.Title = ViewName;
                TargetView.FloatingHeight = content.Height;
                TargetView.FloatingWidth = content.Width;
                TargetView.FloatingTop = this.ActualHeight / 2;
                TargetView.FloatingWidth = this.ActualWidth / 2;
                Binding titlebinding = new Binding("Resources.ViewTitle_" + ViewName);
                titlebinding.Source = ResourceService.Current;
                BindingOperations.SetBinding(TargetView, LayoutAnchorable.TitleProperty, titlebinding);
                (sender as Fluent.ToggleButton).SetBinding(Fluent.ToggleButton.HeaderProperty, titlebinding);
            }
            ToggleBinding.Source = TargetView;
            ToggleBinding.Path = new PropertyPath("IsVisible");
            ToggleBinding.Mode = BindingMode.TwoWay;
            (sender as Fluent.ToggleButton).SetBinding(Fluent.ToggleButton.IsCheckedProperty, ToggleBinding);
        }
    }
}
