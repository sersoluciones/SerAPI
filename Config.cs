using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI
{
    public class Config
    {
        public static IEnumerable<ApiResource> Apis =>
              new List<ApiResource>
              {
                new ApiResource
                {
                    Name = "ser_api", //TODO Replace
                    Description = "Ser API",  // TODO: Replace                   
                      // include the following using claims in access token (in addition to subject id)
                    //UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Email, JwtClaimTypes.Role },

                }//"api1",        
              };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                ///////////////////////////////////////////
                // Console Public Resource delegation for third access
                //////////////////////////////////////////
                 new Client
                {
                    ClientId = "third_Ser_client", // TODO: Replace
                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("XXXXX-XXXXX-XXXX-XXXX".Sha256()) // TODO: Fill
                    },
                    AllowedGrantTypes = { "delegation" },
                    AllowOfflineAccess = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Profile,
                        "ser_api.full_access", // TODO: Replace
                        "roles"
                    },

                    IncludeJwtId = false,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    AccessTokenLifetime = 3600 * 24 * 7,

                    RefreshTokenUsage = TokenUsage.OneTimeOnly, // Or ReUse
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    // AbsoluteRefreshTokenLifetime =  3600 * 180,
                    // SlidingRefreshTokenLifetime =  3600 * 180,
                    UpdateAccessTokenClaimsOnRefresh = true,
                 },

                ///////////////////////////////////////////
                // Console Public Resource Owner Flow Sample
                //////////////////////////////////////////
                new Client
                {
                    ClientId = "Ser_client", // TODO: Replace
                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("XXXXX-XXXXX-XXXX-XXXX".Sha256()) // TODO: Fill
                    },
                    IncludeJwtId = false,
                    AlwaysIncludeUserClaimsInIdToken = false,
                    AccessTokenLifetime = 3600 * 24 * 7,
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    AccessTokenType = AccessTokenType.Jwt,
                    AllowOfflineAccess = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Profile,
                        "ser_api.full_access", // TODO: Replace
                        "roles"
                    },

                    RefreshTokenUsage = TokenUsage.OneTimeOnly, // Or ReUse
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    // AbsoluteRefreshTokenLifetime =  3600 * 180,
                    // SlidingRefreshTokenLifetime =  3600 * 180,
                    UpdateAccessTokenClaimsOnRefresh = true,
                },

                new Client
                {
                    ClientId = "crendentials_Ser_client", // TODO: Replace
                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("XXXXX-XXXXX-XXXX-XXXX".Sha256()) // TODO: Fill
                    },

                    AccessTokenLifetime = 120,
                     // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes =
                    {
                        "ser_api.read_only" // TODO: Replace
                    }
                },
            };

        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email(),
                new IdentityResources.Profile(),
                new IdentityResources.Phone(),
                new IdentityResources.Address(),
                new IdentityResource
                {
                   Name = "roles",
                   UserClaims = { JwtClaimTypes.Role }
                }
            };

    }
}
