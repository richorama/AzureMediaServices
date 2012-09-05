using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Collections.ObjectModel;
using System.Data.Services.Client;

namespace MediaServicesClient
{
    public delegate void ContextAcquired();
    public delegate void AssetUploaded();
    public delegate void AssetDeleted();
    public delegate void RegularCheck();


    public class MediaServicesConnector
    {
        private CloudMediaContext context;

        public ObservableCollection<IAsset> assetsList = new ObservableCollection<IAsset>();
        public List<String> encodingOptions;

        private IAsset currentAsset;
        private IMediaProcessor mediaProcessor;

        public event ContextAcquired HandleContextAcquired;
        public event AssetUploaded HandleAssetUploaded;
        public event AssetDeleted HandleAssetDeleted;
        public event RegularCheck HandleRegularCheck;

        Boolean checking = true;
        public MediaServicesConnector()
        {
            encodingOptions = getEncodingOptionsAudio();
        }

        public void AcquireContext(String accountName, String accountKey)
        {
            Thread thread = new Thread(() =>
                {
                    context = new CloudMediaContext(accountName, accountKey);
                    //startRegularChecks();
                    HandleContextAcquired();
                }
            );
            thread.Start();
        }

        public void UploadAsset(String filePath, AssetCreationOptions assetOption)
        {
            if (context == null) throw new ArgumentNullException("context null");
            if (HandleAssetUploaded == null) throw new Exception("HandleAssetUploaded not subscribed");
            Thread thread = new Thread(() =>
                {
                    currentAsset = context.Assets.Create(filePath, assetOption);
                    HandleAssetUploaded();
                }
            );
            thread.Start();
        }

        public void DeleteAsset(IAsset asset)
        {
            if (HandleAssetDeleted == null) throw new Exception("HandleAssetDeleted not subscribed");
            Thread thread = new Thread(() =>
                {
                    context.Assets.Delete(asset);
                    if (HandleAssetDeleted != null)
                    {
                        HandleAssetDeleted();
                    }
                }
            );
            thread.Start();
        }
        public void DeleteAssets(List<IAsset> assets)
        {

            if (HandleAssetDeleted == null) throw new Exception("HandleAssetDeleted not subscribed");
            Thread thread = new Thread(() =>
                {
                    foreach (IAsset asset in assets)
                    {
                        context.Assets.Delete(asset);
                    }
                    if (HandleAssetDeleted != null)
                    {
                        HandleAssetDeleted();
                    }
                }
            );

            thread.Start();
        }
        public void DeleteAsset(string assetID)
        {
            var asset = GetAssetFromID(assetID);
            if (asset != null)
            {
                DeleteAsset(asset);
            }
        }

        private IAsset GetAssetFromID(string assetID)
        {
            var asset =
            from a in context.Assets
            where a.Id == assetID
            select a;

            return (asset.First());
        }

        public void EncodeAsset(IAsset asset, List<String> options)
        {
            IJob job = context.Jobs.Create("New Job");
            mediaProcessor = GetMediaProcessor("Windows Azure Media Encoder");

            foreach (String configOption in options)
            {
                ITask task = job.Tasks.AddNew("My encoding task",
                mediaProcessor,
                configOption,
                TaskCreationOptions.None);
                task.InputMediaAssets.Add(asset);
                task.OutputMediaAssets.AddNew("Output asset", true, AssetCreationOptions.None);
            }

            job.Submit();
        }

        public void EncodeAsset(List<String> options)
        {
            EncodeAsset(currentAsset, options);
        }

        private List<String> getEncodingOptionsAudio()
        {
            List<String> options = new List<String>();
            options.Add("AAC Good Quality Audio");
            options.Add("AAC High Quality Audio");
            options.Add("AAC Low Quality Audio");
            options.Add("WMA Best Quality VBR");
            options.Add("WMA Good Quality Audio");
            options.Add("WMA High Quality Audio");
            options.Add("WMA Lossless 5.1 Audio");
            options.Add("WMA Low Quality Audio");
            options.Add("WMA Voice Audio");
            return options;
        }

        public void UpdateAssetList()
        {
            assetsList.Clear();
            foreach (IAsset item in context.Assets.ToList())
            {
                assetsList.Add(item);
            }
        }

        private IMediaProcessor GetMediaProcessor(string mediaProcessor)
        {
            var theProcessor =
                                from p in context.MediaProcessors
                                where p.Name == mediaProcessor
                                select p;
            IMediaProcessor processor = theProcessor.First();

            if (processor == null)
            {
                throw new ArgumentException("Unknown Processor");
            }
            return processor;
        }

        private void startRegularChecks()
        {
            Thread thread = new Thread(() =>
                {
                    while (false)
                    {

                        Thread.Sleep(1000);
                        if (context.Assets.ToList().Count() != assetsList.Count)
                        {
                            if (HandleRegularCheck != null)
                            {
                                HandleRegularCheck();
                            }
                        }
                    }
                }
            );

            thread.Start();
        }
    }
}
