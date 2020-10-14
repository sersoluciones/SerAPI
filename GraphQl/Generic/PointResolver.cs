using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using SerAPI.Utilities;
using SerAPI.Utils;
using System;

namespace SerAPI.GraphQl.Generic
{
    public class PointResolver : IFieldResolver
    {
        private string _nameField;

        public PointResolver(string nameField)
        {
            _nameField = nameField;
        }

        public object Resolve(IResolveFieldContext context)
        {
            Point point = (Point)context.Source.GetPropertyValue(_nameField);
            Console.WriteLine($"point {point} {context.Source.GetPropertyValue(_nameField).GetType()}");
            return JsonExtensions.SerializeWithGeoJson<Point>(point, formatting: Formatting.None);
        }

    }
}
