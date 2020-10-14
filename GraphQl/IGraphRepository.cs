﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.GraphQl
{
    public interface IGraphRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(string alias, List<string> includeExpressions = null,
            string orderBy = "", string whereArgs = "", int? take = null, int? offset = null, params object[] args);
        IQueryable<T> GetQuery(string alias, List<string> includeExpressions = null,
            string orderBy = "", string whereArgs = "", int? take = null, int? offset = null, params object[] args);
        Task<T> GetByIdAsync(string alias, int id, List<string> includeExpressions = null,
          string whereArgs = "", params object[] args);
        T Create(T entity, string alias = "");
        T Update(int id, T entity, string alias = "");
        T Delete(int id, string alias = "");
    }
}
