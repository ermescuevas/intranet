using Newtonsoft.Json;

namespace Seaboard.Intranet.Domain.Models
{
    public class Contribuyente
    {
        [JsonProperty("RGE_RUC")]
        public string RNC { get; set; }
        [JsonProperty("RGE_NOMBRE")]
        public string RazonSocial { get; set; }
        [JsonProperty("NOMBRE_COMERCIAL")]
        public string NombreComercial { get; set; }
        [JsonProperty("CATEGORIA")]
        public string Categoria { get; set; }
        [JsonProperty("REGIMEN_PAGOS")]
        public string RegimenPagos { get; set; }
        [JsonProperty("ESTATUS")]
        public string Estatus { get; set; }
    }
}
