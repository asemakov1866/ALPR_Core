using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using OpenAlprApi;
using OpenAlprApi.Api;
using OpenAlprApi.Client;
using OpenAlprApi.Model;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB;

namespace ALPR_Core
{
    public class SMS_Manager
    {
        string SMS_in_Greeting;             // Original SMS sent by Visitor when the car is parked
        string SMS_in_Cancel;               // Cancels registration in case of entry error or typo
        string SMS_in_Bye;                  // Original SMS sent by Visitor when the car is leaving
        string SMS_in_Time;                 // Original SMS sent by Visitor to check remaining time
        string SMS_in_Update;               // SMS sent by Administrator to request a list of parked cars
        string SMS_in_Help;
        string SMS_out_Greeting;            // System reply to Visitor's original SMS
        string SMS_out_Cancel;              // System reply on cancel
        string SMS_out_Registering;         // System reply to Visitor's registration info
        string SMS_out_Bye;
        string SMS_out_InvalidEntry;
        string SMS_out_Help;
        string SMS_out_UseCase1;
        string SMS_out_UseCase2;
        string SMS_out_UseCase3;

        int Camera_TimerInterval_min = 60;
        int SMS_TimerInterval_sec = 5;
        string database = "Test";
        string DBConnectionString = "mongodb://localhost:27017";
        //string Documents_Path = @"c:\ALPR\Documents";
        string login;
        string key;
        string phone;
        string[] UnitList;
        double VisitorParkingLimit_hrs = 48;
        double VisitorNotifyTime1_min = 0;
        double VisitorNotifyTime2_min = 0;
        double VisitorNotifyTime3_min = 0;
        NLog.Logger _Log;
        System.Timers.Timer aTimer;
        System.Timers.Timer bTimer;
        System.Timers.Timer cTimer; // Checks Visitor Dictionary for expired time

        System.Threading.Timer timer2;

        MongoDB db;
        SMS _sms;
        Dictionary<string, Visitor_Car> Visitor_Dict;
        Dictionary<string, Resident_Car> Resident_Dict;
        List<Camera> Camera_List = new List<Camera>();
        List<string> AdministratorPhones = new List<string>();
        long LatestTime;
        GoogleSheets gs = new GoogleSheets();

        public void Start_SMS_Service()
        {
            try
            {
                _Log = CLogger.Instance().getLogger();
                db = new MongoDB(DBConnectionString, database);

                // Load resident list
                StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\Residents.csv");
                string line = "";

                List<Resident_Car> carList = new List<Resident_Car>();

                while ((line = sr.ReadLine()) != null)
                {
                    string[] row = line.Split(',');
                    if (row.Length == 6)
                    {
                        Resident_Car obj = new Resident_Car();
                        obj.LicensePlate = row[0].ToUpper();
                        obj.State = row[1];
                        obj.CarOwnerName = row[2];
                        obj.CarOwnerPhone = row[3];
                        obj.UnitNumber = int.Parse(row[4]);
                        obj.RentedSpotNumber = int.Parse(row[5]);
                        carList.Add(obj);
                    }
                }

                db.WriteDataToDatabase(carList.ToArray(), "Residents");
                Thread.Sleep(300);
                Resident_Dict = new Dictionary<string, Resident_Car>();
                foreach (Resident_Car car in carList)
                {
                    Resident_Dict[car.CarOwnerPhone] = car;
                }

                //if (Resident_Dict.Count > 0) MessageBox.Show("Residents List was updated!");


                //InitializeGridView();

                ReadConfig();
                _sms = new SMS(login, key, phone);

                
                LatestTime = db.GetLatestTime();


                // Populate Visitor dictionary
                Visitor_Dict = new Dictionary<string, Visitor_Car>();
                Visitor_Car[] car_ar = new Visitor_Car[0]; 
                var a = db.PopulateVisitorDictionary();
                if(a != null) car_ar = db.PopulateVisitorDictionary();
                
                if (car_ar.Length > 0)
                {
                    foreach (Visitor_Car car in car_ar)
                    {
                        Visitor_Dict[car.ContactPhone] = car;
                        ////dataGridView2.Rows.Add();
                        ////dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[0].Value = UnixTimeStampToDateTime(double.Parse(car.Time)).ToString("yyyy-MM-dd hh:mm:ss tt");
                        ////dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[1].Value = car.LicensePlate;
                        ////dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[2].Value = car.HostUnitNumber;
                        ////dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[3].Value = car.ContactPhone; ;
                    }
                    UpdateGoogleSheets();
                }

                StartTimer2();
                StartTimer3();

                //StartTimer();
                //log("The Timer has started");

                // Find Reolink Client window

                ////int hwnd = 0;
                ////IntPtr hwndChild = IntPtr.Zero;
                ////Process[] processlist = Process.GetProcesses();

                ////foreach (Process process in processlist)
                ////{
                ////    if (!String.IsNullOrEmpty(process.MainWindowTitle))
                ////    {
                ////        string name = process.ProcessName;
                ////        int id = process.Id;
                ////        string window = process.MainWindowTitle;
                ////    }
                ////}

                //Get a handle for the Calculator Application main window
                ////hwnd = FindWindow(null, "Reolink Client");
                ////if (hwnd == 0)
                ////{
                ////    log("Reolink Client Window was not found.");
                ////}

                ////IntPtr result = IntPtr.Zero;

                ////Dictionary<IntPtr, string> Controls_Dict = new Dictionary<IntPtr, string>();
                ////Dictionary<IntPtr, string> AllControls_Dict = new Dictionary<IntPtr, string>();
                ////foreach (KeyValuePair<IntPtr, string> kvp in Controls_Dict) AllControls_Dict[kvp.Key] = Controls_Dict[kvp.Key];

               
                log("The program has been configured");

            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }

        }

