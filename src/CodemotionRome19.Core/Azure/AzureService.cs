﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure.Deployment;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure
{
    public class AzureService : IAzureService
    {
        public IAzure Azure { get; private set; }
        public IAuthenticated Auth { get; private set; }

        public DeploymentOptions DeploymentOptions { get; set; } = DeploymentOptions.Default;

        public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
        {
            if (Azure == null)
            {
                return Enumerable.Empty<Subscription>();
            }

            var subscriptionsList = await Azure.Subscriptions.ListAsync();

            return subscriptionsList.Select(s => new Subscription
            {
                DisplayName = s.DisplayName,
                SubscriptionId = s.SubscriptionId,
            });
        }

        public Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync()
        {
            var groups = Azure
                .ResourceGroups
                .List();

            var result = groups.Select(g => new ResourceGroup
            {
                Id = g.Id,
                Name = g.Name,
                Key = g.Key,
                Type = g.Type
            });

            return Task.FromResult(result);
        }

        public Task<IEnumerable<Ubication>> GetRegionsAsync()
        {
            var regions = new List<Ubication> 
            { 
                new Ubication { Name = "US East", Region = Region.USEast },
                new Ubication { Name = "US West", Region = Region.USWest },
                new Ubication { Name = "Europe West", Region = Region.EuropeWest },
                new Ubication { Name = "Asia East", Region = Region.AsiaEast },
            };

            return Task.FromResult(regions.AsEnumerable());
        }

        public Task<IAzure> AuthenticateAzure(string clientId, string clientSecret, string tenantId, string subscriptionid)
        {
            try
            {
                var credentials = SdkContext
                    .AzureCredentialsFactory
                    .FromServicePrincipal(
                        clientId,
                        clientSecret,
                        tenantId,
                        AzureEnvironment.AzureGlobalCloud);

                Azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(subscriptionid);

                return Task.FromResult(Azure);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }            
        }

        public Task<IAuthenticated> Authenticate(string clientId, string clientSecret, string tenantId)
        {
            try
            {
                var credentials = SdkContext
                    .AzureCredentialsFactory
                    .FromServicePrincipal(
                        clientId,
                        clientSecret,
                        tenantId,
                        AzureEnvironment.AzureGlobalCloud);

                Auth = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials);

                return Task.FromResult(Auth);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}