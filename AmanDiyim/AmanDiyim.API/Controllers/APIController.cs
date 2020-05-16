using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AmanDiyim.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class APIController : ControllerBase
    {
        static CultureInfo enCulture = CultureInfo.GetCultureInfo("en-US");
        private readonly ILogger<APIController> _logger;

        public IOptions<MyConfig> MyConfig { get; }

        public APIController(ILogger<APIController> logger, IOptions<MyConfig> myConfig)
        {
            _logger = logger;
            this.MyConfig = myConfig;
        }

        /*
            Mevcut konumdan 1KM mesafe uzaklıkta olan kaza bildirimlerini görüntüler.
        */
        [HttpPost("query")]
        [HttpGet("query")]
        public IActionResult Query()
        {
            try
            {
                /*
                    Method a gelen json isteği deserialize edilir.
                */
                var req = HttpContext.Request;

                if (req.ContentType.ToLower(enCulture) != "application/json")
                {
                    throw new Exception("Content-Type application/json olmalı");
                }

                string bodyStr;

                using (StreamReader reader
                          = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
                {
                    var t = reader.ReadToEndAsync();
                    t.Wait();
                    bodyStr = t.Result;
                }

                //Database bağlantısı yapılır.

                ResultActions result = new ResultActions();
                MySqlCompiler qcompiler;
                var query = new QueryFactory(new MySqlConnection(MyConfig.Value.Database), qcompiler = new MySqlCompiler());
                //Longtitude, Latitude bilgisi gelen requestten parse edilir
                dynamic reqParams = JsonConvert.DeserializeObject(bodyStr);
                decimal lng = reqParams.longtitude;
                decimal lat = reqParams.latitude;
                //Api Key kontrolü yapılır.
                try
                {
                    string key = HttpContext.Request.Headers["apiKey"].ToString();
                    if (key != MyConfig.Value.Key)
                    {
                        return new ObjectResult(new ResultActions()
                        {
                            Success = false,
                            ErrorMessage = "Kimlik hatalı",
                            Actions = null
                        });
                    }
                }
                catch (Exception)
                {
                    //Api Key hatalı ise aşağıdaki hata mesajı görüntülenir.
                    return new ObjectResult(new ResultActions()
                    {
                        Success = false,
                        ErrorMessage = "Kimlik hatalı",
                        Actions = null
                    });
                }

                //1KM mesafe bilgileri hesaplanır.

                decimal lngKM1 = 0.004073m;
                decimal latKM1 = 0.010857m;

                decimal lngLow = lng - lngKM1 / 2m;
                decimal latLow = lat - latKM1 / 2m;

                decimal lngHigh = lng + lngKM1 / 2m;
                decimal latHigh = lat + latKM1 / 2m;

                //Database üzerinden kaza bildirimleri alınır.

                var q = query.FromQuery(new SqlKata.Query("actions")).WhereBetween<decimal>("Lng", lngLow, lngHigh).WhereBetween<decimal>("Lat", latLow, latHigh);
                var r = qcompiler.Compile(q);

                var results = q.Get().ToArray();
                //Alınan sonuçlar map edilir.
                result.Actions = (from item in results
                                  select new AmanDiyimAction()
                                  {
                                      Id = item.Id,
                                      Action = item.ActionDetail,
                                      Lng = Convert.ToDecimal(item.Lng),
                                      Lat = Convert.ToDecimal(item.Lat),
                                      Type = item.ActionType
                                  }).ToList();

                result.Success = true;
                //Map edilen sonuçlar response olarak dönülür.
                return new ObjectResult(result);

            }
            catch (Exception)
            {
                //Serviste bir hata oluştuğunda aşağıdaki hata mesajı görüntülenir.
                return new ObjectResult(new ResultActions()
                {
                    Actions = null,
                    ErrorMessage = "İstek hatalı",
                    Success = false
                });
            }
        }
    }
}
