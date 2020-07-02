using SerAPI.GraphQl.Generic;
using GraphQL;
using GraphQL.Types;
using GraphQL.Utilities;
using System;
using NetTopologySuite.Geometries;
using SerAPI.Utils;

namespace SerAPI.GraphQl
{
    public class AppSchema : Schema
    {
        public AppSchema(IServiceProvider provider)
            : base(provider)
        {
            ValueConverter.Register(typeof(string), typeof(Point), ParsePoint);
            ValueConverter.Register(typeof(string), typeof(Coordinate), ParseCoordinate);
            ValueConverter.Register(typeof(string), typeof(LineString), ParseLineString);
            ValueConverter.Register(typeof(string), typeof(MultiLineString), ParseMultiLineString);

            Query = provider.GetRequiredService<GraphQLQuery>();
            Mutation = provider.GetRequiredService<AppMutation>();
        }

        private object ParsePoint(object geometryInpunt)
        {
            try
            {
                var geometryString = (string)geometryInpunt;                
                return JsonExtensions.DeserializeWithGeoJson<Point>(geometryString);
            }
            catch
            {
                throw new FormatException($"Failed to parse {nameof(Point)} from input '{geometryInpunt}'. Input should be a string of geojson representation");
            }
        }

        private object ParseCoordinate(object geometryInpunt)
        {
            try
            {
                var geometryString = (string)geometryInpunt;
                return JsonExtensions.DeserializeWithGeoJson<Coordinate>(geometryString);
            }
            catch
            {
                throw new FormatException($"Failed to parse {nameof(Coordinate)} from input '{geometryInpunt}'. Input should be a string of geojson representation");
            }
        }

        private object ParseLineString(object geometryInpunt)
        {
            try
            {
                var geometryString = (string)geometryInpunt;
                return JsonExtensions.DeserializeWithGeoJson<LineString>(geometryString);
            }
            catch
            {
                throw new FormatException($"Failed to parse {nameof(LineString)} from input '{geometryInpunt}'. Input should be a string of geojson representation");
            }
        }

        private object ParseMultiLineString(object geometryInpunt)
        {
            try
            {
                var geometryString = (string)geometryInpunt;
                return JsonExtensions.DeserializeWithGeoJson<MultiLineString>(geometryString);
            }
            catch
            {
                throw new FormatException($"Failed to parse {nameof(MultiLineString)} from input '{geometryInpunt}'. Input should be a string of geojson representation");
            }
        }

    }
}