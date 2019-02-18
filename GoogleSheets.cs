using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ALPR_Core
{
    public class GoogleSheets
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.Spreadsheets }; // static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "TimeSheetUpdation By Cybria Technology";

        private readonly object gsLock = new object();

        public void UpdateSheet(IList<IList<Object>> data, string SheetName)
        {
            try
            {
                Monitor.Enter(gsLock);

                UserCredential credential;

                using (var stream =
                    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    string credPath = System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.Personal);


                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                    Console.WriteLine("Credential file saved to: " + credPath);
                }

                // Create Google Sheets API service.
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                String spreadsheetId2 = "16HWTcJg5mFDbFch2HIYl_OI5xpe9FxzFeNrFQv-k7cU";
                String range2 = SheetName + "!A2";  // update cell F5 
                ValueRange valueRange = new ValueRange();
                valueRange.MajorDimension = "ROWS";//"ROWS";//COLUMNS

                //var oblist = new List<object>() { "1" };
                //oblist.Add("2");
                valueRange.Values = data;//new List<IList<object>> { oblist };

                SpreadsheetsResource.ValuesResource.UpdateRequest update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId2, range2);
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                UpdateValuesResponse result2 = update.Execute();

                Console.WriteLine("done!");
            }
            catch
            {
            }
            finally
            {
                Monitor.Exit(gsLock);
            }

        }
        public void UpdateLogSheet(string message, string SheetName)
        {
            try
            {
                Monitor.Enter(gsLock);


                IList<Object> obj = new List<Object>() { DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") };
                obj.Add(message);
                List<IList<Object>> data = new List<IList<Object>>();
                data.Add(obj);

                UserCredential credential;

                using (var stream =
                    new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    string credPath = System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.Personal);


                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                    //Console.WriteLine("Credential file saved to: " + credPath);
                }

                // Create Google Sheets API service.
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                String spreadsheetId2 = "16HWTcJg5mFDbFch2HIYl_OI5xpe9FxzFeNrFQv-k7cU";
                String range2 = SheetName + "!A1";  // update cell F5 
                ValueRange valueRange = new ValueRange();
                valueRange.MajorDimension = "ROWS";//"ROWS";//COLUMNS

                //var oblist = new List<object>() { "1" };
                //oblist.Add("2");
                valueRange.Values = data;//new List<IList<object>> { oblist };

                SpreadsheetsResource.ValuesResource.AppendRequest request =
                   service.Spreadsheets.Values.Append(new ValueRange() { Values = data }, spreadsheetId2, range2);
                request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var response = request.Execute();
            }

            catch
            {
            }
            finally
            {
                Monitor.Exit(gsLock);
            }

        }
        public IList<IList<Object>> GenerateEmptyData()
        {
            List<IList<Object>> objNewRecords = new List<IList<Object>>();
            IList<Object> obj = new List<Object>();
            for (int i = 0; i < 10; i++) obj.Add("");
            for (int i = 0; i < 50; i++) objNewRecords.Add(obj);
            return objNewRecords;
        }
    }
}