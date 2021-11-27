using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.Infrastructure.LocalData
{
    public class LocalDataRepository
    {
        private readonly string DbConnectionString;

        public LocalDataRepository()
        {
            DbConnectionString = JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(@"appsettings.json"))["DatabaseConnectionString"];
        }

        public void CreateCollection(Collection collection)
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
                    sqlCmd.Parameters.Add(new SqlParameter("IsCityCollection", collection.IsCityCollection));
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

        public void CreateCollectionProperties(int collectionId, List<long> propertyIds)
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

        public List<long> GetCollectionPropertyIds(int collectionId)
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

        public List<Collection> GetCollections()
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
                                    SlottedPropertyIds = new List<long>(),
                                    EligablePropertyIds = new List<long>(),
                                    Description = (string)reader["Description"],
                                    Reward = (int)reader["Reward"],
                                    CityId = (reader.IsDBNull("CityId") ? -1 : (int)reader["CityId"]),
                                    IsCityCollection = (bool)reader["IsCityCollection"]
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

        public List<StatsObject> GetCityStats()
        {
            List<StatsObject> stats = new List<StatsObject>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCityStats]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(
                                new StatsObject
                                {
                                    Id = (int)reader["Id"],
                                    FSA = (bool)reader["FSA"],
                                    Status = (string)reader["Status"],
                                    PropCount = (int)reader["PropCount"],
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

                return stats;
            }
        }

        public List<StatsObject> GetNeighborhoodStats()
        {
            List<StatsObject> stats = new List<StatsObject>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNeighborhoodStats]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(
                                new StatsObject
                                {
                                    Id = (int)reader["Id"],
                                    FSA = (bool)reader["FSA"],
                                    Status = (string)reader["Status"],
                                    PropCount = (int)reader["PropCount"],
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

                return stats;
            }
        }

        public List<StatsObject> GetStreetStats()
        {
            List<StatsObject> stats = new List<StatsObject>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetStreetStats]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(
                                new StatsObject
                                {
                                    Id = (int)reader["Id"],
                                    FSA = (bool)reader["FSA"],
                                    Status = (string)reader["Status"],
                                    PropCount = (int)reader["PropCount"],
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

                return stats;
            }
        }

        public List<StatsObject> GetCollectionStats()
        {
            List<StatsObject> stats = new List<StatsObject>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCollectionStats]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(
                                new StatsObject
                                {
                                    Id = (int)reader["Id"],
                                    FSA = (bool)reader["FSA"],
                                    Status = (string)reader["Status"],
                                    PropCount = (int)reader["PropCount"],
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

                return stats;
            }
        }

        public void UpsertProperty(Property property)
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
                    sqlCmd.CommandText = "[UPL].[UpsertProperty]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", property.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Address", property.Address));
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", property.CityId));
                    sqlCmd.Parameters.Add(new SqlParameter("StreetId", property.StreetId));
                    sqlCmd.Parameters.Add(new SqlParameter("Size", property.Size));
                    sqlCmd.Parameters.Add(new SqlParameter("MonthlyEarnings", property.MonthlyEarnings));
                    sqlCmd.Parameters.Add(new SqlParameter("NeighborhoodId", property.NeighborhoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("Latitude", property.Latitude));
                    sqlCmd.Parameters.Add(new SqlParameter("Longitude", property.Longitude));
                    sqlCmd.Parameters.Add(new SqlParameter("Status", property.Status));
                    sqlCmd.Parameters.Add(new SqlParameter("FSA", property.FSA));
                    sqlCmd.Parameters.Add(new SqlParameter("Owner", property.Owner));

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

        public void UpsertEOSUser(string eosAccount, string uplandUsername)
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
                    sqlCmd.CommandText = "[UPL].[UpsertEOSUser]";
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", eosAccount));
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

        public void UpsertSaleHistory(SaleHistoryEntry saleHistory)
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
                    sqlCmd.CommandText = "[UPL].[UpsertSaleHistory]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", saleHistory.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("DateTime", saleHistory.DateTime));
                    sqlCmd.Parameters.Add(new SqlParameter("SellerEOS", saleHistory.SellerEOS));
                    sqlCmd.Parameters.Add(new SqlParameter("BuyerEOS", saleHistory.BuyerEOS));
                    sqlCmd.Parameters.Add(new SqlParameter("PropId", saleHistory.PropId));
                    sqlCmd.Parameters.Add(new SqlParameter("Amount", saleHistory.Amount));
                    sqlCmd.Parameters.Add(new SqlParameter("AmountFiat", saleHistory.AmountFiat));
                    sqlCmd.Parameters.Add(new SqlParameter("OfferPropId", saleHistory.OfferPropId));
                    sqlCmd.Parameters.Add(new SqlParameter("Offer", saleHistory.Offer));
                    sqlCmd.Parameters.Add(new SqlParameter("Accepted", saleHistory.Accepted));

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

        public Property GetProperty(long id)
        {
            List<Property> properties = GetProperties(new List<long> { id });

            if (properties == null || properties.Count == 0)
            {
                return null;
            }

            return properties.First();
        }

        public List<Property> GetProperties(List<long> propertyIds)
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
                                    StreetId = (int)reader["StreetId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                                    Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null,
                                    Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null,
                                    Status = reader["Status"] != DBNull.Value ? (string?)reader["Status"] : null,
                                    FSA = (bool)reader["FSA"],
                                    Owner = reader["Owner"] != DBNull.Value ? (string?)reader["Owner"] : null
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

        public List<Property> GetPropertiesByUplandUsername(string uplandUsername)
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
                    sqlCmd.CommandText = "[UPL].[GetPropertiesByUplandUsername]";
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", uplandUsername));
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
                                    StreetId = (int)reader["StreetId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                                    Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null,
                                    Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null,
                                    Status = reader["Status"] != DBNull.Value ? (string?)reader["Status"] : null,
                                    FSA = (bool)reader["FSA"],
                                    Owner = reader["Owner"] != DBNull.Value ? (string?)reader["Owner"] : null
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

        public List<Property> GetPropertiesByCollectionId(int collectionId)
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
                    sqlCmd.CommandText = "[UPL].[GetPropertiesByCollectionId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CollectionId", collectionId));
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
                                    StreetId = (int)reader["StreetId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                                    Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null,
                                    Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null,
                                    Status = reader["Status"] != DBNull.Value ? (string?)reader["Status"] : null,
                                    FSA = (bool)reader["FSA"],
                                    Owner = reader["Owner"] != DBNull.Value ? (string?)reader["Owner"] : null
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

        public List<Property> GetPropertiesByCityId(int cityId)
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
                    sqlCmd.CommandText = "[UPL].[GetPropertiesByCityId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
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
                                    StreetId = (int)reader["StreetId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                                    Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null,
                                    Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null,
                                    Status = reader["Status"] != DBNull.Value ? (string?)reader["Status"] : null,
                                    FSA = reader["FSA"] != DBNull.Value ? (bool)reader["FSA"] : false,
                                    Owner = reader["Owner"] != DBNull.Value ? (string?)reader["Owner"] : null
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

        public Property GetPropertyByCityIdAndAddress(int cityId, string address)
        {
            Property property = new Property();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertyByCityIdAndAddress]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    sqlCmd.Parameters.Add(new SqlParameter("Address", address));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            property.Id = (long)reader["Id"];
                            property.Address = (string)reader["Address"];
                            property.CityId = (int)reader["CityId"];
                            property.Size = (int)reader["Size"];
                            property.MonthlyEarnings = decimal.ToDouble((decimal)reader["MonthlyEarnings"]);
                            property.StreetId = (int)reader["StreetId"];
                            property.NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null;
                            property.Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null;
                            property.Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null;
                            property.Status = reader["Status"] != DBNull.Value ? (string?)reader["Status"] : null;
                            property.FSA = reader["FSA"] != DBNull.Value ? (bool)reader["FSA"] : false;
                            property.Owner = reader["Owner"] != DBNull.Value ? (string?)reader["Owner"] : null;      
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

                return property;
            }
        }

        public void CreateOptimizationRun(OptimizationRun optimizationRun)
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

        public void CreateNeighborhood(Neighborhood neighborhood)
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
                    sqlCmd.CommandText = "[UPL].[CreateNeighborhood]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", neighborhood.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Name", neighborhood.Name));
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", neighborhood.City_Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Coordinates", Newtonsoft.Json.JsonConvert.SerializeObject(neighborhood.Coordinates)));

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

        public void CreateHistoricalCityStatus(CollatedStatsObject statsObject)
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
                    sqlCmd.CommandText = "[UPL].[CreateHistoricalCityStatus]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", statsObject.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("TotalProps", statsObject.TotalProps));
                    sqlCmd.Parameters.Add(new SqlParameter("Locked", statsObject.LockedProps));
                    sqlCmd.Parameters.Add(new SqlParameter("UnlockedNonFSA", statsObject.UnlockedNonFSAProps));
                    sqlCmd.Parameters.Add(new SqlParameter("UnlockedFSA", statsObject.UnlockedFSAProps));
                    sqlCmd.Parameters.Add(new SqlParameter("ForSale", statsObject.ForSaleProps));
                    sqlCmd.Parameters.Add(new SqlParameter("Owned", statsObject.OwnedProps));
                    sqlCmd.Parameters.Add(new SqlParameter("PercentMinted", statsObject.PercentMinted));
                    sqlCmd.Parameters.Add(new SqlParameter("PercentMintedNonFSA", statsObject.PercentNonFSAMinted));
                    sqlCmd.Parameters.Add(new SqlParameter("TimeStamp", statsObject.TimeStamp));

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

        public void CreateStreet(Street street)
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
                    sqlCmd.CommandText = "[UPL].[CreateStreet]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", street.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Name", street.Name));
                    sqlCmd.Parameters.Add(new SqlParameter("Type", street.Type));
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", street.CityId));

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

        public List<Neighborhood> GetNeighborhoods()
        {
            List<Neighborhood> neighborhoods = new List<Neighborhood>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNeighborhoods]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            neighborhoods.Add(
                                new Neighborhood
                                {
                                    Id = (int)reader["Id"],
                                    Name = (string)reader["Name"],
                                    CityId = (int)reader["CityId"],
                                    Coordinates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<List<List<List<double>>>>>((string)reader["Coordinates"])
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

                return neighborhoods;
            }
        }

        public List<Street> GetStreets()
        {
            List<Street> streets = new List<Street>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetStreets]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            streets.Add(
                                new Street
                                {
                                    Id = (int)reader["Id"],
                                    Name = (string)reader["Name"],
                                    Type = (string)reader["Type"],
                                    CityId = (int)reader["CityId"],
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

                return streets;
            }
        }

        public List<CollatedStatsObject> GetHistoricalCityStatusByCityId(int cityId)
        {
            List<CollatedStatsObject> cityStats = new List<CollatedStatsObject>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetHistoricalCityStatusByCityId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cityStats.Add(
                                new CollatedStatsObject
                                {
                                    DbId = (int)reader["Id"],
                                    Id = (int)reader["CityId"],
                                    TotalProps = (int)reader["TotalProps"],
                                    LockedProps = (int)reader["Locked"],
                                    UnlockedNonFSAProps = (int)reader["UnlockedNonFSA"],
                                    UnlockedFSAProps = (int)reader["UnlockedFSA"],
                                    ForSaleProps = (int)reader["ForSale"],
                                    OwnedProps = (int)reader["Owned"],
                                    PercentMinted = (int)reader["PercentMinted"],
                                    PercentNonFSAMinted = (int)reader["PercentMintedNonFSA"],
                                    TimeStamp = (DateTime)reader["TimeStamp"]
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

                return cityStats;
            }
        }

        public string GetConfigurationValue(string name)
        {
            string configValue = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetConfigurationValue]";
                    sqlCmd.Parameters.Add(new SqlParameter("Name", name));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            configValue = (string)reader["Value"];
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

                return configValue;
            }
        }

        public List<SaleHistoryEntry> GetSaleHistoryByPropertyId(long propertyId)
        {
            List<SaleHistoryEntry> saleHistoryEntries = new List<SaleHistoryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByPropertyId]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropId", propertyId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                new SaleHistoryEntry
                                {
                                    Id = (int)reader["Id"],
                                    DateTime = (DateTime)reader["DateTime"],
                                    SellerEOS = reader["SellerEOS"] == DBNull.Value ? null : (string)reader["SellerEOS"],
                                    BuyerEOS = reader["BuyerEOS"] == DBNull.Value ? null : (string)reader["BuyerEOS"],
                                    PropId = (long)reader["PropId"],
                                    Amount = reader["Amount"] == DBNull.Value ? (double?)null : decimal.ToDouble((decimal)reader["Amount"]),
                                    AmountFiat = reader["AmountFiat"] == DBNull.Value ? (double?)null : decimal.ToDouble((decimal)reader["AmountFiat"]),
                                    OfferPropId = reader["OfferPropId"] == DBNull.Value ? (long?)null : (long)reader["OfferPropId"],
                                    Offer = (bool)reader["Offer"],
                                    Accepted = (bool)reader["Accepted"],
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

                return saleHistoryEntries;
            }
        }

        public void SetOptimizationRunStatus(OptimizationRun optimizationRun)
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

        public OptimizationRun GetLatestOptimizationRun(decimal discordUserId)
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

        public void CreateRegisteredUser(RegisteredUser registeredUser)
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

        public void SetRegisteredUserPaid(string uplandUsername)
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

        public void SetRegisteredUserVerified(decimal discordUserId)
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

        public void IncreaseRegisteredUserRunCount(decimal discordUserId)
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

        public RegisteredUser GetRegisteredUser(decimal discordUserId)
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

        public string GetUplandUserNameByEOSAccount(string eosAccount)
        {
            string uplandUsername = "";
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetEOSUserByEOSAccount]";
                    sqlCmd.Parameters.Add(new SqlParameter("@EOSAccount", eosAccount));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uplandUsername = (string)reader["UplandUsername"];
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

                return uplandUsername;
            }
        }

        public void DeleteRegisteredUser(decimal discordUserId)
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

        public void DeleteOptimizerRuns(decimal discordUserId)
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

        public void UpdateSaleHistoryVistorToUplander(string oldEOS, string newEOS)
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
                    sqlCmd.CommandText = "[UPL].[UpdateSaleHistoryVistorToUplander]";
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", oldEOS));
                    sqlCmd.Parameters.Add(new SqlParameter("NewEOSAccount", newEOS));

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


        public void DeleteSaleHistoryById(int id)
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
                    sqlCmd.CommandText = "[UPL].[DeleteSaleHistoryById]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", id));

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

        public void DeleteSaleHistoryByBuyerEOS(string eosAccount)
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
                    sqlCmd.CommandText = "[UPL].[DeleteSaleHistoryByBuyerEOS]";
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", eosAccount));

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

        public void UpsertConfigurationValue(string name, string value)
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
                    sqlCmd.CommandText = "[UPL].[UpsertConfigurationValue]";
                    sqlCmd.Parameters.Add(new SqlParameter("Name", name));
                    sqlCmd.Parameters.Add(new SqlParameter("Value", value));

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

        public void CreatePropertyStructure(PropertyStructure propertyStructure)
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
                    sqlCmd.CommandText = "[UPL].[CreatePropertyStructure]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", propertyStructure.PropertyId));
                    sqlCmd.Parameters.Add(new SqlParameter("StructureType", propertyStructure.StructureType));

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

        public void TruncatePropertyStructure()
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
                    sqlCmd.CommandText = "[UPL].[TruncatePropertyStructure]";

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

        public List<PropertyStructure> GetPropertyStructures()
        {
            List<PropertyStructure> propertyStructures = new List<PropertyStructure>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertyStructures]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            propertyStructures.Add(new PropertyStructure
                            {
                                Id = (int)reader["Id"],
                                PropertyId = (long)reader["PropertyId"], 
                                StructureType = (string)reader["StructureType"]
                            });
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

                return propertyStructures;
            }
        }

        private DataTable CreatePropertyIdTable(List<long> propertyIds)
        {
            DataTable table = new DataTable();
            table.Columns.Add("PropertyId", typeof(long));
            foreach (long id in propertyIds)
            {
                table.Rows.Add(id);
            }

            return table;
        }

        private SqlConnection GetSQLConnector()
        {
            string connectionString = DbConnectionString;

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
