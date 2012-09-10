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

namespace Converter
{
    class blob
    {
        private static CloudBlobContainer blobContainer;
        static string storage = "media10store";
        static string accKey  = "NTWiBFsIvyPZaYs0WN7SNrjUKi24q4DHAmu9Z/h+TWAsQsa2+eZalcohusy0Wj01C10q0dKr2X29Avwfv8QwIA==";
        private static CloudStorageAccount csa = CloudStorageAccount.Parse
            ("DefaultEndpointsProtocol=http;AccountName="+storage+";AccountKey="+accKey);
        private static CloudBlobClient blobClient = csa.CreateCloudBlobClient();

        public void viewBlob()
        {
            try
            {   
                foreach (var con in blobClient.ListContainers())
                {
                    Console.WriteLine(con.Uri);
                }
                Console.WriteLine();
            }
            catch 
            { 
                Console.WriteLine("Error reading storage");
            }
        }

        public void deleteAll()
        {
            foreach (var con in blobClient.ListContainers())
            {
                con.Delete();
            }
            Console.WriteLine("All Containers/Blobs deleted\n");
        }

        public void delete(string name)
        {
            name = name.ToLower();
            blobContainer = blobClient.GetContainerReference(name);
            blobContainer.Delete();
            Console.WriteLine("The container "+name+" has been deleted\n");
        }

        public void newContainer(string name)
        {
            BlobContainerPermissions containerPermissions;
            try
            {
                name = name.ToLower();
                blobContainer = blobClient.GetContainerReference(name);
                blobContainer.CreateIfNotExist();
                containerPermissions = new BlobContainerPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                blobContainer.SetPermissions(containerPermissions);
                Console.WriteLine("Container "+name+" has been created\n");
            }
            catch (Exception e)
            { 
                Console.WriteLine("Error creating container: "+e);
            }
        }

        public void uploadFile(string blobname, string filename, string path)
        {
            try
            {
                blobname = blobname.ToLower();
                blobContainer  = blobClient.GetContainerReference(blobname);
                CloudBlob blob = blobContainer.GetBlobReference(filename);
                Console.WriteLine("Starting file upload");
                blob.UploadFile(path);
                Console.WriteLine("File upload complete to blob " + blob.Uri+"\n");
            }
            catch (Exception e)
            { 
                Console.WriteLine("Error uploading file: "+e);
            }
        }

        public void uploadFile(string blobname, string[] arr)
        {
            try
            {
                blobname = blobname.ToLower();
                blobContainer = blobClient.GetContainerReference(blobname);
                var i = 0;
                foreach(var s in arr){
                    CloudBlob blob = blobContainer.GetBlobReference("File"+i);
                    Console.WriteLine("Starting file upload");
                    blob.UploadFile(s);
                    Console.WriteLine("File upload complete to blob " + blob.Uri + "\n");
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error uploading file: " + e);
            }
        }
    }
}
