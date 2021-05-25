using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            int age = int.Parse(GetEnvironmentVariable("age"));
            string codes = GetEnvironmentVariable("pincodes");
            List<string> pingcodeList = codes.Split(',').ToList();
            string dateString = DateTime.Now.ToString("dd-MM-yyyy");

            foreach(var pin in pingcodeList)
            {
                await SendSlotsMsgForPincodeAsync(age, dateString, pin, log).ConfigureAwait(false);
            }
        }

        private static async Task SendSlotsMsgForPincodeAsync(int age, string dateString, string pin, ILogger log)
        {
            string url = string.Format(GetEnvironmentVariable("cowinURL"), pin, dateString);
            bool isDose1 = bool.Parse(GetEnvironmentVariable("isDose1"));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            string responseString = string.Empty;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    responseString = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return;
            }            

            SessionsCalendarEntity sessionsEntity = JsonConvert.DeserializeObject<SessionsCalendarEntity>(responseString);
            var sessionList = sessionsEntity.centers;
            var sessions18 = sessionsEntity.centers.Where(x => x.sessions.Any(y => y.min_age_limit == age && ((isDose1 && y.available_capacity_dose1 > 0) || (!isDose1 && y.available_capacity_dose2 > 0)))).ToList();
            if(sessions18.Count() > 0)
            {
                TGSessionsCalendarEntity msgDTO = MapToTGEntity(sessions18);
                string msg = JsonConvert.SerializeObject(msgDTO, Formatting.Indented);

                await SentTelegramMessageAsync(msg).ConfigureAwait(false);
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
                        vaccine = ss.vaccine
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
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
