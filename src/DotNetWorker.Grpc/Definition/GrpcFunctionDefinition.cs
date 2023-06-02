// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Definition
{
    internal class GrpcFunctionDefinition : FunctionDefinition
    {
        private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
        private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

        public GrpcFunctionDefinition(FunctionLoadRequest loadRequest, IMethodInfoLocator methodInfoLocator)
        {
            EntryPoint = loadRequest.Metadata.EntryPoint;
            Name = loadRequest.Metadata.Name;
            Id = loadRequest.FunctionId;

            Console.WriteLine($"LanguageWorkerConsoleLog FUNCTIONS_APPLICATION_DIRECTORY env variable value:{Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)}, FUNCTIONS_WORKER_DIRECTORY env variable value:{Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey)}");

            var allEnvVariables = Environment.GetEnvironmentVariables();
            Console.WriteLine($"LanguageWorkerConsoleLog [Worker.GrpcFunctionDefinition] EnvironmentVariables count:{allEnvVariables.Count}");

            foreach (DictionaryEntry variable in allEnvVariables )
            {
                Console.WriteLine($"LanguageWorkerConsoleLog [Worker.GrpcFunctionDefinition] {variable.Key}:{variable.Value}");
            }

            // The long-term solution is FUNCTIONS_APPLICATION_DIRECTORY, but that change has not rolled out to 
            // production at this time. Use FUNCTIONS_WORKER_DIRECTORY as a fallback. They are currently identical, but
            // this will change once dotnet-isolated placeholder support rolls out. Eventually we can remove this.
            string ? scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey) ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

            // In the Linux environment ,this value is coming as “/tmp/functions\standby\wwwroot”. Our worker assembly
            // is not present in that location. It is in “/home/site/wwwroot/”
            // Hardcoding for our initial validation. Will remove when we fix this on the host side.
            //scriptRoot = @"/home/site/wwwroot/";

            if (string.IsNullOrWhiteSpace(scriptRoot))
            {
                throw new InvalidOperationException($"The '{FunctionsApplicationDirectoryKey}' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
            }

            if (string.IsNullOrWhiteSpace(loadRequest.Metadata.ScriptFile))
            {
                throw new InvalidOperationException($"Metadata for function '{loadRequest.Metadata.Name} ({loadRequest.Metadata.FunctionId})' does not specify a 'ScriptFile'.");
            }

            string scriptFile = Path.Combine(scriptRoot, loadRequest.Metadata.ScriptFile);
            PathToAssembly = Path.GetFullPath(scriptFile);
            Console.WriteLine($"LanguageWorkerConsoleLog PathToAssembly:{PathToAssembly}");

            var grpcBindingsGroup = loadRequest.Metadata.Bindings.GroupBy(kv => kv.Value.Direction);
            var grpcInputBindings = grpcBindingsGroup.Where(kv => kv.Key == BindingInfo.Types.Direction.In).FirstOrDefault();
            var grpcOutputBindings = grpcBindingsGroup.Where(kv => kv.Key != BindingInfo.Types.Direction.In).FirstOrDefault();
            var infoToMetadataLambda = new Func<KeyValuePair<string, BindingInfo>, BindingMetadata>(kv => new GrpcBindingMetadata(kv.Key, kv.Value));

            InputBindings = grpcInputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
                ?? ImmutableDictionary<string, BindingMetadata>.Empty;

            OutputBindings = grpcOutputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
                ?? ImmutableDictionary<string, BindingMetadata>.Empty;

            Parameters = methodInfoLocator.GetMethod(PathToAssembly, EntryPoint)
                .GetParameters()
                .Where(p => p.Name != null)
                .Select(p => new FunctionParameter(p.Name!, p.ParameterType, GetAdditionalPropertiesDictionary(p)))
                .ToImmutableArray();
        }

        public override string PathToAssembly { get; }

        public override string EntryPoint { get; }

        public override string Id { get; }

        public override string Name { get; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        private ImmutableDictionary<string, object> GetAdditionalPropertiesDictionary(ParameterInfo parameterInfo)
        {
            // Get the input converter attribute information, if present on the parameter.
            var inputConverterAttribute = parameterInfo?.GetCustomAttribute<InputConverterAttribute>();

            if (inputConverterAttribute != null)
            {
                return new Dictionary<string, object>()
                {
                    { PropertyBagKeys.ConverterType, inputConverterAttribute.ConverterType.AssemblyQualifiedName! }
                }.ToImmutableDictionary();
            }

            return ImmutableDictionary<string, object>.Empty;
        }
    }
}
