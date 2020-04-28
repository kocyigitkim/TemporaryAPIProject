namespace AmanDiyim.API.Controllers
{
    public class AmanDiyimAction
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public decimal Lng { get; set; }
        public decimal Lat { get; set; }
        public string Type { get; set; }
    }
}