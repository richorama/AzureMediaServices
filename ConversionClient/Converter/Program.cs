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
    const string accKey  = "qrtMwX0RJWZFzK0kjOzZUn7y5Cm/zcpM6lX5dnCmUu0=";
    const string accName = "media101tut";
    private static string input = Path.GetFullPath(@"C:\Users\richard.astbury\Videos\IMG_0345.MOV");
    private static string output = Path.GetFullPath(@"D:\output");
    
    static void Main(string[] args)
    {
        Converter.Media med = new Media(accName, accKey);
        IAsset asset = med.mediaContext.Assets.Create(input);
        
        using (var stream = new StreamReader("./encodings.txt"))
        {
            // a maximum of 18 tasks are supported in one job
            var encodings = stream.ReadToEnd().Split('\n').Select(x => x.Trim()).Take(18).ToArray();
            med.Encode(asset, encodings);
        }

        // Upload and encode a file via media services
        //var mediaContext = new CloudMediaContext(accName, accKey);
        //IAsset asset = mediaContext.Assets.Create(input);
        //Console.WriteLine("Upload complete");
        //Encode(asset, output);

        // View the current blob containers
        //var storAcc = new Blob();
        //storAcc.viewBlob();
        Console.ReadLine();
    }
    
    static void DownloadAssetToLocal(IJob job, string outputFolder)
    {
        IAsset outputAsset = job.OutputMediaAssets[0];

        Console.WriteLine();
        Console.WriteLine("Files are downloading... please wait.");

        foreach (IFileInfo outputFile in outputAsset.Files)
        {
            string localDownloadPath = Path.GetFullPath(outputFolder + @"\" + outputFile.Name);
            Console.WriteLine("File is downloading to:  " + localDownloadPath);
            outputFile.DownloadToFile(Path.GetFullPath(outputFolder + @"\" + outputFile.Name));
        }
    }

    static void WriteToFile(string URL, string outputFolder)
    {
        // Write the string to a file.
        using (StreamWriter file = new StreamWriter(outputFolder + "URL.txt"))
        {
            file.WriteLine(URL);
        }

    }    
}

