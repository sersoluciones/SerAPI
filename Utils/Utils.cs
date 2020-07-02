using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerAPI.Utils
{
    public static class Utils
    {
        public static void FilterAllFields(Type type, List<object> args, StringBuilder whereArgs, int i, string value,
               string parentModel = "", bool isList = false)
        {
            var lastValid = false;
            whereArgs.Append("( ");
            foreach (var (propertyInfo, j) in type.GetProperties().Select((v, j) => (v, j)))
            {
                if (!propertyInfo.GetCustomAttributes(true).Any(x => x.ToString() == "System.Text.Json.Serialization.JsonIgnoreAttribute")
                    && !propertyInfo.GetCustomAttributes(true).Any(x => x.ToString() == "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute")
                    && !propertyInfo.PropertyType.Name.Contains("List"))
                {
                    var key = propertyInfo.Name;

                    if (!string.IsNullOrEmpty(parentModel))
                        key = $"{parentModel}.{propertyInfo.Name}";
                    if (isList)
                        key = $"{parentModel}.Any({propertyInfo.Name}";
                    var isValid = SqlCommandExt.ConcatFilter(args, whereArgs, $"@{i}", key, value, "¬",
                        typeProperty: propertyInfo.PropertyType, index: j, isList: isList, isValid: lastValid);

                    if (isValid)
                    {
                        lastValid = true;
                        i++;
                    }
                }
            }
            if (isList)
                whereArgs.Append(")");
            whereArgs.Append(" )");
        }

        public static void ConcatFilter(List<object> values, StringBuilder expresion, int index,
          string key, object value, bool isList = false, Type type = null)
        {
            bool appendKey = true;
            string select;
            var patternStr = @"_iext";
            Match matchStr = Regex.Match(key, patternStr);
            if (matchStr.Success)
            {
                key = Regex.Replace(key, patternStr, "");
                var patternSeparate = @"¬";
                if (Regex.Match(value.ToString(), patternSeparate).Success)
                {
                    var arrayValues = Regex.Split(value.ToString(), patternSeparate);
                    if (TypeExtensions.IsNumber(type))
                    {
                        select = string.Format("@{0}.Contains(int({1}))", index, key);
                        values.Add(Array.ConvertAll(arrayValues, s => int.Parse(s)));
                    }
                    else
                    {
                        select = string.Format("@{0}.Contains({1})", index, key);
                        values.Add(arrayValues);
                    }

                    appendKey = false;
                }
                else
                {
                    if (type == typeof(string))
                    {
                        values.Add(((string)value).ToLower());
                        select = string.Format(".ToLower().Contains(@{0})", index);
                    }
                    else if (TypeExtensions.IsNumber(type))
                    {
                        appendKey = false;
                        values.Add(value.ToString());
                        select = string.Format("string(object({0})).Contains(@{1})", key, index);
                    }
                    else
                    {
                        values.Add(value);
                        select = string.Format(" = @{0}", index);
                    }
                }

            }
            else
            {
                if (value is DateTime)
                {
                    var symbol = " = ";
                    if (Regex.Match(key, "_gte").Success) { symbol = " >= "; patternStr = "_gte"; }
                    else if (Regex.Match(key, "_gt").Success) { symbol = " > "; patternStr = "_gt"; }
                    else if (Regex.Match(key, "_lte").Success) { symbol = " <= "; patternStr = "_lte"; }
                    else if (Regex.Match(key, "_lt").Success) { symbol = " < "; patternStr = "_lt"; }

                    key = Regex.Replace(key, patternStr, "");

                    values.Add(value);
                    select = string.Format(" {0} @{1}", symbol, index);
                }
                else
                {
                    values.Add(value);
                    select = string.Format(" = @{0}", index);
                }
            }

            if (appendKey)
                expresion.Append(key);

            expresion.Append(select);
            if (isList)
                expresion.Append(")");
        }
    }

   
}
