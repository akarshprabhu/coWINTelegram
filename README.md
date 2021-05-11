# CoWIN Telegram Notifier
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

