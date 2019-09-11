using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Rest.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThrottlingTest
{
    public class NICTest
    {
        public async Task Run(IAzure[] azureConnections, string resourceGroupName)
        {
            string location = Program.GetConfigItem("location", "westeurope");
            string vnetName = Program.GetConfigItem("vnetName", "vnet");
            string subnetName = Program.GetConfigItem("subnetName", "subnet");
            string vnetPrefix = Program.GetConfigItem("vnetPrefix", "10.0.0.0/16");
            string subnetPrefix = Program.GetConfigItem("subnetPrefix", "10.0.0.0/16");
            string nicPrefix = Program.GetConfigItem("nicPrefix", "nic-");
            string targetNicCountString = Program.GetConfigItem("targetNicCount", "10");
            if (!int.TryParse(targetNicCountString, out int targetNicCount))
            {
                targetNicCount = 10;
            }

            var vnet = await CreateBasicsAsync(azureConnections[0], location, resourceGroupName, vnetName, subnetName, vnetPrefix, subnetPrefix);
            List<Task> tasks = new List<Task>();
            var startTime = DateTime.Now;
            for (int j = 0; j < targetNicCount; j++)
            {
                int index = j % azureConnections.Length;
                var azureConnection = azureConnections[index];
                var clientId = ((AzureCredentials)azureConnection.ManagementClients.First().Credentials).ClientId;
                Program.Log($"Using app id {clientId} to create NIC {j}");
                var taskNic = azureConnection.NetworkInterfaces.Define(nicPrefix + j.ToString("00000")).
                    WithRegion(location).
                    WithExistingResourceGroup(vnet.ResourceGroupName).
                    WithExistingPrimaryNetwork(vnet).
                    WithSubnet(subnetName).
                    WithPrimaryPrivateIPAddressDynamic().
                    CreateAsync();
                tasks.Add(taskNic);
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception ex)
            {
                Program.Log("Error: " + ex.Message);
            }
            Program.Log($"Created (or tried to) {targetNicCount} NICs in {(DateTime.Now - startTime).TotalSeconds} seconds");
        }

        private static async Task<Microsoft.Azure.Management.Network.Fluent.INetwork> CreateBasicsAsync(IAzure azure, string location, string resourceGroupName, string vnetName, string subnetName, string vnetPrefix, string subnetPrefix)
        {
            Program.Log("Creating Basics");
            return await azure.Networks.Define(vnetName).
                WithRegion(location).
                WithNewResourceGroup(resourceGroupName).
                WithAddressSpace(vnetPrefix).
                WithSubnet(subnetName, subnetPrefix).
                CreateAsync();
        }
    }
}
