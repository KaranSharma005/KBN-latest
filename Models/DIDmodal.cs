using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KBN.Models
{
    public class DIDmodal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Range(10000000000, 99999999999, ErrorMessage = "Number must be exactly 11 digits.")]
        public long didNo { get; set; }
        public string city { get; set; }
        public bool isVoid { get; set; } = true;
        public string country { get; set; }
        public string numberType { get; set; }
        public string created_by { get; set; }
        public string username { get; set; }
    }


    public class DIDEntry
    {
        public long DID { get; set; }
        public string Country { get; set; }
        public string City { get; set; }

        [JsonPropertyName("Number Type")]
        public string NumberType { get; set; }
    }

    public class UpdateDidModal
    {
        public int Id { get; set; }
        public long? DID { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
    }   

    public class PaginationModal
    {
        public long? did {  get; set; }
        public string city { get; set; }

        public string country { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
    }
}
