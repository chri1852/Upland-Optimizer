using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class ProfileAppraiser : IProfileAppraiser
    {
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _previousSalesData;
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _currentFloorData;
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _currentMarkupFloorData;
        private Dictionary<string, double> _buildingData;

        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _newPreviousSalesData;
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _newCurrentFloorData;
        private Dictionary<Tuple<string, int, string>, PropertyAppraisalData> _newCurrentMarkupFloorData;
        private Dictionary<string, double> _newBuildingData;

        private DateTime? _expirationDate;

        private ILocalDataManager _localDataManager;
        private IUplandApiManager _uplandApiManager;
        private ICachingProcessor _cachingProcessor;

        private List<Collection> _collections;

        public ProfileAppraiser(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager, ICachingProcessor cachingProcessor)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
            _cachingProcessor = cachingProcessor;

            _collections = _localDataManager.GetCollections();

            RefreshData();
        }

        public Dictionary<int, double> GetNeighborhoodPricePerUP2()
        {
            RefreshData();

            return _previousSalesData.Where(i => i.Value.Type == "NEIGHBORHOOD").ToDictionary(i => i.Value.Id, i => (double)i.Value.Value);
        }

        public Dictionary<int, double> GetNeighborhoodMarkupFloor()
        {
            RefreshData();

            return _currentMarkupFloorData.Where(i => i.Value.Type == "NEIGHBORHOOD").ToDictionary(i => i.Value.Id, i => (double)i.Value.Value);
        }

        public async Task<AppraisalResults> RunAppraisal(RegisteredUser registeredUser)
        {
            List<PropertyAppraisal> appraisals = await RunAppraisal(registeredUser.UplandUsername.ToLower());

            AppraisalResults results = BuildAppraisalResults(appraisals, registeredUser.UplandUsername);

            _localDataManager.DeleteAppraisalRuns(registeredUser.Id);
            _localDataManager.CreateAppraisalRun(new AppraisalRun
                {
                    RegisteredUserId = registeredUser.Id,
                    Results = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results))
                }
            );

            return results;
        }

        private async Task<List<PropertyAppraisal>> RunAppraisal(string username)
        {
            UplandUserProfile userProfile = await _uplandApiManager.GetUplandUserProfile(username.ToLower());
            List<Property> properties = _localDataManager.GetProperties(userProfile.propertyList);
            List<PropertyAppraisal> propertyAppraisals = new List<PropertyAppraisal>();
            Dictionary<long, string> propertyStructures = _cachingProcessor.GetPropertyStructuresFromCache();

            RefreshData();

            foreach (Property property in properties)
            {
                PropertyAppraisal propertyAppraisal = new PropertyAppraisal();
                propertyAppraisal.Notes = new List<string>();
                propertyAppraisal.Figures = new List<PropertyAppraisalFigure>();
                propertyAppraisal.Property = property;

                List<decimal> salesValues = new List<decimal>();
                List<decimal> floorValues = new List<decimal>();

                int maxCollectionCategory = 0;
                double maxFloor = 0;
                double upperBound = propertyAppraisal.Property.Mint;
                bool isLargeProp = false;

                // Street
                Tuple<string, int, string> streetTupleUPX = new Tuple<string, int, string>("STREET", property.StreetId, "UPX");
                if (_previousSalesData.ContainsKey(streetTupleUPX)) 
                {
                    decimal val = _previousSalesData[streetTupleUPX].Value * property.Size;
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Street Average Previous Sale", val));
                    salesValues.Add(val);
                    isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[streetTupleUPX].AverageSize * 10;
                }
                if (_currentFloorData.ContainsKey(streetTupleUPX)) 
                {
                    decimal val = _currentFloorData[streetTupleUPX].Value;
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Street UPX Floor", val));
                    floorValues.Add(val); 
                }
                if (_currentMarkupFloorData.ContainsKey(streetTupleUPX))
                {
                    decimal val = _currentMarkupFloorData[streetTupleUPX].Value * (decimal)property.Mint;
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Street Markup Floor", val));
                    floorValues.Add(val);
                }

                // Neighborhood
                if (property.NeighborhoodId != null)
                {
                    Tuple<string, int, string> neighborhoodTupleUPX = new Tuple<string, int, string>("NEIGHBORHOOD", property.NeighborhoodId.Value, "UPX");

                    if (_previousSalesData.ContainsKey(neighborhoodTupleUPX)) 
                    {
                        decimal val = _previousSalesData[neighborhoodTupleUPX].Value * property.Size;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Neighborhood Average Previous Sale", val));
                        salesValues.Add(val);
                        isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[neighborhoodTupleUPX].AverageSize * 10;
                    }
                    if (_currentFloorData.ContainsKey(neighborhoodTupleUPX)) 
                    {
                        decimal val = _currentFloorData[neighborhoodTupleUPX].Value;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Neighborhood UPX Floor", val));
                        floorValues.Add(_currentFloorData[neighborhoodTupleUPX].Value); 
                    }
                    if (_currentMarkupFloorData.ContainsKey(neighborhoodTupleUPX))
                    {
                        decimal val = _currentMarkupFloorData[neighborhoodTupleUPX].Value * (decimal)property.Mint;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Neighborhood Markup Floor", val));
                        floorValues.Add(val);
                    }
                }
                else
                {
                    propertyAppraisal.Notes.Add("Missing Neighborhood");
                }

                // Collection
                foreach (Collection collection in _collections.Where(c => c.MatchingPropertyIds.Contains(property.Id)))
                {
                    Tuple<string, int, string> collectionTupleUPX = new Tuple<string, int, string>("COLLECTION", collection.Id, "UPX");

                    if (_previousSalesData.ContainsKey(collectionTupleUPX)) 
                    {
                        decimal val = _previousSalesData[collectionTupleUPX].Value * property.Size;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure(string.Format("Collection - {0} - Average Previous Sale", HelperFunctions.GetCollectionCategory(collection.Category)), val));
                        salesValues.Add(val);
                        isLargeProp = isLargeProp ? isLargeProp : propertyAppraisal.Property.Size > _previousSalesData[collectionTupleUPX].AverageSize * 10;
                    }
                    if (_currentFloorData.ContainsKey(collectionTupleUPX)) 
                    {
                        decimal val = _currentFloorData[collectionTupleUPX].Value;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure(string.Format("Collection - {0} - UPX Floor", HelperFunctions.GetCollectionCategory(collection.Category)), val));
                        floorValues.Add(val); 
                    }
                    if (_currentMarkupFloorData.ContainsKey(collectionTupleUPX))
                    {
                        decimal val = _currentMarkupFloorData[collectionTupleUPX].Value * (decimal)property.Mint;
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure(string.Format("Collection - {0} - Markup Floor", HelperFunctions.GetCollectionCategory(collection.Category)), val));
                        floorValues.Add(val);
                    }

                    if (maxCollectionCategory < collection.Category) 
                    { 
                        maxCollectionCategory = collection.Category; 
                    }
                }

                // City
                Tuple<string, int, string> cityTupleUPX = new Tuple<string, int, string>("CITY", property.CityId, "UPX");
                if (_currentFloorData.ContainsKey(cityTupleUPX) && floorValues.Count == 0)
                {
                    decimal val = _currentFloorData[cityTupleUPX].Value;
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("City UPX Floor", val));
                    floorValues.Add(val);
                }
                if (_currentMarkupFloorData.ContainsKey(cityTupleUPX) && floorValues.Count == 1)
                {
                    decimal val = _currentMarkupFloorData[cityTupleUPX].Value * (decimal)property.Mint;
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("City Markup Floor", val));
                    floorValues.Add(val);
                }

                // Handle Low Data
                if (salesValues.Count == 0 && floorValues.Count == 0)
                {
                    propertyAppraisal.UPX_Upper = propertyAppraisal.Property.Mint;
                    propertyAppraisal.UPX_Lower = 0;
                    propertyAppraisal.Notes.Add("No Data Found To Calculate Value");
                }
                else
                {
                    if (salesValues.Count == 0)
                    {
                        propertyAppraisal.Notes.Add("No Sales Data");
                    }

                    if (floorValues.Count == 0)
                    {
                        propertyAppraisal.Notes.Add("No Floor Data");
                    }
                }

                salesValues.Add((int)Math.Round(property.Mint));
                propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Property Mint", (decimal)property.Mint));

                if (floorValues.Count > 0)
                {
                    maxFloor = (double)floorValues.Max(v => v);
                }

                if(salesValues.Count > 0)
                {
                    upperBound = (double)salesValues.GroupBy(v => v).Select(g => g.First()).OrderByDescending(v => v).First();
                }

                if (maxFloor > upperBound)
                {
                    propertyAppraisal.UPX_Upper = maxFloor;
                    propertyAppraisal.UPX_Lower = upperBound;
                }
                else if(upperBound > maxFloor)
                {
                    propertyAppraisal.UPX_Upper = upperBound;
                    propertyAppraisal.UPX_Lower = maxFloor;
                }
                else
                {
                    // max floor and upxValues[0] are equal, drop one and put the mint in
                    if (propertyAppraisal.Property.Mint >= maxFloor)
                    {
                        propertyAppraisal.UPX_Upper = propertyAppraisal.Property.Mint;
                        propertyAppraisal.UPX_Lower = maxFloor;
                    }
                    else
                    {
                        propertyAppraisal.UPX_Upper = maxFloor;
                        propertyAppraisal.UPX_Lower = maxFloor;
                    }
                }

                if (maxCollectionCategory == 5)
                {
                    propertyAppraisal.Notes.Add(string.Format("Ultra Rare"));
                }

                if (isLargeProp)
                {
                    propertyAppraisal.Notes.Add(string.Format("Large Property"));
                }

                List<decimal> totalValuesList = floorValues.Concat(salesValues).OrderBy(v => v).ToList();

                if (totalValuesList.Count() > 0)
                {
                    int halfIndex = totalValuesList.Count() / 2;

                    if ((totalValuesList.Count() % 2) == 0)
                    {
                        propertyAppraisal.UPX_Mid = (double)((totalValuesList.ElementAt(halfIndex) + totalValuesList.ElementAt(halfIndex - 1)) / 2);
                    }
                    else
                    {
                        propertyAppraisal.UPX_Mid = (double)totalValuesList.ElementAt(halfIndex);
                    }
                    propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Median Figure", (decimal)propertyAppraisal.UPX_Mid));
                }
                else
                {
                    propertyAppraisal.UPX_Mid = property.Mint;
                }

                if (propertyStructures.ContainsKey(property.Id))
                {
                    if (_buildingData.ContainsKey(propertyStructures[property.Id]))
                    {
                        propertyAppraisal.UPX_Lower += _buildingData[propertyStructures[property.Id]];
                        propertyAppraisal.UPX_Mid += _buildingData[propertyStructures[property.Id]];
                        propertyAppraisal.UPX_Upper += _buildingData[propertyStructures[property.Id]];
                        propertyAppraisal.Figures.Add(new PropertyAppraisalFigure("Building Value Added", (decimal)_buildingData[propertyStructures[property.Id]]));
                        propertyAppraisal.Notes.Add(string.Format("{0}", propertyStructures[property.Id]));
                    }
                    else
                    {
                        propertyAppraisal.Notes.Add(string.Format("Not Enough Sales Data to Appraise a {0}", propertyStructures[property.Id]));
                    }
                }

                // Now lets sort them
                List<double> finalValues = new List<double>();
                finalValues.Add(propertyAppraisal.UPX_Lower);
                finalValues.Add(propertyAppraisal.UPX_Mid);
                finalValues.Add(propertyAppraisal.UPX_Upper);
                finalValues.Sort();
                propertyAppraisal.UPX_Lower = finalValues[0];
                propertyAppraisal.UPX_Mid = finalValues[1];
                propertyAppraisal.UPX_Upper = finalValues[2];

                propertyAppraisal.Figures = propertyAppraisal.Figures.OrderBy(f => f.Value).ToList();

                propertyAppraisals.Add(propertyAppraisal);
            }

            return propertyAppraisals;
        }

        public List<string> BuildAppraisalCsvStrings(AppraisalResults results)
        {
            List<string> output = new List<string>();

            output.Add(string.Format("City,Address,Size,Mint,Lower UPX Value,Middle UPX Value,Upper UPX Value,Note"));

            foreach (AppraisalProperty property in results.Properties)
            {
                output.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}"
                    , property.City
                    , property.Address
                    , property.Size
                    , property.Mint
                    , property.LowerValue
                    , property.MiddleValue
                    , property.UpperValue
                    , string.Join(" ", property.Note)
                ));
            }

            return output;
        }

        private AppraisalResults BuildAppraisalResults(List<PropertyAppraisal> propertyAppraisals, string username)
        {
            AppraisalResults results = new AppraisalResults();

            results.Username = username;
            results.RunDateTime = DateTime.UtcNow;
            results.Properties = new List<AppraisalProperty>();

            propertyAppraisals = propertyAppraisals.OrderByDescending(a => a.UPX_Upper).OrderBy(a => a.Property.CityId).ToList();

            foreach (PropertyAppraisal appraisal in propertyAppraisals)
            {
                AppraisalProperty property = new AppraisalProperty();

                property.City = Consts.Cities[appraisal.Property.CityId];
                property.Address = appraisal.Property.Address;
                property.Size = appraisal.Property.Size;
                property.Collections = _collections.Where(c => c.MatchingPropertyIds.Contains(appraisal.Property.Id)).Select(c => c.Id).ToList();
                property.Mint = appraisal.Property.Mint;
                property.LowerValue = appraisal.UPX_Lower;
                property.MiddleValue = appraisal.UPX_Mid;
                property.UpperValue = appraisal.UPX_Upper;
                property.Note = string.Join(", ", appraisal.Notes);
                property.Figures = appraisal.Figures;

                results.Properties.Add(property);
            }

            return results;
        }

        public List<string> BuildAppraisalTxtStrings(AppraisalResults results)
        {
            List<string> output = new List<string>();

            int addressPad = results.Properties.Max(p => p.Address.Length);
            int sizePad = 9;
            int mintPad = 15;
            int upperPad = 15;
            int midPad = 16;
            int lowerPad = 15;
            int notePad = results.Properties.Max(p => string.Join(", ", p.Note).Length) < 4 ? 4 : results.Properties.Max(p => string.Join(", ", p.Note).Length);

            output.Add(string.Format("Property Appraisal For {0} as of {1:MM/dd/yy H:mm:ss}", results.Username.ToUpper(), results.RunDateTime));
            output.Add("");
            output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                , "Address".PadLeft(addressPad)
                , "Size".PadLeft(sizePad)
                , "Mint".PadLeft(mintPad)
                , "Lower UPX Value".PadLeft(lowerPad)
                , "Middle UPX Value".PadLeft(midPad)
                , "Upper UPX Value".PadLeft(upperPad)
                , "Note".PadLeft(notePad))); ;

            string city = "";

            foreach (AppraisalProperty property in results.Properties)
            {
                if (city != property.City)
                {
                    city = property.City;
                    output.Add("");
                    output.Add(string.Format("{0} - {1:N2} -> {2:N2} -> {3:N2}"
                        , city
                        , results.Properties.Where(p => p.City == city).Sum(p => p.LowerValue)
                        , results.Properties.Where(p => p.City == city).Sum(p => p.MiddleValue)
                        , results.Properties.Where(p => p.City == city).Sum(p => p.UpperValue)
                        ));
                }

                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6}"
                    , property.Address.PadLeft(addressPad)
                    , string.Format("{0:N0}", property.Size).PadLeft(sizePad)
                    , string.Format("{0:N2}", property.Mint).PadLeft(mintPad)
                    , string.Format("{0:N}", property.LowerValue).PadLeft(lowerPad)
                    , string.Format("{0:N}", property.MiddleValue).PadLeft(midPad)
                    , string.Format("{0:N}", property.UpperValue).PadLeft(upperPad)
                    , string.Join(", ", property.Note).PadLeft(notePad)
                ));
            }

            output.Add("");
            output.Add(string.Format("Total - {0:N2} -> {1:N2} -> {2:N2}"
                , results.Properties.Sum(p => p.LowerValue)
                , results.Properties.Sum(p => p.MiddleValue)
                , results.Properties.Sum(p => p.UpperValue)
                ));

            return output;
        }

        private void RefreshData()
        {
            if (_expirationDate == null)
            {
                _previousSalesData = _localDataManager.GetPreviousSalesAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                _currentFloorData = _localDataManager.GetCurrentFloorAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                _currentMarkupFloorData = _localDataManager.GetCurrentMarkupFloorAppraisalData()
                    .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                _buildingData = _localDataManager.GetBuildingAppraisalData()
                    .ToDictionary(d => d.Item1, d => d.Item2);

                _expirationDate = DateTime.Now.AddHours(24);
            }

            if (_expirationDate < DateTime.Now)
            {
                _expirationDate = DateTime.Now.AddHours(24);
                Task child = Task.Factory.StartNew(() =>
                {
                    _newPreviousSalesData = _localDataManager.GetPreviousSalesAppraisalData()
                        .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                    _newCurrentFloorData = _localDataManager.GetCurrentFloorAppraisalData()
                        .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                    _newCurrentMarkupFloorData = _localDataManager.GetCurrentMarkupFloorAppraisalData()
                        .ToDictionary(d => new Tuple<string, int, string>(d.Type, d.Id, d.Currency), d => d);
                    _newBuildingData = _localDataManager.GetBuildingAppraisalData()
                        .ToDictionary(d => d.Item1, d => d.Item2);

                    _previousSalesData = new Dictionary<Tuple<string, int, string>, PropertyAppraisalData>(_newPreviousSalesData);
                    _currentFloorData = new Dictionary<Tuple<string, int, string>, PropertyAppraisalData>(_newCurrentFloorData);
                    _currentMarkupFloorData = new Dictionary<Tuple<string, int, string>, PropertyAppraisalData>(_newCurrentMarkupFloorData);
                    _buildingData = new Dictionary<string, double>(_newBuildingData);
                });
            }
        }
    }
}
