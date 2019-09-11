using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;
using Microsoft.Rest.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ThrottlingTest
{
    class Program
    {
        class ConsoleTracer : IServiceClientTracingInterceptor
        {
            public void Information(string message)
            {
                Console.WriteLine(message);
            }

            public void TraceError(string invocationId, Exception exception)
            {
                Console.WriteLine("Exception in {0}: {1}", invocationId, exception);
            }

            public void ReceiveResponse(string invocationId, HttpResponseMessage response)
            {
                Log("Response: response.Headers");
            }

            public void SendRequest(string invocationId, HttpRequestMessage request) { }

            public void Configuration(string source, string name, string value) { }

            public void EnterMethod(string invocationId, object instance, string method, IDictionary<string, object> parameters) { }

            public void ExitMethod(string invocationId, object returnValue) { }
        }

        const int MIN_RG_NUMBER = 100000;
        const int MAX_RG_NUMBER = 999999;

        static void Main(string[] args)
        {
            string resourceGroupPrefix = Program.GetConfigItem("resourceGroupPrefix", "throttlingtest");
            string resourceGroupName = resourceGroupPrefix + new Random().Next(MIN_RG_NUMBER, MAX_RG_NUMBER);
            string subscriptionId = Program.GetConfigItem("subscriptionId");
            string tenantId = Program.GetConfigItem("tenantId");
            string clientIds = Program.GetConfigItem("clientIds");
            string clientSecrets = Program.GetConfigItem("clientSecrets");

            string[] clientIdArray = clientIds.Split(",");
            string[] clientSecretArray = clientSecrets.Split(",");

            if (clientIdArray.Length != clientSecretArray.Length)
            {
                return;
            }
            if (clientIdArray.Length == 0)
            {
                return;
            }

            AzureCredentialsFactory factory = new AzureCredentialsFactory();
            IAzure[] azureConnections = new IAzure[clientIdArray.Length];
            ServiceClientTracing.AddTracingInterceptor(new ConsoleTracer());
            ServiceClientTracing.IsEnabled = true;

            for (int i = 0; i < clientIdArray.Length; i++)
            {
                string clientId = clientIdArray[i];
                string clientSecret = clientSecretArray[i];

                AzureCredentials azureCreds = factory.FromServicePrincipal(clientId, clientSecret, tenantId,
                        AzureEnvironment.AzureGlobalCloud);
                IAzure azure = Azure.Configure().
                    WithDelegatingHandler(new HttpLoggingDelegatingHandler()).
                    WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders).
                    WithRetryPolicy(new RetryPolicy(new TransientErrorIgnoreStrategy(), 0)).
                    Authenticate(azureCreds).
                    WithSubscription(subscriptionId);
                azureConnections[i] = azure;
            }

            //new NICTest().Run(azureConnections, resourceGroupName).Wait();
            new StorageTest().Run(azureConnections, resourceGroupName).Wait();

            Log("Done");
            Console.ReadLine();
        }

        internal static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}] {message}");
        }

        internal static string GetConfigItem(string key, string defaultValue = null)
        {
            string configItem = ConfigurationManager.AppSettings[key];
            if (defaultValue == null && String.IsNullOrEmpty(configItem))
            {
                throw new ConfigurationErrorsException($"Please set {key} in app.config");
            }
            else if (String.IsNullOrEmpty(configItem))
            {
                configItem = defaultValue;
            }

            return configItem;
        }
    }
}
