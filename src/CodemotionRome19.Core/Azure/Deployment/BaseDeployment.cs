﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public abstract class BaseDeployment
    {
        readonly Stopwatch watch;

        protected BaseDeployment(IAuthenticated azure, DeploymentOptions options)
        {
            Azure = azure;
            Options = options;

            watch = new Stopwatch();
        }

        public IAuthenticated Azure { get; }

        public DeploymentOptions Options { get; }

        public async Task<Result> CreateAsync()
        {
            try
            {
                watch.Restart();

                await ExecuteCreateAsync();

                var totalSeconds = watch.Elapsed.TotalSeconds;
                Debug.WriteLine($"'{GetDeploymentName()}' created in {totalSeconds} seconds");

                watch.Stop();

                return Result.Ok();
            }
            catch (Exception e)
            {
                watch.Stop();
                var error = $"Error creating resource '{GetDeploymentName()}':{Environment.NewLine}{e.Message}";
                return Result.Fail(error);
            }
        }

        protected abstract string GetDeploymentName();

        protected abstract Task ExecuteCreateAsync();
    }
}
