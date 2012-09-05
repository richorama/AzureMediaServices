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

        public MediaServicesConnector()
        {
            encodingOptions = getEncodingOptionsAudio();
        }

        public void AcquireContext(String accountName, String accountKey)
        {
            Thread thread = new Thread(() =>
                {
                    context = new CloudMediaContext(accountName, accountKey);
                    HandleContextAcquired();
                }
            );
            thread.Start();
        }

        public void UploadAsset(String filePath, AssetCreationOptions assetOption)
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
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
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
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
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
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
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
            var asset =
            from a in context.Assets
            where a.Id == assetID
            select a;

            return (asset.First());
        }

        public void EncodeAsset(IAsset asset, List<String> options)
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");

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

        private List<String> getEncodingOptionsVideo()
        {
            List<String> options = new List<String>();
            options.Add("H.264 256k DSL CBR");
            options.Add("H.264 256k DSL VBR");
            options.Add("H.264 512k DSL CBR");
            options.Add("H.264 512k DSL VBR");
            options.Add("H.264 AppleTV");
            options.Add("H.264 Broadband CBR");
            options.Add("H.264 Broadband VBR");
            options.Add("H.264 Facebook HD");
            options.Add("H.264 Facebook SD");
            options.Add("H.264 HD 1080p VBR");
            options.Add("H.264 HD 720p VBR");
            options.Add("H.264 High Speed Broadband CBR");
            options.Add("H.264 High Speed Broadband VBR");
            options.Add("H.264 IIS Smooth Streaming - HD 1080p CBR");
            options.Add("H.264 IIS Smooth Streaming - HD 720p CBR");
            options.Add("H.264 IIS Smooth Streaming - Screen Encoding CBR");
            options.Add("H.264 IIS Smooth Streaming - SD 480p CBR");
            options.Add("H.264 IIS Smooth Streaming iPhone Cell");
            options.Add("H.264 IIS Smooth Streaming iPhone WiFi");
            options.Add("H.264 IIS Smooth Streaming Symbian");
            options.Add("H.264 IIS Smooth Streaming Windows Phone 7");
            options.Add("H.264 iPad");
            options.Add("H.264 iPhone / iPod Touch");
            options.Add("H.264 iPod Classic / Nano");
            options.Add("H.264 Motion Thumbnail VBR");
            options.Add("H.264 Screen Encoding VBR");
            options.Add("H.264 Silverlight for Symbian");
            options.Add("H.264 Sony PSP");
            options.Add("H.264 Vimeo HD");
            options.Add("H.264 Vimeo SD");
            options.Add("H.264 Windows Phone 7");
            options.Add("H.264 YouTube HD");
            options.Add("H.264 YouTube SD");
            options.Add("H.264 Zune 2");
            options.Add("H.264 Zune 2 (AV Dock Playback)");
            options.Add("H.264 Zune HD");
            options.Add("H.264 Zune HD (AV Dock Playback)");
            options.Add("VC-1 256k DSL CBR");
            options.Add("VC-1 256k DSL VBR");
            options.Add("VC-1 512k DSL CBR");
            options.Add("VC-1 512k DSL VBR");
            options.Add("VC-1 HD 1080p VBR");
            options.Add("VC-1 HD 720p VBR");
            options.Add("VC-1 High Speed Broadband CBR");
            options.Add("VC-1 High Speed Broadband VBR");
            options.Add("VC-1 IIS Smooth Streaming - HD 1080p VBR");
            options.Add("VC-1 IIS Smooth Streaming - HD 720p CBR");
            options.Add("VC-1 IIS Smooth Streaming - HD 720p VBR");
            options.Add("VC-1 IIS Smooth Streaming - Screen Encoding VBR");
            options.Add("VC-1 IIS Smooth Streaming - SD 480p VBR");
            options.Add("VC-1 IIS Smooth Streaming - Windows Phone 7");
            options.Add("VC-1 Motion Thumbnail VBR");
            options.Add("VC-1 Screen Encoding VBR");
            options.Add("VC-1 Windows Mobile");
            options.Add("VC-1 Windows Phone 7");
            options.Add("VC-1 Xbox 360 HD 1080p");
            options.Add("VC-1 Xbox 360 HD 720p");
            options.Add("VC-1 Zune 1");
            options.Add("VC-1 Zune 2");
            options.Add("VC-1 Zune 2 (AV Dock Playback)");
            options.Add("VC-1 Zune HD");
            options.Add("VC-1 Zune HD (AV Dock Playback)");

            return options;
        }

        public void UpdateAssetList()
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");

            assetsList.Clear();
            foreach (IAsset item in context.Assets.ToList())
            {
                assetsList.Add(item);
            }
        }

        private IMediaProcessor GetMediaProcessor(string mediaProcessor)
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");

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
    }
}
