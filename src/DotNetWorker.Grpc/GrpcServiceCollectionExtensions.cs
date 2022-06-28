﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Azure.Functions.Core;

#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class GrpcServiceCollectionExtensions
    {
        internal static IServiceCollection RegisterOutputChannel(this IServiceCollection services)
        {
            return services.AddSingleton<GrpcHostChannel>(s =>
            {
                UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true
                };

                return new GrpcHostChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>(outputOptions));
            });
        }

        public static IServiceCollection AddGrpc(this IServiceCollection services)
        {
            // Channels
            services.RegisterOutputChannel();

            // Internal logging            
            services.AddLogging(logging =>
            {
                logging.Services.AddSingleton<ILoggerProvider, GrpcFunctionsHostLoggerProvider>();
                logging.Services.AddSingleton<IWorkerDiagnostics, GrpcWorkerDiagnostics>();
            });

            // FunctionMetadataProvider for worker driven function-indexing
            //services.TryAddSingleton<IFunctionMetadataProvider, DefaultFunctionMetadataProvider>();
            services.TryAddSingleton<IFunctionMetadataProvider, SampleFunctionMetadataProvider>();

            // gRPC Core services
            services.AddSingleton<IWorker, GrpcWorker>();
            services.AddSingleton<FunctionRpcClient>(p =>
            {
                IOptions<GrpcWorkerStartupOptions> argumentsOptions = p.GetService<IOptions<GrpcWorkerStartupOptions>>()
                    ?? throw new InvalidOperationException("gRPC Services are not correctly registered.");

                GrpcWorkerStartupOptions arguments = argumentsOptions.Value;

                string uriString = $"http://{arguments.Host}:{arguments.Port}";
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
                }


#if NET5_0_OR_GREATER
                GrpcChannel grpcChannel = GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions()
                {
                    MaxReceiveMessageSize = arguments.GrpcMaxMessageLength,
                    MaxSendMessageSize = arguments.GrpcMaxMessageLength,
                    Credentials = ChannelCredentials.Insecure
                });
#else

                var options = new ChannelOption[]
                {
                    new ChannelOption(Grpc.Core.ChannelOptions.MaxReceiveMessageLength, arguments.GrpcMaxMessageLength),
                    new ChannelOption(Grpc.Core.ChannelOptions.MaxSendMessageLength, arguments.GrpcMaxMessageLength)
                };

                Grpc.Core.Channel grpcChannel = new Grpc.Core.Channel(arguments.Host, arguments.Port, ChannelCredentials.Insecure, options);

#endif           
                return new FunctionRpcClient(grpcChannel);
            });

            services.AddOptions<GrpcWorkerStartupOptions>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            return services;
        }
    }
}
