using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Threading;

namespace MediaServicesClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<String> encodingOptions = new ObservableCollection<String>();

        MediaServicesConnector connector;

        public MainWindow()
        {
            InitializeComponent();
            connector = new MediaServicesConnector(); 
            
            // Subscribing to the events
            connector.HandleContextAcquired += new ContextAcquired(connector_HandleContextAcquired);
            connector.HandleAssetUploaded += new AssetUploaded(connector_HandleAssetUploaded);
            connector.HandleAssetDeleted += new AssetDeleted(connector_HandleAssetDeleted);
            connector.HandleJobsReceived += new JobsReceived(connector_HandleJobsReceived);
            connector.OnUploadReceived += new UploadReceived(connector_OnUploadReceived);

            // Set the item source to display onscreen
            AssetsListBox.ItemsSource = connector.MediaAssets;
            foreach (String option in connector.encodingOptions)
            {
                encodingOptions.Add(option);
            }
            EncodingOptions.ItemsSource = encodingOptions;

            // Subscribe to event
            AssetsListBox.SelectionChanged += new SelectionChangedEventHandler(AssetsListBox_SelectionChanged);
        }

        void AssetsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If something was added
            if (e.AddedItems.Count > 0)
            {
                var itemSource = (e.AddedItems[0] as MediaAsset).Children;
                Console.WriteLine("Number of children: " + itemSource.Count);
                // Show its children on the screen
                ChildBox.ItemsSource = itemSource;
            }
        }

        void connector_OnUploadReceived(UploadProgressEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Update the progress bar to reflect the download progress
                    UploadProgressBar.Value = e.Progress;
                }
            ));
        }

        void connector_HandleJobsReceived(ObservableCollection<IJob> jobs)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Display jobs, mostly irrelevant
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
                    // Update the asset list
                    connector.UpdateAssetList();
                }
            ));
        }

        void connector_HandleAssetUploaded()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Change the state of the UI
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
                    ChildPanel.Visibility = System.Windows.Visibility.Visible;
                    AccountPanel.Visibility = System.Windows.Visibility.Visible;
                    MediaServicesPanel.Visibility = System.Windows.Visibility.Visible;

                    connector.UpdateAssetList();
                    connector.GetJobs();
                }
            ));
        }

        void dialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Make a dialog for saving the file
            SaveFileDialog dialog = sender as SaveFileDialog;
            // Extract the asset
            IAsset asset = (ChildBox.SelectedItems[0] as MediaAsset).GetIAsset();
            // Download it
            connector.DownloadAsset(asset, dialog.FileName);
        }

        private void LoginButton_Clicked(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
            LoggingInPanel.Visibility = System.Windows.Visibility.Visible;
            // Equivalent of logging in
            connector.AcquireContext(AccountNameBox.Text, AccountKeyBox.Text);          
        }

        private void ShowDialogButton_Clicked(object sender, RoutedEventArgs e)
        {
            // Make a dialog for opening the file
            OpenFileDialog fileDialog = new OpenFileDialog();
            // Register for event
            fileDialog.FileOk += fileUpload;
            // Show the dialog
            fileDialog.ShowDialog();            
        }

        private void UploadAsset_Clicked(object sender, RoutedEventArgs e)
        {
            // Extract the encoding options
            List<String> options = new List<String>();
            foreach (var selection in EncodingOptions.SelectedItems)
            {
                options.Add(selection as String);
            }
            // Encode the assets
            connector.EncodeAsset(options);
        }

        private void DeleteAsset_Clicked(object sender, RoutedEventArgs e)
        {
            List<IAsset> assets = new List<IAsset>();
            // Assign the source based on what has something selected
            var selectedItems = (AssetsListBox.SelectedItems.Count == 0) ? ChildBox.SelectedItems : AssetsListBox.SelectedItems;
            // Collect the assets into the list
            foreach (MediaAsset asset in selectedItems)
            {
                assets.Add(asset.GetIAsset());
            }
            // Delete the assets in the list
            connector.DeleteAssets(assets);
        }

        private void fileUpload(System.Object sender, System.EventArgs e)
        {
            // Extract the filename
            String fileName = (sender as OpenFileDialog).FileName;
            // Upload the asset to the cloud
            connector.UploadAsset(fileName, AssetCreationOptions.StorageEncrypted);
            // Slightly change the UI
            UploadProgressPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Refresh the update asset list
            connector.UpdateAssetList();
        }

        private void DownloadAsset_Clicked(object sender, RoutedEventArgs e)
        {
            if (AssetsListBox.SelectedItems.Count > 0)
            {
                // Form a dialog
                SaveFileDialog dialog = new SaveFileDialog();
                // Register for the event
                dialog.FileOk += new System.ComponentModel.CancelEventHandler(dialog_FileOk);
                // Show the dialog
                dialog.ShowDialog();                
            }
        }       
    }
}