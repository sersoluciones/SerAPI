using GraphQL.DataLoader;
using GraphQL.Types;
using Humanizer;
using SerAPI.GraphQl;
using SerAPI.GraphQl.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace SerAPI.Models
{
    public class Country : BasicModel
    {
        [StringLength(100)]
        public string name_es { get; set; }

        [StringLength(2)]
        public string code_1 { get; set; }

        [StringLength(3)]
        public string code_2 { get; set; }

        [StringLength(20)]
        public string code_3 { get; set; }

        [StringLength(7)]
        public string phone_code { get; set; }

        [JsonIgnore]
        public List<City> cities { get; set; }
    }

    public class CountryType : ObjectGraphType<Country>
    {
        public CountryType(IGraphRepository<City> cityRepository, IDataLoaderContextAccessor dataLoader)
        {
            var permission = GetType().BaseType.GetGenericArguments()[0].Name.ToLower().Pluralize() + ".view";
            // this.RequirePermissions(permission);

            Field(x => x.id, type: typeof(IdGraphType)).Description("Id property from the country object.");
            Field(x => x.name_es).Description("Name property from the country object.");
            Field(x => x.code_1).Description("Code property from the country object.");
            Field(x => x.code_2).Description("Optional value property from the country object.");
            Field(x => x.code_3).Description("Optional value property from the country object.");

            Field<ListGraphType<CityType>>(
                "cities",
                resolve: context =>
                {
                    var loader = dataLoader.Context.GetOrAddCollectionBatchLoader<int?, City>($"GetCitiesByStateIds",
                        (ids) => ((CityGraphRepository)cityRepository).GetCitiesByStateIds(ids));
                    return loader.LoadAsync(context.Source.id);
                });
        }
    }
}