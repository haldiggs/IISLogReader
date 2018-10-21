﻿using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Nancy.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq;
using IISLogReader.Configuration;
using System.IO;
using IISLogReader.BLL.Data.Stores;
using SystemWrapper.IO;
using IISLogReader.BLL.Security;
using Encryption;
using AutoMapper;
using IISLogReader.BLL.Data.Models;
using IISLogReader.ViewModels.User;
using System.Diagnostics;
using IISLogReader.BLL.Data;
using IISLogReader.ViewModels.Project;
using IISLogReader.BLL.Data.Repositories;

namespace IISLogReader
{

    public class WebConsoleBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            // override maximum JSON length for Nancy
            Nancy.Json.JsonSettings.MaxJsonLength = int.MaxValue;

            // register settings first as we will use that below
            IAppSettings settings = new AppSettings();
            container.Register<IAppSettings>(settings);

            // IO Wrappers
            container.Register<IDirectoryWrap, DirectoryWrap>();
            //container.Register<IPathWrap, PathWrap>();
            container.Register<IFileWrap, FileWrap>();
            //container.Register<IPathHelper, PathHelper>();

            // security
            container.Register<IEncryptionProvider, AESGCM>();
            container.Register<IPasswordProvider, PasswordProvider>();

            // set up mappings
            Mapper.Initialize((cfg) => {
                cfg.CreateMap<ProjectViewModel, ProjectModel>();
                cfg.CreateMap<UserViewModel, UserModel>();//.ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.DocumentId));
            });

            // DbContext - initialise now
            InitialiseDatabase(container);

            // set up the repositories
            var dataPath = Path.Combine(this.RootPathProvider.GetRootPath(), "Data");
            var userStorePath = Path.Combine(dataPath, "users.json");
            
            IUserStore userStore = new UserStore(userStorePath, container.Resolve<IFileWrap>(), container.Resolve<IDirectoryWrap>(), container.Resolve<IPasswordProvider>());
            userStore.Load();
            container.Register<IUserStore>(userStore);
            container.Register<IProjectRepository, ProjectRepository>();

            // add "Shared" folder for views
            this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                return string.Concat("Views/Shared/", viewName);
            });

        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            
            IAppSettings settings = container.Resolve<IAppSettings>();

            // WebConsole classes and controllers
            container.Register<IUserMapper, UserMapper>();

        }

        private void InitialiseDatabase(TinyIoCContainer container)
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "IISLogReader.db");
            container.Register<IDbContext>((c, o) => new SQLiteDbContext(dbPath));
            using (IDbContext dbc = container.Resolve<IDbContext>())
            {
                dbc.Initialise();
            }
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            var formsAuthConfiguration = new FormsAuthenticationConfiguration()
            {
                RedirectUrl = "~/login",
                UserMapper = container.Resolve<IUserMapper>(),
                DisableRedirect = context.Request.IsAjaxRequest()    
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            // set shared ViewBag details here
            context.ViewBag.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            if (Debugger.IsAttached)
            {
                context.ViewBag.AppVersion = DateTime.Now.ToString("yyyyMMddHHmmssttt");
            }
            context.ViewBag.Scripts = new List<string>();
            context.ViewBag.Claims = new List<string>();

            // before the request builds up, if there is a logged in user then set the user info
            pipelines.BeforeRequest += (ctx) =>
            {
                if (ctx.CurrentUser != null)
                {
                    // set the current user name so it is available for the layout
                    ctx.ViewBag.CurrentUserName = ctx.CurrentUser.UserName;
                    if (ctx.CurrentUser.Claims != null)
                    {
                        ctx.ViewBag.Claims = new List<string>(ctx.CurrentUser.Claims);
                    }

                    // load a list of projects
                    using (IDbContext dbContext = container.Resolve<IDbContext>())
                    {
                        IProjectRepository projectRepo = container.Resolve<IProjectRepository>();
                        ctx.ViewBag.Projects = projectRepo.GetAll(dbContext).ToList();
                    }

                }

                return null;
            };

            //// clean up anything that needs to be
            //pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            //    {
            //        IUnitOfWork uow = container.Resolve<IUnitOfWork>();
            //        uow.Dispose();
            //    });
        }

    }
}