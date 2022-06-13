using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using Upland.Interfaces.Repositories;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.LocalData
{
    public class LocalDataRepository : ILocalDataRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string DbConnectionString;

        public LocalDataRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            DbConnectionString = _configuration["AppSettings:DatabaseConnectionString"];
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

        public void CreateErrorLog(string location, string message)
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
                    sqlCmd.CommandText = "[UPL].[CreateErrorLog]";
                    sqlCmd.Parameters.Add(new SqlParameter("Datetime", DateTime.Now));
                    sqlCmd.Parameters.Add(new SqlParameter("Location", location));
                    sqlCmd.Parameters.Add(new SqlParameter("Message", message));

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

        public List<CachedForSaleProperty> GetCachedForSaleProperties(int cityId)
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<CachedForSaleProperty> cachedForSaleProperties = new List<CachedForSaleProperty>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 60;
                    sqlCmd.CommandText = "[UPL].[GetCachedForSalePropertiesByCityId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cachedForSaleProperties.Add(
                                new CachedForSaleProperty
                                {
                                    Id = (long)reader["Id"],
                                    Address = (string)reader["Address"],
                                    CityId = (int)reader["CityId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int)reader["NeighborhoodId"] : -1,
                                    StreetId = (int)reader["StreetId"],
                                    Size = (int)reader["Size"],
                                    FSA = (bool)reader["FSA"],
                                    Price = decimal.ToDouble((decimal)reader["Price"]),
                                    Currency = (string)reader["Currency"],
                                    Owner = (string)reader["Owner"],
                                    Mint = decimal.ToDouble((decimal)reader["Mint"]),
                                    Markup = decimal.ToDouble((decimal)reader["Markup"]),
                                    CollectionIds = new List<int>()
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
            }

            return cachedForSaleProperties;
        }

        public List<CachedSaleHistoryEntry> GetCachedSaleHistoryEntries(WebSaleHistoryFilters filters)
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<CachedSaleHistoryEntry> cachedSaleHistoryEntries = new List<CachedSaleHistoryEntry>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 120;
                    sqlCmd.CommandText = "[UPL].[GetCachedSaleEntries]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityIdSearch", filters.CityIdSearch));
                    sqlCmd.Parameters.Add(new SqlParameter("SearchByCityId", filters.SearchByCityId));

                    if (filters.SearchByUsername == null || filters.SearchByUsername.Trim() == "")
                        sqlCmd.Parameters.Add(new SqlParameter("SearchByUsername", DBNull.Value));
                    else
                        sqlCmd.Parameters.Add(new SqlParameter("SearchByUsername", filters.SearchByUsername.ToLower()));

                    sqlCmd.Parameters.Add(new SqlParameter("NoSales", filters.NoSales));
                    sqlCmd.Parameters.Add(new SqlParameter("NoSwaps", filters.NoSwaps));
                    sqlCmd.Parameters.Add(new SqlParameter("NoOffers", filters.NoOffers));

                    if (filters.Currency == null || filters.Currency.Trim() == "" || filters.Currency == "Any")
                        sqlCmd.Parameters.Add(new SqlParameter("Currency", DBNull.Value));
                    else
                        sqlCmd.Parameters.Add(new SqlParameter("Currency", filters.Currency));

                    if (filters.Address == null || filters.Address.Trim() == "")
                        sqlCmd.Parameters.Add(new SqlParameter("Address", DBNull.Value));
                    else
                        sqlCmd.Parameters.Add(new SqlParameter("Address", filters.Address));

                    if (filters.Username == null || filters.Username.Trim() == "")
                        sqlCmd.Parameters.Add(new SqlParameter("Username", DBNull.Value));
                    else
                        sqlCmd.Parameters.Add(new SqlParameter("Username", filters.Username.ToLower()));

                    sqlCmd.Parameters.Add(new SqlParameter("FromDate", filters.FromDate));
                    sqlCmd.Parameters.Add(new SqlParameter("ToDate", filters.ToDate));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CachedSaleHistoryEntry entry = new CachedSaleHistoryEntry
                            {
                                TransactionDateTime = (DateTime)reader["DateTime"],
                                Seller = (string)reader["Seller"],
                                Buyer = (string)reader["Buyer"],
                                Price = null,
                                Currency = null,
                                Offer = (bool)reader["Offer"],
                                Property = new CachedSaleHistoryEntryProperty
                                {
                                    Id = (long)reader["Id"],
                                    Address = (string)reader["Address"],
                                    CityId = (int)reader["CityId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int)reader["NeighborhoodId"] : -1,
                                    Mint = decimal.ToDouble((decimal)reader["Mint"]),
                                    CollectionIds = new List<int>()
                                },
                                OfferProperty = reader["OfferProp_Id"] == DBNull.Value ? null : new CachedSaleHistoryEntryProperty
                                {
                                    Id = (long)reader["OfferProp_Id"],
                                    Address = (string)reader["OfferProp_Address"],
                                    CityId = (int)reader["OfferProp_CityId"],
                                    NeighborhoodId = reader["OfferProp_NeighborhoodId"] != DBNull.Value ? (int)reader["OfferProp_NeighborhoodId"] : -1,
                                    Mint = decimal.ToDouble((decimal)reader["OfferProp_Mint"]),
                                    CollectionIds = new List<int>()
                                }
                            };

                            if (reader["Price"] != DBNull.Value)
                                entry.Price = decimal.ToDouble((decimal)reader["Price"]);
                            else
                                entry.Price = null;

                            if (reader["Currency"] != DBNull.Value)
                                entry.Currency = (string)reader["Currency"];
                            else
                                entry.Currency = null;

                            if (entry.Price != null)
                            {
                                if (entry.Currency == "UPX")
                                {
                                    entry.Markup = entry.Price / Math.Max(entry.Property.Mint, 1);
                                }
                                else
                                {
                                    entry.Markup = (entry.Price * 1000) / Math.Max(entry.Property.Mint, 1);
                                }
                            }
                            else
                            {
                                entry.Markup = null;
                            }

                            cachedSaleHistoryEntries.Add(entry);
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

            return cachedSaleHistoryEntries;
        }

        public List<CachedSaleHistoryEntry> GetCachedSaleHistoryEntriesByPropertyId(long propertyId)
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<CachedSaleHistoryEntry> cachedSaleHistoryEntries = new List<CachedSaleHistoryEntry>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 120;
                    sqlCmd.CommandText = "[UPL].[GetCachedSaleEntriesByPropertyId]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", propertyId));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CachedSaleHistoryEntry entry = new CachedSaleHistoryEntry
                            {
                                TransactionDateTime = (DateTime)reader["DateTime"],
                                Seller = (string)reader["Seller"],
                                Buyer = (string)reader["Buyer"],
                                Price = null,
                                Currency = null,
                                Offer = (bool)reader["Offer"],
                                Property = new CachedSaleHistoryEntryProperty
                                {
                                    Id = (long)reader["Id"],
                                    Address = (string)reader["Address"],
                                    CityId = (int)reader["CityId"],
                                    NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int)reader["NeighborhoodId"] : -1,
                                    Mint = decimal.ToDouble((decimal)reader["Mint"]),
                                    CollectionIds = new List<int>()
                                },
                                OfferProperty = reader["OfferProp_Id"] == DBNull.Value ? null : new CachedSaleHistoryEntryProperty
                                {
                                    Id = (long)reader["OfferProp_Id"],
                                    Address = (string)reader["OfferProp_Address"],
                                    CityId = (int)reader["OfferProp_CityId"],
                                    NeighborhoodId = reader["OfferProp_NeighborhoodId"] != DBNull.Value ? (int)reader["OfferProp_NeighborhoodId"] : -1,
                                    Mint = decimal.ToDouble((decimal)reader["OfferProp_Mint"]),
                                    CollectionIds = new List<int>()
                                }
                            };

                            if (reader["Price"] != DBNull.Value)
                                entry.Price = decimal.ToDouble((decimal)reader["Price"]);
                            else
                                entry.Price = null;

                            if (reader["Currency"] != DBNull.Value)
                                entry.Currency = (string)reader["Currency"];
                            else
                                entry.Currency = null;

                            if (entry.Price != null)
                            {
                                if (entry.Currency == "UPX")
                                {
                                    entry.Markup = entry.Price / Math.Max(entry.Property.Mint, 1);
                                }
                                else
                                {
                                    entry.Markup = (entry.Price * 1000) / Math.Max(entry.Property.Mint, 1);
                                }
                            }
                            else
                            {
                                entry.Markup = null;
                            }

                            cachedSaleHistoryEntries.Add(entry);
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

            return cachedSaleHistoryEntries;
        }

        public List<Tuple<string, double>> CatchWhales()
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<Tuple<string, double>> whales = new List<Tuple<string, double>>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[CatchWhales]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            whales.Add(
                                new Tuple<string, double>(
                                    (string)reader["UplandUsername"],
                                    decimal.ToDouble((decimal)reader["TotalMint"])
                                )
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

            return whales;
        }

        public List<Tuple<int, long>> GetCollectionPropertyTable()
        {
            SqlConnection sqlConnection = GetSQLConnector();
            List<Tuple<int, long>> collectionProperties = new List<Tuple<int, long>>();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCollectionPropertyTable]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collectionProperties.Add(
                                new Tuple<int, long>(
                                    (int)reader["CollectionId"],
                                    (long)reader["PropertyId"]
                                )
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

            return collectionProperties;
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
                                    CityId = reader.IsDBNull("CityId") ? -1 : (int)reader["CityId"],
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
                                    IsBuilt = (bool)reader["IsBuilt"],
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
                                    IsBuilt = (bool)reader["IsBuilt"],
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
                    sqlCmd.CommandTimeout = 120;
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
                                    IsBuilt = (bool)reader["IsBuilt"],
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
                                    IsBuilt = (bool)reader["IsBuilt"],
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

        public List<AcquiredInfo> GetAcquiredInfoByUser(string uplandUsername)
        {
            List<AcquiredInfo> acquiredInfoList = new List<AcquiredInfo>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetAcquiredOnByPlayer]";
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", uplandUsername));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AcquiredInfo newEntry = new AcquiredInfo
                            {
                                PropertyId = (long)reader["Id"],
                                Minted = (bool)reader["Minted"]
                            };

                            if (reader["AcquiredDateTime"] != DBNull.Value)
                            {
                                newEntry.AcquiredDateTime = (DateTime)reader["AcquiredDateTime"];
                            }
                            else
                            {
                                newEntry.AcquiredDateTime = null;
                            }
                            acquiredInfoList.Add(newEntry);
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

                return acquiredInfoList;
            }
        }

        public List<PropertyAppraisalData> GetPreviousSalesAppraisalData()
        {
            List<PropertyAppraisalData> data = new List<PropertyAppraisalData>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 600;
                    sqlCmd.CommandText = "[UPL].[GetPreviousSalesAppraisalData]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(
                                new PropertyAppraisalData
                                {
                                    Type = (string)reader["Type"],
                                    Id = (int)reader["Id"],
                                    Currency = (string)reader["Currency"],
                                    Value = (decimal)reader["PerUp2"],
                                    AverageSize = (int)reader[ "AverageSize"]
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

                return data;
            }
        }

        public List<PropertyAppraisalData> GetCurrentFloorAppraisalData()
        {
            List<PropertyAppraisalData> data = new List<PropertyAppraisalData>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 600;
                    sqlCmd.CommandText = "[UPL].[GetCurrentFloorAppraisalData]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(
                                new PropertyAppraisalData
                                {
                                    Type = (string)reader["Type"],
                                    Id = (int)reader["Id"],
                                    Currency = (string)reader["Currency"],
                                    Value = (decimal)reader["FloorValue"],
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

                return data;
            }
        }

        public List<PropertyAppraisalData> GetCurrentMarkupFloorAppraisalData()
        {
            List<PropertyAppraisalData> data = new List<PropertyAppraisalData>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 600;
                    sqlCmd.CommandText = "[UPL].[GetCurrentMarkupFloorAppraisalData]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(
                                new PropertyAppraisalData
                                {
                                    Type = (string)reader["Type"],
                                    Id = (int)reader["Id"],
                                    Currency = (string)reader["Currency"],
                                    Value = (decimal)reader["MarkUpFloor"],
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

                return data;
            }
        }

        public List<Tuple<string, double>> GetBuildingAppraisalData()
        {
            List<Tuple<string, double>> data = new List<Tuple<string, double>>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandTimeout = 600;
                    sqlCmd.CommandText = "[UPL].[GetBuildingApprasialData]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(
                                new Tuple<string, double>
                                (
                                    (string)reader["StructureType"],
                                    (double)reader["Median"]
                                )
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

                return data;
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
                    sqlCmd.Parameters.Add(new SqlParameter("Mint", property.Mint));
                    sqlCmd.Parameters.Add(new SqlParameter("NeighborhoodId", property.NeighborhoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("Latitude", property.Latitude));
                    sqlCmd.Parameters.Add(new SqlParameter("Longitude", property.Longitude));
                    sqlCmd.Parameters.Add(new SqlParameter("Status", property.Status));
                    sqlCmd.Parameters.Add(new SqlParameter("FSA", property.FSA));
                    sqlCmd.Parameters.Add(new SqlParameter("Owner", property.Owner));
                    sqlCmd.Parameters.Add(new SqlParameter("MintedOn", property.MintedOn));
                    sqlCmd.Parameters.Add(new SqlParameter("MintedBy", property.MintedBy));
                    sqlCmd.Parameters.Add(new SqlParameter("Boost", property.Boost));
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

        public void UpsertEOSUser(EOSUser eosUser)
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
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", eosUser.EOSAccount));
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", eosUser.UplandUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("Joined", eosUser.Joined));
                    sqlCmd.Parameters.Add(new SqlParameter("Spark", eosUser.Spark));

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
                                ReadPropertyFromReader(reader)
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

        public List<NFT> GetNFTsByNFTMetadataId(List<int> metadataIds)
        {
            List<NFT> nfts = new List<NFT>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTsByNFTMetadataIds]";
                    sqlCmd.Parameters.Add(new SqlParameter("NFTMetadataIds", CreateNFTIdTable(metadataIds)));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nfts.Add(
                                new NFT
                                {
                                    DGoodId = (int)reader["DGoodId"],
                                    NFTMetadataId = (int)reader["NFTMetadataId"],
                                    SerialNumber = (int)reader["SerialNumber"],
                                    Burned = (bool)reader["Burned"],
                                    CreatedOn = ReadNullParameterSafe<DateTime>(reader, "CreatedDateTime"),
                                    BurnedOn = ReadNullParameterSafe<DateTime>(reader, "BurnedDateTime"),
                                    FullyLoaded = (bool)reader["FullyLoaded"],
                                    Metadata = (byte[])reader["Metadata"],
                                    Owner = reader["UplandUsername"] == DBNull.Value ? "" : (string)reader["UplandUsername"]
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

                return nfts;
            }
        }

        public List<NFT> GetNFTsByOwnerEOS(string EOSAccount)
        {
            List<NFT> nfts = new List<NFT>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTsByOwnerEOS]";
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", EOSAccount));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nfts.Add(
                                new NFT
                                {
                                    DGoodId = (int)reader["DGoodId"],
                                    NFTMetadataId = (int)reader["NFTMetadataId"],
                                    SerialNumber = (int)reader["SerialNumber"],
                                    Burned = (bool)reader["Burned"],
                                    CreatedOn = ReadNullParameterSafe<DateTime>(reader, "CreatedDateTime"),
                                    BurnedOn = ReadNullParameterSafe<DateTime>(reader, "BurnedDateTime"),
                                    FullyLoaded = (bool)reader["FullyLoaded"],
                                    Metadata = (byte[])reader["Metadata"],
                                    Owner = reader["UplandUsername"] == DBNull.Value ? "" : (string)reader["UplandUsername"]
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

                return nfts;
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
                                ReadPropertyFromReader(reader)
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
                                ReadPropertyFromReader(reader)
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
                    sqlCmd.CommandTimeout = 60;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesByCityId]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(
                                ReadPropertyFromReader(reader)
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

        public List<UplandForSaleProp> GetPropertiesForSale_City(int cityId, bool onlyBuildings)
        {
            List<UplandForSaleProp> properties = new List<UplandForSaleProp>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesForSale_City]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    sqlCmd.Parameters.Add(new SqlParameter("OnlyBuildings", onlyBuildings));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new UplandForSaleProp
                            {
                                Prop_Id = (long)reader["PropId"],
                                Price = decimal.ToDouble((decimal)reader["Price"]),
                                Currency = (string)reader["Currency"],
                                Owner = (string)reader["Owner"]
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

                return properties;
            }
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Neighborhood(int neighborhoodId, bool onlyBuildings)
        {
            List<UplandForSaleProp> properties = new List<UplandForSaleProp>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesForSale_Neighborhood]";
                    sqlCmd.Parameters.Add(new SqlParameter("NeighborhoodId", neighborhoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("OnlyBuildings", onlyBuildings));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new UplandForSaleProp
                            {
                                Prop_Id = (long)reader["PropId"],
                                Price = decimal.ToDouble((decimal)reader["Price"]),
                                Currency = (string)reader["Currency"],
                                Owner = (string)reader["Owner"]
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

                return properties;
            }
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Street(int streetId, bool onlyBuildings)
        {
            List<UplandForSaleProp> properties = new List<UplandForSaleProp>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesForSale_Street]";
                    sqlCmd.Parameters.Add(new SqlParameter("StreetId", streetId));
                    sqlCmd.Parameters.Add(new SqlParameter("OnlyBuildings", onlyBuildings));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new UplandForSaleProp
                            {
                                Prop_Id = (long)reader["PropId"],
                                Price = decimal.ToDouble((decimal)reader["Price"]),
                                Currency = (string)reader["Currency"],
                                Owner = (string)reader["Owner"]
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

                return properties;
            }
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Collection(int collectionId, bool onlyBuildings)
        {
            List<UplandForSaleProp> properties = new List<UplandForSaleProp>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesForSale_Collection]";
                    sqlCmd.Parameters.Add(new SqlParameter("CollectionId", collectionId));
                    sqlCmd.Parameters.Add(new SqlParameter("OnlyBuildings", onlyBuildings));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new UplandForSaleProp
                            {
                                Prop_Id = (long)reader["PropId"],
                                Price = decimal.ToDouble((decimal)reader["Price"]),
                                Currency = (string)reader["Currency"],
                                Owner = (string)reader["Owner"]
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

                return properties;
            }
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Seller(string uplandUsername, bool onlyBuildings)
        {
            List<UplandForSaleProp> properties = new List<UplandForSaleProp>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertiesForSale_Seller]";
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", uplandUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("OnlyBuildings", onlyBuildings));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new UplandForSaleProp
                            {
                                Prop_Id = (long)reader["PropId"],
                                Price = decimal.ToDouble((decimal)reader["Price"]),
                                Currency = (string)reader["Currency"],
                                Owner = (string)reader["Owner"]
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

                return properties;
            }
        }

        public List<Property> GetPropertyByCityIdAndAddress(int cityId, string address)
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
                    sqlCmd.CommandText = "[UPL].[GetPropertyByCityIdAndAddress]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    sqlCmd.Parameters.Add(new SqlParameter("Address", address));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(ReadPropertyFromReader(reader));
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

        public List<PropertySearchEntry> SearchProperties(int cityId, string address)
        {
            List<PropertySearchEntry> properties = new List<PropertySearchEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[SearchPropertyByCityIdAndAddress]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", cityId));
                    sqlCmd.Parameters.Add(new SqlParameter("Address", address));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(new PropertySearchEntry
                            {
                                Id = (long)reader["Id"],
                                CityId = (int)reader["CityId"],
                                Address = (string)reader["Address"],
                                StreetId = (int)reader["StreetId"],
                                NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                                Size = (int)reader["Size"],
                                Mint = Math.Round(decimal.ToDouble((decimal)reader["Mint"]), 2),
                                Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : "",
                                FSA = (bool)reader["FSA"],
                                Owner = reader["Owner"] != DBNull.Value ? (string)reader["Owner"] : "",
                                Building = reader["Building"] != DBNull.Value ? (string)reader["Building"] : "None",
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

                return properties;
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
                    sqlCmd.Parameters.Add(new SqlParameter("RegisteredUserId", optimizationRun.RegisteredUserId));
                    sqlCmd.Parameters.Add(new SqlParameter("RequestedDateTime", DateTime.UtcNow));

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

        public void CreateAppraisalRun(AppraisalRun appraisalRun)
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
                    sqlCmd.CommandText = "[UPL].[CreateAppraisalRun]";
                    sqlCmd.Parameters.Add(new SqlParameter("RegisteredUserId", appraisalRun.RegisteredUserId));
                    sqlCmd.Parameters.Add(new SqlParameter("RequestedDateTime", DateTime.UtcNow));
                    sqlCmd.Parameters.Add(new SqlParameter("Results", appraisalRun.Results));
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
                                    Coordinates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<List<List<List<double>>>>>((string)reader["Coordinates"]),
                                    RGB = reader["RGB"] == DBNull.Value ? new List<int>() :  Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>((string)reader["RGB"])
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

        public DateTime GetLastHistoricalCityStatusDate()
        {
            DateTime lastValue = DateTime.MinValue;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetLastHistoricalCityStatusDate]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lastValue = (DateTime)reader["TimeStamp"];
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

                return lastValue;
            }
        }

        public DateTime GetLastSaleHistoryDateTime()
        {
            DateTime lastValue = DateTime.MinValue;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetLastSaleHistoryDateTime]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lastValue = (DateTime)reader["DateTime"];
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

                return lastValue;
            }
        }

        public List<SaleHistoryEntry> GetRawSaleHistoryByPropertyId(long propertyId)
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
                    sqlCmd.CommandText = "[UPL].[GetRawSaleHistoryByPropertyId]";
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

        public OptimizationRun GetLatestOptimizationRun(int id)
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
                    sqlCmd.CommandText = "[UPL].[GetLatestOptimizationRun]";
                    sqlCmd.Parameters.Add(new SqlParameter("@RegisteredUserId", id));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            optimizationRun = new OptimizationRun
                            {
                                Id = (int)reader["Id"],
                                RegisteredUserId = (int)reader["RegisteredUserId"],
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

        public AppraisalRun GetLatestAppraisalRun(int id)
        {
            AppraisalRun appraisalRun = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetLatestAppraisalRun]";
                    sqlCmd.Parameters.Add(new SqlParameter("@RegisteredUserId", id));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appraisalRun = new AppraisalRun
                            {
                                Id = (int)reader["Id"],
                                RegisteredUserId = (int)reader["RegisteredUserId"],
                                RequestedDateTime = (DateTime)reader["RequestedDateTime"],
                                Results = reader["Results"] == DBNull.Value ? null : (byte[])reader["Results"]
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

                return appraisalRun;
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
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<decimal?>("DiscordUserId", registeredUser.DiscordUserId));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("DiscordUsername", registeredUser.DiscordUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", registeredUser.UplandUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", registeredUser.PropertyId));
                    sqlCmd.Parameters.Add(new SqlParameter("Price", registeredUser.Price));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("VerifyType", registeredUser.VerifyType));

                    sqlCmd.Parameters.Add(new SqlParameter("VerifyExpirationDateTime", registeredUser.VerifyExpirationDateTime));

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

        public void UpdateRegisteredUser(RegisteredUser registeredUser)
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
                    sqlCmd.CommandText = "[UPL].[UpdateRegisteredUser]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", registeredUser.Id));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<decimal?>("DiscordUserId", registeredUser.DiscordUserId));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("DiscordUsername", registeredUser.DiscordUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("UplandUsername", registeredUser.UplandUsername));
                    sqlCmd.Parameters.Add(new SqlParameter("RunCount", registeredUser.RunCount));
                    sqlCmd.Parameters.Add(new SqlParameter("Paid", registeredUser.Paid));
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", registeredUser.PropertyId));
                    sqlCmd.Parameters.Add(new SqlParameter("Price", registeredUser.Price));
                    sqlCmd.Parameters.Add(new SqlParameter("SendUpx", registeredUser.SendUPX));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("PasswordSalt", registeredUser.PasswordSalt));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("PasswordHash", registeredUser.PasswordHash));
                    sqlCmd.Parameters.Add(new SqlParameter("DiscordVerified", registeredUser.DiscordVerified));
                    sqlCmd.Parameters.Add(new SqlParameter("WebVerified", registeredUser.WebVerified));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("VerifyType", registeredUser.VerifyType));
                    sqlCmd.Parameters.Add(new SqlParameter("VerifyExpirationDateTime", registeredUser.VerifyExpirationDateTime));

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
                                DiscordUsername = reader["DiscordUsername"] != DBNull.Value ? (string)reader["DiscordUsername"] : null,
                                UplandUsername = (string)reader["UplandUsername"],
                                RunCount = (int)reader["RunCount"],
                                Paid = (bool)reader["Paid"],
                                PropertyId = (long)reader["PropertyId"],
                                Price = (int)reader["Price"],
                                SendUPX = (int)reader["SendUpx"],
                                PasswordSalt = reader["PasswordSalt"] != DBNull.Value ? (string)reader["PasswordSalt"] : null,
                                PasswordHash = reader["PasswordHash"] != DBNull.Value ? (string)reader["PasswordHash"] : null,
                                DiscordVerified = (bool)reader["DiscordVerified"],
                                WebVerified = (bool)reader["WebVerified"],
                                VerifyType = reader["VerifyType"] != DBNull.Value ? (string)reader["VerifyType"] : null,
                                VerifyExpirationDateTime = (DateTime)reader["VerifyExpirationDateTime"]
                            };

                            if (reader["DiscordUserId"] != DBNull.Value)
                            {
                                registeredUser.DiscordUserId = (decimal)reader["DiscordUserId"];
                            }
                            else
                            {
                                registeredUser.DiscordUserId = null;
                            }
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

        public RegisteredUser GetRegisteredUserByUplandUsername(string uplandUsername)
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
                    sqlCmd.CommandText = "[UPL].[GetRegisteredUserByUplandUsername]";
                    sqlCmd.Parameters.Add(new SqlParameter("@UplandUsername", uplandUsername));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            registeredUser = new RegisteredUser
                            {
                                Id = (int)reader["Id"],
                                DiscordUsername = reader["DiscordUsername"] != DBNull.Value ? (string)reader["DiscordUsername"] : null,
                                UplandUsername = (string)reader["UplandUsername"],
                                RunCount = (int)reader["RunCount"],
                                Paid = (bool)reader["Paid"],
                                PropertyId = (long)reader["PropertyId"],
                                Price = (int)reader["Price"],
                                SendUPX = (int)reader["SendUpx"],
                                PasswordSalt = reader["PasswordSalt"] != DBNull.Value ? (string)reader["PasswordSalt"] : null,
                                PasswordHash = reader["PasswordHash"] != DBNull.Value ? (string)reader["PasswordHash"] : null,
                                DiscordVerified = (bool)reader["DiscordVerified"],
                                WebVerified = (bool)reader["WebVerified"],
                                VerifyType = reader["VerifyType"] != DBNull.Value ? (string)reader["VerifyType"] : null,
                                VerifyExpirationDateTime = (DateTime)reader["VerifyExpirationDateTime"],
                            };

                            if(reader["DiscordUserId"] != DBNull.Value)
                            {
                                registeredUser.DiscordUserId = (decimal)reader["DiscordUserId"];
                            }
                            else
                            {
                                registeredUser.DiscordUserId = null;
                            }    
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


        public EOSUser GetUplandUsernameByEOSAccount(string eosAccount)
        {
            EOSUser user = null;
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
                            user = new EOSUser
                            {
                                Id = (int)reader["Id"],
                                EOSAccount = (string)reader["EOSAccount"],
                                UplandUsername = (string)reader["UplandUsername"],
                                Joined = (DateTime)reader["Joined"],
                                Spark = (decimal)reader["Spark"]
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

                return user;
            }
        }

        public EOSUser GetEOSAccountByUplandUsername(string uplandUsername)
        {
            EOSUser user = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetEOSAccountByUplandUserName]";
                    sqlCmd.Parameters.Add(new SqlParameter("@UplandUsername", uplandUsername));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user = new EOSUser
                            {
                                Id = (int)reader["Id"],
                                EOSAccount = (string)reader["EOSAccount"],
                                UplandUsername = (string)reader["UplandUsername"],
                                Joined = (DateTime)reader["Joined"],
                                Spark = (decimal)reader["Spark"]
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

                return user;
            }
        }

        public List<Tuple<decimal, string, string>> GetRegisteredUsersEOSAccounts()
        {
            List<Tuple<decimal, string, string>> EOSAccounts = new List<Tuple<decimal, string, string>>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetRegisteredUsersEOSAccounts]";
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EOSAccounts.Add(new Tuple<decimal, string, string>(
                                reader.IsDBNull("DiscordUserId") ? -1 : (decimal)reader["DiscordUserId"],
                                (string)reader["UplandUsername"],
                                (string)reader["EOSAccount"]
                            ));
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

                return EOSAccounts;
            }
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCityId(int cityId)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByCityId]";
                    sqlCmd.Parameters.Add(new SqlParameter("@CityId", cityId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryByNeighborhoodId(int neighborhoodId)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByNeighborhoodId]";
                    sqlCmd.Parameters.Add(new SqlParameter("@NeighborhoodId", neighborhoodId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCollectionId(int collectionId)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByCollectionId]";
                    sqlCmd.Parameters.Add(new SqlParameter("@CollectionId", collectionId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryByStreetId(int streetId)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByStreetId]";
                    sqlCmd.Parameters.Add(new SqlParameter("@StreetId", streetId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryByPropertyId(long propertyId)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
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
                    sqlCmd.Parameters.Add(new SqlParameter("@PropertyId", propertyId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryByBuyerUsername(string buyerUsername)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryByBuyerUsername]";
                    sqlCmd.Parameters.Add(new SqlParameter("@BuyerUsername", buyerUsername));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        public List<SaleHistoryQueryEntry> GetSaleHistoryBySellerUsername(string sellerUsername)
        {
            List<SaleHistoryQueryEntry> saleHistoryEntries = new List<SaleHistoryQueryEntry>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSaleHistoryBySellerUsername]";
                    sqlCmd.Parameters.Add(new SqlParameter("@SellerUsername", sellerUsername));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleHistoryEntries.Add(
                                ReadSaleHistoryQueryEntryFromReader(reader)
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

        private SaleHistoryQueryEntry ReadSaleHistoryQueryEntryFromReader(SqlDataReader reader)
        {
            return new SaleHistoryQueryEntry
            {
                DateTime = (DateTime)reader["DateTime"],
                Seller = (string)reader["Seller"],
                Buyer = (string)reader["Buyer"],
                Offer = (bool)reader["Offer"],
                CityId = (int)reader["CityId"],
                Address = (string)reader["Address"],
                Mint = decimal.ToDouble((decimal)reader["Mint"]),
                Price = decimal.ToDouble((decimal)reader["Price"]),
                Currency = (string)reader["Currency"],
                Markup = decimal.ToDouble((decimal)reader["Markup"])
            };
        }

        private Property ReadPropertyFromReader(SqlDataReader reader)
        {
            return new Property
            {
                Id = (long)reader["Id"],
                Address = (string)reader["Address"],
                CityId = (int)reader["CityId"],
                Size = (int)reader["Size"],
                Mint = decimal.ToDouble((decimal)reader["Mint"]),
                StreetId = (int)reader["StreetId"],
                NeighborhoodId = reader["NeighborhoodId"] != DBNull.Value ? (int?)reader["NeighborhoodId"] : null,
                Latitude = reader["Latitude"] != DBNull.Value ? (decimal?)reader["Latitude"] : null,
                Longitude = reader["Longitude"] != DBNull.Value ? (decimal?)reader["Longitude"] : null,
                Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null,
                FSA = (bool)reader["FSA"],
                Owner = reader["Owner"] != DBNull.Value ? (string)reader["Owner"] : null,
                MintedOn = reader["MintedOn"] != DBNull.Value ? (DateTime?)reader["MintedOn"] : null,
                MintedBy = reader["MintedBy"] != DBNull.Value ? (string)reader["MintedBy"] : null,
                Boost = (decimal)reader["Boost"]
            };
        }

        public void DeleteRegisteredUser(int id)
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

        public void DeleteOptimizerRuns(int id)
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
                    sqlCmd.Parameters.Add(new SqlParameter("RegisteredUserId", id));

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

        public void DeleteAppraisalRuns(int id)
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
                    sqlCmd.CommandText = "[UPL].[DeleteAppraisalRuns]";
                    sqlCmd.Parameters.Add(new SqlParameter("RegisteredUserId", id));

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

        public void SetPropertyBoost(long propertyId, decimal boost)
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
                    sqlCmd.CommandText = "[UPL].[SetPropertyBoost]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", propertyId));
                    sqlCmd.Parameters.Add(new SqlParameter("Boost", boost));

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

        public void DeleteEOSUser(string eosAccount)
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
                    sqlCmd.CommandText = "[UPL].[DeleteEOSUser]";
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

        public void DeleteSaleHistoryByPropertyId(long propertyId)
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
                    sqlCmd.CommandText = "[UPL].[DeleteSaleHistoryByPropertyId]";
                    sqlCmd.Parameters.Add(new SqlParameter("PropertyId", propertyId));

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

        public void UpsertSparkStaking(SparkStaking sparkStaking)
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
                    sqlCmd.CommandText = "[UPL].[UpsertSparkStaking]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", sparkStaking.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", sparkStaking.DGoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", sparkStaking.EOSAccount));
                    sqlCmd.Parameters.Add(new SqlParameter("Amount", sparkStaking.Amount));
                    sqlCmd.Parameters.Add(new SqlParameter("Start", sparkStaking.Start));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("End", sparkStaking.End));

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

        public List<SparkStaking> GetSparkStakingByEOSAccount(string eosAccount)
        {
            List<SparkStaking> sparkStakings = new List<SparkStaking>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetSparkStakingByEOSAccount]";
                    sqlCmd.Parameters.Add(new SqlParameter("EOSAccount", eosAccount));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sparkStakings.Add(new SparkStaking
                            {
                                Id = (int)reader["Id"],
                                DGoodId = (int)reader["DGoodId"],
                                EOSAccount = (string)reader["EOSAccount"],
                                Amount = (decimal)reader["Amount"],
                                Start = (DateTime)reader["Start"],
                                End = ReadNullParameterSafe<DateTime?>(reader, "End")
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

                return sparkStakings;
            }
        }

        public void UpsertNft(NFT nft)
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
                    sqlCmd.CommandText = "[UPL].[UpsertNFT]";
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", nft.DGoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("NFTMetadataId", nft.NFTMetadataId));
                    sqlCmd.Parameters.Add(new SqlParameter("SerialNumber", nft.SerialNumber));
                    sqlCmd.Parameters.Add(new SqlParameter("Burned", nft.Burned));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("CreatedDateTime", nft.CreatedOn));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("BurnedDateTime", nft.BurnedOn));
                    sqlCmd.Parameters.Add(new SqlParameter("FullyLoaded", nft.FullyLoaded));
                    sqlCmd.Parameters.Add(new SqlParameter("Metadata", nft.Metadata));

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

        public void UpsertNftMetadata(NFTMetadata nftMetadata)
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
                    sqlCmd.CommandText = "[UPL].[UpsertNFTMetadata]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", nftMetadata.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Name", nftMetadata.Name));
                    sqlCmd.Parameters.Add(new SqlParameter("Category", nftMetadata.Category));
                    sqlCmd.Parameters.Add(new SqlParameter("FullyLoaded", nftMetadata.FullyLoaded));
                    sqlCmd.Parameters.Add(new SqlParameter("Metadata", nftMetadata.Metadata));

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

        public void UpsertNftHistory(NFTHistory nftHistory)
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
                    sqlCmd.CommandText = "[UPL].[UpsertNFTHistory]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", nftHistory.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", nftHistory.DGoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("Owner", nftHistory.Owner));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("ObtainedDateTime", nftHistory.ObtainedOn));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("DisposedDateTime", nftHistory.DisposedOn));

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

        public NFT GetNftByDGoodId(int dGoodId)
        {
            NFT nft = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTByDGoodId]";
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", dGoodId));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nft = new NFT();
                            nft.DGoodId = (int)reader["DGoodId"];
                            nft.NFTMetadataId = (int)reader["NFTMetadataId"];
                            nft.SerialNumber = (int)reader["SerialNumber"];
                            nft.Burned = (bool)reader["Burned"];
                            nft.CreatedOn = (DateTime)reader["CreatedDateTime"];
                            nft.BurnedOn = ReadNullParameterSafe<DateTime?>(reader, "BurnedDateTime");
                            nft.FullyLoaded = (bool)reader["FullyLoaded"];
                            nft.Metadata = (byte[])reader["Metadata"];

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

                return nft;
            }
        }

        public List<NFT> GetNftByDGoodIds(List<int> dGoodIds)
        {
            List<NFT> nfts = new List<NFT>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTs]";
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodIds", CreateNFTIdTable(dGoodIds)));
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nfts.Add(new NFT
                            {
                                DGoodId = (int)reader["DGoodId"],
                                NFTMetadataId = (int)reader["NFTMetadataId"],
                                SerialNumber = (int)reader["SerialNumber"],
                                Burned = (bool)reader["Burned"],
                                CreatedOn = (DateTime)reader["CreatedDateTime"],
                                BurnedOn = ReadNullParameterSafe<DateTime?>(reader, "BurnedDateTime"),
                                FullyLoaded = (bool)reader["FullyLoaded"],
                                Metadata = (byte[])reader["Metadata"]
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

                return nfts;
            }
        }

        public NFTMetadata GetNftMetadataById(int id)
        {
            NFTMetadata nftMetadata = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTMetadataById]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", id));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nftMetadata = new NFTMetadata();
                            nftMetadata.Id = (int)reader["Id"];
                            nftMetadata.Name = (string)reader["Name"];
                            nftMetadata.Category = (string)reader["Category"];
                            nftMetadata.FullyLoaded = (bool)reader["FullyLoaded"];
                            nftMetadata.Metadata = (byte[])reader["Metadata"];
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

                return nftMetadata;
            }
        }

        public NFTMetadata GetNftMetadataByNameAndCategory(string name, string category)
        {
            NFTMetadata nftMetadata = null;
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTMetadataByNameAndCategory]";
                    sqlCmd.Parameters.Add(new SqlParameter("Name", name));
                    sqlCmd.Parameters.Add(new SqlParameter("Category", category));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nftMetadata = new NFTMetadata();
                            nftMetadata.Id = (int)reader["Id"];
                            nftMetadata.Name = (string)reader["Name"];
                            nftMetadata.Category = (string)reader["Category"];
                            nftMetadata.FullyLoaded = (bool)reader["FullyLoaded"];
                            nftMetadata.Metadata = (byte[])reader["Metadata"];
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

                return nftMetadata;
            }
        }

        public List<NFTMetadata> GetAllNFTMetadata()
        {
            List<NFTMetadata> nftMetadata = new List<NFTMetadata>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetAllNFTMetadata]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nftMetadata.Add(new NFTMetadata
                            {
                                Id = (int)reader["Id"],
                                Name = (string)reader["Name"],
                                Category = (string)reader["Category"],
                                FullyLoaded = (bool)reader["FullyLoaded"],
                                Metadata = (byte[])reader["Metadata"]
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

                return nftMetadata;
            }
        }

        public List<NFTHistory> GetNftHistoryByDGoodId(int dGoodId)
        {
            List<NFTHistory> nftHistory = new List<NFTHistory>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTHistoryByDGoodId]";
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", dGoodId));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nftHistory.Add(new NFTHistory
                            {
                                Id = (int)reader["Id"],
                                DGoodId = (int)reader["DGoodId"],
                                Owner = (string)reader["Owner"],
                                ObtainedOn = (DateTime)reader["ObtainedDateTime"],
                                DisposedOn = ReadNullParameterSafe<DateTime?>(reader, "DisposedDateTime")
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

                return nftHistory;
            }
        }

        public Dictionary<int, int> GetCurrentNFTCounts()
        {
            Dictionary<int, int> nftCounts = new Dictionary<int, int>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCurrentNFTCounts]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nftCounts.Add(
                            
                                (int)reader["Id"],
                                (int)reader["Count"]
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

                return nftCounts;
            }
        }

        public void UpsertNFTSaleData(NFTSaleData saleData)
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
                    sqlCmd.CommandText = "[UPL].[UpsertNFTSaleData]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", saleData.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", saleData.DGoodId));
                    sqlCmd.Parameters.Add(new SqlParameter("SellerEOS", saleData.SellerEOS));
                    sqlCmd.Parameters.Add(new SqlParameter("BuyerEOS", saleData.BuyerEOS));
                    sqlCmd.Parameters.Add(new SqlParameter("Amount", saleData.Amount));
                    sqlCmd.Parameters.Add(new SqlParameter("AmountFiat", saleData.AmountFiat));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<DateTime?>("DateTime", saleData.DateTime));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("SubMerchant", saleData.SubMerchant));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<decimal?>("SubMerchantFee", saleData.SubMerchantFee));

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

        public List<NFTSaleData> GetNFTSaleDataByDGoodId(int dGoodId)
        {
            List<NFTSaleData> saleData = new List<NFTSaleData>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetNFTSaleDataByDGoodId]";
                    sqlCmd.Parameters.Add(new SqlParameter("DGoodId", dGoodId));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            saleData.Add(new NFTSaleData
                            {
                                Id = (int)reader["Id"],
                                DGoodId = (int)reader["DGoodId"],
                                SellerEOS = (string)reader["SellerEOS"],
                                BuyerEOS = (string)reader["BuyerEOS"],
                                Amount = (decimal)reader["Amount"],
                                AmountFiat = (decimal)reader["AmountFiat"],
                                DateTime = (DateTime)reader["DateTime"],
                                SubMerchant = (string)reader["SubMerchant"],
                                SubMerchantFee = (decimal)reader["SubMerchantFee"]
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

                return saleData;
            }
        }

        public List<Tuple<byte[], byte[]>> GetPropertyBuildingsMetadata()
        {
            List<Tuple<byte[], byte[]>> propertyStructureMetadata = new List<Tuple<byte[], byte[]>>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetPropertyBuildingsMetadata]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            propertyStructureMetadata.Add(new Tuple<byte[], byte[]>((byte[])reader["NFTMetadata"], (byte[])reader["Metadata"]));
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

                return propertyStructureMetadata;
            }
        }

        public void UpsertCity(City city)
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
                    sqlCmd.CommandText = "[UPL].[UpsertCity]";
                    sqlCmd.Parameters.Add(new SqlParameter("CityId", city.CityId));
                    sqlCmd.Parameters.Add(new SqlParameter("Name", city.Name));
                    sqlCmd.Parameters.Add(AddNullParmaterSafe<string>("SquareCoordinates", city.SquareCoordinates));
                    sqlCmd.Parameters.Add(new SqlParameter("StateCode", city.StateCode));
                    sqlCmd.Parameters.Add(new SqlParameter("CountryCode", city.CountryCode));

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

        public List<City> GetCities()
        {
            List<City> cities = new List<City>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetCities]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cities.Add(new City
                            {
                                CityId = (int)reader["CityId"],
                                Name = (string)reader["Name"],
                                SquareCoordinates = (string)reader["SquareCoordinates"],
                                StateCode = (string)reader["StateCode"],
                                CountryCode = (string)reader["CountryCode"]
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

                return cities;
            }
        }

        public void UpsertLandVehicleFinish(LandVehicleFinishInfo finishInfo)
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
                    sqlCmd.CommandText = "[UPL].[UpsertLandVehicleFinish]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", finishInfo.Id));
                    sqlCmd.Parameters.Add(new SqlParameter("Title", finishInfo.Title));
                    sqlCmd.Parameters.Add(new SqlParameter("Wheels", finishInfo.Wheels));
                    sqlCmd.Parameters.Add(new SqlParameter("DriveTrain", finishInfo.DriveTrain));
                    sqlCmd.Parameters.Add(new SqlParameter("MintingEnd", finishInfo.MintingEnd));
                    sqlCmd.Parameters.Add(new SqlParameter("CarClassId", finishInfo.CarClassId));
                    sqlCmd.Parameters.Add(new SqlParameter("CarClassName", finishInfo.CarClassName));
                    sqlCmd.Parameters.Add(new SqlParameter("CarClassNumber", finishInfo.CarClassNumber));
                    sqlCmd.Parameters.Add(new SqlParameter("Horsepower", finishInfo.Horsepower));
                    sqlCmd.Parameters.Add(new SqlParameter("Weight", finishInfo.Weight));
                    sqlCmd.Parameters.Add(new SqlParameter("Speed", finishInfo.Speed));
                    sqlCmd.Parameters.Add(new SqlParameter("Acceleration", finishInfo.Acceleration));
                    sqlCmd.Parameters.Add(new SqlParameter("Braking", finishInfo.Braking));
                    sqlCmd.Parameters.Add(new SqlParameter("Handling", finishInfo.Handling));
                    sqlCmd.Parameters.Add(new SqlParameter("EnergyEfficiency", finishInfo.EnergyEfficiency));
                    sqlCmd.Parameters.Add(new SqlParameter("Reliability", finishInfo.Reliability));
                    sqlCmd.Parameters.Add(new SqlParameter("Durability", finishInfo.Durability));
                    sqlCmd.Parameters.Add(new SqlParameter("Offroad", finishInfo.Offroad));

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

        public LandVehicleFinishInfo GetLandVehicleFinishInfoById(int finishId)
        {
            LandVehicleFinishInfo finishInfo = new LandVehicleFinishInfo();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetLandVehicleFinishById]";
                    sqlCmd.Parameters.Add(new SqlParameter("Id", finishId));

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            finishInfo = new LandVehicleFinishInfo
                            {
                                Id = (int)reader["Id"],
                                Title = (string)reader["Title"],
                                Wheels = (short)reader["Wheels"],
                                DriveTrain = (string)reader["DriveTrain"],
                                MintingEnd = (DateTime)reader["MintingEnd"],
                                CarClassId = (int)reader["CarClassId"],
                                CarClassName = (string)reader["CarClassName"],
                                CarClassNumber = (int)reader["CarClassNumber"],
                                Horsepower = (int)reader["Horsepower"],
                                Weight = (int)reader["Weight"],
                                Speed = (short)reader["Speed"],
                                Acceleration = (short)reader["Acceleration"],
                                Braking = (short)reader["Braking"],
                                Handling = (short)reader["Handling"],
                                EnergyEfficiency = (short)reader["EnergyEfficiency"],
                                Reliability = (short)reader["Reliability"],
                                Durability = (short)reader["Durability"],
                                Offroad = (short)reader["Offroad"],
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

                return finishInfo;
            }
        }

        public List<LandVehicleFinishInfo> GetAllLandVehicleFinishInfos()
        {
            List<LandVehicleFinishInfo> finishInfos = new List<LandVehicleFinishInfo>();
            SqlConnection sqlConnection = GetSQLConnector();

            using (sqlConnection)
            {
                sqlConnection.Open();

                try
                {
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConnection;
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.CommandText = "[UPL].[GetAllLandVehicleFinishes]";

                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            finishInfos.Add(new LandVehicleFinishInfo
                            {
                                Id = (int)reader["Id"],
                                Title = (string)reader["Title"],
                                Wheels = (short)reader["Wheels"],
                                DriveTrain = (string)reader["DriveTrain"],
                                MintingEnd = (DateTime)reader["MintingEnd"],
                                CarClassId = (int)reader["CarClassId"],
                                CarClassName = (string)reader["CarClassName"],
                                CarClassNumber = (int)reader["CarClassNumber"],
                                Horsepower = (int)reader["Horsepower"],
                                Weight = (int)reader["Weight"],
                                Speed = (short)reader["Speed"],
                                Acceleration = (short)reader["Acceleration"],
                                Braking = (short)reader["Braking"],
                                Handling = (short)reader["Handling"],
                                EnergyEfficiency = (short)reader["EnergyEfficiency"],
                                Reliability = (short)reader["Reliability"],
                                Durability = (short)reader["Durability"],
                                Offroad = (short)reader["Offroad"],
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

                return finishInfos;
            }
        }

        private SqlParameter AddNullParmaterSafe<T>(string parameterName, T value)
        {
            if (value == null)
            {
                return new SqlParameter(parameterName, DBNull.Value);
            }
            else
            {
                return new SqlParameter(parameterName, value);
            }
        }

        private T ReadNullParameterSafe<T>(SqlDataReader reader, string parameter)
        {
            if (reader[parameter] != DBNull.Value)
            {
                return (T)reader[parameter];
            }
            else
            {
                return default(T);
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

        private DataTable CreateNFTIdTable (List<int> nftIds)
        {
            DataTable table = new DataTable();
            table.Columns.Add("NFTMetadataId", typeof(int));
            foreach (int id in nftIds)
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
