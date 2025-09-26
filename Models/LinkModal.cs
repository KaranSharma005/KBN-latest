namespace KBN.Models
{
    public class LinkModal
    {
        public string didNo { get; set; }
        public int sub_id { get; set; }
    }

    public class LinkPaginationModal
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public long? did { get; set; }
        public string subscriber { get; set; }
    }

    public class PaginatedLinkModal
    {
        public int id { get; set; }
        public long didNo { get; set; }
        public string username { get; set; }
        public DateTime linked_at { get; set; }
        public DateTime? unlink_at { get; set; }
        public string linked_by { get; set; }
    }

    public class DropdownModal
    {
        public int id { get; set; }
        public string username { get; set; }
    }

    public class DIDSubscriberAliasModal
    {
        public int id { get; set; }
        public string username { get; set; }
        public string alias_username { get; set; }
        public string recorded_at { get; set; }
        public string recorded_by { get; set; }
    }

    public class PaginatedAliasModal
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string username { get; set; }
        public long? did { get; set; }
    }
}
