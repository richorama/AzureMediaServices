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
    private static string accKey  = "qrtMwX0RJWZFzK0kjOzZUn7y5Cm/zcpM6lX5dnCmUu0=";
    private static string accName = "media101tut";
    private static string input  = Path.GetFullPath(@"D:\input\alpha.txt");
    private static string input2 = Path.GetFullPath(@"D:\input\beta.txt");
    private static string output = Path.GetFullPath(@"D:\output");
    // Path to configuration file if required, i.e. for streamer conversion
    private static string configFilePath = Path.GetFullPath(@"D:\input\MP4 to Smooth Streams.xml");

    static void Main(string[] args)
    {
        //Converter.media med = new media(accName, accKey);
        //IAsset asset = med.mediaContext.Assets.Create(input);
        //med.Encode(asset, "H.264 YouTube SD");

        // Upload and encode a file via media services
        //mediaContext = new CloudMediaContext(accName, accKey);
        //IAsset asset = mediaContext.Assets.Create(input);
        //Console.WriteLine("Upload complete");
        //Encode(asset, output);

        // View the current blob containers
        Converter.blob storAcc = new blob();
        storAcc.viewBlob();
        Console.ReadLine();
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

    static void writeToFile(string URL, string outputFolder)
    {
        // Write the string to a file.
        System.IO.StreamWriter file = new System.IO.StreamWriter(outputFolder+"URL.txt");
        file.WriteLine(URL);

        file.Close();
    }    
}

