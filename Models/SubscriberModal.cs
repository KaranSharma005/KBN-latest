using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KBN.Models
{
    public class SubscriberModal
    {
        [Required]
        public string username { get; set; }
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string password { get; set; }
        [Required]
        public string customername { get; set; }
    }

    [Table("customerData")]
    public class CustomerDetailModal
    {
        public int id { get; set; }
        public string customer_name { get; set; }
        public int sub1 { get; set; }
        public int sub2 { get; set; }
    }

    public class SubscriberPaginationModal
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string username { get; set; }
        public string createdby {get; set;}
    }

    public class SubscriberTableModal
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public string customer_name { get; set; }
        public DateTime recorded_at { get; set; }
        public string recorded_by { get; set; }
    }

    public class UpdateSubscriber
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public string? customer_name { get; set; }   
    }
}
