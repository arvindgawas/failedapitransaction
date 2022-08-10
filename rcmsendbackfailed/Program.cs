using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace rcmsendbackfailed
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
          
            FaildTran();
            MissedTran();
        }
        public static async void MissedTran()
        {
            try
            {
                datafeed objdatafeed = new datafeed();
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri("https://cmslive.cms.com/ApiCMS/");

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                RCMProdDBEntities dbcon = new RCMProdDBEntities();

                var missedtrans = (from a in dbcon.BusinessCallLogs
                                   where DbFunctions.TruncateTime(a.GenDate) == DateTime.Today && a.CustomerCode == "C00051" && a.AttendBy != null
                                   && DbFunctions.TruncateTime(a.AttendDate) == DateTime.Today
                                   && !(from o in dbcon.CustomerAPICallDetails
                                        where DbFunctions.TruncateTime(o.CreatedDate) == DateTime.Today
                                        select o.CallNo)
                                       .Contains(a.CallNo)
                                   select new
                                   {
                                       a.CallNo
                                   }).AsQueryable();

                foreach (var objfailed in missedtrans)
                {
                    objdatafeed.CallNo = objfailed.CallNo;
                    objdatafeed.UserId = "9610111";
                    objdatafeed.isMobileUpdate = false;

                    HttpResponseMessage response = await client.PostAsJsonAsync("api/CustomerData/Push", objdatafeed);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var objapicalldetails = dbcon.CustomerAPICallDetails.Where(x => x.CallNo == objdatafeed.CallNo);

                        foreach (CustomerAPICallDetail objcalls in objapicalldetails)
                        {

                            CustomerAPICallDetail contextCallDetails = dbcon.CustomerAPICallDetails.Where(x => x.id == objcalls.id).FirstOrDefault();

                            contextCallDetails.ResponseStatus = "success";

                        };
                    }
                }
                dbcon.SaveChanges();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

           public static async void FaildTran()
        {
            try
            {
                RCMProdDBEntities dbcon = new RCMProdDBEntities();

                datafeed objdatafeed = new datafeed();
                var failedTrans = (from a in dbcon.CustomerAPICallDetails
                                      //where DbFunctions.TruncateTime(a.CreatedDate) == DbFunctions.AddDays(DateTime.Today,-6)
                                      where DbFunctions.TruncateTime(a.CreatedDate) == DateTime.Today && a.CustomerCode =="C00051" 
                                       && a.ResponseStatus == "error" && 
                                       (a.Response.Contains("unexpected.exception") ||
                                       a.Response.Contains("Index was outside the bounds of the array"))
                                      select new
                                      {   a.CallNo,
                                          a.UTR
                                      }).AsQueryable();
                
                 foreach (var objfailed in failedTrans)
                 {
                 
                        objdatafeed.CallNo = objfailed.CallNo;
                        objdatafeed.UserId = "9610111";
                        objdatafeed.isMobileUpdate = false;

                    using (HttpClient clientnew = new HttpClient())
                    {
                        clientnew.BaseAddress = new Uri("https://cmslive.cms.com/ApiCMS/");
                        clientnew.DefaultRequestHeaders.Accept.Clear();
                        clientnew.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage response = await clientnew.PostAsJsonAsync("api/CustomerData/Push", objdatafeed);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var objapicalldetails = dbcon.CustomerAPICallDetails.Where(x => x.CallNo == objdatafeed.CallNo);

                            foreach (CustomerAPICallDetail objcalls in objapicalldetails)
                            {
                                CustomerAPICallDetail contextCallDetails = dbcon.CustomerAPICallDetails.Where(x => x.id == objcalls.id).FirstOrDefault();
                                contextCallDetails.ResponseStatus = "success";

                            }
                        }
                    }
                  }
                  dbcon.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

   

    public class datafeed
    {
        public decimal CallNo { get; set; }
        public string UserId { get; set; }
        public Boolean isMobileUpdate { get; set; }
    }

}

/*

try
{
    //FaildTran();

    datafeed objdatafeed = new datafeed();
    HttpClient client = new HttpClient();

    client.BaseAddress = new Uri("https://cmslive.cms.com/ApiCMS/");

    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    RCMProdDBEntities dbcon = new RCMProdDBEntities();

    var missedtrans = (from a in dbcon.CustomerAPICallDetails
                           //where DbFunctions.TruncateTime(a.CreatedDate) == DbFunctions.AddDays(DateTime.Today,-6)
                       where DbFunctions.TruncateTime(a.CreatedDate) == DateTime.Today && a.CustomerCode == "C00051"
                        && a.ResponseStatus == "error" &&
                        (a.Response.Contains("unexpected.exception") ||
                        a.Response.Contains("Index was outside the bounds of the array"))
                       select new
                       {
                           a.CallNo,
                           a.UTR
                       }).AsQueryable();



    foreach (var objfailed in missedtrans)
    {
        objdatafeed.CallNo = objfailed.CallNo;
        objdatafeed.UserId = "9610111";
        objdatafeed.isMobileUpdate = false;

        HttpResponseMessage response = await client.PostAsJsonAsync("api/CustomerData/Push", objdatafeed);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var objapicalldetails = dbcon.CustomerAPICallDetails.Where(x => x.CallNo == objdatafeed.CallNo);

            foreach (CustomerAPICallDetail objcalls in objapicalldetails)
            {

                CustomerAPICallDetail contextCallDetails = dbcon.CustomerAPICallDetails.Where(x => x.id == objcalls.id).FirstOrDefault();

                contextCallDetails.ResponseStatus = "success";

            };
        }
    }
    dbcon.SaveChanges();

}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

*/