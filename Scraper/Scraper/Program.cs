using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputURL = "";
            List<string> albumIDs = new List<string>();
            List<AlbumImage> allImages = new List<AlbumImage>();
            bool useGalleryID = false;
            string dumpDirectory = "";
            Console.Title = "Input settings";
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Name of dump directory (where everything is saved). Leave blank if you want it to be the gallery ID");
            Console.ForegroundColor = ConsoleColor.White;
            dumpDirectory = Console.ReadLine().Trim();
            if (dumpDirectory.Length == 0)
            {
                useGalleryID = true;
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Please enter in the URLs to scrape; one at a time. Enter in 'end' when you are finished. The URLs must be in the format 'https://imgur.com/gallery/XXXXX'");
            while (inputURL.ToLower() != "end")
            {
                Console.ForegroundColor = ConsoleColor.White;
                inputURL = Console.ReadLine();
                if (inputURL.ToLower() == "end")
                {
                    continue;
                }
                try
                {
                    string galleryID = inputURL.Split(new string[] { "gallery/" }, StringSplitOptions.None)[1].Trim(new char[] { '/' });
                    albumIDs.Add(galleryID);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("That URL seems to be in an invalid format!");
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Title = "Scraping URLs";
            Console.WriteLine("Scraping image URLs...");
            foreach (string singleAlbum in albumIDs)
            {
                try
                {
                    string rawDownload = new WebClient().DownloadString("http://imgur.com/ajaxalbums/getimages/" + singleAlbum + "/hit.json?all=true");
                    dynamic decodedDownload = DynamicJson.Deserialize(rawDownload);
                    try
                    {
                        foreach (dynamic singleItem in decodedDownload.data.images)
                        {
                            allImages.Add(new AlbumImage(singleItem, (useGalleryID) ? singleAlbum : dumpDirectory));
                            try
                            {
                                Directory.CreateDirectory((useGalleryID) ? singleAlbum : dumpDirectory);
                            } catch { }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("{0}: Invalid URL", singleAlbum);
                    }
                } catch { }
            }
            Console.WriteLine("Detected a total of {0} image{1}.", allImages.Count().ToString(), (allImages.Count() == 1) ? "" : "s");
            Console.WriteLine("Beginning download...");
            string downloadTitleTemplate = "Downloaded {0}/{1} with {2} error(s)";
            int downloadCount = 0;
            int errorCount = 0;
            Parallel.ForEach(allImages.Cast<AlbumImage>(), singleImage =>
            {
                if (singleImage.Download())
                {
                    downloadCount++;
                }
                else
                {
                    errorCount++;
                }
                Console.Title = string.Format(downloadTitleTemplate, downloadCount.ToString(), allImages.Count().ToString(), errorCount.ToString());
            });
            Console.WriteLine("Finished all downloads.");
            Console.ReadLine();
        }
    }
    public class AlbumImage
    {
        string fileName;
        string fileURL;
        string filePath;
        public AlbumImage(dynamic imageData, string galleryID)
        {
            fileName = (string)imageData.hash + (string)imageData.ext;
            fileURL = "http://i.imgur.com/" + fileName;
            filePath = galleryID + "/" + fileName;
        }
        public bool Download()
        {
            try
            {
                new WebClient().DownloadFile(fileURL, filePath);
                Console.WriteLine("{0}: Success", fileName);
                return true;
            }
            catch
            {
                Console.WriteLine("{0}: Failed", fileName);
                return false;
            }
        }
    }
}
