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

namespace Converter
{
    class media
    {
        public CloudMediaContext mediaContext;

        public media(string accName, string accKey)
        {
            mediaContext = new CloudMediaContext(accName, accKey);
        }

        public void Encode(IAsset asset, string encode)
        {
            IJob job = mediaContext.Jobs.Create("Encoding task");
            var theProcessor = from p in mediaContext.MediaProcessors
                               where p.Name == "Windows Azure Media Encoder"
                               select p;

            //string configuration = File.ReadAllText(configFilePath);

            IMediaProcessor processor = theProcessor.First();
            ITask task = job.Tasks.AddNew("WMV task", processor, encode, TaskCreationOptions.ProtectedConfiguration);
            task.InputMediaAssets.Add(asset);
            task.OutputMediaAssets.AddNew("Output asset", true, AssetCreationOptions.CommonEncryptionProtected);
            job.Submit();
            
            CheckJobProgress(job.Id);
            job = GetJob(job.Id);
            IAsset outputAsset = job.OutputMediaAssets[0];
            GetSasUrl(outputAsset, TimeSpan.FromMinutes(5));
        }

        private void CheckJobProgress(string jobId)
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
                        Console.WriteLine(i + ": Job state is: " + theJob.State + ".");
                        Console.WriteLine("Please wait while local tasks complete...");
                        //DownloadAssetToLocal(theJob, output);
                        Console.WriteLine();
                        break;
                    case JobState.Queued:
                    case JobState.Scheduled:
                    case JobState.Processing:
                        Console.WriteLine(i + ": Job state is: " + theJob.State + ".");
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

        private IJob GetJob(string jobId)
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

        public String GetSasUrl(IAsset asset, TimeSpan accessPolicyTimeout)
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
            //writeToFile(uriBuilder.Uri.AbsoluteUri, output);
            // Return the SAS URL.  
            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}
