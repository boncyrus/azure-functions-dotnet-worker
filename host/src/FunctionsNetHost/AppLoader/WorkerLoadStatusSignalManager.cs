// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost;

/// <summary>
/// Provides a signaling mechanism to wait and get notified about successful load of worker assembly.
/// </summary>
public class WorkerLoadStatusSignalManager
{
    private WorkerLoadStatusSignalManager()
    {
        WaitHandle = new ManualResetEvent(false);
    }

    public static WorkerLoadStatusSignalManager Instance { get; } = new();

    /// <summary>
    /// A wait handle which will be signaled when worker assembly load is successful.
    /// </summary>
    public readonly ManualResetEvent WaitHandle;
}
