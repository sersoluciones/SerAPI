using GraphQL.Types;
using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace SerAPI.Models
{
    public class City : BasicModel
    {
        [StringLength(4)]
        public string state_code { get; set; }

        [StringLength(6)]
        public string city_code { get; set; }

        [Required]
        [StringLength(255)]
        public string name { get; set; }

        [StringLength(80)]
        public string state_name { get; set; }

        [Required]
        [StringLength(5)]
        public string code { get; set; }

        [Required]
        public int? country_id { get; set; }

        [JsonIgnore]
        [ForeignKey("country_id")]
        public Country country { get; set; }

        //[JsonIgnore]
        //public List<Customer> customers { get; set; }

        //[JsonIgnore]
        //public List<Supplier> suppliers { get; set; }
    }

    public class CityType : ObjectGraphType<City>
    {
        public CityType()
        {
            var permission = GetType().BaseType.GetGenericArguments()[0].Name.ToLower().Pluralize() + ".view";
            // this.RequirePermissions(permission);

            Field(x => x.id, type: typeof(IdGraphType)).Description("Id property from the city object.");
            Field(x => x.name).Description("Name property from the city object.");
            Field(x => x.city_code).Description("City Code property from the city object.");
            Field(x => x.state_code).Description("State Code property from the city object.");
            Field(x => x.code).Description("Code property from the city object.");
            Field(x => x.state_name).Description("State name value property from the city object.");
            Field(x => x.country_id, type: typeof(IdGraphType)).Description("Country property from the City object.");
            Field<CountryType>(nameof(City.country));
        }
    }
}
