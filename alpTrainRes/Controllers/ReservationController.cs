using alpTrainRes.Requests;
using alpTrainRes.Response;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


[ApiController]
[Route("api/[controller]")]
public class ReservationController : ControllerBase
{

    [HttpPost]
    public IActionResult MakeReservation([FromBody] JObject request)
    {
        
        ReservationRequest tt = ProcessRequest(request);
        ReservationResult placePassengersResult = PlacePassengers(tt);
        string parseData = "";
        Tren tren = new Tren();
        var t = new ReservationRequest();
        var a = new ReservationBaseResponse();
        a.YolcuVagonBilgisi = new List<yerlesimAyrinti>();
        
        try
        {
            #region Ayrıştırma,dönüştürme, atama
            
            JObject json = JObject.Parse(request.ToString());
            JObject trenJson = json["Tren"].ToObject<JObject>();
            tren.Ad = (string)trenJson["Ad"];
            tren.Vagonlar = trenJson["Vagonlar"].ToObject<List<Vagon>>();
            t.Tren = tren;
            t.RezervasyonYapilacakKisiSayisi = (int)json["RezervasyonYapilacakKisiSayisi"];
            t.KisilerFarkliVagonlaraYerlestirilebilir = (bool)json["KisilerFarkliVagonlaraYerlestirilebilir"];

            
            if (request != null)
            {
                t.Tren.Ad = (string)request["Ad"];
                t.Tren.Vagonlar = new List<Vagon>();
                var vagonBilgileri = trenJson["Vagonlar"].ToObject<List<Vagon>>();
                t.Tren.Vagonlar.AddRange(vagonBilgileri);
                t.KisilerFarkliVagonlaraYerlestirilebilir = (bool)request["KisilerFarkliVagonlaraYerlestirilebilir"];
                t.RezervasyonYapilacakKisiSayisi = (int)request["RezervasyonYapilacakKisiSayisi"];
                int sayi = t.Tren.Vagonlar.Count;
            }

            
            Boolean isSeatAvailable = false;
            int sayac = 0;
            int vagonSayisi = 0;
            vagonSayisi = t.Tren.Vagonlar.Count;
            ReservationResult _placePassengersResult = PlacePassengers(t);
            #endregion
            
            #region If-Else
            if (_placePassengersResult.RezervasyonYapilabilir == true)
            {
                
                foreach (var kvp in _placePassengersResult.YolcuVagonBilgisi)
                {
                    a.YolcuVagonBilgisi.Add(new yerlesimAyrinti
                    {
                        vagonAdi = kvp.Value,
                        kisiSayisi = kvp.Key 
                    });
                }
                
                var yerlesimAyrintiDictionary =
                    ConvertYerlesimAyrintiToDictionary(_placePassengersResult.YerlesimAyrinti);
                foreach (var kvp in yerlesimAyrintiDictionary)
                {
                    a.YolcuVagonBilgisi.Add(new yerlesimAyrinti
                    {
                        vagonAdi = kvp.Value, 
                        kisiSayisi = kvp.Key 
                    });
                }
                

                var sonuc = PlacePassengers(t);

                if (t.KisilerFarkliVagonlaraYerlestirilebilir == false)
                {
                    foreach (var item in t.Tren.Vagonlar)
                    {
                        var sonucum = JsonConvert.SerializeObject(sonuc);
                        if ((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) > 0) //limit aşılmamış
                        {
                            isSeatAvailable = true;
                            //vagona yerleştir ve değerleri dön

                            return Ok(sonucum);

                        }

                        if ((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) <= 0 &&
                            vagonSayisi == 1) //limit aşıldı ve tek vagon ise
                        {
                            sonuc.RezervasyonYapilabilir = false;
                            return Ok(sonucum);
                        }

                        if ((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) <= 0 &&
                            vagonSayisi > 1) //limit aşıldı ve 1 den çok vagon var ise 
                        {
                            // demekki 1 den çok var ama temelde farklı vagonlara yerleştirilemez
                            // bu yüzden tüm kişilere tek vagonda yer bulmak gerekir
                            // sayacı 1 arttır ve bir sonraki vagona bak, yer varsa rezervasyon yap yoksa vagon sayııs tamamlanana kadar devam et
                            // eğer süreç başarılı tamamlanamazsa rez yapılmaz
                            break;
                        }
                    }
                }

                if (t.KisilerFarkliVagonlaraYerlestirilebilir == true)
                {
                    foreach (var item in t.Tren.Vagonlar)
                    {
                        if (((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) <= 0) &&
                            vagonSayisi == 1) //limit aşılmış ve tek vagon var ise
                        {
                            var sonucum = JsonConvert.SerializeObject(sonuc);
                            return Ok(sonucum);
                        }

                        if (((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) <= 0) &&
                            vagonSayisi > 1) //limit aşılmış ama 1 den çok vagon var 
                        {
                            var yerlestirilenSayisi = sonuc.YolcuVagonBilgisi.Count;
                            string sonucum = JsonConvert.SerializeObject(sonuc);
                            Console.WriteLine(parseData);
                            if (yerlestirilenSayisi == t.RezervasyonYapilacakKisiSayisi)
                            {
                                Console.WriteLine("Rezervasyon İşlemi Başarılı" + sonucum);
                                return Ok(sonucum);
                            }
                            else
                            {
                                if (sonuc.YolcuVagonBilgisi != null)
                                {
                                    sonuc.RezervasyonYapilabilir = true;
                                    var s = JsonConvert.SerializeObject(sonuc);
                                    return Ok(s);
                                }
                                sonuc.RezervasyonYapilabilir = false;
                                var nesne = JsonConvert.SerializeObject(sonuc);
                    
                                return Ok("Rezervasyon yapılamadı, yetersiz vagon kapasitesi." + nesne);
                            }
                        }

                        if ((item.Kapasite) * (0.7) - (item.DoluKoltukAdet) > 0) // limit aşılmamış
                        {
                            string sonucum = JsonConvert.SerializeObject(sonuc);
                            return Ok(sonucum);
                        }
                        else
                        {
                            string sonucum = JsonConvert.SerializeObject(sonuc);
                            Console.WriteLine("Hatalı giriş yapıldı");
                            return BadRequest(sonucum);
                        }
                    }
                }
                
                
                var result = new ReservationResult();
                placePassengersResult = PlacePassengers(t);

                if (placePassengersResult.RezervasyonYapilabilir)
                {
                    Dictionary<string, string> yeniYolcuVagonBilgisi = placePassengersResult.YolcuVagonBilgisi
                        .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                    var yolcuVagonBilgisi = placePassengersResult.YolcuVagonBilgisi;

                   
                    result.RezervasyonYapilabilir = placePassengersResult.RezervasyonYapilabilir;
                    result.YolcuVagonBilgisi = placePassengersResult.YolcuVagonBilgisi;
                    result.YerlesimAyrinti = placePassengersResult.YerlesimAyrinti;
                    
                    var response = new
                    {
                        RezervasyonYapilabilir = placePassengersResult.RezervasyonYapilabilir,
                        YolcuVagonBilgisi = placePassengersResult.YolcuVagonBilgisi,
                        YerlesimAyrinti = placePassengersResult.YerlesimAyrinti
                    };
                    return Ok(response);
                }
                else
                {
                    result.RezervasyonYapilabilir = false;
                    var nesne = JsonConvert.SerializeObject(result);
                    
                    return BadRequest("Rezervasyon yapılamadı, yetersiz vagon kapasitesi." + nesne);
                }
                
            }
            if (_placePassengersResult.RezervasyonYapilabilir == false)
            {
                var sonuc = PlacePassengers(t);
                var sonucum = JsonConvert.SerializeObject(sonuc);
                sonuc.RezervasyonYapilabilir = false;
                return Ok(sonucum);
            }
            #endregion
        }

        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return Ok();
    }
    

