﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octopus.Client.Model;
using Octopus.Client.Repositories;

namespace Octopus_Cmdlets.Tests
{
    [TestClass]
    public class GetProjectTests
    {
        private const string CmdletName = "Get-OctoProject";
        private PowerShell _ps;

        [TestInitialize]
        public void Init()
        {
            _ps = Utilities.CreatePowerShell(CmdletName, typeof (GetProject));

            var octoRepo = Utilities.AddOctopusRepo(_ps.Runspace.SessionStateProxy.PSVariable);

            // Create some projects
            var projectRepo = new Mock<IProjectRepository>();

            var projectResources = new List<ProjectResource>
            {
                new ProjectResource {Name = "Octopus", Id = "projects-1", ProjectGroupId = "ProjectGroups-1"},
                new ProjectResource {Name = "Deploy", Id = "projects-2", ProjectGroupId = "ProjectGroups-1"},
                new ProjectResource {Name = "Automation", Id = "projects-3", ProjectGroupId = "ProjectGroups-2"},
                new ProjectResource {Name = "Server", Id = "projects-4", ProjectGroupId = "ProjectGroups-3"},
            };
            projectRepo.Setup(p => p.FindAll(null, null)).Returns(projectResources);

            // Implement filtering by name on the project list
            projectRepo.Setup(p => p.FindByNames(It.IsAny<string[]>(), null, null)).Returns(
                (string[] names) =>
                    (from n in names
                        from p in projectResources
                        where p.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase)
                        select p).ToList());

            octoRepo.Setup(o => o.Projects).Returns(projectRepo.Object);

            // Create a project group for filtering
            var projectGroupRepo = new Mock<IProjectGroupRepository>();
            var projectGroupResources = new List<ProjectGroupResource>
            {
                new ProjectGroupResource {Name = "Octopus", Id = "ProjectGroups-1"},
            };

            projectGroupRepo.Setup(pg => pg.FindByNames(It.IsAny<string[]>(), null, null)).Returns((string[] names) =>
                (from n in names
                    from p in projectGroupResources
                    where p.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase)
                    select p).ToList());
            octoRepo.Setup(o => o.ProjectGroups).Returns(projectGroupRepo.Object);
        }

        [TestMethod]
        public void No_Arguments()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName);
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(4, projects.Count);
        }

        [TestMethod]
        public void With_Name()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddArgument("Octopus");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual("Octopus", projects[0].Name);
        }

        [TestMethod]
        public void With_Invalid_Name()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddArgument("Gibberish");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(0, projects.Count);
        }

        [TestMethod]
        public void With_Group()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("ProjectGroup", "Octopus");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(1, projects.Count(p => p.Name == "Octopus"));
            Assert.AreEqual(1, projects.Count(p => p.Name == "Deploy"));
        }

        [TestMethod]
        public void With_Invalid_Group()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("ProjectGroup", "Gibberish");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(0, projects.Count);
        }

        [TestMethod]
        public void With_Id()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("Id", "projects-1");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual("Octopus", projects[0].Name);
        }

        [TestMethod]
        public void With_Invalid_Id()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("Id", "projects-5000");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(0, projects.Count);
        }

        [TestMethod]
        public void With_Exclude()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("Exclude", new[]{"Deploy", "Server"});
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(1, projects.Count(p => p.Name == "Octopus"));
            Assert.AreEqual(1, projects.Count(p => p.Name == "Automation"));
        }

        [TestMethod]
        public void With_Invalid_Exclude()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("Exclude", "Gibberish");
            var projects = _ps.Invoke<ProjectResource>();

            Assert.AreEqual(4, projects.Count);
        }

        [TestMethod, ExpectedException(typeof(ParameterBindingException))]
        public void With_Id_And_Name()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName).AddParameter("Name", "Name").AddParameter("Id", "Id");
            _ps.Invoke();
        }
    }
}
