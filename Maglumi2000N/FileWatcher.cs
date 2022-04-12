using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Maglumi2000N
{
    public static class FileWatcher
    {
        private static FileSystemWatcher watcher = new FileSystemWatcher();
        public static AppDbContext _db = new AppDbContext();
        public static MAHDbContext mdb = new MAHDbContext();
        private static StringBuilder sb1 = new StringBuilder();
        private static StringBuilder sb2 = new StringBuilder();
        public static int Fflag = 0;

        public static void Startwatching()
        {
            FileWatcher.watcher.Path = Constants.DumpPath;
            FileWatcher.watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.LastAccess;
            FileWatcher.watcher.Filter = "*.txt";
            FileWatcher.watcher.Created += new FileSystemEventHandler(FileWatcher.OnChanged);
            FileWatcher.watcher.Deleted += new FileSystemEventHandler(FileWatcher.OnChanged);
            FileWatcher.watcher.EnableRaisingEvents = true;
        }
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string PId = "";
                string InstrumentName = "";
                string code = "";
                Thread.Sleep(2000);
                string content = File.ReadAllText(e.FullPath);
                Parse(content);



            }
            else
            {
                if (e.ChangeType != WatcherChangeTypes.Deleted)
                    return;
                Console.WriteLine("File Deleted: " + e.FullPath);
            }
        }
        public static void Parse(string data)
        {
            List<ReportItem> Reports = new List<ReportItem>();
            string SampleId = "";
            string MachineName = "";
            string code = "";
            string Pid = "";
            string value = "";
            string codde = "";
            string reportdate = "";
            string unit = "";
            string range = "";
            float TCirculate = 10;
            
            DateTime? Date = new DateTime?();
            string[] lines = data.Split(new[] {Constants.lf }, StringSplitOptions.None);
            try
            {
              foreach(var item in lines)
                {
                    var splitPipeValue = item.Split('|');
                    var lastChar = splitPipeValue[0].Substring(splitPipeValue[0].Length - 1);
                    switch (lastChar)
                    {
                        case "H":
                            
                            //model.ReportDate = splitPipeValue[13].ConvertSysmax800DateTime();
                            break;
                        case "P":
                            SampleId = splitPipeValue[4].Trim();
                            //model.PatientId = Convert.ToInt32(45);
                            break;
                        
                            break;
                        case "C":
                            break;
                        case "L":
                            break;
                        case "R":
                            if (splitPipeValue[2].Contains("^"))
                            {
                                
                                string _code = splitPipeValue[2].Split('^')[3].Trim();
                                string _name = splitPipeValue[2].Split('^')[3].Trim();
                                string _value = splitPipeValue[3].Trim();
                                if(_code=="WBC")
                                {
                                    float temp = 0;
                                    float.TryParse(_value, out temp);
                                    TCirculate = TCirculate * temp;
                                }
                                if (_code == "NE%" || _code == "LY%" || _code == "MO%" || _code == "EO%" || _code == "BA%")
                                {

                                    if (_value.Contains("."))
                                    {
                                        int decim;
                                        int frac;

                                        var sp1 = _value.Split('.')[0];
                                        var sp2 = _value.Split('.')[1];
                                        int.TryParse(sp1, out decim);
                                        int.TryParse(sp2, out frac);
                                        if (frac > 5)
                                            decim++;

                                        _value = decim.ToString();
                                        if(_value.Length==1)
                                        {
                                            _value = "0" + _value;
                                        }

                                        if (_code == "EO%")
                                        {
                                            TCirculate = TCirculate * decim;
                                        }
                                    }
                                }
                                Reports.Add(new ReportItem()
                                {
                                    Code = _code,

                                    Value = _value,
                                    Unit = splitPipeValue[4].Trim(),
                                    //Range = splitPipeValue[5].Trim()

                                });
                            }
                            // Console.WriteLine("End of Parsing");

                            break;
                    }
                }
                Reports.Add(new ReportItem()
                {
                    Code = "T.CirEos",

                    Value = TCirculate.ToString(),
                    
                    //Range = splitPipeValue[5].Trim()

                });
                SaveToDb(SampleId, Reports);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Khaise");
            }

        }

        private static void SaveToDb(string sampleId, List<ReportItem> reports)
        {
            List<ResultRecord> _resultList = new List<ResultRecord>();
            PatientRecord pr = new PatientRecord();
            pr.InstrumentName = "Nihon_CBC";
            pr.PatientId = sampleId;
            pr.ReportDate = DateTime.Now;
            _db.PatientRecords.Add(pr);
            _db.SaveChanges();
            foreach (var item in reports)
            {
                ResultRecord rr = new ResultRecord();
                rr.PatientRecordId = pr.PatientRecordId;
                rr.Code = item.Code;
                rr.Name = item.Code;
                if(item.Value.Length==1)
                {
                    item.Value = "0" + item.Value;
                }
                rr.Value = item.Value;
                rr.Unit = item.Unit;
                rr.Range = item.Range;
                //rr.PrintOrder = GetPrintOrder(item.Code, assayPrintorder);
                _db.Resultrecords.Add(rr);

                _resultList.Add(rr);
                Console.WriteLine(item.Code + "   " + item.Value);

            }
            _db.SaveChanges();
            Task.Factory.StartNew((Action)(() => Execute_DataBase(sampleId)));
        }

        private static void DeleteExistingRecord(string pid, string machineName)
        {
            Console.WriteLine("Delete :" + pid);

            var SpecificRecod = FileWatcher._db.PatientRecords.Where(x => x.PatientId == pid && x.InstrumentName == machineName).FirstOrDefault();
            {
                if (SpecificRecod != null)
                {
                    var SpecificRecordItem = FileWatcher._db.Resultrecords.Where(x => x.PatientRecordId == SpecificRecod.PatientRecordId).ToList();
                    foreach (var item in SpecificRecordItem)
                    {
                        FileWatcher._db.Resultrecords.Remove(item);
                    }
                    FileWatcher._db.PatientRecords.Remove(SpecificRecod);
                    FileWatcher._db.SaveChanges();
                }
            }

        }

        

        public static void Execute_DataBase(string Pid)
        {
            Console.WriteLine("Executing Sql for patientId: " + Pid);
            using (AppDbContext _dbContext = new AppDbContext())
            {
                var param1 = new SqlParameter("@pid1", Pid);
                var param2 = new SqlParameter("@pid2", Pid);
                _dbContext.Database.ExecuteSqlCommand(@"Exec [dbo].[spUpdateReportDefId] @pid1 ", parameters: new[] { param1 });
                Thread.Sleep(100);
                _dbContext.Database.ExecuteSqlCommand(@"Exec [dbo].[spUpdateAlkamyERPTEMPLIS]  @pid2 ", parameters: new[] { param2 });
            }
        }

        public static DateTime? ConvertMaglumi2000DateTime(string datetime)
        {


            //20 08 01 12 20 16
            var year = int.Parse("20" + datetime.Substring(0, 2));
            var month = int.Parse(datetime.Substring(2, 2));
            var day = int.Parse(datetime.Substring(4, 2));
            var hour = int.Parse(datetime.Substring(6, 2));
            var min = int.Parse(datetime.Substring(8, 2));
            var sec = int.Parse(datetime.Substring(10, 2));

            return new DateTime(year, month, day, hour, min, sec);
        }
        //public DateTime rp_date(string date)
        //{
        //    return 
        //}
    }
}
