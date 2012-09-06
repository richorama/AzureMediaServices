using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Collections.ObjectModel;

namespace MediaServicesClient
{
    public class MediaAsset
    {
        IAsset root;
        List<IAsset> children = new List<IAsset>();

        public String Name
        {
            get
            {
                return root.Name;
            }
        }
        
        public ObservableCollection<String> Children
        {
            get
            {
                ObservableCollection<String> names = new ObservableCollection<String>();
                foreach (IAsset asset in children)
                {
                    if (asset.Files.Count > 0)
                    {
                        names.Add(asset.Files[0].Name);
                    }
                }
                return names;
            }
        }

        public MediaAsset(IAsset root)
        {
            this.root = root;
        }

        public void AddChildIfChild(IAsset newChild)
        {
            if (newChild.ParentAssets.Count > 0)
            {
                if (newChild.ParentAssets[0].Id == root.Id)
                {
                    children.Add(newChild);
                    Console.WriteLine("Child added");
                }
                else
                {
                    Console.WriteLine("Child not related to root");
                }
            }
            else
            {
                Console.WriteLine("No Parent Assets");
            }
        }
    }
}
