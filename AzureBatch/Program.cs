using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System;
using System.Collections.Generic;

namespace AzureBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            string batchAccountName = "XXXX";
            string batchAccountKey = "XXXX";
            string batchAccountUrl = "XXXX";

            BatchSharedKeyCredentials cred = 
                new BatchSharedKeyCredentials(
                batchAccountUrl, 
                batchAccountName, 
                batchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(cred))
            {
                string poolId = "batchpool";

                // Create a Windows Server image
                ImageReference imageReference = new ImageReference(
                    publisher: "MicrosoftWindowsServer",
                    offer: "WindowsServer",
                    sku: "2012-R2-Datacenter-smalldisk",
                    version: "latest");

                // Create a VM configuration
                VirtualMachineConfiguration vmConfiguration = 
                    new VirtualMachineConfiguration(
                        imageReference: imageReference,
                        nodeAgentSkuId: "batch.node.windows amd64");

                // Create a batch pool
                CloudPool pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: 2,
                    virtualMachineSize: "STANDARD_A1",
                    virtualMachineConfiguration: vmConfiguration);

                string appId = "Multiply";
                string appVersion = "1.0";

                // Associate the application package to the batch pool
                pool.ApplicationPackageReferences = 
                    new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = appId,
                        Version = appVersion
                    }
                };

                pool.Commit();

                string jobId = "batchjob";

                CloudJob job = batchClient.JobOperations.CreateJob();
                job.Id = jobId;
                job.PoolInformation = new PoolInformation { PoolId = poolId };

                job.Commit();

                List<CloudTask> tasks = new List<CloudTask>();

                string appPath = $"%AZ_BATCH_APP_PACKAGE_{appId}#{appVersion}%";

                string taskId = $"Task1";
                string taskCommandLine = $"cmd /c {appPath}\\Multiply.exe 6 4";

                CloudTask cloudTask = new CloudTask(taskId, taskCommandLine)
                {
                    ApplicationPackageReferences = new List<ApplicationPackageReference>
                    {
                        new ApplicationPackageReference
                        {
                            ApplicationId = appId,
                            Version = appVersion
                        }
                    }
                };

                tasks.Add(cloudTask);

                // Add all tasks to the job
                batchClient.JobOperations.AddTask(jobId, tasks);

                // Monitor task success/failure, specifying a maximum timeout.
                TimeSpan timeout = TimeSpan.FromMinutes(30);
                Console.WriteLine("Monitoring all tasks for 'Completed'");

                IEnumerable<CloudTask> addedTasks = 
                    batchClient.JobOperations.ListTasks(jobId);

                batchClient.Utilities.CreateTaskStateMonitor().WaitAll(
                    addedTasks, TaskState.Completed, timeout);

                Console.WriteLine("All tasks reached state Completed.");

                // Print task output
                Console.WriteLine();
                Console.WriteLine("Printing task output...");

                IEnumerable<CloudTask> completedtasks = 
                    batchClient.JobOperations.ListTasks(jobId);

                foreach (CloudTask task in completedtasks)
                {
                    string output = task.GetNodeFile(
                        Constants.StandardOutFileName).ReadAsString();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"Output: {output}");
                    }

                    string errorMessage = task.GetNodeFile(
                        Constants.StandardErrorFileName).ReadAsString();

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {errorMessage}");
                        Console.ResetColor();
                    }
                }

                // Delete the pool
                batchClient.PoolOperations.DeletePool(poolId);

                // Terminate the job (marking it as completed)
                batchClient.JobOperations.TerminateJob(jobId);
            }

            Console.ReadKey();
        }


    }
}
