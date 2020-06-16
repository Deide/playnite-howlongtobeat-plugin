﻿using HowLongToBeat.Models;
using Playnite.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Windows.Media.Imaging;

namespace HowLongToBeat.Views
{
    /// <summary>
    /// Logique d'interaction pour HowLongToBeat.xaml
    /// </summary>
    public partial class HowLongToBeat : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public string CoverImage { get; set; }
        public string GameName { get; set; }
        public string MainStoryFormat { get; set; }
        public string MaintExtraFormat { get; set; }
        public string CompletionistFormat { get; set; }

        public HowLongToBeat(HltbDataUser data, Game game, IPlayniteAPI PlayniteApi)
        {
            InitializeComponent();

            CoverImage = PlayniteApi.Database.GetFullFilePath(game.CoverImage);
            GameName = game.Name;
            MainStoryFormat = data.GameHltbData.MainStoryFormat;
            MaintExtraFormat = data.GameHltbData.MaintExtraFormat;
            CompletionistFormat = data.GameHltbData.CompletionistFormat;



            long MaxValue = data.GameHltbData.Completionist;
            if (game.Playtime > data.GameHltbData.Completionist)
            {
                MaxValue = game.Playtime;
            }

            ProgressMainStory.Value = data.GameHltbData.MainStory;
            ProgressMainStory.Maximum = MaxValue;

            ProgressMainExtra.Value = data.GameHltbData.MaintExtra;
            ProgressMainExtra.Maximum = MaxValue;

            ProgressCompletionist.Value = data.GameHltbData.Completionist;
            ProgressCompletionist.Maximum = MaxValue;

            SliderPlaytime.Value = game.Playtime;
            SliderPlaytime.Maximum = MaxValue;



            // Set Binding data
            DataContext = this;
        }

        private void DockPanel_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }
    }
}