﻿using NSubstitute;
using NUnit.Framework;
using IISLogReader.BLL.Data.Models;
using IISLogReader.BLL.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IISLogReader.BLL.Data.Repositories;
using IISLogReader.BLL.Data;
using IISLogReader.BLL.Commands.Project;

namespace Test.IISLogReader.BLL.Repositories
{
    [TestFixture]
    public class LogFileRepositoryTest
    {
        private IDbContext _dbContext;


        [SetUp]
        public void LogFileRepositoryTest_SetUp()
        {
            _dbContext = Substitute.For<IDbContext>();
        }

        [TearDown]
        public void LogFileRepositoryTest_TearDown()
        {
            // delete all .db files (in case previous tests have failed)
            TestHelper.DeleteTestFiles(AppContext.BaseDirectory, "*.dbtest");

        }


        /// <summary>
        /// Tests that the GetByHash method loads a file by the correct Id
        /// </summary>
        [Test]
        public void GetById_Integration_ReturnsData()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName() + ".dbtest");
            string fileHash = Guid.NewGuid().ToString();

            using (SQLiteDbContext dbContext = new SQLiteDbContext(filePath))
            {
                dbContext.Initialise();

                ILogFileRepository logFileRepo = new LogFileRepository(dbContext);

                // create the project
                ProjectModel project = DataHelper.CreateProjectModel();
                ICreateProjectCommand createProjectCommand = new CreateProjectCommand(dbContext, new ProjectValidator());
                project = createProjectCommand.Execute(project);

                // create the log file record
                LogFileModel logFile = DataHelper.CreateLogFileModel();
                logFile.ProjectId = project.Id;
                logFile.FileHash = fileHash;

                ICreateLogFileCommand createLogFileCommand = new CreateLogFileCommand(dbContext, new LogFileValidator());
                logFile = createLogFileCommand.Execute(logFile);

                LogFileModel result = logFileRepo.GetByHash(project.Id, fileHash);
                Assert.IsNotNull(result);
                Assert.AreEqual(logFile.FileName, result.FileName);

                result = logFileRepo.GetByHash(0, fileHash);
                Assert.IsNull(result);
            }

        }


    }
}
