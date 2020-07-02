using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Utils
{
    public class Constantes
    {
        public const byte SENT = 0;
        public const byte RECEIVED = 1;

        public const byte STATE_STORED = 0;
        public const byte STATE_SENT = 1;
        public const byte STATE_RECEIVED = 2;

        public const string SuperUser = "Super-User";
        public const string Administrador = "Administrador";
        public const string Auditoria = "Auditoria";
        public const string Usuario = "Usuario";

        public static string[] SystemTables = new string[]
        {
            "AspNetRoles", "AspNetUsers", "AspNetUserRoles",
            "AspNetRoleClaims", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens"
        };

        public static string[] SystemTablesSnakeCase = new string[]
        {
            "asp_net_role_claims", "asp_net_user_claims", "asp_net_user_logins", "asp_net_user_tokens"
        };

        public static string[] SystemTablesSingular = new string[]
        {
             "ApplicationRole", "ApplicationUser", "ApplicationUserRole"
        };

        public static string[] Permissions = new string[] {
            "users.view", "users.add", "users.update", "users.delete",
            "claims.view", "claims.add", "claims.update", "claims.delete",
            "permissions.view", "permissions.add", "permissions.update", "permissions.delete",
            "roles.view", "roles.add", "roles.update", "roles.delete",
            "role.view", "role.add", "role.update", "role.delete",
            "references.view", "audits.view",
            "attachments.view", "attachments.add", "attachments.update", "attachments.delete",
            "commonoptions.view", "commonoptions.add", "commonoptions.update", "commonoptions.delete",
        };
    }

    public class LoggingEvents
    {
        public const int GENERATE_ITEMS = 1000;
        public const int LIST_ITEMS = 1001;
        public const int GET_ITEM = 1002;
        public const int INSERT_ITEM = 1003;
        public const int UPDATE_ITEM = 1004;
        public const int DELETE_ITEM = 1005;

        public const int GET_ITEM_NOTFOUND = 4000;
        public const int UPDATE_ITEM_NOTFOUND = 4001;
    }

    public class CustomClaimTypes
    {
        public const string Permission = "http://ngcore/claims/permission";
        public const string CompanyId = "http://ngcore/claims/owner_id";
    }

}