        public void log(string message)
        {
            _Log.Debug(message);
        }

        private void ReadConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + @"\alprconfig.xml");
                Camera_TimerInterval_min = int.Parse(doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("Camera_TimerInterval_min").InnerText);
                database = doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("Database").InnerText;
                DBConnectionString = doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("DBConnectionString").InnerText;
                VisitorParkingLimit_hrs = double.Parse(doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("VisitorParkingLimit_hrs").InnerText);
                double.TryParse(doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("VisitorNotifyTime1_min").InnerText, out VisitorNotifyTime1_min);
                double.TryParse(doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("VisitorNotifyTime2_min").InnerText, out VisitorNotifyTime2_min);
                double.TryParse(doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("VisitorNotifyTime3_min").InnerText, out VisitorNotifyTime3_min);
                //Documents_Path = doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("Documents_Path").InnerText;
                SMS_TimerInterval_sec = int.Parse(doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("SMS_TimerInterval_sec").InnerText);
                login = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("SMS_login").InnerText;
                key = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("SMS_key").InnerText;
                phone = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("SMS_phone").InnerText;
                UnitList = doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("UnitList").InnerText.Split(',');
                SMS_in_Greeting = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Greeting").InnerText.ToUpper();
                SMS_in_Cancel = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Cancel").InnerText.ToUpper();
                SMS_out_Cancel = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_Cancel").InnerText.ToUpper().Replace(@"\r", Environment.NewLine);
                SMS_in_Bye = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Bye").InnerText.ToUpper().Replace(@"\r", Environment.NewLine);
                SMS_in_Time = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Time").InnerText.ToUpper().Replace(@"\r", Environment.NewLine);
                SMS_in_Update = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Update").InnerText.ToUpper().Replace(@"\r", Environment.NewLine);
                SMS_out_Greeting = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_Greeting").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_Registering = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_Registering").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_Bye = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_Bye").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_InvalidEntry = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_InvalidEntry").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_in_Help = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Inbound").SelectSingleNode("SMS_in_Help").InnerText.ToUpper().Replace(@"\r", Environment.NewLine);
                SMS_out_Help = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_Help").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_UseCase1 = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_UseCase1").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_UseCase2 = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_UseCase2").InnerText.Replace(@"\r", Environment.NewLine);
                SMS_out_UseCase3 = doc.SelectSingleNode("Config").SelectSingleNode("SMS").SelectSingleNode("Outbound").SelectSingleNode("SMS_out_UseCase3").InnerText.Replace(@"\r", Environment.NewLine);
                string admin = doc.SelectSingleNode("Config").SelectSingleNode("Properties").SelectSingleNode("AdministratorPhones").InnerText.ToUpper();
                AdministratorPhones = admin.Split(',').ToList();
                
                XmlNode cameras = doc.SelectSingleNode("Config").SelectSingleNode("Cameras");
                XmlSerializer serializer = new XmlSerializer(typeof(List<Camera>), new XmlRootAttribute("Cameras"));
                XmlNodeReader nodeReader = new XmlNodeReader(cameras);
                Camera_List = (List<Camera>)serializer.Deserialize(nodeReader);
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
        }
        void InitializeGridView()
        {
            try
            {
                ////dataGridView1.Rows.Clear();
                ////dataGridView1.Columns.Clear();
                ////dataGridView1.ClearSelection();
                ////dataGridView1.Refresh();

                ////this.dataGridView1.DefaultCellStyle.Font = new Font("Arial", 12);

                ////dataGridView1.Columns.Add("0", "Camera Name");
                ////this.dataGridView1.Columns[0].Width = 100;
                ////this.dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.Columns.Add("1", "Car Type");
                ////this.dataGridView1.Columns[1].Width = 100;
                ////this.dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.Columns.Add("2", "License Plate");
                ////this.dataGridView1.Columns[2].Width = 100;
                ////this.dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.Columns.Add("3", "State");
                ////this.dataGridView1.Columns[3].Width = 50;
                ////this.dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.Columns.Add("4", "Confidence,%");
                ////this.dataGridView1.Columns[4].Width = 80;
                ////this.dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.Columns.Add("5", "Status");
                ////this.dataGridView1.Columns[5].Width = 190;
                ////this.dataGridView1.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                ////dataGridView1.Columns.Add("6", "Hours Parked");
                ////this.dataGridView1.Columns[6].Width = 80;
                ////this.dataGridView1.Columns[6].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView1.AllowUserToAddRows = false;

                ////dataGridView2.Rows.Clear();
                ////dataGridView2.Columns.Clear();
                ////dataGridView2.ClearSelection();
                ////dataGridView2.Refresh();
                ////this.dataGridView2.DefaultCellStyle.Font = new Font("Arial", 12);
                ////dataGridView2.Columns.Add("0", "Time Registered");
                ////this.dataGridView2.Columns[0].Width = 200;
                ////this.dataGridView2.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView2.Columns.Add("1", "License Plate");
                ////this.dataGridView2.Columns[1].Width = 150;
                ////this.dataGridView2.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView2.Columns.Add("2", "Host's Unit#");
                ////this.dataGridView2.Columns[2].Width = 150;
                ////this.dataGridView2.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                ////dataGridView2.Columns.Add("3", "Phone");
                ////this.dataGridView2.Columns[3].Width = 150;
                ////this.dataGridView2.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                ////dataGridView2.AllowUserToAddRows = false;

            }
            catch (Exception ex)
            {
                log(ex.Message);
            }

        }

      
        // Start a timer for checking SMS
        void StartTimer2()
        {
            //// Timer for polling
            //bTimer = new System.Timers.Timer(SMS_TimerInterval_sec * 1000);
            //// Hook up the Elapsed event for the timer. 
            //bTimer.Elapsed += OnTimedEvent2;
            //bTimer.AutoReset = true;
            //bTimer.Enabled = true;
            //bTimer.Start();

            // Changing to Threading.Timer to dynamically apply tiner interval in the end of the task
            var callback = new TimerCallback(OnTimedEvent2);
            timer2 = new System.Threading.Timer(callback, null, 0, Timeout.Infinite);
        }

        void StartTimer3() // for expiration warning
        {
            // Timer for polling
            cTimer = new System.Timers.Timer(60 * 1000);
            // Hook up the Elapsed event for the timer. 
            cTimer.Elapsed += OnTimedEvent3;
            cTimer.AutoReset = true;
            cTimer.Enabled = true;
            cTimer.Start();
        }

        private void OnTimedEvent3(Object source, ElapsedEventArgs e)
        {
            try
            {
                if (Visitor_Dict.Count > 0)
                {
                    foreach (KeyValuePair<string, Visitor_Car> vc_kvp in Visitor_Dict)
                    {
                        long sec_passed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(vc_kvp.Value.Time);

                        switch (vc_kvp.Value.Status)
                        {
                            case 0:
                                if (VisitorNotifyTime1_min != 0)
                                {
                                    if (sec_passed > VisitorParkingLimit_hrs * 3600 - VisitorNotifyTime1_min * 60 && vc_kvp.Value.Status < (int)CarStatus.Notified1)
                                    {
                                        _sms.send_sms(vc_kvp.Value.ContactPhone, "Your parking authorization expires in " + (Math.Round((VisitorParkingLimit_hrs * 3600 - sec_passed) / 60)).ToString() + " min\rPlease remove your vehicle from the parking lot to avoid getting towed!");
                                        vc_kvp.Value.Status = (int)CarStatus.Notified1;
                                        Visitor_Dict[vc_kvp.Key] = vc_kvp.Value;
                                        db.UpdateVisitorStatusInDatabase(vc_kvp.Value.LicensePlate, ((int)CarStatus.Notified1).ToString());
                                        gs.UpdateLogSheet("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.", "Log");
                                        log("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.");
                                    }
                                }
                                break;
                            case 1:
                                if (VisitorNotifyTime2_min != 0)
                                {
                                    if (sec_passed > VisitorParkingLimit_hrs * 3600 - VisitorNotifyTime2_min * 60 && vc_kvp.Value.Status < (int)CarStatus.Notified2)
                                    {
                                        _sms.send_sms(vc_kvp.Value.ContactPhone, "Your parking authorization expires in " + (Math.Round((VisitorParkingLimit_hrs * 3600 - sec_passed) / 60)).ToString() + " min\rPlease remove your vehicle from the parking lot to avoid getting towed!");
                                        vc_kvp.Value.Status = (int)CarStatus.Notified2;
                                        Visitor_Dict[vc_kvp.Key] = vc_kvp.Value;
                                        db.UpdateVisitorStatusInDatabase(vc_kvp.Value.LicensePlate, ((int)CarStatus.Notified2).ToString());
                                        gs.UpdateLogSheet("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.", "Log");
                                        log("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.");
                                    }
                                }
                                break;
                            case 2:
                                if (VisitorNotifyTime3_min != 0)
                                {
                                    if (sec_passed > VisitorParkingLimit_hrs * 3600 - VisitorNotifyTime3_min * 60 && vc_kvp.Value.Status < (int)CarStatus.Notified3)
                                    {
                                        _sms.send_sms(vc_kvp.Value.ContactPhone, "Your parking authorization expires in " + (Math.Round((VisitorParkingLimit_hrs * 3600 - sec_passed) / 60)).ToString() + " min\rPlease remove your vehicle from the parking lot to avoid getting towed!");
                                        vc_kvp.Value.Status = (int)CarStatus.Notified3;
                                        Visitor_Dict[vc_kvp.Key] = vc_kvp.Value;
                                        db.UpdateVisitorStatusInDatabase(vc_kvp.Value.LicensePlate, ((int)CarStatus.Notified3).ToString());
                                        gs.UpdateLogSheet("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.", "Log");
                                        log("License Plate " + vc_kvp.Value.LicensePlate + " was notified about expiring time.");
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log(ex.Message);
            }
        }

       

        private void OnTimedEvent2(object obj)//(Object source, ElapsedEventArgs e)
        {
            try
            {
                if (_sms == null) return;

                TextM[] SMSs = _sms.GetInboundSMSs();

                if (SMSs == null || SMSs.Length <= 0) return;

                foreach (TextM sms in SMSs)
                {
                    if (sms == null) return;

                    if (long.Parse(sms.timestamp) > LatestTime)
                    {
                        gs.UpdateLogSheet("New SMS from " + sms.from + ", Text: " + sms.body + ", Time stamp: " + sms.timestamp, "Log");
                        _Log.Debug("New SMS from " + sms.from + ", Text: " + sms.body + ", Time stamp: " + sms.timestamp);

                        LatestTime = long.Parse(sms.timestamp);

                        string response = "";
                        if (sms.body.Length > 0) response = sms.body.Trim();
                        if (response.Contains("  ")) response.Replace("  ", " ");
                        if (response != "") response = response.ToUpper();

                        // Select registration info
                        if (response.Split(' ').Length > 1)
                        {
                            if (response.Contains("CHECKOUT"))
                            {
                                if (AdministratorPhones.Contains(sms.from))
                                {


                                    string lp = "";
                                    lp = response.Split(' ')[1];

                                    if (lp == "") return;

                                    string phone = "";
                                    phone = FindVisitorPhoneByLP(lp);

                                    if (CheckIfVisitor(lp))
                                    {

                                        // Log info to the log file and Google Sheets
                                        gs.UpdateLogSheet(lp + " was removed at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"), "Log");
                                        log(lp + " was removed at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"));

                                        // Remove a leaving visitor from the database
                                        if (db == null) return;
                                        db.DeleteItem("Visitors", "LicensePlate", lp);

                                        // Send data to database Visitor_Log
                                        Visitor_Car visitor = new Visitor_Car();
                                        visitor.Time = sms.timestamp;
                                        visitor.Status = (int)CarStatus.CheckedOut;
                                        visitor.ContactPhone = sms.from;
                                        visitor.LicensePlate = lp;
                                        visitor.HostUnitNumber = Visitor_Dict[phone].HostUnitNumber;
                                        db.WriteDataToDatabase(new Visitor_Car[] { visitor }, "Visitor_Log");

                                        // Remove a leaving visitor from the dictionary

                                        if (Visitor_Dict.ContainsKey(phone)) Visitor_Dict.Remove(phone);

                                        // Remove from dataGridView
                                        int index = 0;
                                        ////foreach (DataGridViewRow row in dataGridView2.Rows)
                                        ////{
                                        ////    if (dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[3].Value.ToString() == phone)
                                        ////    {
                                        ////        index = row.Index;
                                        ////    }
                                        ////}
                                        ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows.Remove(dataGridView2.Rows[index]); }));

                                        gs.UpdateLogSheet("License Plate " + lp + " has been checked out", "Log");
                                        log("License Plate " + lp + " has been checked out");
                                        _sms.send_sms(sms.from, "License Plate " + lp + " has been checked out");

                                    }
                                    else
                                    {
                                        _sms.send_sms(sms.from, lp + " is not registered as a visitor's car");
                                    }

                                }
                                else
                                {
                                    _sms.send_sms(sms.from, "This request is authorized for administrators only");
                                    gs.UpdateLogSheet("Unauthorized request for checkout.", "Log");
                                    log("SMS has been sent to " + sms.from + " : This request is authorized for administrators only");
                                }

                            }

                            else
                            {
                                if (response.Length < 6) return;

                                string UnitNumber = response.Substring(response.Length - 4);
                                string LicensePlate = response.Substring(0, response.Length - 4).Trim();



                                if (CheckIfResident(LicensePlate))
                                {
                                    _sms.send_sms(sms.from, SMS_out_UseCase3);
                                    gs.UpdateLogSheet("License Plate " + LicensePlate + " Attempt to check in a resident's car.", "Log");
                                    log("License Plate " + Visitor_Dict[sms.from].LicensePlate + " Attempt to check in a resident's car.");
                                }
                                else
                                {
                                    if (CheckIfUnitExists(UnitNumber))
                                    {
                                        if (!Visitor_Dict.ContainsKey(sms.from))
                                        {
                                            Visitor_Car car = new Visitor_Car();
                                            car.LicensePlate = LicensePlate;
                                            car.Time = sms.timestamp;
                                            car.HostUnitNumber = UnitNumber;
                                            car.ContactPhone = sms.from;
                                            car.Status = (int)CarStatus.CheckedIn;
                                            Visitor_Dict[sms.from] = car;



                                            ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows.Add(); }));
                                            ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[0].Value = UnixTimeStampToDateTime(double.Parse(car.Time)).ToString("yyyy-MM-dd hh:mm:ss tt"); }));
                                            ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[1].Value = car.LicensePlate; }));
                                            ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[2].Value = car.HostUnitNumber; }));
                                            ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[3].Value = car.ContactPhone; }));


                                            _sms.send_sms(sms.from, string.Format(SMS_out_Registering, Visitor_Dict[sms.from].LicensePlate, DateTime.Now.ToString(), car.HostUnitNumber, VisitorParkingLimit_hrs));
                                            log(Visitor_Dict[sms.from].LicensePlate + " has registered for parking at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"));

                                            db.WriteDataToDatabase(new Visitor_Car[] { car }, "Visitor_Log");
                                            db.WriteDataToDatabase(new Visitor_Car[] { car }, "Visitors");

                                            UpdateGoogleSheets();
                                            gs.UpdateLogSheet("License Plate " + Visitor_Dict[sms.from].LicensePlate + " has been registered for parking", "Log");
                                        }
                                        else
                                        {
                                            _sms.send_sms(sms.from, SMS_out_UseCase2);
                                            gs.UpdateLogSheet("Phone: " + sms.from + " Attempt to use a phone that has already been used to check in a visitor's car.", "Log");
                                            log("Phone: " + sms.from + " Attempt to use a phone that has already been used to check in a visitor's car.");
                                        }
                                    }
                                    else
                                    {
                                        _sms.send_sms(sms.from, "The entered Unit# does not exist.");
                                        gs.UpdateLogSheet("Phone: " + sms.from + " attempted to check in a car " + LicensePlate + " with non-existing Unit# " + UnitNumber, "Log");
                                        log("Phone: " + sms.from + " attempted to check in a car " + LicensePlate + " with non-existing Unit# " + UnitNumber);
                                    }
                                }
                            }
                        }
                        else if (response.Contains(SMS_in_Bye))
                        {
                            if (Visitor_Dict.ContainsKey(sms.from))
                            {
                                // Send farewell message
                                _sms.send_sms(sms.from, SMS_out_Bye);

                                // Log info to the log file and Google Sheets
                                gs.UpdateLogSheet(Visitor_Dict[sms.from].LicensePlate + " left parking lot at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"), "Log");
                                log(Visitor_Dict[sms.from].LicensePlate + " left parking lot at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"));

                                // Remove a leaving visitor from the database
                                db.DeleteItem("Visitors", "LicensePlate", Visitor_Dict[sms.from].LicensePlate);

                                // Send data to database Visitor_Log
                                Visitor_Car visitor = new Visitor_Car();
                                visitor.Time = sms.timestamp;
                                visitor.Status = (int)CarStatus.CheckedOut;
                                visitor.ContactPhone = sms.from;
                                visitor.LicensePlate = Visitor_Dict[sms.from].LicensePlate;
                                visitor.HostUnitNumber = Visitor_Dict[sms.from].HostUnitNumber;
                                db.WriteDataToDatabase(new Visitor_Car[] { visitor }, "Visitor_Log");

                                // Remove a leaving visitor from the dictionary
                                gs.UpdateLogSheet("License Plate " + Visitor_Dict[sms.from].LicensePlate + " has been checked out", "Log");
                                log("License Plate " + Visitor_Dict[sms.from].LicensePlate + " has been checked out");
                                Visitor_Dict.Remove(sms.from);

                                // Remove from dataGridView
                                ////int index = 0;
                                ////foreach (DataGridViewRow row in dataGridView2.Rows)
                                ////{
                                ////    if (dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[3].Value.ToString() == sms.from)
                                ////    {
                                ////        index = row.Index;
                                ////    }
                                ////}
                                ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows.Remove(dataGridView2.Rows[index]); }));
                            }
                            else
                            {
                                _sms.send_sms(sms.from, SMS_out_UseCase1);
                                gs.UpdateLogSheet("License Plate " + Visitor_Dict[sms.from].LicensePlate + ". Attempt to check out a visitor's car that has not been checked in.", "Log");
                                log("License Plate " + Visitor_Dict[sms.from].LicensePlate + ". Attempt to check out a visitor's car that has not been checked in.");
                            }
                            UpdateGoogleSheets();
                        }
                        else if (response.Contains(SMS_in_Help))
                        {
                            _sms.send_sms(sms.from, SMS_out_Help);
                            gs.UpdateLogSheet("Help SMS has been sent to " + sms.from, "Log");
                            log("Help SMS has been sent to " + sms.from);
                        }
                        else if (response.Contains(SMS_in_Time))
                        {
                            if (Visitor_Dict.ContainsKey(sms.from))
                            {
                                long sec_passed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(Visitor_Dict[sms.from].Time);
                                long sec_left = (long)(VisitorParkingLimit_hrs * 3600 - sec_passed);

                                int hrs = 0;
                                int min = 0;

                                CalculateTimeLeft(sec_passed, (long)(VisitorParkingLimit_hrs * 3600), out hrs, out min);
                                if (min >= 0 || hrs >= 0)
                                {
                                    _sms.send_sms(sms.from, string.Format("Remaining parking time is {0} hrs {1} min", hrs, min));
                                    gs.UpdateLogSheet("SMS has been sent to " + sms.from + " : " + string.Format("Remaining parking time is {0} hrs {1} min", hrs, min), "Log");
                                    log("SMS has been sent to " + sms.from + " : " + string.Format("Remaining parking time is {0} hrs {1} min", hrs, min));
                                }
                                else
                                {
                                    _sms.send_sms(sms.from, "Parking time has expired!");
                                    gs.UpdateLogSheet("SMS has been sent to " + sms.from + " : Parking time has expired!", "Log");
                                    log("SMS has been sent to " + sms.from + " : Parking time has expired! ");
                                }
                            }
                            else
                            {
                                _sms.send_sms(sms.from, SMS_out_UseCase1);
                                gs.UpdateLogSheet(string.Format("Unauthorized request. {0} has not been used to check in a visitor's car ", sms.from), "Log");
                                log("SMS has been sent to " + sms.from + " : Unauthorized request. " + string.Format("{0} has not been used to check in a visitor's car ", sms.from));
                            }

                        }
                        else if (response.Contains(SMS_in_Greeting))
                        {
                            if (!Visitor_Dict.ContainsKey(sms.from))
                            {
                                _sms.send_sms(sms.from, SMS_out_Greeting);
                                gs.UpdateLogSheet("Greeting SMS has been sent to " + sms.from, "Log");
                                log("Greeting SMS has been sent to " + sms.from);
                            }
                            else
                            {
                                _sms.send_sms(sms.from, string.Format("Unauthorized request. {0} has been used to check in a visitor's car ", sms.from));
                                gs.UpdateLogSheet(string.Format("Unauthorized request. {0} has been used to check in a visitor's car ", sms.from), "Log");
                                log("SMS has been sent to " + sms.from + " : Unauthorized request. " + string.Format("{0} has been used to check in a visitor's car ", sms.from));
                            }
                        }
                        else if (response.Contains(SMS_in_Update))
                        {
                            if (AdministratorPhones.Contains(sms.from))
                            {

                                long timenowunix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                string carlist = "";
                                foreach (KeyValuePair<string, Visitor_Car> kvp in Visitor_Dict)
                                {
                                    long sec_passed = timenowunix - long.Parse(kvp.Value.Time);
                                    long sec_left = (long)(VisitorParkingLimit_hrs * 3600 - sec_passed);

                                    int hrs = 0;
                                    int min = 0;

                                    CalculateTimeLeft(sec_passed, (long)(VisitorParkingLimit_hrs * 3600), out hrs, out min);

                                    string smstime = hrs.ToString() + ":" + min.ToString();

                                    if (min < 0 || hrs < 0) smstime = "Expired " + (Math.Abs(hrs)).ToString() + " hrs " + (Math.Abs(min)).ToString() + " min ago";
                                    else smstime = "Time left: " + hrs.ToString() + " hrs " + min.ToString() + " min";

                                    carlist += kvp.Value.LicensePlate + ", # " + kvp.Value.HostUnitNumber + ", Phone: " + kvp.Value.ContactPhone + ", " + smstime + "\r";
                                }

                                _sms.send_sms(sms.from, carlist);
                                gs.UpdateLogSheet("Administrator requested an update.", "Log");
                                gs.UpdateLogSheet(carlist, "Log");
                                log("SMS has been sent to " + sms.from + " : List of registered vistor cars");
                                log(carlist);
                            }
                            else
                            {
                                _sms.send_sms(sms.from, "This request is authorized for administrators only");
                                gs.UpdateLogSheet("Unauthorized request for update.", "Log");
                                log("SMS has been sent to " + sms.from + " : This request is authorized for administrators only");
                            }
                        }
                        else if (response.Contains(SMS_in_Cancel))
                        {
                            if (Visitor_Dict.ContainsKey(sms.from))
                            {
                                // Send cancellation confirmation
                                _sms.send_sms(sms.from, SMS_out_Cancel);

                                // Log info to the log file
                                log(Visitor_Dict[sms.from].LicensePlate + " cancelled registration at " + UnixTimeStampToDateTime(double.Parse(sms.timestamp)).ToString("yyyy-MM-dd hh:mm:ss tt"));

                                // Remove a leaving visitor from the database
                                db.DeleteItem("Visitors", "LicensePlate", Visitor_Dict[sms.from].LicensePlate);

                                // Send data to database Visitor_Log
                                Visitor_Car visitor = new Visitor_Car();
                                visitor.Time = sms.timestamp;
                                visitor.Status = (int)CarStatus.Cancelled;
                                visitor.ContactPhone = sms.from;
                                visitor.LicensePlate = Visitor_Dict[sms.from].LicensePlate;
                                visitor.HostUnitNumber = Visitor_Dict[sms.from].HostUnitNumber;
                                db.WriteDataToDatabase(new Visitor_Car[] { visitor }, "Visitor_Log");

                                // Remove a leaving visitor from the dictionary
                                gs.UpdateLogSheet("License Plate " + Visitor_Dict[sms.from].LicensePlate + " cancelled registration", "Log");
                                log("License Plate " + Visitor_Dict[sms.from].LicensePlate + " cancelled registration");
                                Visitor_Dict.Remove(sms.from);

                                // Remove from dataGridView
                                ////int index = 0;
                                ////foreach (DataGridViewRow row in dataGridView2.Rows)
                                ////{
                                ////    if (dataGridView2.Rows[dataGridView2.Rows.Count - 1].Cells[3].Value.ToString() == sms.from)
                                ////    {
                                ////        index = row.Index;
                                ////    }
                                ////}
                                ////dataGridView2.Invoke(new Action(() => { dataGridView2.Rows.Remove(dataGridView2.Rows[index]); }));
                            }
                        }
                        else
                        {
                            _sms.send_sms(sms.from, SMS_out_InvalidEntry);
                            gs.UpdateLogSheet("Invalid entry SMS has been sent to " + sms.from, "Log");
                            log("Invalid entry SMS has been sent to " + sms.from);
                        }
                    }
                }
            }
            finally
            {
                timer2.Change(SMS_TimerInterval_sec * 1000, Timeout.Infinite);
            }
        }

        
        public IList<IList<Object>> GenerateVisitorData()
        {
            List<IList<Object>> objNewRecords = new List<IList<Object>>();
            foreach (KeyValuePair<string, Visitor_Car> kvp in Visitor_Dict)
            {
                IList<Object> obj = new List<Object>();
                obj.Add(UnixTimeStampToDateTime(double.Parse(kvp.Value.Time)).ToString("yyyy-MM-dd hh:mm:ss tt"));
                obj.Add(kvp.Value.LicensePlate);
                obj.Add(kvp.Value.HostUnitNumber);
                obj.Add(kvp.Value.ContactPhone);
                objNewRecords.Add(obj);
            }
            return objNewRecords;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public ViolationType CheckForViolation(string LicensePlate, Camera cam, out string type)
        {
            object obj = new object();
            lock (obj)
            {
                type = "unknown";
                string VisitorKey = "";
                string ResidentKey = "";

                ViolationType VT = ViolationType.NONE;

                // 1. Check for Visitor's overtime

                foreach (KeyValuePair<string, Visitor_Car> kvp in Visitor_Dict)
                {
                    if (kvp.Value.LicensePlate == LicensePlate)
                    {
                        type = "visitor";
                        VisitorKey = kvp.Key;
                    }
                }
                foreach (KeyValuePair<string, Resident_Car> kvp in Resident_Dict)
                {
                    if (kvp.Value.LicensePlate == LicensePlate)
                    {
                        type = "resident";
                        ResidentKey = kvp.Key;
                    }
                }

                switch (type)
                {
                    case "unknown":
                        VT = ViolationType.UNKNOWN_CAR;
                        break;
                    case "visitor":
                        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(Visitor_Dict[VisitorKey].Time) > VisitorParkingLimit_hrs * 3600) VT = ViolationType.VISITOR_OVERTIME;
                        else if (cam.SpotType == "HOA") VT = ViolationType.VISITOR_HOA_SPOT;
                        break;
                    case "resident":
                        if (cam.SpotType == "VISITOR") VT = ViolationType.RESIDENT_VISITOR_SPOT;
                        if (Resident_Dict[ResidentKey].RentedSpotNumber == 0) VT = ViolationType.RESIDENT_HOA_SPOT_NoCONTRACT;
                        break;
                }


                return VT;
            }
        }
        ////void UpdateGoogleSheets(ParkingLot pl)
        ////{

        ////    List<IList<Object>> objNewRecords = new List<IList<Object>>();
        ////    foreach (CameraShot camshot in pl.CamShot_List)
        ////    {
        ////        IList<Object> obj = new List<Object>();
        ////        obj.Add(camshot.CameraName);
        ////        obj.Add(camshot.CarType);
        ////        obj.Add(camshot.LicensePlate);
        ////        obj.Add(camshot.LicensePlateState);
        ////        obj.Add(camshot.Confidence);
        ////        obj.Add(camshot.ViolationType);
        ////        obj.Add(camshot.HoursParked);
        ////        objNewRecords.Add(obj);
        ////    }
        ////    gs.UpdateSheet(gs.GenerateEmptyData(), "Parking Lot");
        ////    gs.UpdateSheet(objNewRecords, "Parking Lot");
        ////}
        void UpdateGoogleSheets()
        {
            List<IList<Object>> objNewRecords = new List<IList<Object>>();
            foreach (KeyValuePair<string, Visitor_Car> car in Visitor_Dict)
            {
                IList<Object> obj = new List<Object>();
                obj.Add(UnixTimeStampToDateTime(double.Parse(car.Value.Time)).ToString("yyyy-MM-dd hh:mm:ss tt"));
                obj.Add(car.Value.LicensePlate);
                obj.Add(car.Value.HostUnitNumber);
                obj.Add(car.Value.ContactPhone);
                objNewRecords.Add(obj);
            }
            gs.UpdateSheet(gs.GenerateEmptyData(), "Visitors");
            gs.UpdateSheet(objNewRecords, "Visitors");
        }
        ////public void DataGridView2_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        ////{
        ////    foreach (DataGridViewRow row in ((DataGridView)sender).SelectedRows)
        ////    {
        ////        _sms.send_sms(row.Cells[3].Value.ToString(), string.Format("License plate {0} has been checked out. You will need to check in again when you park in the Visitor area of The Belmont HOA.", row.Cells[1].Value.ToString()));

        ////        // Log info to the log file
        ////        log(Visitor_Dict[row.Cells[3].Value.ToString()].LicensePlate + " has been manually deleted from the Visitors List at " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));

        ////        // Remove a leaving visitor from the database
        ////        db.DeleteItem("Visitors", "LicensePlate", Visitor_Dict[row.Cells[3].Value.ToString()].LicensePlate);

        ////        //Remove a leaving visitor from Google Sheets
        ////        gs.UpdateLogSheet("License Plate " + Visitor_Dict[row.Cells[3].Value.ToString()].LicensePlate + " has been manually deleted from the Visitors List", "Log");
        ////        log("License Plate " + Visitor_Dict[row.Cells[3].Value.ToString()].LicensePlate + " has been manually deleted from the Visitors List");

        ////        // Remove a leaving visitor from the dictionary
        ////        Visitor_Dict.Remove(row.Cells[3].Value.ToString());

        ////        //Update Visitors Page in Google Sheets
        ////        UpdateGoogleSheets();
        ////    }
        ////}

        public bool CheckIfUnitExists(string Unit)
        {
            bool status = false;

            foreach (string unit in UnitList)
            {
                if (unit == Unit)
                {
                    status = true;
                }
            }

            return status;
        }

        public bool CheckIfResident(string LicensePlate)
        {
            bool status = false;

            foreach (KeyValuePair<string, Resident_Car> kvp in Resident_Dict)
            {
                if (kvp.Value.LicensePlate == LicensePlate)
                {
                    status = true;
                }
            }

            return status;
        }
        public bool CheckIfVisitor(string LicensePlate)
        {
            bool status = false;
            foreach (KeyValuePair<string, Visitor_Car> kvp in Visitor_Dict)
            {
                if (kvp.Value.LicensePlate == LicensePlate)
                {
                    status = true;
                }
            }
            return status;
        }

        public string FindVisitorPhoneByLP(string LicensePlate)
        {
            string phone = "";
            foreach (KeyValuePair<string, Visitor_Car> kvp in Visitor_Dict)
            {
                if (kvp.Value.LicensePlate == LicensePlate)
                {
                    phone = kvp.Key;
                }
            }
            return phone;
        }

        public void CalculateTimeLeft(long secpassed, long seclimit, out int hrs, out int min)
        {
            long secleft = seclimit - secpassed;

            if (secleft >= 0)
            {
                hrs = (int)Math.Floor((double)secleft / 3600);
                min = (int)Math.Floor((double)(secleft - hrs * 3600) / 60);
            }
            else
            {
                hrs = (int)Math.Ceiling((double)secleft / 3600);
                min = (int)Math.Ceiling((double)(secleft - hrs * 3600) / 60);
            }
        }
    }
}
