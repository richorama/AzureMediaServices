using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Configuration;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Converter;


class Program
{
    private static CloudMediaContext mediaContext;
    private static string accKey  = "qrtMwX0RJWZFzK0kjOzZUn7y5Cm/zcpM6lX5dnCmUu0=";
    private static string accName = "media101tut";
    private static string input = Path.GetFullPath(@"D:\input\dail.wmv");
    private static string input2 = Path.GetFullPath(@"D:\input\beta.txt");
    private static string output = Path.GetFullPath(@"D:\output");
    // Path to configuration file if required, i.e. for streamer conversion
    private static string configFilePath = Path.GetFullPath(@"D:\input\MP4 to Smooth Streams.xml");

    static void Main(string[] args)
    {
        // Upload and encode a file via media services
        mediaContext = new CloudMediaContext(accName, accKey);
        IAsset asset = mediaContext.Assets.Create(input);
        Console.WriteLine("Upload complete");
        Encode(asset, output);

        // View the current blob containers
        Converter.blob storAcc = new blob();
        storAcc.viewBlob();
        Console.ReadLine();
    }

    static void Encode(IAsset asset, string outputFolder)
    {
        IJob job = mediaContext.Jobs.Create("My Job");
        var theProcessor = from p in mediaContext.MediaProcessors
                           where p.Name == "Windows Azure Media Encoder"
                           select p;
        string configuration = File.ReadAllText(configFilePath);

        IMediaProcessor processor = theProcessor.First();
        ITask task = job.Tasks.AddNew("WMV task", processor, "H.264 HD 1080p VBR", TaskCreationOptions.ProtectedConfiguration);
        task.InputMediaAssets.Add(asset);
        task.OutputMediaAssets.AddNew("Output asset", true, AssetCreationOptions.None);
        job.Submit();
        CheckJobProgress(job.Id);
        job = GetJob(job.Id);
        IAsset outputAsset = job.OutputMediaAssets[0];
        // GetStreamingOriginLocator(outputAsset); Operation fails with null value
        string sasUrl = GetAssetSasUrl(outputAsset, TimeSpan.FromMinutes(30));
    }

    private static void CheckJobProgress(string jobId)
    {
        bool jobCompleted = false;
        // Expected polling interval in milliseconds
        const int JobProgressInterval = 10000;
        var i = 0;
        while (!jobCompleted)
        {
            // Update the current job status
            IJob theJob = GetJob(jobId);

            // Check job and report state. 
            switch (theJob.State)
            {
                case JobState.Finished:
                    jobCompleted = true;
                    Console.WriteLine("");
                    Console.WriteLine("#===============#");
                    Console.WriteLine(i+": Job state is: " + theJob.State + ".");
                    Console.WriteLine("Please wait while local tasks complete...");
                    DownloadAssetToLocal(theJob, output);
                    Console.WriteLine();
                    break;
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine(i+": Job state is: " + theJob.State + ".");
                    Console.WriteLine("Please wait...");
                    Console.WriteLine();
                    break;
                case JobState.Error:
                    break;
                default:
                    Console.WriteLine(theJob.State.ToString());
                    break;
            }
            // Wait for the specified job interval before checking state again.
            i++;
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

    static String GetAssetSasUrl(IAsset asset, TimeSpan accessPolicyTimeout)
    {

        // Create a policy for the asset. 
        IAccessPolicy readPolicy = mediaContext.AccessPolicies.Create("My Test Policy", accessPolicyTimeout, AccessPermissions.Read);
        ILocator locator = mediaContext.Locators.CreateSasLocator(asset,
            readPolicy,
            DateTime.UtcNow.AddMinutes(-5));

        // Print the path for the locator you created. 
        Console.WriteLine("Locator path: ");
        Console.WriteLine(locator.Path);
        Console.WriteLine();

        var theOutputFile =
                            from f in asset.Files
                            where f.Name.EndsWith(".mp4")
                            select f;
        // Cast the IQueryable variable back to an IFileInfo. 
        IFileInfo theFile = theOutputFile.FirstOrDefault();
        string fileName = theFile.Name;

        // Now take the locator path, add the file name, and build a complete SAS URL to browse to the asset. 
        var uriBuilder = new UriBuilder(locator.Path);
        uriBuilder.Path += "/" + fileName;

        // Print the full SAS URL 
        Console.WriteLine("Full URL to file: ");
        Console.WriteLine(uriBuilder.Uri.AbsoluteUri);
        Console.WriteLine();
        writeToFile(uriBuilder.Uri.AbsoluteUri, output);
        // Return the SAS URL.  
        return uriBuilder.Uri.AbsoluteUri;
    }

    static void writeToFile(string URL, string outputFolder)
    {
        // Write the string to a file.
        System.IO.StreamWriter file = new System.IO.StreamWriter(outputFolder+"URL.txt");
        file.WriteLine(URL);

        file.Close();
    }

    
}

