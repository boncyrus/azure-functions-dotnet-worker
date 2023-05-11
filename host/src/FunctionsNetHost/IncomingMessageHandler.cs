﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost
{

    internal class IncomingMessageHandler
    {
        private readonly Channel<StreamingMessage> _outgoingMessageChannel;
        private bool _specializationDone;
        public IncomingMessageHandler(Channel<StreamingMessage> outgoingMessageChannel)
        {
            this._outgoingMessageChannel = outgoingMessageChannel;
        }

        internal Task ProcessMessageAsync(StreamingMessage message)
        {
            Task.Run(() => Process(message));

            return Task.CompletedTask;
        }

        private async Task Process(StreamingMessage msg)
        {
            Logger.Log($"New message received in client:{msg.ContentCase}");

            if (_specializationDone)
            {
                // Specialization done. So we will simply forward all messages to customer payload.
                Logger.Log($"Specialization done. Forwarding messages to customer payload:{msg.ContentCase}");
                await InboundMessageChannel.Instance.SendAsync(msg);
                return;
            }
            
            StreamingMessage responseMessage = new StreamingMessage();

            switch (msg.ContentCase)
            {
                case StreamingMessage.ContentOneofCase.WorkerInitRequest:
                {
                    var response = BuildWorkerInitResponse();
                    responseMessage.WorkerInitResponse = response;
                    break;
                }
                case StreamingMessage.ContentOneofCase.FunctionsMetadataRequest:
                {
                    var response = new FunctionMetadataResponse {  UseDefaultMetadataIndexing = true , Result = new StatusResult { Status = StatusResult.Types.Status.Success } };
                    responseMessage.FunctionMetadataResponse = response;
                    break;
                }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:
                {
                    var exePath = msg.FunctionEnvironmentReloadRequest.FunctionAppDirectory;
                    Logger.Log($"exePath: {exePath}");

                    // to do: call method to load hostfxr.
                    // wait until we get a signal that it is loaded.
                    _specializationDone = true;
                    
                    // Forward the env reload request to customer payload.
                    await InboundMessageChannel.Instance.SendAsync(msg);
                    break;
                }
            }

            await _outgoingMessageChannel.Writer.WriteAsync(responseMessage);
        }

        private static WorkerInitResponse BuildWorkerInitResponse()
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success }
            };

            return response;
        }
    }
}
