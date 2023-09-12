using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Labiba.Tamkeen.WebAPI.Models
{
    public class LiteDBServices
    {
        public void InsertLogRow(LogModel ModelObj)
        {
            string Today = DateTime.Now.ToString("yyyyMMdd");
            try
            {
                using (var db = new LiteDatabase(HostingEnvironment.MapPath($"~/Logs/Log_{Today}.db")))
                {

                    var Log = db.GetCollection<LogModel>($"Log_{Today}");
                    Log.Insert(ModelObj);
                }
            }
            catch
            {
            }
        }
        public class LogModel
        {
            public Guid Id { get; set; }
            public string LogType { get; set; }
            public string LogText { get; set; }
            public string ActionName { get; set; }
            public string Parameter { get; set; }
            public string ResponseFromLabiba { get; set; }
            public DateTime LogTime { get; set; }
        }
    }
}
