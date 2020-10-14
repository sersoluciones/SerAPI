using SerAPI.Data;
using SerAPI.Models;
using GraphQL.DataLoader;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace SerAPI.GraphQl.Repositories
{
    public class CityGraphRepository : GenericGraphRepository<City>
    {

        private readonly ApplicationDbContext _context;
        private readonly FillDataExtensions _fillDataExtensions;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CityGraphRepository(
            ApplicationDbContext context,
            FillDataExtensions fillDataExtensions,
            IHttpContextAccessor httpContextAccessor,
            IDataLoaderContextAccessor dataLoader,
            IConfiguration config)
             : base(context, httpContextAccessor, fillDataExtensions, dataLoader, config)
        {
            _context = context;
            _fillDataExtensions = fillDataExtensions;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<City> GetAllCitiesPerState(int countryId) => _context.cities
            .Where(a => a.country_id == countryId)
            .ToList();

        public Task<ILookup<int?, City>> GetCitiesByStateIds(IEnumerable<int?> stateIds)
        {
            //var cities = await _context.cities.Where(a => stateIds.Contains(a.country_id)).ToListAsync();
            var cities = _context.cities.Where("@0.Contains(country_id)", stateIds).AsNoTracking().ToListAsync();
            return Task.FromResult(cities.Result.ToLookup(x => x.country_id));
        }
    }
}