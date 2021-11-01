using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Data.Entity;

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
            try
            {
                datafeed objdatafeed = new datafeed();
                HttpClient client = new HttpClient();

                //client.BaseAddress = new Uri("http://localhost:52514/");

                client.BaseAddress = new Uri("https://localhost:44314/");

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                RCMProdDBEntities dbcon = new RCMProdDBEntities();

                var failedTrans = (from a in dbcon.CustomerAPICallDetails
                                  where DbFunctions.TruncateTime(a.CreatedDate) == DbFunctions.AddDays(DateTime.Today,-6)
                                   //where DbFunctions.TruncateTime(a.CreatedDate) == DateTime.Today
                                   && a.ResponseStatus != "success"
                                  select new
                                  {   a.CallNo,
                                      a.UTR
                                  }).AsQueryable();
                
                foreach (var objfailed in failedTrans)
                {
                    objdatafeed.CallNo = objfailed.CallNo;
                    objdatafeed.UserId = "9610976";
                    objdatafeed.isMobileUpdate = false;
                    
                    HttpResponseMessage response = await client.PostAsJsonAsync("/api/CustomerData/Push", objdatafeed);

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
    }
    public class datafeed
    {
        public decimal CallNo { get; set; }
        public string UserId { get; set; }
        public Boolean isMobileUpdate { get; set; }
    }

}
