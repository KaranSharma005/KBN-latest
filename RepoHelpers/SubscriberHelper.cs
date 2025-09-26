using AspNetCoreHero.ToastNotification.Abstractions;
using Dapper;   
using KBN.Models;
using Microsoft.Data.SqlClient;


namespace KBN.RepoHelpers
{
    public class SubscriberHelper
    {
        private readonly string _connectionString;
        private readonly INotyfService _notyf;
        private object connnection;

        public SubscriberHelper(
            IConfiguration config,
            INotyfService notyf
        )
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _notyf = notyf;
        }

        public void AddSubscriber(SubscriberModal modal, string email)      //to store email in the field of created by
        {
            try
            {
                string hasdedPassword = AesEncryption.Encrypt(modal.password);
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"insert into subscriber(username, password_hash, recorded_by) values(@username, @password, @recordedby)
                                   SELECT CAST(SCOPE_IDENTITY() AS INT)";
                    int subId = connection.QuerySingle<int>(sql, new {username = modal.username, password = hasdedPassword, recordedby = email});

                    CustomerDetailModal customer = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where customer_name = @name", new {name = modal.customername});
                    if(customer == null)
                    {
                        connection.Execute("insert into customerdata(customer_name, sub1) values(@name, @subId)", new {name = modal.customername, subId});
                    }
                    else if (customer != null && customer.sub2 != 0 && customer.sub1 != 0)
                    {
                        return;
                    }
                    else if(customer != null && customer.sub2 == 0)
                    {
                        connection.Execute("update customerdata set sub2 = @subId where customer_name = @name", new { name = modal.customername, subId });
                    }
                    else if (customer != null && customer.sub1 == 0)
                    {
                        connection.Execute("update customerdata set sub1 = @subId where customer_name = @name", new { name = modal.customername, subId });
                    }
                }
                _notyf.Success("Added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public bool IsUniqueUsername(string username)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    List<string> names = connection.Query<string>("select username from subscriber where is_void = '0'").ToList();
                    if (names.Contains(username))   
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        public bool CanUpdateCustomer(string name, int subId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var case1 = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where customer_name = @name", new { name });
                    var customerNameToUpdate = connection.QuerySingleOrDefault<string>(@"select b.customer_name from subscriber a 
                                                                                        inner join customerData b on (a.id = b.sub1 or a.id = b.sub2) where a.id = @id", new { id = subId });

                    if (customerNameToUpdate == null)
                        return true;
                    var case2 = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where customer_name = @name", new { name = customerNameToUpdate });
                    if ((case2 != null && case2 != null && case1?.sub2 == 0) || case1 == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        public List<SubscriberTableModal> PaginatedEntries(SubscriberPaginationModal modal)
        {
            try
            {
                SubscriberPaginationModal mdl = new SubscriberPaginationModal
                {
                    username = string.IsNullOrWhiteSpace(modal.username) ? null : modal.username,
                    createdby = string.IsNullOrWhiteSpace(modal.createdby) ? null : modal.createdby,
                    pageNumber = modal.pageNumber,
                    pageSize = modal.pageSize == 0 ? 10 : modal.pageSize,
                };
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.Query<SubscriberTableModal>(@"select a.id, a.username, a.password_hash, b.customer_name, a.recorded_at, a.recorded_by from subscriber a left join customerData b on (a.id = b.sub1 or a.id = b.sub2)
                                                                where is_void = '0' 
                                                                and (@username is null or username like '%' + @username + '%')
                                                                and (@createdby is null or recorded_by like '%' + @createdby + '%')
                                                                ORDER BY a.id
                                                                OFFSET (@PageNumber * @PageSize) ROWS
                                                                FETCH NEXT @PageSize ROWS ONLY", new { username = mdl.username, createdby = mdl.createdby, PageNumber = modal.pageNumber, PageSize = modal.pageSize }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public int GetTotalEntries()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    return connection.ExecuteScalar<int>("SELECT COUNT(*) from subscriber");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute("update subscriber set is_void = '1' where id = @id", new { id });
                    var customer = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where sub1 = @id or sub2 = @id", new { id });
                    
                    if(customer != null && customer.sub1 == id && customer.sub2 != 0)
                    {
                        connection.Execute("update customerdata set sub1 = @val, sub2 = @nullValue where sub1 = @id or sub2 = @id", new { val = customer.sub2, nullValue = (string?)null, id });
                    }
                    if(customer != null && customer.sub2 == id)
                    {
                        connection.Execute("update customerdata set sub2 = @nullValue where sub2 = @id", new { nullValue = (string?)null, id });
                    }
                    connection.Execute("update didSubscriberMap set is_void = '1', unlink_at = @time where sub_id = @id", new { id, time = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public UpdateSubscriber GetSubscriberToUpdate(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"select a.id, a.username, a.password_hash, b.customer_name from subscriber a 
                                left join customerData b on (a.id = b.sub1 or a.id = b.sub2) where a.id = @id";
                    return connection.QuerySingleOrDefault<UpdateSubscriber>(sql, new { id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void Update(UpdateSubscriber modal)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var case1 = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where customer_name = @name", new { name = modal.customer_name });
                    var customerNameToUpdate = connection.QuerySingleOrDefault<string>(@"select b.customer_name from subscriber a 
                                                                                        inner join customerData b on (a.id = b.sub1 or a.id = b.sub2) where a.id = @id", new { id = modal.id });

                    if (customerNameToUpdate == null && case1 == null)
                    {
                        connection.Execute("insert into customerdata(customer_name, sub1) values(@name, @sub)", new { name = modal.customer_name, sub = modal.id });
                        return;
                    }
                    if (customerNameToUpdate == null && case1.sub2 == 0)
                    {
                        connection.Execute("update customerdata set sub2 = @sub where customer_name = @name", new { sub = modal.id, name = modal.customer_name });
                        return;
                    }
                    
                    if (case1 == null)
                    {
                        connection.Execute("update customerdata set customer_name = @name where customer_name = @oldname", new { name = modal.customer_name, oldname = customerNameToUpdate });
                        return;
                    }
                    var case2 = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where customer_name = @name", new { name = customerNameToUpdate });

                    if (customerNameToUpdate != null && case1.sub2 == 0)
                    {
                        var customer = connection.QuerySingleOrDefault<CustomerDetailModal>("select * from customerdata where sub1 = @id or sub2 = @id", new { id = modal.id });

                        if (customer != null && customer.sub2 == modal.id)
                        {
                            connection.Execute("update customerdata set sub2 = @nullValue where sub2 = @id", new { nullValue = (string?)null, id = modal.id });
                        }
                        if (customer != null && customer.sub1 == modal.id)
                        {
                            connection.Execute("update customerdata set sub2 = @nullValue where sub2 = @id", new { nullValue = (string?)null, id = modal.id });
                            connection.Execute("delete from customerdata where customer_name = @name", new { name = customerNameToUpdate });
                        }
                        connection.Execute("update customerdata set sub2 = @sub where customer_name = @name", new { sub = modal.id, name = modal.customer_name });
                        return;
                    }

                    connection.Execute("delete from customerdata where customer_name = @name", new { name = customerNameToUpdate });
                    if (case1 != null && case2 != null)
                    {
                        connection.Execute("update customerdata set sub2 = @sub where customer_name = @name", new { sub = case2.sub1, name = modal.customer_name });
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }   
        }

        public bool UpdateNamePassword(UpdateSubscriber modal)
        {
            try
            {
                string hasdedPassword = AesEncryption.Encrypt(modal.password_hash);
                using (var connection = new SqlConnection(_connectionString))
                {
                    string oldName = connection.QuerySingleOrDefault<string>("select username from subscriber where id = @id", new { id = modal.id });
                    connection.Execute("update subscriber set password_hash = @password, username = @user where id = @id", new { password = hasdedPassword, user = modal.username, id = modal.id });
                    connection.Execute("update DBaliases set username = @newName where username = @oldName and is_void = '0'", new { newName = modal.username, oldName });

                    string customerOldName = connection.QuerySingleOrDefault<string>("select customer_name from customerdata where sub1 = @id or sub2 = @id", new { id = modal.id });
                    if (customerOldName == modal.customer_name)
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
