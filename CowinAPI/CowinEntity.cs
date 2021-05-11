using System;
using System.Collections.Generic;
using System.Text;

namespace CowinAPI
{

    public class SessionsEntity
    {
        public Session[] sessions { get; set; }
    }


    public class SessionsCalendarEntity
    {
        public IList<Center> centers { get; set; }
    }

    public class TGSessionsCalendarEntity
    {
        public IList<TGCenter> centers { get; set; }
    }

    public class Center
    {
        public int center_id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string state_name { get; set; }
        public string district_name { get; set; }
        public string block_name { get; set; }
        public int pincode { get; set; }
        public int lat { get; set; }
        public int _long { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string fee_type { get; set; }
        public IList<Session> sessions { get; set; }
    }

    public class Session
    {
        public string session_id { get; set; }
        public string date { get; set; }
        public int available_capacity { get; set; }
        public int min_age_limit { get; set; }
        public string vaccine { get; set; }
        public IList<string> slots { get; set; }
    }

    public class TGCenter
    {
        public string name { get; set; }
        public string fee_type { get; set; }
        public IList<TGSession> sessions { get; set; }
    }

    public class TGSession
    {
        public string date { get; set; }
        public int available_capacity { get; set; }
        public string vaccine { get; set; }
    }

}
