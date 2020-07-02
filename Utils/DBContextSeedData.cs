using SerAPI.Models;
using SerAPI.Data;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SerAPI.Utils
{
    public static class DBContextSeedData
    {
        public static async Task InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    await context.SaveChangesAsync();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.Ids)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    await context.SaveChangesAsync();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.Apis)
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task SeedPermissions(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var _dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await _dbContext.Database.EnsureCreatedAsync();
                var permissionsDb = _dbContext.permissions.ToList();

                foreach (string permission in Constantes.Permissions)
                {
                    //Console.WriteLine("{0}: {1}", market.Code, market.Name);
                    if (permissionsDb.Any(x => x.name == permission))
                    {
                        continue;
                    }
                    var per = new Permission() { name = permission };
                    permissionsDb.Add(per);
                    _dbContext.permissions.Add(per);
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        private static string[] roles = new[] { Constantes.SuperUser, Constantes.Administrador, Constantes.Usuario };

        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                using (var scope = serviceProvider.CreateScope())
                {
                    var _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                    foreach (var role in roles)
                    {
                        Console.WriteLine("Role: {0}", role);
                        if (!await _roleManager.RoleExistsAsync(role))
                        {
                            var newRole = new ApplicationRole(role);
                            await _roleManager.CreateAsync(newRole);
                        }
                    }
                }

            }
        }

        public static async Task SeedClaims(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                using (var scope = serviceProvider.CreateScope())
                {
                    var _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                    var role = await _roleManager.FindByNameAsync(Constantes.Administrador);
                    var claimsDB = await _roleManager.GetClaimsAsync(role);

                    foreach (var permission in Constantes.Permissions)
                    {
                        if (!claimsDB.Any(x => x.Type == CustomClaimTypes.Permission && x.Value == permission))
                            await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
                    }
                }

            }
        }

        public static async Task CreateSuperUser(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                using (var scope = serviceProvider.CreateScope())
                {
                    var _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                    var _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                    var user = new ApplicationUser
                    {
                        UserName = "superuser@mail.com",
                        Email = "superuser@mail.com",
                        Name = "Super",
                        LastName = "User",
                        PhoneNumber = "123"
                    };

                    var result = await _userManager.CreateAsync(user, "Abc123");
                    if (result.Succeeded)
                    {
                        if (await _roleManager.RoleExistsAsync(Constantes.SuperUser))
                        {
                            if (!await _userManager.IsInRoleAsync(user, Constantes.SuperUser))
                            {
                                await _userManager.AddToRoleAsync(user, Constantes.SuperUser);
                            }
                        }
                    }

                    user = new ApplicationUser
                    {
                        UserName = "admin@mail.com",
                        Email = "admin@mail.com",
                        Name = "Administrador",
                        LastName = "",
                        PhoneNumber = "31654987"
                    };

                    result = await _userManager.CreateAsync(user, "Abc123");
                    if (result.Succeeded)
                    {
                        if (await _roleManager.RoleExistsAsync(Constantes.Administrador))
                        {
                            if (!await _userManager.IsInRoleAsync(user, Constantes.Administrador))
                            {
                                await _userManager.AddToRoleAsync(user, Constantes.Administrador);
                            }
                        }
                    }
                }
            }
        }

    }
}
