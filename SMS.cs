using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using ClickSend;
using ClickSend.Models;
using Newtonsoft.Json;
using System.IO;

namespace ALPR_Core
{
    public class SMS
    {
        ClickSendClient client;
        NLog.Logger _Log;
        string Phone_Number;
        private readonly object smsLock = new object();

        public SMS(string login, string key, string phone)
        {
            Phone_Number = phone;

            client = new ClickSendClient("asemakov", "E060C00E-309C-E7DF-0028-C831783423C8");

            // POST https://rest.clicksend.com/v3/sms/send

            _Log = CLogger.Instance().getLogger();
        }

        public bool send_sms(string number, string message)
        {
            try
            {
                if (number == "") return false;

                Monitor.Enter(smsLock);

                // Create the SMS object and specify the SMS details
                SmsMessage sms = new SmsMessage();

                sms.Source = "c#"; //Your method of sending

                sms.To = number; // Recipient phone number in E.164 format.
                                 //sms.ListId = 428;   Your list ID if sending to a whole list. Can be used instead of 'to'.

                //sms.Body = DateTime.Now.ToString() + ":\r\n " + message;
                sms.Body = message;

                sms.From = Phone_Number; //Your sender id - more info: https://help.clicksend.com/SMS/what-is-a-sender-id-or-sender-number.
                                         //sms.Schedule = 1477476000; //Leave blank for immediate delivery. Your schedule time in unix format 
                sms.CustomString = "Custom kn0ChLhwn6"; //Your reference. Will be passed back with all replies and delivery reports.

                SmsMessageCollection SMSs = new SmsMessageCollection();

                SMSs.Messages = new List<SmsMessage>();
                SMSs.Messages.Add(sms);

                string SMSresult = client.SMS.SendSms(SMSs);

                //client.SMS.GetInboundSms();

                _Log.Debug("MESSAGE: " + message + " SENT TO: " + number);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Monitor.Exit(smsLock);
            }
        }

        public TextM[] GetInboundSMSs()
        {
            try
            {
                Monitor.Enter(smsLock);

                string SMSs = client.SMS.GetInboundSms();

                var f = JsonConvert.DeserializeObject<TextM_obj>(SMSs);

                TextM_obj eObj = (TextM_obj)f;
                TextMArray email_array = eObj.data;
                if (email_array.data.Length > 0)
                {
                    client.SMS.MarkAllInboundSMSAsRead(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                }

                return email_array.data;
            }
            catch(Exception ex)
            {
                return null;
            }
            finally
            {
                Monitor.Exit(smsLock);
            }
        }
    }

    public class TextM
    {
       public string timestamp;
       public string from;
       public string body;
       public string original_body;
       public string original_message_id;
       public string to;
       public string custom_string;
       public string message_id;
       public string _keyword;
    }

    public class TextMArray
    {
        public string total;
        public string per_page;
        public string current_page;
        public string last_page;
        public string next_page_url;
        public string prev_page_url;
        public string from;
        public string to;
        

        public TextM[] data;
    }

    public class TextM_obj
    {
        public string http_code;
        public string response_code;
        public string response_msg;

        public TextMArray data;
    }
}
