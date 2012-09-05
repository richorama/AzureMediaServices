using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Configuration;
using Microsoft.WindowsAzure.MediaServices.Client;


class Program
{
    private static CloudMediaContext mediaContext;
    private static string accKey = "qrtMwX0RJWZFzK0kjOzZUn7y5Cm/zcpM6lX5dnCmUu0=";
    private static string accName = "media101tut";
    private static string input = Path.GetFullPath(@"D:\input\fail.wmv");
    private static string output = Path.GetFullPath(@"D:\output");
    private static string configFilePath = Path.GetFullPath(@"D:\input\MP4 to Smooth Streams.xml");

    static void Main(string[] args)
    {
        mediaContext = new CloudMediaContext(accName, accKey);
        IAsset asset = mediaContext.Assets.Create(input);
        Console.WriteLine("Upload complete");
        Encode(asset, output);
    }

    static void Encode(IAsset asset, string outputFolder)
    {
        IJob job = mediaContext.Jobs.Create("My Job");
        var theProcessor = from p in mediaContext.MediaProcessors
                           where p.Name == "Windows Azure Media Encoder"
                           select p;
        string configuration = File.ReadAllText(configFilePath);

        IMediaProcessor processor = theProcessor.First();
        ITask task = job.Tasks.AddNew("My WMV task", processor, "H.264 256k DSL CBR", TaskCreationOptions.ProtectedConfiguration);
        task.InputMediaAssets.Add(asset);
        task.OutputMediaAssets.AddNew("Output asset", true, AssetCreationOptions.None);
        job.Submit();
        CheckJobProgress(job.Id);
        job = GetJob(job.Id);
        IAsset outputAsset = job.OutputMediaAssets[0];
        GetStreamingOriginLocator(outputAsset);
    }

    private static void CheckJobProgress(string jobId)
    {
        // Flag to indicate when job state is finished. 
        bool jobCompleted = false;
        // Expected polling interval in milliseconds.  Adjust this 
        // interval as needed based on estimated job completion times.
        const int JobProgressInterval = 10000;

        while (!jobCompleted)
        {
            // Get an updated reference to the job in case 
            // reference gets 'stale' while thread waits.
            IJob theJob = GetJob(jobId);

            // Check job and report state. 
            switch (theJob.State)
            {
                case JobState.Finished:
                    jobCompleted = true;
                    Console.WriteLine("");
                    Console.WriteLine("********************");
                    Console.WriteLine("Job state is: " + theJob.State + ".");
                    Console.WriteLine("Please wait while local tasks complete...");
                    DownloadAssetToLocal(theJob, output);
                    Console.WriteLine();
                    break;
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Job state is: " + theJob.State + ".");
                    Console.WriteLine("Please wait...");
                    Console.WriteLine();
                    break;
                case JobState.Error:
                    // Log error as needed.
                    break;
                default:
                    Console.WriteLine(theJob.State.ToString());
                    break;
            }

            // Wait for the specified job interval before checking state again.
            Thread.Sleep(JobProgressInterval);
        }

    }

    static void WriteToFile(string outFilePath, string fileContent)
    {
        StreamWriter sr = File.CreateText(outFilePath);
        sr.Write(fileContent);
        sr.Close();
    }

    static IJob GetJob(string jobId)
    {
        // Use a Linq select query to get an updated 
        // reference by Id. 
        var job =
            from j in mediaContext.Jobs
            where j.Id == jobId
            select j;
        // Return the job reference as an Ijob. 
        IJob theJob = job.FirstOrDefault();

        // Confirm whether job exists, and return. 
        if (theJob != null)
        {
            return theJob;
        }
        else
            Console.WriteLine("Job does not exist.");
        return null;
    }

    static void DownloadAssetToLocal(IJob job, string outputFolder)
    {
        IAsset outputAsset = job.OutputMediaAssets[0];

        Console.WriteLine();
        Console.WriteLine("Files are downloading... please wait.");
        Console.WriteLine();

        foreach (IFileInfo outputFile in outputAsset.Files)
        {
            string localDownloadPath = Path.GetFullPath(outputFolder + @"\" + outputFile.Name);
            Console.WriteLine("File is downloading to:  " + localDownloadPath);
            outputFile.DownloadToFile(Path.GetFullPath(outputFolder + @"\" + outputFile.Name));
            Console.WriteLine();
        }
    }

    static void GetStreamingOriginLocator(IAsset assetToStream)
    {
        var theManifest =
            from f in assetToStream.Files
            where f.Name.EndsWith(".ism")
            select f;


        Console.WriteLine("--> "+theManifest.ToString());


        IFileInfo manifestFile = theManifest.FirstOrDefault();

        IAccessPolicy streamingPolicy = mediaContext.AccessPolicies.Create("Streaming policy",
            TimeSpan.FromDays(1),
            AccessPermissions.Read);

        ILocator originLocator = mediaContext.Locators.CreateOriginLocator(assetToStream,
            streamingPolicy,
            DateTime.UtcNow.AddMinutes(-5));

        string urlForClientStreaming = "/manifest";

        if (manifestFile != null)
        {
            urlForClientStreaming = originLocator.Path + manifestFile.Name + "/manifest";
        }

        // Display the full URL to the streaming manifest file.
        Console.WriteLine("URL to manifest for client streaming: ");
        Console.WriteLine(urlForClientStreaming);
        Console.ReadLine();
    }
}

