# CoWIN Telegram Notifier
## Why this?
- Sends a telegram message instead of a mail or push notification on the phone. Can use telegram on phone as well as desktop. 
- Commercially available notifiers run once an hour. You can make this run any number of times you need. 
- What's more trustworthy than your own code and app? You know when it has a bug or is down. 

## Steps to setup Telegram 
- Create a bot using botfather
- Create a channel
- Add bot to channel as Admin
- Get chat id of the channel and apikey of the bot to use in this program

## Required configuration values for this program
```
{
  "IsEncrypted": false,
  "Values": {
    "chatid": "<chat id here>",
    "botkey": "<bot APIkey here>",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "*",
    "AzureWebJobsDashboard": "",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "*",
    "Schedule": "0 * * * * *",
    "pincodes": "<CSV pincodes>",
    "AzureWebJobsStorage": "",
    "age": "18",
    "cowinURL": "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByPin?pincode={0}&date={1}"
  }
}
```

