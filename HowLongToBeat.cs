﻿using HowLongToBeat.Models;
using HowLongToBeat.Services;
using HowLongToBeat.Views;
using HowLongToBeat.Views.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace HowLongToBeat
{
    public class HowLongToBeat : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private HowLongToBeatSettings settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        private readonly IntegrationUI ui = new IntegrationUI();


        public HowLongToBeat(IPlayniteAPI api) : base(api)
        {
            settings = new HowLongToBeatSettings(this);


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);


            if (settings.EnableIntegrationInCustomTheme)
            {
                EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));
            }
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "HowLongToBeat",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath());
                        if (data.GetData() != null)
                        {
                            new Views.HowLongToBeat(data, GameSelected, PlayniteApi).ShowDialog();
                        }
                    })
            };
        }


        #region Interface integration
        private Game GameSelected { get; set; }

        /// <summary>
        /// Button event for call plugin view in custom theme.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            string ButtonName = "";

            try
            {
                ButtonName = ((Button)sender).Name;

                if (ButtonName == "PART_HltbCustomButton")
                {
                    OnBtGameSelectedActionBarClick(sender, e);
                }
            }
            catch (Exception ex)

            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] - On {ButtonName} ");
            }
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];

                    Integration();
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] - OnGameSelected() ");
            }
        }

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath());
            if (data.GetData() != null)
            {
                new Views.HowLongToBeat(data, GameSelected, PlayniteApi).ShowDialog();
            }
        }

        /// <summary>
        /// Integration plugin interface in application.
        /// </summary>
        private void Integration()
        {
            try
            {
                // Delete
                logger.Info("HowLongToBeat - Delete");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_HltbButton");
                ui.RemoveElementInGameSelectedDescription("PART_HltbProgressBarIntegration");
                ui.ClearElementInCustomTheme("PART_hltbProgressBarWithTitle");
                ui.ClearElementInCustomTheme("PART_hltbProgressBar");


                HowLongToBeatData data = new HowLongToBeatData(GameSelected, this.GetPluginUserDataPath(), false);


                if (settings.EnableIntegrationButton)
                {
                    Button HltbButton = new Button();
                    HltbButton.Name = "PART_HltbButton";
                    HltbButton.FontFamily = new FontFamily(new Uri("pack://application:,,,/HowLongToBeat;component/Resources/"), "./#font");
                    HltbButton.Margin = new Thickness(10, 0, 0, 0);
                    HltbButton.Click += OnBtGameSelectedActionBarClick;
                    HltbButton.Content = TransformIcon.Get("HowLongToBeat");

                    ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(HltbButton);
                }

                // Auto integration
                if (settings.EnableIntegrationInDescription)
                {
                    if (data.GetData() != null)
                    {
                        StackPanel spHltb = CreateHltb(GameSelected.Playtime, data.GetData(), settings.IntegrationShowTitle);
                        spHltb.Name = "PART_HltbProgressBarIntegration";

                        ui.AddElementInGameSelectedDescription(spHltb, settings.IntegrationTopGameDetails);
                    }
                }

                // Custom theme
                if (settings.EnableIntegrationInCustomTheme)
                {
                    // Create 
                    StackPanel spHltb = CreateHltb(GameSelected.Playtime, data.GetData(), true);
                    UserControl hltbProgressBar = new HltbProgressBar(GameSelected.Playtime, data.GetData());

                    ui.AddElementInCustomTheme(spHltb, "PART_hltbProgressBarWithTitle");
                    ui.AddElementInCustomTheme(hltbProgressBar, "PART_hltbProgressBar");
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                string FileName = new StackTrace(ex, true).GetFrame(0).GetFileName();
                logger.Error(ex, $"HowLongToBeat [{FileName} {LineNumber}] - Impossible integration ");
            }
        }

        /// <summary>
        /// Create the StackPanel with the ProgressBar.
        /// </summary>
        /// <param name="Playtime"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private StackPanel CreateHltb(long Playtime, HltbDataUser data, bool IntegrationShowTitle)
        {
            StackPanel spHltb = new StackPanel();

            TextBlock tbHltb = new TextBlock();
            Separator hltbsep = new Separator();
            if (IntegrationShowTitle)
            {
                tbHltb.Name = "PART_tbHltb";
                tbHltb.Text = resources.GetString("LOCHowLongToBeatTitle");
                tbHltb.Style = (Style)resources.GetResource("BaseTextBlockStyle");
                tbHltb.Margin = new Thickness(0, 15, 0, 10);

                hltbsep.Name = "PART_hltbsep";
                hltbsep.Background = (Brush)resources.GetResource("PanelSeparatorBrush");
            }

            UserControl hltbProgressBar = new HltbProgressBar(Playtime, data);
            hltbProgressBar.Name = "PART_hltbProgressBar";
            hltbProgressBar.Margin = new Thickness(0, 5, 0, 5);

            if (IntegrationShowTitle)
            {
                spHltb.Children.Add(tbHltb);
                spHltb.Children.Add(hltbsep);
            }
            spHltb.Children.Add(hltbProgressBar);
            spHltb.UpdateLayout();

            return spHltb;
        }
        #endregion

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.

            Integration();
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HowLongToBeatSettingsView();
        }
    }
}
