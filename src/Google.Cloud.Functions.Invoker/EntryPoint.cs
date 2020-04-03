﻿// Copyright 2020, Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Google.Cloud.Functions.Invoker
{
    /// <summary>
    /// The entry point for the invoker. This is used automatically by the entry point generated by MS Build
    /// targets within the Google.Cloud.Functions.Invoker NuGet package.
    /// </summary>
    public static class EntryPoint
    {
        /// <summary>
        /// The environment variable used to detect the function target name, when not otherwise provided.
        /// </summary>
        public const string FunctionTargetEnvironmentVariable = "FUNCTION_TARGET";

        /// <summary>
        /// The environment variable used to detect the port to listen on.
        /// </summary>
        public const string PortEnvironmentVariable = "PORT";

        /// <summary>
        /// Starts a web server to serve the function in the specified assembly. This method is called
        /// automatically be the generated entry point.
        /// </summary>
        /// <param name="functionAssembly">The assembly containing the function to execute.</param>
        /// <param name="args">Arguments to parse </param>
        /// <returns>A task representing the asynchronous operation.
        /// The result of the task is an exit code for the process, which is 0 for success or non-zero
        /// for any failures.
        /// </returns>
        public static async Task<int> StartAsync(Assembly functionAssembly, string[] args)
        {
            // Clear out the ASPNETCORE_URLS environment variable in order to avoid a warning when we start the server.
            // An alternative would be to *use* the environment variable, but as it's populated (with a non-ideal value) by
            // default, I suspect that would be tricky.
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

            // TODO: Catch exceptions and return 1, or just let the exception propagate? It probably
            // doesn't matter much. Potentially catch exceptions during configuration, but let any
            // during web server execution propagate.
            var environment = FunctionEnvironment.Create(functionAssembly, args, ConfigurationVariableProvider.System);
            await environment.CreateHostBuilder().Build().RunAsync();
            return 0;
        }

        /// <summary>
        /// Creates an <see cref="IWebHostBuilder"/> for the function type specified by
        /// <typeparamref name="TFunction"/>. This is a convenience method equivalent to calling
        /// <see cref="CreateHostBuilder(IReadOnlyDictionary{string, string}, Assembly)"/>, passing in a dictionary
        /// that only contains the function target environment variable and the assembly
        /// containing the function type. This method is primarily used for writing integration tests.
        /// See the documentation for a fuller example.
        /// </summary>
        /// <returns>A host builder that can be used for integration testing.</returns>
        /// <typeparam name="TFunction">The function type to host.</typeparam>
        public static IHostBuilder CreateHostBuilder<TFunction>() => CreateHostBuilder(typeof(TFunction));

        /// <summary>
        /// Creates an <see cref="IWebHostBuilder"/> for the function type specified by
        /// <paramref name="functionType"/>. This is a convenience method equivalent to calling
        /// <see cref="CreateHostBuilder(IReadOnlyDictionary{string, string}, Assembly)"/>, passing in a dictionary
        /// that only contains the function target environment variable and the assembly
        /// containing the function type. This method is primarily used for writing integration tests
        /// where the function type isn't known at compile time.
        /// </summary>
        /// <param name="functionType">The function type to host.</param>
        /// <returns>A host builder that can be used for integration testing.</returns>
        public static IHostBuilder CreateHostBuilder(Type functionType)
        {
            Preconditions.CheckNotNull(functionType, nameof(functionType));
            var functionTarget = functionType.FullName ?? throw new ArgumentException("Target function type has no name.");
            var environment = new Dictionary<string, string> { { FunctionTargetEnvironmentVariable, functionTarget } };
            return CreateHostBuilder(environment, functionType.Assembly);
        }

        /// <summary>
        /// Creates an <see cref="IWebHostBuilder"/> as if the environment variables are set as per <paramref name="environment"/>.
        /// The actual system environment variables are ignored. Command line arguments are not supported, as (by design) everything
        /// that can be specified on the command line can also be specified via environment variables.
        /// This method is primarily used for writing integration tests where a high degree of control is required,
        /// for example to simulate the difference between running in a container or not.
        /// </summary>
        /// <param name="environment">The fake environment variables to use when constructing the host builder.</param>
        /// <param name="functionAssembly">The assembly containing the target function. May be null, in which case the calling assembly
        /// is used.</param>
        /// <returns>A host builder that can be used for integration testing.</returns>
        public static IHostBuilder CreateHostBuilder(IReadOnlyDictionary<string, string> environment, Assembly? functionAssembly = null)
        {
            var variables = ConfigurationVariableProvider.FromDictionary(environment);
            var functionEnvironment = FunctionEnvironment.Create(functionAssembly ?? Assembly.GetCallingAssembly(), new string[0], variables);
            return functionEnvironment.CreateHostBuilder();
        }
    }
}
