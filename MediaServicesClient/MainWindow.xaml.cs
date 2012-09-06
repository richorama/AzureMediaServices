using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAzure.MediaServices;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;

namespace MediaServicesClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<String> encodingOptions = new ObservableCollection<String>();
        OpenFileDialog fileDialog;

        MediaServicesConnector connector;

        public MainWindow()
        {
            InitializeComponent();
            connector = new MediaServicesConnector(); 

            fileDialog = new OpenFileDialog();
            fileDialog.FileOk += fileUpload;
            connector.HandleContextAcquired += new ContextAcquired(connector_HandleContextAcquired);
            connector.HandleAssetUploaded += new AssetUploaded(connector_HandleAssetUploaded);
            connector.HandleAssetDeleted += new AssetDeleted(connector_HandleAssetDeleted);
            connector.HandleJobsReceived += new JobsReceived(connector_HandleJobsReceived);
            connector.OnUploadReceived += new UploadReceived(connector_OnUploadReceived);

            AssetsListBox.ItemsSource = connector.assetsList;
            foreach (String option in connector.encodingOptions)
            {
                encodingOptions.Add(option);
            }
            EncodingOptions.ItemsSource = encodingOptions;
        }

        void connector_OnUploadReceived(UploadProgressEventArgs e)
        {
            //Console.WriteLine("Upload Progress Received");
            //Console.WriteLine(e.Progress);
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UploadProgressBar.Value = e.Progress;
                }
            ));
        }

        void connector_HandleJobsReceived(ObservableCollection<IJob> jobs)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    JobsBox.ItemsSource = jobs;
                    Console.WriteLine("Jobs Length: " + jobs.Count);
                    JobsBox.Visibility = System.Windows.Visibility.Visible;
                }
            ));            
        }

        void connector_HandleAssetDeleted()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    connector.UpdateAssetList();
                }
            ));
        }

        void connector_HandleAssetUploaded()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UploadProgressPanel.Visibility = System.Windows.Visibility.Collapsed;
                    EncodeButton.Visibility = System.Windows.Visibility.Visible;

                    EncodingPanel.Visibility = System.Windows.Visibility.Visible;
                    MediaServicesPanel.Visibility = System.Windows.Visibility.Collapsed;
                    UploadPanel.Visibility = System.Windows.Visibility.Visible;
                    connector.UpdateAssetList();
                }
            ));
        }

        void connector_HandleContextAcquired()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoggingInPanel.Visibility = System.Windows.Visibility.Collapsed;

                    AccountLabel.Content = AccountNameBox.Text;

                    AssetsPanel.Visibility = System.Windows.Visibility.Visible;
                    AccountPanel.Visibility = System.Windows.Visibility.Visible;
                    MediaServicesPanel.Visibility = System.Windows.Visibility.Visible;

                    connector.UpdateAssetList();
                    connector.GetJobs();
                }
            ));
        }

        private void LoginButton_Clicked(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
            LoggingInPanel.Visibility = System.Windows.Visibility.Visible;
            connector.AcquireContext(AccountNameBox.Text, AccountKeyBox.Text);          
        }

        private void ShowDialogButton_Clicked(object sender, RoutedEventArgs e)
        {
            fileDialog.ShowDialog();
        }

        private void UploadAsset_Clicked(object sender, RoutedEventArgs e)
        {
            List<String> options = new List<String>();
            foreach (var selection in EncodingOptions.SelectedItems)
            {
                options.Add(selection as String);
            }
            connector.EncodeAsset(options);
        }

        private void DeleteAsset_Clicked(object sender, RoutedEventArgs e)
        {
            List<IAsset> assets = new List<IAsset>();
            foreach (IAsset asset in AssetsListBox.SelectedItems)
            {
                assets.Add(asset);
            }
            connector.DeleteAssets(assets);
        }

        private void fileUpload(System.Object sender, System.EventArgs e)
        {
            String fileNames = fileDialog.FileName;
            connector.UploadAsset(fileNames, AssetCreationOptions.StorageEncrypted);
            UploadProgressPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            connector.UpdateAssetList();
        }

        private void DownloadAsset_Clicked(object sender, RoutedEventArgs e)
        {
            if (AssetsListBox.SelectedItems.Count > 0)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                
                dialog.FileOk += new System.ComponentModel.CancelEventHandler(dialog_FileOk);
                dialog.ShowDialog();                
            }
        }

        void dialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveFileDialog dialog = sender as SaveFileDialog;

            IAsset asset = AssetsListBox.SelectedItems[0] as IAsset;
            if (asset.Files.Count > 0)
            {
                asset.Files[0].DownloadToFile(dialog.FileName);
            }
            else
            {
                Console.WriteLine("Nothing to download");
            }
        }
    }
}