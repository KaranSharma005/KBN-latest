using AspNetCoreHero.ToastNotification.Abstractions;
using Dapper;
using KBN.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace KBN.RepoHelpers
{

    public class LinkHelper
    {
        private readonly string _connectionString;
        private readonly INotyfService _notyf;
        public LinkHelper(
            IConfiguration config,
            INotyfService notyf
        )
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _notyf = notyf;
        }

        public void LinkOne(LinkModal modal, string createdBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    int didId = connection.QuerySingleOrDefault<int>("select id from did_main where didno = @did and isVoid = '1'", new { did = long.Parse(modal.didNo) });
                    if (didId != 0)
                    {
                        connection.Execute("insert into didSubscriberMap(did_id, sub_id,linked_by) values(@didId, @subId, @createdBy)", new { didId, subId = modal.sub_id, createdBy });
                        string subName = connection.QuerySingleOrDefault<string>("select username from subscriber where id = @id", new { id = modal.sub_id });
                        if (subName != null)
                        {
                            connection.Execute("insert into DBaliases(username, alias_username, recorded_by) values(@subName, @did, @createdBy)", new {subName, did = long.Parse(modal.didNo), createdBy});
                        }
                    }
                }
                _notyf.Success("Linked Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<PaginatedLinkModal> GetPaginatedLinks(LinkPaginationModal modal)
        {
            try
            {
                LinkPaginationModal mdl = new LinkPaginationModal
                {
                    did = modal.did == 0 ? null : modal.did,
                    subscriber = string.IsNullOrWhiteSpace(modal.subscriber) ? null : modal.subscriber,
                    pageNumber = modal.pageNumber,
                    pageSize = modal.pageSize == 0 ? 10 : modal.pageSize,
                };
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"select a.didNo, c.id ,b.username, c.linked_at, c.linked_by, c.unlink_at from did_main a inner join didSubscriberMap c
                            on a.id = c.did_id
                            inner join subscriber b
                            on c.sub_id = b.id
                            where c.is_void = '0' and a.isvoid = '1' and b.is_void = '0'
                            and (@did is null or a.didno = @did)
                            and (@subscriber is null or b.username like '%' + @subscriber + '%')
                            ORDER BY a.id
                            OFFSET (@PageNumber * @PageSize) ROWS
                            FETCH NEXT @PageSize ROWS ONLY";
                    return connection.Query<PaginatedLinkModal>(sql, new { did = mdl.did, subscriber = mdl.subscriber, PageNumber = mdl.pageNumber, PageSize = mdl.pageSize }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void UnlinkOne(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute("update didSubscriberMap set is_void = '1', unlink_at = @time where id = @id", new { id, time = DateTime.Now });
                    int sub_id = connection.QuerySingleOrDefault<int>("select sub_id from didSubscriberMap where id = @id", new { id });
                    string username = connection.QuerySingleOrDefault<string>("select username from subscriber where id = @id", new { id = sub_id });
                    connection.Execute("update DBaliases set deleted_at = @time, is_void = '1' where username = @name", new { time = DateTime.Now, name = username });
                }
                _notyf.Success("Unlink DID");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public (bool DidExists, bool SubExists) IsAlreadyLinked(LinkModal modal)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    int didId = connection.QuerySingleOrDefault<int>("select id from did_main where didno = @did and isVoid = '1'", new { did = long.Parse(modal.didNo) });
                    if (didId != 0)
                    {
                        string sql = @"
                                SELECT 
                                MAX(CASE WHEN did_id = @didId THEN 1 ELSE 0 END) AS DidExists,
                                MAX(CASE WHEN sub_id = @subId THEN 1 ELSE 0 END) AS SubExists
                                FROM didSubscriberMap
                                WHERE is_void = '0'";
                        var result = connection.QuerySingle<(bool DidExists, bool SubExists)>(sql, new { didId, subId = modal.sub_id });
                        return result;
                    }
                }
                return (false, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, false);
            }
        }

        public List<SelectListItem> SubscriberList()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    List<DropdownModal> allSubscriber = connection.Query<DropdownModal>("select id, username from subscriber where is_void = '0'").ToList();
                    List<int> assigned = connection.Query<int>("select sub_id from didSubscriberMap where is_void = '0'").ToList();

                    List<SelectListItem> subscriberList = new List<SelectListItem>();
                    foreach (var subscriber in allSubscriber)
                    {
                        if (!assigned.Contains(subscriber.id))
                        {
                            subscriberList.Add(new SelectListItem { Text = subscriber.username, Value = subscriber.id.ToString() });
                        }
                    }
                    return subscriberList;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public bool IsValid(string id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    int didNo = connection.QuerySingleOrDefault<int>("select id from did_main where didNo = @id and isVoid = '1'", new { id = long.Parse(id) });
                    if (didNo == 0)
                        return false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        public int GetTotalLinks()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.ExecuteScalar<int>("select count(*) from didSubscriberMap where is_void = '0'");  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public List<DIDSubscriberAliasModal> GetDIDSubscriberAlias(PaginatedAliasModal modal)
        {
            try
            {
                PaginatedAliasModal mdl = new PaginatedAliasModal
                {
                    did = modal.did == 0 ? null : modal.did,
                    username = string.IsNullOrWhiteSpace(modal.username) ? null : modal.username,
                    pageNumber = modal.pageNumber,
                    pageSize = modal.pageSize == 0 ? 10 : modal.pageSize,
                };
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.Query<DIDSubscriberAliasModal>(@"select id, username, alias_username, recorded_by, recorded_at from DBaliases where is_void = '0'
                                                    and (@did is null or alias_username = @did)
                                                    and (@name is null or username like '%' + @name + '%')
                                                    ORDER BY id
                                                    OFFSET (@PageNumber * @PageSize) ROWS
                                                    FETCH NEXT @PageSize ROWS ONLY
                                                ", new { name = mdl.username, did = mdl.did, PageNumber = mdl.pageNumber, PageSize = mdl.pageSize}).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
