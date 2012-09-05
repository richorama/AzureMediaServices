using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace MediaServicesTest
{
    class Program
    {
        static String _accountName = "pierstest";
        static String _accountKey = "pz6FkbQ50lEgPPClMKuz7isNlzU1yj7eYVGQTzvQzLQ=";
        static CloudMediaContext _context = GetContext();

        static void Main(string[] args)
        {

            ListAssets();
            Console.WriteLine("Enter file path");
            string filePath = Console.ReadLine();

            IAsset asset = _context.Assets.Create(filePath, AssetCreationOptions.StorageEncrypted);
            Console.WriteLine("Asset Created");

            IJob job = _context.Jobs.Create("My encoding job");

            IMediaProcessor processor = GetMediaProcessor("Windows Azure Media Encoder");
            List<String> configOptions = new List<String>();

            while (true)
            {
                Console.WriteLine("Enter new config option: -1 to exit");
                var temp = Console.ReadLine();
                if (temp == "-1")
                {
                    break;
                }
                else
                {
                    configOptions.Add(temp);
                }
            }

            foreach(String configOption in configOptions){
                ITask task = job.Tasks.AddNew("My encoding task",
                processor,
                configOption,
                TaskCreationOptions.None);
                task.InputMediaAssets.Add(asset);
                task.OutputMediaAssets.AddNew("Output asset",
                    true,
                    AssetCreationOptions.None);
            }

            Console.WriteLine("Starting batch job");
            // Launch the job. 
            job.Submit();

            Console.WriteLine("Finished");
            ListAssets();
            Console.ReadLine();
            
        }

        static CloudMediaContext GetContext()
        {
            // Gets the service context 
            Console.WriteLine("Getting context");
            CloudMediaContext temp = null;
            temp = new CloudMediaContext(_accountName, _accountKey);

            Console.WriteLine("Context acquired");
            return temp;
        }

        private static IMediaProcessor GetMediaProcessor(string mediaProcessor)
        {
            // Query for a media processor to get a reference.
            var theProcessor =
                                from p in _context.MediaProcessors
                                where p.Name == mediaProcessor
                                select p;
            // Cast the reference to an IMediaprocessor.
            IMediaProcessor processor = theProcessor.First();

            if (processor == null)
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    "Unknown processor",
                    mediaProcessor));
            }
            return processor;
        }

        static void ListAssets()
        {
            string waitMessage = "Building the list. This may take a few "
                + "seconds to a few minutes depending on how many assets "
                + "you have."
                + Environment.NewLine + Environment.NewLine
                + "Please wait..."
                + Environment.NewLine;
            Console.Write(waitMessage);

            // Create a Stringbuilder to store the list that we build. 
            StringBuilder builder = new StringBuilder();

            foreach (IAsset asset in _context.Assets)
            {
                // Display the collection of assets.
                builder.AppendLine("");
                builder.AppendLine("******ASSET******");
                builder.AppendLine("Asset ID: " + asset.Id);
                builder.AppendLine("Name: " + asset.Name);
                builder.AppendLine("==============");
                builder.AppendLine("******ASSET FILES******");

                // Display the files associated with each asset. 
                foreach (IFileInfo fileItem in asset.Files)
                {
                    builder.AppendLine("Name: " + fileItem.Name);
                    builder.AppendLine("Size: " + fileItem.ContentFileSize);
                    builder.AppendLine("==============");
                }
            }

            // Display output in console.
            Console.Write(builder.ToString());
        }
    }
}
