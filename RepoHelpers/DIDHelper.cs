using AspNetCoreHero.ToastNotification.Abstractions;
using Dapper;
using KBN.Data;
using KBN.Models;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Linq;
using Z.Dapper.Plus;
using static NuGet.Packaging.PackagingConstants;

namespace KBN.RepoHelpers
{
    public class DIDHelper
    {
        private readonly string _connectionString;
        private readonly INotyfService _notyf;
        public DIDHelper(
            IConfiguration config,
            INotyfService notyf
        )
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _notyf = notyf;
        }

        public List<long> GenerateRange(long start, long end)
        {
            try
            {
                List<long> range = new List<long>();
                for (long i = start; i <= end; i++)
                {
                    range.Add(i);
                }
                return range;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void AddDids(List<DIDEntry> enteries, string email)
        {
            try
            {
                var uniqueIds = enteries.DistinctBy(p => p.DID).ToList();
                var filteredEnteries = uniqueIds.Where(d => d.DID.ToString().Length == 11).ToList();

                if (filteredEnteries.Count > 0)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string selectSql = "SELECT didno FROM did_main where isvoid = '1'";
                        var assignedDids = connection.Query<long>(selectSql).ToHashSet();

                        var dids = filteredEnteries
                            .Where(f => !assignedDids.Contains(f.DID))
                            .Select(f => new
                            {
                                didNo = f.DID,
                                city = f.City,
                                country = f.Country,
                                numberType = f.NumberType,
                                isVoid = 1,
                                user = email
                            })
                            .ToList();

                        if (dids.Any())
                        {
                            string insertSql = @"
                                INSERT INTO did_main (didno, city, isVoid, country, numberType, created_by)
                                VALUES (@didNo, @city, @isVoid, @country, @numberType, @user)";

                            connection.Execute(insertSql, dids);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddDids: " + ex.Message);
            }
        }



        public List<DIDmodal> GetAll(UpdateDidModal filters)
        {
            try
            {
                UpdateDidModal mdl = new UpdateDidModal
                {
                    DID = filters.DID == 0 ? null : filters.DID,
                    Country = string.IsNullOrWhiteSpace(filters.Country) ? null : filters.Country,
                    City = string.IsNullOrWhiteSpace(filters.City) ? null : filters.City,
                };
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.Query<DIDmodal>(@"
                                        select * from did_main
                                        where isvoid = 1
                                        and (@didno is null or didno = @didno)
                                        and (@country is null or country like '%' + @country + '%')
                                        and (@city is null or city like '%' + @city + '%')
                                        ",
                                        new { didno = mdl.DID, country = mdl.Country, city = mdl.City }
                                        ).ToList();

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void Delete(double id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute("update did_main set isvoid = '0', deleted_At = GETDATE() where id = @id", new { id });
                    connection.Execute("update didSubscriberMap set is_void = '1', unlink_at = @time where did_id = @id", new { id, time = DateTime.Now });
                    long DID = connection.QuerySingleOrDefault<long>("select didno from did_main where id = @id", new { id });
                    connection.Execute("update DBaliases set deleted_at = @time, is_void = '1' where alias_username = @DID", new { time = DateTime.Now, DID });
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public DIDmodal GetDetailsToUpdate(double id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    _notyf.Success("Success");
                    return connection.QuerySingleOrDefault<DIDmodal>("select * from did_main where id = @id", new { id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void Update(UpdateDidModal modal)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var assignedDids = connection.Query<long>("SELECT didno FROM did_main").ToList();
                    if (assignedDids.Contains(modal.DID ?? 0))
                    {
                        connection.Execute("update did_main set city = @city, country = @country where id = @id", new { city = modal.City, country = modal.Country, id = modal.Id });
                    }
                    else
                    {
                        long DID = connection.QuerySingleOrDefault<long>("select didno from did_main where id = @id and isVoid = '1'", new { id = modal.Id});
                        connection.Execute("update did_main set didno = @did, city = @city, country = @country where id = @id", new { did = modal.DID, city = modal.City, country = modal.Country, id = modal.Id });
                        connection.Execute("update DBaliases set alias_username = @newDID where alias_username = @DID and is_void = '0'", new { newDID = modal.DID, DID });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<DIDmodal> GetPaginatedPage(PaginationModal modal)
        {
            try
            {
                PaginationModal mdl = new PaginationModal
                {
                    did = modal.did == 0 ? null : modal.did,
                    country = string.IsNullOrWhiteSpace(modal.country) ? null : modal.country,
                    city = string.IsNullOrWhiteSpace(modal.city) ? null : modal.city,
                    pageNumber = modal.pageNumber,
                    pageSize = modal.pageSize == 0 ? 10 : modal.pageSize,
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.Query<DIDmodal>(@"
                                        select 
                                        a.id, a.didno, a.city, a.isVoid, a.country, a.numberType, a.created_by, c.username
                                        from did_main a
                                        left join didSubscriberMap b 
                                        on a.id = b.did_id 
                                        and b.is_void = 0   
                                        left join subscriber c    
                                        on b.sub_id = c.id 
                                        where a.isvoid = '1'
                                        ORDER BY a.didno
                                        OFFSET (@PageNumber * @PageSize) ROWS
                                        FETCH NEXT @PageSize ROWS ONLY
                                        ",
                                        new { didno = modal.did, country = modal.country, city = modal.city, PageNumber = modal.pageNumber, PageSize = modal.pageSize }
                                        ).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public int GetTotal()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM did_main");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}
