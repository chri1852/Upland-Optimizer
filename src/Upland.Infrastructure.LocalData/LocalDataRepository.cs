using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.Infrastructure.LocalData
{
    public static class LocalDataRepository
    {
        public static void CreateCollection(Collection collection)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CreateCollection]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", collection.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Name", collection.Name));
                    sqlCmd.Parameters.Add(new SqlParameter("Category", collection.Category));
                    sqlCmd.Parameters.Add(new SqlParameter("Boost", collection.Boost));
                    sqlCmd.Parameters.Add(new SqlParameter("NumberOfProperties", collection.NumberOfProperties));
                    sqlCmd.Parameters.Add(new SqlParameter("Description", collection.Description));
                    sqlCmd.Parameters.Add(new SqlParameter("Reward", collection.Reward));
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", collection.CityId));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void CreateCollectionProperties(int collectionId, List<long> propertyIds)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CreateCollectionProperties]";
                    sqlCmd.Parameters.Add(new SqlParameter("CollectionId", collectionId));
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyIds", CreatePropertyIdTable(propertyIds)));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static List<long> GetCollectionPropertyIds(int collectionId)
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<long> propertyIds = new List<long>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertyIdsForCollectionId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CollectionId", collectionId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            propertyIds.Add(
                                (long)reader["PropertyId"]
                             );
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }

            return propertyIds;
        }

        public static List<Collection> GetCollections()
        {
            List<Collection> collections = new List<Collection>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCollections]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collections.Add(
                                new Collection
                                {
                                    Id = (int)reader["Id"],
                                    Name = (string)reader["Name"],
                                    Category = (int)reader["Category"],
                                    Boost = decimal.ToDouble((decimal)reader["Boost"]),
                                    NumberOfProperties = (int)reader["NumberOfProperties"],
                                    Description = (string)reader["Description"],
                                    Reward = (int)reader["Reward"],
                                    CityId = (reader.IsDBNull("CityId") ? -1 : (int)reader["CityId"])
                                }
                             );
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }

                return collections;
            }
        }

        public static void CreateProperty(Property property)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CreateProperty]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", property.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Address", property.Address));
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", property.CityId));
                    sqlCmd.Parameters.Add(new SqlParameter("StreetId", property.StreetId));
                    sqlCmd.Parameters.Add(new SqlParameter("Size", property.Size));
                    sqlCmd.Parameters.Add(new SqlParameter("MonthlyEarnings", property.MonthlyEarnings));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static List<Property> GetProperties(List<long> propertyIds)
        {
            List<Property> properties = new List<Property>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetProperties]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyIds", CreatePropertyIdTable(propertyIds)));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(
                                new Property
                                {
                                    Id = (long)reader["Id"],
                                    Address = (string)reader["Address"],
                                    CityId = (int)reader["CityId"],
                                    Size = (int)reader["Size"],
                                    MonthlyEarnings = decimal.ToDouble((decimal)reader["MonthlyEarnings"]),
                                    StreetId = (int)reader["StreetId"]
                                }
                             );
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }

                return properties;
            }
        }

        public static void CreateOptimizationRun(OptimizationRun optimizationRun)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CreateOptimizationRun]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", optimizationRun.DiscordUserId));
                    sqlCmd.Parameters.Add(new SqlParameter("RequestedDateTime", DateTime.Now));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void SetOptimizationRunStatus(OptimizationRun optimizationRun)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[SetOptimizationRunStatus]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", optimizationRun.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Status", optimizationRun.Status));
                    sqlCmd.Parameters.Add(new SqlParameter("Results", optimizationRun.Results));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static OptimizationRun GetLatestOptimizationRun(decimal discordUserId)
        {
            OptimizationRun optimizationRun = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetLatestOptimizationRunForDiscordUserId]";
                    sqlCmd.Parameters.Add(new SqlParameter("@DiscordUserId", discordUserId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            optimizationRun = new OptimizationRun
                            {
                                Id = (int)reader["Id"],
                                DiscordUserId = (decimal)reader["DiscordUserId"],
                                RequestedDateTime = (DateTime)reader["RequestedDateTime"],
                                Results = reader["Results"] == DBNull.Value ? null : (byte[])reader["Results"],
                                Status = (string)reader["Status"],
                            };
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }

                return optimizationRun;
            }
        }

        public static void CreateRegisteredUser(RegisteredUser registeredUser)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CreateRegisteredUser]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", registeredUser.DiscordUserId));
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUsername", registeredUser.DiscordUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", registeredUser.UplandUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", registeredUser.PropertyId));
                    sqlCmd.Parameters.Add(new SqlParameter("Price", registeredUser.Price));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void SetRegisteredUserPaid(string uplandUsername)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[SetRegisteredUserPaid]";
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", uplandUsername));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void SetRegisteredUserVerified(decimal discordUserId)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[SetRegisteredUserVerified]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", discordUserId));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void IncreaseRegisteredUserRunCount(decimal discordUserId)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[IncreaseRegisteredUserRunCount]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", discordUserId));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static RegisteredUser GetRegisteredUser(decimal discordUserId)
        {
            RegisteredUser registeredUser = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetRegisteredUser]";
                    sqlCmd.Parameters.Add(new SqlParameter("@DiscordUserId", discordUserId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            registeredUser = new RegisteredUser
                            {
                                Id = (int)reader["Id"],
                                DiscordUserId = (decimal)reader["DiscordUserId"],
                                DiscordUsername = (string)reader["DiscordUsername"],
                                UplandUsername = (string)reader["UplandUsername"],
                                RunCount = (int)reader["RunCount"],
                                Paid = (bool)reader["Paid"],
                                PropertyId = (long)reader["PropertyId"],
                                Price = (int)reader["Price"],
                                Verified = (bool)reader["Verified"],
                            };
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }

                return registeredUser;
            }
        }

        public static void DeleteRegisteredUser(decimal discordUserId)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[DeleteRegisteredUser]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", discordUserId));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        public static void DeleteOptimizerRuns(decimal discordUserId)
        {
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[DeleteOptimizerRuns]";
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordUserId", discordUserId));

                    sqlCmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        private static DataTable CreatePropertyIdTable(List<long> propertyIds)
        {
            DataTable table = new DataTable();
            table.Columns.Add("PropertyId", typeof(long));
            foreach (long id in propertyIds)
            {
                table.Rows.Add(id);
            }

            return table;
        }

        private static SqlConnection GetSQLConnector()
        {
            string connectionString = Consts.LocalDBConnectionString;

            if (connectionString == null)
            {
                throw new Exception("Invalid DB Connection");
            }
            else
            {
                return new SqlConnection(connectionString);
            }
        }
    }
}
