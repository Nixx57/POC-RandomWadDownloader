using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;

using static System.Net.WebRequestMethods;

class Program
{
    //private static readonly string downloadPath = @"~/randomwads";
    private static readonly string downloadPath = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/randomwads";
    private static readonly int[] contentsIds = [
        15, 32, 33, 65, 86, 90, 116, 162, 196, 326, //Doom ports (ports ????)
        21, 22, 28, 29, 34, 38, 99, 104, 130, 167, //Doom
        3, 7, 12, 13, 14, 37, 46, 64, 66, 106, //Doom 2 ports
        8, 18, 20, 30, 41, 57, 70, 72, 81, 82]; //Doom 2

    static async Task Main()
    {
        Console.WriteLine("Récupération d'un wad aléatoire...");
        do
        {
            int id = contentsIds[RandomNumberGenerator.GetInt32(contentsIds.Length)];
            string apiUrl = $"https://www.doomworld.com/idgames/api/api.php?action=getcontents&id={id}";

            string response = await SendRequest(apiUrl);

            Console.WriteLine("Fiche :\r\n---------");
            bool downloaded = DisplayFileAsCard(response);

            if (downloaded)
                Thread.Sleep(3000);
            Console.Clear();
        }
        while (true);
    }

    static async Task<string> SendRequest(string apiUrl)
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : $"Erreur de la requête : {response.StatusCode}";
    }

    static bool DisplayFileAsCard(string xml)
    {
        try
        {
            XDocument xDoc = XDocument.Parse(xml);
            
            //var randomFile = xDoc.Descendants("file").OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            var randomFile = xDoc.Descendants("file").ToArray()[RandomNumberGenerator.GetInt32(xDoc.Descendants("file").Count())];

            if (randomFile != null)
            {
                // Extraire les propriétés du fichier.
                string? title = randomFile.Element("title")?.Value;
                string? author = randomFile.Element("author")?.Value;
                string? description = randomFile.Element("description")?.Value;
                string? rating = randomFile.Element("rating")?.Value;
                string? votes = randomFile.Element("votes")?.Value;
                string? url = randomFile.Element("url")?.Value;
                string? idgamesprotocol = randomFile.Element("idgamesurl")?.Value;

                // Afficher les informations sous forme de fiche.
                Console.WriteLine($"Titre : {title}");
                Console.WriteLine($"Auteur : {author}");
                Console.WriteLine($"Description : {description}");
                Console.WriteLine($"Note moyenne : {rating}");
                Console.WriteLine($"Votes : {votes}");
                Console.WriteLine($"URL : {url}");
                Console.WriteLine($"IdGames Protocol : {idgamesprotocol}");

                Console.Write("Voulez-vous télécharger ce fichier ? (oui/non): ");
                string? userResponse = Console.ReadLine();

                if ((userResponse == "oui" || userResponse == string.Empty) && idgamesprotocol is not null)
                {
                    DownloadFile(idgamesprotocol);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Aucun élément 'file' trouvé dans la réponse XML.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'analyse XML : {ex.Message}");
        }
        return false;
    }

    static void DownloadFile(string fileUrl)
    {
        try
        {
            string mirrorUrl = GetMirrorUrl(fileUrl);

            using HttpClient client = new();
            byte[] fileData = client.GetByteArrayAsync(mirrorUrl).Result;

            string cleanedFileName = CleanFileName(fileUrl);

            string extension = Path.GetExtension(mirrorUrl);
            string filePath = Path.Combine(downloadPath, $"{cleanedFileName}");
           
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }

            System.IO.File.WriteAllBytes(filePath, fileData);

            Console.WriteLine($"Le fichier a été téléchargé avec succès à l'emplacement : {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du téléchargement du fichier : {ex.Message}");
        }
    }



    static string CleanFileName(string fileName)
    {
        return fileName.Split('/').Last();
    }


    static string GetMirrorUrl(string idgamesUrl)
    {
        return idgamesUrl.Replace("idgames://", "https://www.gamers.org/pub/idgames/");
    }

}
