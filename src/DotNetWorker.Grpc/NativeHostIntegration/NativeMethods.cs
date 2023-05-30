// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal unsafe class NativeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int GetApplicationPropertiesDelegate(NativeHost hostData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int RegisterCallbacksDelegate(NativeSafeHandle nativeApplicationHandle, delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr workerHandler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SendStreamingMessageDelegate(NativeSafeHandle nativeApplicationHandle, byte[] streamingMessage, int streamingMessageSize);

        readonly GetApplicationPropertiesDelegate _getApplicationPropertiesMethod;
        readonly RegisterCallbacksDelegate _registerCallbacksMethod;
        readonly SendStreamingMessageDelegate _sendStreamingMessageMethod;

        public NativeMethods()
        {
#if NET7_0
            IntPtr mainExecutableHandle = NativeLibrary.GetMainProgramHandle();

            var getAppProperties = NativeLibrary.GetExport(mainExecutableHandle, "get_application_properties");
            _getApplicationPropertiesMethod = Marshal.GetDelegateForFunctionPointer<GetApplicationPropertiesDelegate>(getAppProperties);

            var registerCallbacksPtr = NativeLibrary.GetExport(mainExecutableHandle, "register_callbacks");
            _registerCallbacksMethod = Marshal.GetDelegateForFunctionPointer<RegisterCallbacksDelegate>(registerCallbacksPtr);

            var sendStreamingMessagePtr = NativeLibrary.GetExport(mainExecutableHandle, "send_streaming_message");
            _sendStreamingMessageMethod = Marshal.GetDelegateForFunctionPointer<SendStreamingMessageDelegate>(sendStreamingMessagePtr);

#else
            throw new PlatformNotSupportedException("Interop communication with native layer is not supported in current platform. Consider upgrading your project to net7.0 or later.");
#endif
        }

        public NativeHost GetNativeHostData()
        {
            var hostData = new NativeHost
            {
                pNativeApplication = IntPtr.Zero
            };

            var result = _getApplicationPropertiesMethod(hostData);
            if (result == 1)
            {
                return hostData;
            }

            throw new InvalidOperationException(
                $"Invoking get_application_properties native method failed. Expected result:1, Actual result:{result}");
        }

        public void RegisterCallbacks(NativeSafeHandle nativeApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = _registerCallbacksMethod(nativeApplication, requestCallback, grpcHandler);
        }

        public void SendStreamingMessage(NativeSafeHandle nativeApplication, StreamingMessage streamingMessage)
        {
            byte[] bytes = streamingMessage.ToByteArray();
            _sendStreamingMessageMethod(nativeApplication, bytes, bytes.Length);
        }
    }
}
