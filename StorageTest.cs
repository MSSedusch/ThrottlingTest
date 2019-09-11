using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ThrottlingTest
{
    public class StorageTest
    {
        const int MIN_STORAGE_SUFFIX = 100000;
        const int MAX_STORAGE_SUFFIX = 999999;

        public async Task Run(IAzure[] azureConnections, string resourceGroupName)
        {
            string location = Program.GetConfigItem("location", "westeurope");
            string storageAccountPrefix = Program.GetConfigItem("storageAccountPrefix", "dfhfhd4454548");
            var storageAccountName = storageAccountPrefix + new Random().Next(MIN_STORAGE_SUFFIX, MAX_STORAGE_SUFFIX);

            var storageAccount = await CreateBasicsAsync(azureConnections[0], location, resourceGroupName, storageAccountName);

            var startTime = DateTime.Now;
            int requestCount = 0;
            try
            {
                while (true)
                {
                    requestCount++;
                    int index = requestCount % azureConnections.Length;
                    var azureConnection = azureConnections[index];
                    var result = azureConnection.StorageAccounts.ListByResourceGroup(storageAccount.ResourceGroupName);
                }
            }
            catch (CloudException cex)
            {
                Program.Log($"Error in loop {requestCount}: {cex.Body.Code} {cex.Body.Message}");
            }
            catch (Exception ex)
            {
                Program.Log($"Error in loop {requestCount}: {ex.Message}");
            }

            Program.Log($"Storage Account Test done. {requestCount} requests in {(DateTime.Now - startTime).TotalSeconds} seconds with {azureConnections.Length} client ids");
        }

        private static async Task<IStorageAccount> CreateBasicsAsync(IAzure azure, string location, string resourceGroupName, string storageAccountName)
        {
            Program.Log("Creating Basics");

            return await azure.StorageAccounts.Define(storageAccountName).
                WithRegion(location).
                WithNewResourceGroup(resourceGroupName).
                CreateAsync();
        }
    }
}
