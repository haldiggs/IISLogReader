﻿using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Forms;
using Nancy.ModelBinding;
using Nancy.Security;
using IISLogReader.Navigation;
using IISLogReader.ViewModels.Login;
using Nancy.Responses.Negotiation;
using IISLogReader.ViewModels;
using IISLogReader.BLL.Data.Stores;
using IISLogReader.BLL.Models;
using IISLogReader.BLL.Security;
using IISLogReader.ViewModels.Project;
using AutoMapper;
using IISLogReader.BLL.Validators;
using IISLogReader.BLL.Commands;
using IISLogReader.BLL.Data;
using IISLogReader.BLL.Repositories;
using System.IO;
using Tx.Windows;

namespace IISLogReader.Modules
{
    public class ProjectModule : DefaultSecureModule
    {

        private IDbContext _dbContext;
        private IProjectValidator _projectValidator;
        private ICreateProjectCommand _createProjectCommand;
        private IDeleteProjectCommand _deleteProjectCommand;
        private IProjectRepository _projectRepo;
        private ILogFileRepository _logFileRepo;
        private IRequestRepository _requestRepo;

        public ProjectModule(IDbContext dbContext, IProjectValidator projectValidator
            , ICreateProjectCommand createProjectCommand, IDeleteProjectCommand deleteProjectCommand
            , IProjectRepository projectRepo, ILogFileRepository logFileRepo, IRequestRepository requestRepo)
        {
            _dbContext = dbContext;
            _projectValidator = projectValidator;
            _createProjectCommand = createProjectCommand;
            _deleteProjectCommand = deleteProjectCommand;
            _projectRepo = projectRepo;
            _logFileRepo = logFileRepo;
            _requestRepo = requestRepo;

            Post[Actions.Project.Files()] = x =>
            {
                return Files(x.projectId);
            };

            Post[Actions.Project.AvgLoadTimes()] = x =>
            {
                return AvgLoadTimes(x.projectId);
            };

            Post[Actions.Project.Delete()] = x =>
            {
                return DeleteProject(x.projectId);
            };

            Get[Actions.Project.View()] = x =>
            {
                return ProjectView(x.projectId);
            };

            Post[Actions.Project.Save] = x =>
            {
                this.RequiresClaims(new[] { Claims.ProjectSave });
                return ProjectSave();
            };

        }

        public dynamic AvgLoadTimes(dynamic pId)
        {
            // make sure the id is a valid integer
            int projectId = 0;
            if (!Int32.TryParse((pId ?? "").ToString(), out projectId))
            {
                return HttpStatusCode.BadRequest;
            }

            IEnumerable<RequestPageLoadTimeModel> loadTimes = _requestRepo.GetPageLoadTimes(projectId);
            return this.Response.AsJson(loadTimes);
        }

        public dynamic DeleteProject(dynamic pId)
        {
            // make sure the id is a valid integer
            int projectId = 0;
            if (!Int32.TryParse((pId ?? "").ToString(), out projectId))
            {
                return HttpStatusCode.BadRequest;
            }

            _deleteProjectCommand.Execute(projectId);
            return this.Response.AsJson("");
        }

        public dynamic Files(dynamic pId)
        {
            // make sure the id is a valid integer
            int projectId = 0;
            if (!Int32.TryParse((pId ?? "").ToString(), out projectId))
            {
                return HttpStatusCode.BadRequest;
            }

            IEnumerable<LogFileModel> logFiles = _logFileRepo.GetByProject(projectId);
            return this.Response.AsJson<IEnumerable<LogFileModel>>(logFiles);
        }

        public dynamic ProjectSave()
        {
            ProjectFormViewModel model = this.Bind<ProjectFormViewModel>();
            ProjectModel project = Mapper.Map<ProjectFormViewModel, ProjectModel>(model);

            ValidationResult validationResult = _projectValidator.Validate(project);
            if (validationResult.Success)
            {
                _dbContext.BeginTransaction();
                project = _createProjectCommand.Execute(project);
                _dbContext.Commit();
            }

            ProjectSaveResultModel result = new ProjectSaveResultModel(project.Id, validationResult.Success, validationResult.Messages.ToArray());
            return this.Response.AsJson(result);
        }

        public dynamic ProjectView(dynamic pId)
        {
            // make sure the id is a valid integer
            int projectId = 0;
            if (!Int32.TryParse((pId ?? "").ToString(), out projectId))
            {
                return HttpStatusCode.BadRequest;
            }

            // look up the project - return a 404 if we can't load it
            ProjectModel project = _projectRepo.GetById(projectId);
            if (project == null)
            {
                return HttpStatusCode.NotFound;
            }

            // set up the view model
            ProjectViewViewModel viewModel = new ProjectViewViewModel();
            viewModel.ProjectId = projectId;
            viewModel.ProjectName = project.Name;
            viewModel.UnprocessedCount = _projectRepo.GetUnprocessedLogFileCount(projectId);

            AddScript(Scripts.ProjectView);
            return this.View[Views.Project.View, viewModel];
        }
    }
}
