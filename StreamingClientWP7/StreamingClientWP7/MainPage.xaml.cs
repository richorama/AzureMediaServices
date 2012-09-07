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
using Microsoft.SilverlightMediaFramework.Core.Media;

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

            PlaylistItem item = new PlaylistItem();
            item.MediaSource = new Uri("http://ecn.channel9.msdn.com/o9/content/smf/smoothcontent/bbbwp7/big buck bunny.ism/manifest");
            //item.MediaSource = new Uri("http://media10store.blob.core.windows.net/asset-dbe37166-33f9-4c6a-829b-67a329410c0f/dail.ism/manifest");
            item.DeliveryMethod = Microsoft.SilverlightMediaFramework.Plugins.Primitives.DeliveryMethods.AdaptiveStreaming;
            strmPlayer.Playlist.Add(item);
            strmPlayer.Play(); 
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