    #region Metodlar

    private static ReservationResult PlacePassengers(ReservationRequest request)
{
    int remainingPassengers = request.RezervasyonYapilacakKisiSayisi;
    var vagonlar = request.Tren.Vagonlar
        .OrderByDescending(v => (double)(v.Kapasite - v.DoluKoltukAdet) / v.Kapasite).ToList();
    
    var result = new ReservationResult
    {
        RezervasyonYapilabilir = true,
        YolcuVagonBilgisi = new Dictionary<int, string>(),
        YerlesimAyrinti = new List<yerlesimAyrinti>()
    };
    
    var passengers = Enumerable.Range(1, request.RezervasyonYapilacakKisiSayisi).ToList();

    foreach (var passenger in passengers)
    {
        bool placed = false;
        bool canReserve = false;

        foreach (var vagon in vagonlar)
        {
            if (vagon.Kapasite == 0 && vagon.DoluKoltukAdet < 0)
            {
                result.RezervasyonYapilabilir = false;
                result.YerlesimAyrinti = new List<yerlesimAyrinti>();
                return result;
            }

            var (passengerResult, passengerParseData) = PlacePassengerInVagon(new List<Vagon> { vagon }, passenger);

            result.RezervasyonYapilabilir = passengerResult.RezervasyonYapilabilir;
            result.YolcuVagonBilgisi = passengerResult.YolcuVagonBilgisi;

            if (vagon.DoluKoltukAdet < (vagon.Kapasite) * (0.7))
            {
                canReserve = true;

                var passengerVagonBilgisi = new Dictionary<int, string>(result.YolcuVagonBilgisi);
                if (!result.YolcuVagonBilgisi.ContainsKey(passenger))
                {
                    result.YolcuVagonBilgisi[passenger] = vagon.VagonAd;
                }

                var yerlesim = new yerlesimAyrinti
                {
                    vagonAdi = vagon.VagonAd,
                    kisiSayisi = 1 // Her yolcu 1 kişilik yer kaplar
                };
                result.YerlesimAyrinti.Add(yerlesim);

                vagon.DoluKoltukAdet++;
                placed = true;

                result.YolcuVagonBilgisi = passengerVagonBilgisi;
                remainingPassengers--;
                if (remainingPassengers == 0)
                {
                    return result;
                }
            }
        }

        if (!result.RezervasyonYapilabilir)
        {
            result.RezervasyonYapilabilir = true;
        }

        result.RezervasyonYapilabilir = canReserve;
        if (!placed)
        {
            result.RezervasyonYapilabilir = false;
            result.YerlesimAyrinti = new List<yerlesimAyrinti>();
            string parseData = JsonConvert.SerializeObject(result);
            return result;
        }

        return result;
    }

    return result;
}
    
