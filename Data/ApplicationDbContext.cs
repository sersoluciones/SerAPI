using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SerAPI.Utils;
using SerAPI.Models;

namespace SerAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser, ApplicationRole, string,
        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        public DbSet<Audit> audits { get; set; }
        public DbSet<Attachment> attachments { get; set; }
        public DbSet<CommonOption> common_options { get; set; }
        public DbSet<Permission> permissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("postgis");

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Console.WriteLine($"entity.GetTableName(): {entity.GetTableName()} {entity.IsOwned()}");

                if (Constantes.SystemTables.Contains(entity.GetTableName()))
                {
                    // Replace table names
                    entity.SetTableName(entity.GetTableName().ToSnakeCase());

                    // Replace column names            
                    foreach (var property in entity.GetProperties())
                    {
                        property.SetColumnName(property.GetColumnName().ToSnakeCase());
                    }
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetDefaultName().ToSnakeCase());
                }

                foreach (var key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName().ToSnakeCase());
                }

                foreach (var index in entity.GetIndexes())
                {
                    index.SetName(index.GetName().ToSnakeCase());
                }

            }

            modelBuilder.Entity<ApplicationUser>()
              .Property(b => b.IsActive)
              .HasDefaultValue(true);

            modelBuilder.Entity<ApplicationUser>()
                .Property(b => b.CreateDate)
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                // Each User can have many UserClaims
                b.HasMany(e => e.Claims)
                    .WithOne()
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();

                // Each User can have many UserLogins
                b.HasMany(e => e.Logins)
                    .WithOne()
                    .HasForeignKey(ul => ul.UserId)
                    .IsRequired();

                // Each User can have many UserTokens
                b.HasMany(e => e.Tokens)
                    .WithOne()
                    .HasForeignKey(ut => ut.UserId)
                    .IsRequired();

                // Each User can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<ApplicationRole>(b =>
            {
                // Each Role can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                     .WithOne(e => e.Role)
                     .HasForeignKey(ur => ur.RoleId)
                     .IsRequired();

                // Each Role can have many associated RoleClaims
                b.HasMany(e => e.Claims)
                    .WithOne()
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<Permission>().HasIndex(u => u.name).IsUnique();
        }
        }
}
