using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace ThrottlingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        public static async Task Run()
        {
            string subscriptionIds = GetConfigItem("subscriptionIds");
            string clientId = GetConfigItem("clientId");
            string clientSecret = GetConfigItem("clientSecret");
            string tenantId = GetConfigItem("tenantId");

            AzureCredentialsFactory factory = new AzureCredentialsFactory();
            AzureCredentials azureCreds = factory.FromServicePrincipal(clientId, clientSecret, tenantId,
                    AzureEnvironment.AzureGlobalCloud);
            Azure.IAuthenticated azure = Azure.Configure().Authenticate(azureCreds);
        }

        private static string GetConfigItem(string key)
        {
            string configItem = ConfigurationManager.AppSettings[key];
            if (String.IsNullOrEmpty(configItem))
            {
                throw new ConfigurationErrorsException($"Please set {key} in app.config");
            }

            return configItem;
        }
    }
}
