using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using ProjectToDeploy = CodemotionRome19.Core.Models.ProjectToDeploy;

namespace CodemotionRome19.Core.AzureDevOps
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        readonly IConfiguration configuration;
        readonly Uri devOpsUri;
        readonly VssBasicCredential creds;

        public AzureDevOpsService(IConfiguration configuration)
        {
            this.configuration = configuration;
            devOpsUri = new Uri($"https://dev.azure.com/{configuration.DevOpsOrganization}/");
            creds = new VssBasicCredential("username", configuration.DevOpsToken);
        }

        public async Task<Result> TriggerBuild(ProjectToBuild pb)
        {
            try
            {
                using (var connection = new VssConnection(devOpsUri, creds))
                using (var pClient = await connection.GetClientAsync<ProjectHttpClient>())
                using (var bClient = await connection.GetClientAsync<BuildHttpClient>())
                {
                    #region Project

                    var project = (await pClient.GetProjects()).Single(p => p.Id.ToString() == pb.Id);

                    #endregion

                    #region Build

                    var buildDefinition = (await bClient.GetDefinitionsAsync(project.Id)).Single(b => b.Name == pb.PipelineName);

                    var newBuild = new Build
                    {
                        Definition = new DefinitionReference
                        {
                            Id = buildDefinition.Id
                        },
                        Project = buildDefinition.Project,
                    };
                    newBuild.Properties.Add(nameof(pb.RequestedByUser), pb.RequestedByUser);

                    await bClient.QueueBuildAsync(newBuild);

                    #endregion
                }

                return Result.Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Fail(e.Message);
            }
        }

        public async Task<Result> TriggerRelease(ProjectToDeploy pd)
        {
            try
            {
                using (var connection = new VssConnection(devOpsUri, creds))
                //using (var pClient = await connection.GetClientAsync<ProjectHttpClient>())
                using (var bClient = await connection.GetClientAsync<BuildHttpClient>())
                using (var rClient = await connection.GetClientAsync<ReleaseHttpClient2>())        
                {
                    #region Project

                    //var project = (await pClient.GetProjects()).Single(p => p.Name == pd.ProjectName);
                    //GetProject("b59a6f1c-ab6f-4e8b-bdf5-8628bb1bf030");

                    #endregion

                    #region Release

                    var releaseDefinition = (await rClient.GetReleaseDefinitionsAsync(pd.Id, pd.PipelineName, ReleaseDefinitionExpands.Artifacts))
                                            .Single(rd => rd.Name == pd.PipelineName);

                    var primaryArtifact = releaseDefinition.Artifacts.Single(a => a.IsPrimary);
                    var projectId = primaryArtifact.DefinitionReference["project"].Id;
                    var buildDefinitionId = Convert.ToInt32(primaryArtifact.DefinitionReference["definition"].Id);

                    var lastBuild = (await bClient.GetBuildsAsync(projectId, new[] { buildDefinitionId }, statusFilter: BuildStatus.Completed))
                                    .OrderByDescending(b => b.Id)
                                    .First();

                    var metadata = new ReleaseStartMetadata
                    {
                        DefinitionId = releaseDefinition.Id,
                        IsDraft = true,
                        Description = "Created by Aldo",
                        Artifacts = new[]
                        {
                            new ArtifactMetadata
                            {
                                Alias = primaryArtifact.Alias,
                                InstanceReference = new BuildVersion
                                {
                                    Id = lastBuild.Id.ToString(),
                                    Name = lastBuild.BuildNumber
                                }
                            }
                        }
                    };

                    var release = await rClient.CreateReleaseAsync(metadata, pd.Id);
                    release.Properties.Add(nameof(pd.RequestedByUser), pd.RequestedByUser);
                          
                    // Variables substitution
                    if (pd.Variables.Any())
                        foreach (var v in pd.Variables)
                        {
                            if(release.Variables.ContainsKey(v.Key))
                                release.Variables[v.Key].Value = v.Value;
                        }

                    release = await rClient.UpdateReleaseAsync(release, pd.Id, release.Id);

                    // Activate release
                    var updateMetadata = new ReleaseUpdateMetadata { Status = ReleaseStatus.Active, Comment = "Triggered by Aldo" };
                    release = await rClient.UpdateReleaseResourceAsync(updateMetadata, pd.Id, release.Id);

                    // Trigger the deployment on a specific environment
                    release.Environments[0].Status = EnvironmentStatus.InProgress;
                    release = await rClient.UpdateReleaseAsync(release, pd.Id, release.Id);

                    #endregion
                }

                return Result.Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Fail(e.Message);
            }
        }

        public async Task<Result<string>> GetReleaseRequestor(string idProject, int idRelease)
        {
            try
            {
                using (var connection = new VssConnection(devOpsUri, creds))
                using (var rClient = await connection.GetClientAsync<ReleaseHttpClient2>())
                {
                    var release = await rClient.GetReleaseAsync(idProject, idRelease, propertyFilters: new List<string>{"RequestedByUser"});

                    return release.Properties.TryGetValue("RequestedByUser", out var requestedByUser) ?
                        Result.Ok(requestedByUser.ToString()) : 
                        Result.Fail<string>($"RequestedByUser not found. Release {release.Name} for Project {release.ProjectReference.Name}");
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Fail<string>(e.Message);
            }
        }

        public async Task<Result<string>> GetBuildRequestor(string idProject, int idBuild)
        {
            try
            {
                using (var connection = new VssConnection(devOpsUri, creds))
                using (var bClient = await connection.GetClientAsync<BuildHttpClient>())
                {
                    var build = await bClient.GetBuildAsync(idProject, idBuild);
                    var buildProps = await bClient.GetBuildPropertiesAsync(idProject, idBuild);

                    return buildProps.TryGetValue("RequestedByUser", out var requestedByUser) ? 
                        Result.Ok(requestedByUser.ToString()) :
                        Result.Fail<string>($"RequestedByUser not found. Build {build.BuildNumber} for Project {build.Project.Name}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Fail<string>(e.Message);
            }
        }
    }
}
