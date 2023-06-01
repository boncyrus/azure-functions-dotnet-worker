// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Nodes;

namespace FunctionsNetHost.Grpc
{
    internal static class PathUtils
    {
        /// <summary>
        /// Gets the absolute path to worker application executable.
        /// Builds the path by reading the worker.config.json
        /// </summary>
        /// <param name="applicationDirectory">The FunctionAppDirectory value from environment reload request.</param>
        internal static string? GetApplicationExePath(string applicationDirectory)
        {
            try
            {
                var workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

                var fileExists = File.Exists(workerConfigPath);
                Logger.LogInfo($"workerConfigPath:{workerConfigPath}. File exists:{fileExists}");

                if (!fileExists)
                {
                    throw new FileNotFoundException($"worker.config.json file not found", fileName: workerConfigPath);
                }

                var jsonString = File.ReadAllText(workerConfigPath);
                var workerConfigJsonNode = JsonNode.Parse(jsonString)!;
                var executableName = workerConfigJsonNode["description"]?["defaultWorkerPath"]?.ToString();

                if (executableName == null)
                {
                    Logger.LogInfo($"Invalid worker configuration. description > defaultWorkerPath property value is null.");
                    return null;
                }

                return Path.Combine(applicationDirectory, executableName);
            }
            catch (Exception ex)
            {
                Logger.LogInfo($"Error in GetApplicationExePath.{ex}");
                return null;
            }
        }
    }
}
