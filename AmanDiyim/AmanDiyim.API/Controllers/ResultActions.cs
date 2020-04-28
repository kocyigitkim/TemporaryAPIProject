using System.Collections.Generic;

namespace AmanDiyim.API.Controllers
{
    public class ResultActions
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<AmanDiyimAction> Actions { get; set; }
    }
}