﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Collections.ObjectModel;
using System.Data.Services.Client;


/**
 * Large quantities of this code is based on the Microsoft Sample code
 * */
namespace MediaServicesClient
{
    public delegate void ContextAcquired();
    public delegate void AssetUploaded();
    public delegate void AssetDeleted();
    public delegate void JobsReceived(ObservableCollection<IJob> jobs);
    public delegate void UploadReceived(UploadProgressEventArgs e);

    public class MediaServicesConnector
    {
        private CloudMediaContext context;

        public ObservableCollection<IAsset> assetsList = new ObservableCollection<IAsset>();
        public List<String> encodingOptions; // Observable or not?

        public ObservableCollection<MediaAsset> MediaAssets = new ObservableCollection<MediaAsset>();

        public IAsset currentAsset { get; private set; }
        private IMediaProcessor mediaProcessor;

        /**
         * Events for clients
         * */
        public event ContextAcquired HandleContextAcquired;
        public event AssetUploaded HandleAssetUploaded;
        public event AssetDeleted HandleAssetDeleted;
        public event JobsReceived HandleJobsReceived;
        public event UploadReceived OnUploadReceived;

        public MediaServicesConnector()
        {
            encodingOptions = getEncodingOptionsAudio();
        }

        public void AcquireContext(String accountName, String accountKey)
        {
            Thread thread = new Thread(() =>
                {
                    context = new CloudMediaContext(accountName, accountKey);

                    context.Assets.OnUploadProgress += new EventHandler<UploadProgressEventArgs>(Assets_OnUploadProgress);

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

        public void DeleteAsset(string assetID)
        {
            var asset = GetAssetFromID(assetID);
            if (asset != null)
            {
                DeleteAsset(asset);
            }
        }

        public void DeleteAssets(List<IAsset> assets)
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
            if (HandleAssetDeleted == null) throw new Exception("HandleAssetDeleted not subscribed");
            Thread thread = new Thread(() =>
                {
                    foreach (IAsset asset in assets)
                    {
                        try
                        {
                            context.Assets.Delete(asset);
                        }
                        catch (DataServiceRequestException e)
                        {
                            Console.WriteLine("Exception Caught: " + e.Message);
                        }
                    }
                    if (HandleAssetDeleted != null)
                    {
                        HandleAssetDeleted();
                    }
                }
            );

            thread.Start();
        }

        public void EncodeAsset(IAsset asset, List<String> options)
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
            if (asset == null) throw new ArgumentNullException("asset null - can't upload");

            IJob job = context.Jobs.Create("");
            mediaProcessor = GetMediaProcessor("Windows Azure Media Encoder");

            foreach (String configOption in options)
            {
                ITask task = job.Tasks.AddNew("task " + configOption,
                mediaProcessor,
                configOption,
                TaskCreationOptions.None);
                task.InputMediaAssets.Add(asset);
                task.OutputMediaAssets.AddNew(asset.Name + " " + configOption, true, AssetCreationOptions.None);
            }
            job.Submit();
        }

        public void EncodeAsset(List<String> options)
        {
            EncodeAsset(currentAsset, options);
        }

        public void DownloadAsset(IAsset asset, string filePath)
        {
            Thread thread = new Thread(() =>
                {
                    if (asset.Files.Count > 0)
                    {
                        asset.Files[0].DownloadToFile(filePath);
                    }
                }
            );
            thread.Start();
        }

        public void DownloadAsset(string assetID, string filePath)
        {            
            DownloadAsset(GetAssetFromID(assetID), filePath);             
        }

        public void UpdateAssetList()
        {
            if (context == null) throw new ArgumentNullException("context null - can't communicate");
            MediaAssets.Clear();
            assetsList.Clear();
            // Has to be looped through twice unfortunatly
            // Could be replaced by a map based algorithm
            foreach (IAsset item in context.Assets.ToList())
            {
                assetsList.Add(item);
                if (item.ParentAssets.Count == 0)
                {
                    MediaAssets.Add(new MediaAsset(item));
                }              
            }
            foreach (IAsset item in context.Assets.ToList())
            {
                foreach (MediaAsset asset in MediaAssets)
                {
                    asset.AddChildIfChild(item);
                }
            }            
        }

        public void GetJobs()
        {
            ObservableCollection<IJob> jobs = new ObservableCollection<IJob>();

            Thread thread = new Thread(() =>
                {
                    foreach (IJob job in context.Jobs)
                    {
                        jobs.Add(job);
                    }
                    if (HandleJobsReceived != null)
                    {
                        HandleJobsReceived(jobs);
                    }
                }
            );
            thread.Start();
        }

        public IJob GetJob(string jobId)
        {
            var job =
                from j in context.Jobs
                where j.Id == jobId
                select j;

            IJob theJob = job.First();

            if (theJob != null)
            {
                return theJob;
            }
            else
                Console.WriteLine("Job does not exist.");
            return null;
        }

        public void DeleteJob(string jobId)
        {
            bool jobDeleted = false;

            while (!jobDeleted)
            {
                IJob theJob = GetJob(jobId);

                // Check and handle various possible job states. You can 
                // only delete a job whose state is Finished, Error, or Canceled.   
                // You can cancel jobs that are Queued, Scheduled, or Processing,  
                // and then delete after they are canceled.
                switch (theJob.State)
                {
                    case JobState.Finished:
                    case JobState.Canceled:
                        theJob.Delete();
                        jobDeleted = true;
                        Console.WriteLine("Job has been deleted.");
                        break;
                    case JobState.Canceling:
                        Console.WriteLine("Job is cancelling and will be deleted "
                            + "when finished.");
                        Console.WriteLine("Wait while job finishes canceling...");
                        Thread.Sleep(5000);
                        break;
                    case JobState.Queued:
                    case JobState.Scheduled:
                    case JobState.Processing:
                        theJob.Cancel();
                        Console.WriteLine("Job is pending or processing and will "
                            + "be canceled, then deleted.");
                        break;
                    case JobState.Error:
                        // Log error as needed.
                        break;
                    default:
                        break;
                }
            }
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

        private void Assets_OnUploadProgress(object sender, UploadProgressEventArgs e)
        {
            if (OnUploadReceived != null)
            {
                OnUploadReceived(e);
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
    }
}
