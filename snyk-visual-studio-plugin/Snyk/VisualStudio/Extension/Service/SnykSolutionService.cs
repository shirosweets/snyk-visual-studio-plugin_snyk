﻿namespace Snyk.VisualStudio.Extension.CLI
{
    using System;
    using System.IO;
    using EnvDTE;
    using Service;
    using Settings;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Incapsulate logic for work with Visual Studio solutions.
    /// </summary>
    public class SnykSolutionService : IVsSolutionLoadManager
    {
        private static SnykSolutionService instance;

        private SnykSolutionSettingsService solutionSettingsService;

        private SnykActivityLogger logger;

        private SnykSolutionService()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        private SnykSolutionService(ISnykServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.logger = serviceProvider.ActivityLogger;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SnykSolutionService"/> singleton instance.
        /// </summary>
        public static SnykSolutionService Instance => instance;

        /// <summary>
        /// Gets a value indicating whether is solution open.
        /// </summary>
        public bool IsSolutionOpen => this.ServiceProvider.DTE.Solution.IsOpen;

        /// <summary>
        /// Gets a value indicating whether VS logger.
        /// </summary>
        public SnykActivityLogger Logger => this.logger;

        /// <summary>
        /// Gets a value indicating whether SolutionSettingsService.
        /// </summary>
        public SnykSolutionSettingsService SolutionSettingsService
        {
            get
            {
                if (this.solutionSettingsService == null)
                {
                    this.solutionSettingsService = new SnykSolutionSettingsService(this);
                }

                return this.solutionSettingsService;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ISnykServiceProvider"/> instance.
        /// </summary>
        public ISnykServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Solution events instance.
        /// </summary>
        public SnykVsSolutionLoadEvents SolutionEvents { get; set; }

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="serviceProvider">Service provider implementation.</param>
        /// <returns>Task.</returns>
        public static async Task InitializeAsync(ISnykServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                instance = new SnykSolutionService(serviceProvider);

                await instance.InitializeSolutionEventsAsync();
            }
        }

        /// <summary>
        /// Get solution projects.
        /// </summary>
        /// <returns>Projects instance.</returns>
        public Projects GetProjects() => this.ServiceProvider.DTE.Solution.Projects;

        /// <summary>
        /// Get solution path.
        /// </summary>
        /// <returns>Path string.</returns>
        public string GetSolutionPath()
        {
            this.logger.LogInformation("Enter GetSolutionPath method");

            var dteSolution = this.ServiceProvider.DTE.Solution;
            var projects = this.GetProjects();

            string solutionPath = string.Empty;
            
            // 1 case: Solution with projects.
            if (!dteSolution.IsDirty && projects.Count > 0)
            {
                this.logger.LogInformation("Get solution path from solution full name in case solution with projects.");

                string fullName = dteSolution.FullName;

                solutionPath = Directory.GetParent(dteSolution.FullName).FullName;
            }

            // 2 case: Flat project without solution.
            // 4 case: Web site (in 2015)
            if (dteSolution.IsDirty && projects.Count > 0)
            {
                this.logger.LogInformation("Solution is 'dirty'. Get solution path from first project full name");

                string projectPath = dteSolution.Projects.Item(1).FullName;

                this.logger.LogInformation($"Project path {projectPath}. Get solution path as project directory.");

                solutionPath = Directory.GetParent(projectPath).FullName;
            }

            // 3 case: Any Folder (in 2019).
            if (!dteSolution.IsDirty && projects.Count == 0)
            {
                this.logger.LogInformation("Solution is not 'dirty' and projects count is 0. Get solution path from dte solution full name.");

                solutionPath = dteSolution.FullName;
            }
            
            this.logger.LogInformation($"Result solution path is {solutionPath}.");
            
            return solutionPath;
        }

        /// <summary>
        /// Handle before open project event.
        /// </summary>
        /// <param name="guidProjectID">Project id.</param>
        /// <param name="guidProjectType">Project type.</param>
        /// <param name="pszFileName">file name.</param>
        /// <param name="pSLMgrSupport">Support.</param>
        /// <returns>Ok constant.</returns>
        public int OnBeforeOpenProject(
            ref Guid guidProjectID,
            ref Guid guidProjectType,
            string pszFileName,
            IVsSolutionLoadManagerSupport pSLMgrSupport) => VSConstants.S_OK;

        /// <summary>
        /// Handle Disconnect event.
        /// </summary>
        /// <returns>Return Ok constant.</returns>
        public int OnDisconnect() => VSConstants.S_OK;

        private async Task InitializeSolutionEventsAsync()
        {
            this.logger.LogInformation("Enter InitializeSolutionEvents method");

            IVsSolution vsSolution = await this.ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            vsSolution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);

            this.SolutionEvents = new SnykVsSolutionLoadEvents();

            uint pdwCookie;
            vsSolution.AdviseSolutionEvents(this.SolutionEvents, out pdwCookie);

            this.logger.LogInformation("Leave InitializeSolutionEvents method");
        }

        private bool IsFilePath(string path) => !File.GetAttributes(path).HasFlag(FileAttributes.Directory);
    }
}