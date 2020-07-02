using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.GraphQl.Generic
{
    public interface ITableNameLookup
    {
        bool InsertKeyName(string friendlyName);
        ObjectGraphType GetOrInsertGraphType(string key, ObjectGraphType objectGraphType);
        bool ExistGraphType(string key);

        ListGraphType<ObjectGraphType> GetOrInsertListGraphType(string key, ListGraphType<ObjectGraphType> objectGraphType);
        bool ExistListGraphType(string key);

        string GetFriendlyName(string correctName);
    }

    public class TableNameLookup : ITableNameLookup
    {
        private IDictionary<string, string> _lookupTable = new Dictionary<string, string>();
        private IDictionary<string, ObjectGraphType> _graphTypeDict = new Dictionary<string, ObjectGraphType>();
        private IDictionary<string, ListGraphType<ObjectGraphType>> _listGraphTypeDict = new Dictionary<string, ListGraphType<ObjectGraphType>>();

        public bool ExistGraphType(string key)
        {
            return _graphTypeDict.ContainsKey(key);
        }

        public ObjectGraphType GetOrInsertGraphType(string key, ObjectGraphType objectGraphType)
        {
            if (!_graphTypeDict.ContainsKey(key))
            {
                Console.WriteLine("Table agregada en diccionario cache: " + key);
                _graphTypeDict.Add(key, objectGraphType);
            }
            return _graphTypeDict[key];
        }

        public bool InsertKeyName(string correctName)
        {
            if (!_lookupTable.ContainsKey(correctName))
            {
                var friendlyName = CanonicalName(correctName);
                _lookupTable.Add(correctName, friendlyName);
                return true;
            }
            return false;
        }

        public string GetFriendlyName(string correctName)
        {
            if (!_lookupTable.TryGetValue(correctName, out string value))
                throw new Exception($"Could not get {correctName} out of the list.");
            return value;
        }
        public static string CanonicalName(string correctName)
        {
            var index = correctName.LastIndexOf("_");
            var result = correctName.Substring(index + 1, correctName.Length - index - 1);
            return char.ToLowerInvariant(result[0]) + result.Substring(1);
        }

        public ListGraphType<ObjectGraphType> GetOrInsertListGraphType(string key, ListGraphType<ObjectGraphType> objectGraphType)
        {
            if (!_listGraphTypeDict.ContainsKey(key))
            {
                Console.WriteLine("Table agregada en diccionario lista cache: " + key);
                _listGraphTypeDict.Add(key, objectGraphType);
            }
            return _listGraphTypeDict[key];
        }

        public bool ExistListGraphType(string key)
        {
            return _listGraphTypeDict.ContainsKey(key);
        }
    }
}
