namespace alpTrainRes.Response;

public class ReservationBaseResponse
{
    public List<yerlesimAyrinti> YerlesimAyrinti { get; set; }
    public List<yerlesimAyrinti> YolcuVagonBilgisi { get; set; }
}

public class yerlesimAyrinti
{
    public string vagonAdi { get; set; }
    public int kisiSayisi { get; set; }
}

public class ReservationResult
{
    public bool RezervasyonYapilabilir { get; set; }
    public Dictionary<int, string> YolcuVagonBilgisi { get; set; }
    public List<yerlesimAyrinti> YerlesimAyrinti { get; set; }
}
