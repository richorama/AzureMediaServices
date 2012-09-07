using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace StreamingClientWP7
{
    public partial class MainPage : PhoneApplicationPage
    {

        private Boolean playPause = false;
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            playPause = !playPause;
            if (playPause)
            {
                player.Source = new Uri(urlInputBox.Text, UriKind.Absolute);
                player.Play();
                PlayPauseButton.Content = "Pause";
            }
            else
            {
                player.Pause();
                PlayPauseButton.Content = "Play";
            }
        }
    }
}