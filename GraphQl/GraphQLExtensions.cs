﻿using GraphQL.Builders;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Types;
using SerAPI.Utilities;
using SerAPI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace SerAPI.GraphQl
{
    public static class GraphQLExtensions
    {
        public static readonly string PermissionsKey = "Permissions";

        public static List<AdditionalPermission> additionalPermissions = null;

        public static string[] typesWithoutPermission = { "categories", "subcategories" };

        public static string[] typesWithoutAuthentication = { "cities", "countries" };

        public static bool RequiresPermissions(this IProvideMetadata type)
        {
            var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
            return permissions.Any();
        }

        public static IEnumerable<string> GetPermissions(this IProvideMetadata type)
        {
            return type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
        }

        public static bool CanAccess(this IProvideMetadata type, IEnumerable<Claim> claims)
        {
            var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
            return permissions.All(x => claims.Select(i => i.Value)?.Contains(x) ?? false);
        }

        public static bool HasPermission(this IProvideMetadata type, string permission)
        {
            var permissions = type.GetMetadata<IEnumerable<string>>(PermissionsKey, new List<string>());
            return permissions.Any(x => string.Equals(x, permission));
        }

        public static List<AdditionalPermission> GetOtherPermissions()
        {
            if (additionalPermissions == null)
            {
                var jsonString = File.ReadAllText("permissions.json");
                additionalPermissions = JsonSerializer.Deserialize<List<AdditionalPermission>>(jsonString);
            }
            return additionalPermissions;
        }

        public static void ValidatePermissions(this IProvideMetadata type, string permission, string friendlyTableName, string typeName)
        {
            if (!typesWithoutAuthentication.Contains(permission) &&
               !typesWithoutAuthentication.Contains(friendlyTableName))
            {
                type.RequireAuthentication();

                if (!typesWithoutPermission.Contains(permission) &&
                !typesWithoutPermission.Contains(friendlyTableName))
                {
                    var otherPerms = GetOtherPermissions().Where(x => x.Name == permission).SelectMany(x => x.Permissions).ToArray();
                    type.RequirePermissions(otherPerms);

                    if (Constantes.SystemTablesSingular.Contains(typeName))
                        type.RequirePermissions($"{friendlyTableName}.view");
                    else
                        type.RequirePermissions($"{permission}.view");
                }
            }
        }

        public static void RequirePermissions(this IProvideMetadata type, params string[] permissionsRequired)
        {
            var permissions = type.GetMetadata<HashSet<string>>(PermissionsKey);

            if (permissions == null)
            {
                permissions = new HashSet<string>();
                type.Metadata[PermissionsKey] = permissions;
            }

            foreach (var per in permissionsRequired)
            {
                //Console.WriteLine($"________________permiso agregado {per}");
                permissions.Add(per);
            }

            //if (!string.IsNullOrEmpty(policy))
            AuthorizationMetadataExtensions.AuthorizeWith(type, "Authorized");
        }

        public static void RequireAuthentication(this IProvideMetadata type)
        {
            AuthorizationMetadataExtensions.AuthorizeWith(type, "Authorized");
        }

        public static void RequirePermission(this IProvideMetadata type, string permission)
        {
            var permissions = type.GetMetadata<List<string>>(PermissionsKey);

            if (permissions == null)
            {
                permissions = new List<string>();
                type.Metadata[PermissionsKey] = permissions;
            }

            permissions.Add(permission);
        }

        public static FieldBuilder<TSourceType, TReturnType> RequirePermission<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string permission)
        {
            builder.FieldType.RequirePermission(permission);
            return builder;
        }
    }
}