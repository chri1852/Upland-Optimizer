using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Upland.Types;

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