    private ReservationRequest ProcessRequest(JObject request)
    {
        ReservationRequest t = new ReservationRequest();

        if (request != null)
        {
            JObject json = JObject.Parse(request.ToString());
            JObject trenJson = json["Tren"].ToObject<JObject>();

            t.Tren = new Tren
            {
                Ad = (string)trenJson["Ad"],
                Vagonlar = trenJson["Vagonlar"].ToObject<List<Vagon>>()
            };

            t.RezervasyonYapilacakKisiSayisi = (int)json["RezervasyonYapilacakKisiSayisi"];
            t.KisilerFarkliVagonlaraYerlestirilebilir = (bool)json["KisilerFarkliVagonlaraYerlestirilebilir"];
        }

        return t;
    }

        private static (ReservationResult, string) PlacePassengerInVagon(List<Vagon> vagonlar, int passenger)
        {
            var result = new ReservationResult()
            {
                RezervasyonYapilabilir = false,
                YerlesimAyrinti = new List<yerlesimAyrinti>(),
                YolcuVagonBilgisi = new Dictionary<int, string>()
            };

            bool placed = false;

            foreach (var vagon in vagonlar)
            {
                if (vagon.DoluKoltukAdet < (vagon.Kapasite) * (0.7))
                {
                    if (!result.YolcuVagonBilgisi.ContainsKey(passenger))
                    {
                        result.YolcuVagonBilgisi[passenger] = vagon.VagonAd;
                    }

                    var yerlesim = new yerlesimAyrinti
                    {
                        vagonAdi = vagon.VagonAd,
                        kisiSayisi = 1
                    };
                    result.YerlesimAyrinti.Add(yerlesim);

                    vagon.DoluKoltukAdet++;
                    placed = true;

                    return (result, null);
                }
            }

            if (!placed)
            {
                result.RezervasyonYapilabilir = false;
                result.YerlesimAyrinti = new List<yerlesimAyrinti>();
                string parseData = JsonConvert.SerializeObject(result);
                return (result, parseData);
            }

            return (result, null);
        }

        private Dictionary<int, string> ConvertYerlesimAyrintiToDictionary(List<yerlesimAyrinti> yerlesimAyrinti)
        {
            var dictionary = new Dictionary<int, string>();

            foreach (var yerlesim in yerlesimAyrinti)
            {
                dictionary[yerlesim.kisiSayisi] = yerlesim.vagonAdi;
            }

            return dictionary;
        }

    #endregion
        
    }
