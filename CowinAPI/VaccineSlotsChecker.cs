using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;

namespace CowinAPI
{
    public static class VaccineSlotsChecker
    {
        [FunctionName("VaccineSlotsChecker")]
        public static async Task RunAsync([TimerTrigger("%Schedule%")]TimerInfo myTimer, ILogger log)
        {
            if (!await ShouldRunFunction().ConfigureAwait(false))
            {
                return;
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            int age = int.Parse(GetEnvironmentVariable("age"));
            string codes = GetEnvironmentVariable("pincodes");
            List<string> pingcodeList = codes.Split(',').ToList();
            string dateString = DateTime.Now.ToString("dd-MM-yyyy");

            foreach (var pin in pingcodeList)
            {
                await SendSlotsMsgForPincodeAsync(age, dateString, pin, log).ConfigureAwait(false);
            }
        }

        private static async Task<FileStream> UploadStateToBlob()
        {
            // Create a local file in the ./data/ directory for uploading and downloading
            string localPath = "./data/";
            string fileName = $"check";
            string localFilePath = Path.Combine(localPath, fileName);

            // Write text to the file
            await File.WriteAllTextAsync(localFilePath, DateTime.UtcNow.ToString());

            BlobContainerClient containerClient = await GetContainerClient().ConfigureAwait(false);
            Parallel.ForEach(containerClient.GetBlobs(), async x => await containerClient.GetBlobClient(x.Name).DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots).ConfigureAwait(false));
            // Get a reference to a blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            FileStream uploadFileStream = File.OpenRead(localFilePath);
            await blobClient.UploadAsync(uploadFileStream, true);
            uploadFileStream.Close();
            return uploadFileStream;
        }

        private static async Task<bool> ShouldRunFunction()
        {
            BlobContainerClient containerClient = await GetContainerClient().ConfigureAwait(false);
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var blobCL = containerClient.GetBlobClient(blobItem.Name);
                BlobDownloadInfo download = await blobCL.DownloadAsync();

                if (download.Details.LastModified.UtcDateTime > DateTime.UtcNow.AddMinutes(-15))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<BlobContainerClient> GetContainerClient()
        {
            string blobConnection = GetEnvironmentVariable("AzureWebJobsDashboard");
            // Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnection);

            //Create a unique name for the container
            string containerName = "check";

            // Create the container and return a container client object
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
            return containerClient;
        }

        private static async Task SendSlotsMsgForPincodeAsync(int age, string dateString, string pin, ILogger log)
        {
            string url = string.Format(GetEnvironmentVariable("cowinURL"), pin, dateString);
            bool isDose1 = bool.Parse(GetEnvironmentVariable("isDose1"));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            string responseString = string.Empty;
            try
            {
                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using Stream stream = response.GetResponseStream();
                using StreamReader reader = new StreamReader(stream);
                responseString = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.LogWarning(e.Message);
                return;
            }            

            SessionsCalendarEntity sessionsEntity = JsonConvert.DeserializeObject<SessionsCalendarEntity>(responseString);
            var sessionList = sessionsEntity.centers;
            var sessions18 = sessionsEntity.centers.Where(x => x.sessions.Any(y => y.min_age_limit == age && ((isDose1 && y.available_capacity_dose1 > 0) || (!isDose1 && y.available_capacity_dose2 > 0)))).ToList();
            if(sessions18.Count() > 0)
            {
                TGSessionsCalendarEntity msgDTO = MapToTGEntity(sessions18);
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < msgDTO.centers.Count(); i++)
                {
                    foreach(var session in msgDTO.centers[i].sessions)
                    {
                        stringBuilder.AppendLine(msgDTO.centers[i].name);
                        stringBuilder.AppendLine(session.vaccine);
                        stringBuilder.AppendLine($"Total {session.available_capacity} slots on {session.date}");
                        stringBuilder.AppendLine($"(Dose 1: {session.available_capacity_dose1}, Dose2: {session.available_capacity_dose2})");
                        stringBuilder.AppendLine();
                    }
                }

                await SentTelegramMessageAsync(stringBuilder.ToString()).ConfigureAwait(false);
                await UploadStateToBlob().ConfigureAwait(false);
            }
        }

        private static async Task SentTelegramMessageAsync(string msg)
        {
            var bot = new TelegramBotClient(GetEnvironmentVariable("botkey"));
            await bot.SendTextMessageAsync(GetEnvironmentVariable("chatid"), msg).ConfigureAwait(false);
        }

        private static TGSessionsCalendarEntity MapToTGEntity(IEnumerable<Center> sessions18)
        {
            TGSessionsCalendarEntity msgDTO = new TGSessionsCalendarEntity();
            msgDTO.centers = new List<TGCenter>();
            foreach (var tgc in sessions18)
            {
                var tGSessions = new List<TGSession>();
                foreach (var ss in tgc.sessions)
                {
                    TGSession tGSession = new TGSession
                    {
                        available_capacity = ss.available_capacity,
                        date = ss.date,
                        vaccine = ss.vaccine,
                        available_capacity_dose1 = ss.available_capacity_dose1,
                        available_capacity_dose2 = ss.available_capacity_dose2
                    };
                    if (tGSession.available_capacity > 0)
                    {
                        tGSessions.Add(tGSession);
                    }
                }
                TGCenter tGCenter = new TGCenter
                {
                    fee_type = tgc.fee_type,
                    name = tgc.name
                };
                tGCenter.sessions = tGSessions;
                msgDTO.centers.Add(tGCenter);
            }

            return msgDTO;
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
