﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace DurableTask.SqlServer.AzureFunctions
{
    using System;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;

    class SqlDurabilityOptions
    {
        [JsonProperty("connectionStringName")]
        public string ConnectionStringName { get; set; } = "SQLDB_Connection";

        [JsonProperty("taskHubName")]
        public string TaskHubName { get; set; } = "default";

        [JsonProperty("taskEventLockTimeout")]
        public TimeSpan TaskEventLockTimeout { get; set; } = TimeSpan.FromMinutes(2);

        [JsonProperty("taskEventBatchSize")]
        public int TaskEventBatchSize { get; set; } = 10;

        [JsonProperty("createDatabaseIfNotExists")]
        public bool CreateDatabaseIfNotExists { get; set; }

        internal ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;
        
        internal SqlOrchestrationServiceSettings GetOrchestrationServiceSettings(
            DurableTaskOptions extensionOptions,
            IConnectionStringResolver connectionStringResolver)
        {
            if (connectionStringResolver == null)
            {
                throw new ArgumentNullException(nameof(connectionStringResolver));
            }

            string? connectionString = connectionStringResolver.Resolve(this.ConnectionStringName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"No SQL connection string configuration was found for the app setting or environment variable named '{this.ConnectionStringName}'.");
            }

            // Validate the connection string
            try
            {
                new SqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("The provided connection string is invalid.", e);
            }

            var settings = new SqlOrchestrationServiceSettings(connectionString, this.TaskHubName)
            {
                CreateDatabaseIfNotExists = this.CreateDatabaseIfNotExists,
                LoggerFactory = this.LoggerFactory,
                WorkItemBatchSize = this.TaskEventBatchSize,
                WorkItemLockTimeout = this.TaskEventLockTimeout,
            };

            if (extensionOptions.MaxConcurrentActivityFunctions.HasValue)
            {
                settings.MaxConcurrentActivities = extensionOptions.MaxConcurrentActivityFunctions.Value;
            }

            if (extensionOptions.MaxConcurrentOrchestratorFunctions.HasValue)
            {
                settings.MaxActiveOrchestrations = extensionOptions.MaxConcurrentOrchestratorFunctions.Value;
            }

            return settings;
        }
    }
}
