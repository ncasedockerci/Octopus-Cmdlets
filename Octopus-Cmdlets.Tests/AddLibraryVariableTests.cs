﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Xunit;
using Moq;
using Octopus.Client.Model;

namespace Octopus_Cmdlets.Tests
{
    public class AddLibraryVariableTests
    {
        private const string CmdletName = "Add-OctoLibraryVariable";
        private PowerShell _ps;
        private readonly VariableSetResource _variableSet = new VariableSetResource();

        public AddLibraryVariableTests()
        {
            _ps = Utilities.CreatePowerShell(CmdletName, typeof(AddLibraryVariable));
            var octoRepo = Utilities.AddOctopusRepo(_ps.Runspace.SessionStateProxy.PSVariable);

            _variableSet.Variables.Clear();
            
            var lib = new LibraryVariableSetResource { Name = "Octopus" };
            var libs = new List<LibraryVariableSetResource> {lib};
            lib.Links.Add("Variables", "variablesets-1");
            octoRepo.Setup(o => o.LibraryVariableSets.FindOne(It.IsAny<Func<LibraryVariableSetResource, bool>>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(
                    (Func<LibraryVariableSetResource, bool> f, string path, string pathParams) =>
                        (from l in libs where f(l) select l).FirstOrDefault());

            octoRepo.Setup(o => o.Projects.FindByName("Gibberish", It.IsAny<string>(), It.IsAny<object>())).Returns((ProjectResource)null);

            octoRepo.Setup(o => o.VariableSets.Get("variablesets-1")).Returns(_variableSet);

            var process = new DeploymentProcessResource();
            process.Steps.Add(new DeploymentStepResource { Name = "Website", Id = "Step-1" });
            octoRepo.Setup(o => o.DeploymentProcesses.Get("deploymentprocesses-1")).Returns(process);

            var envs = new List<EnvironmentResource>
            {
                new EnvironmentResource {Id = "Environments-1", Name = "DEV"}
            };

            octoRepo.Setup(o => o.Environments.FindByNames(new[] { "DEV" }, It.IsAny<string>(), It.IsAny<object>())).Returns(envs);
            var machines = new List<MachineResource>
            {
                new MachineResource {Id = "Machines-1", Name = "web-01"}
            };
            octoRepo.Setup(o => o.Machines.FindByNames(new[] { "web-01" }, It.IsAny<string>(), It.IsAny<object>())).Returns(machines);
        }

        [Fact]
        public void No_Arguments()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName);
            Assert.Throws<ParameterBindingException>(() => _ps.Invoke());
        }

        [Fact]
        public void With_Name()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName)
                .AddParameter(nameof(AddLibraryVariable.VariableSet), "Octopus")
                .AddParameter(nameof(AddLibraryVariable.Name), "Test");
            _ps.Invoke();

            Assert.Equal(1, _variableSet.Variables.Count);
            Assert.Equal("Test", _variableSet.Variables[0].Name);
        }

        [Fact]
        public void With_Invalid_VariableSet()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName)
                .AddParameter(nameof(AddLibraryVariable.VariableSet), "Gibberish")
                .AddParameter(nameof(AddLibraryVariable.Name), "Test");
            Assert.Throws<CmdletInvocationException>(() => _ps.Invoke());
        }

        [Fact]
        public void With_All()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName)
                .AddParameter(nameof(AddLibraryVariable.VariableSet), "Octopus")
                .AddParameter(nameof(AddLibraryVariable.Name), "Test")
                .AddParameter(nameof(AddLibraryVariable.Value), "Test Value")
                .AddParameter(nameof(AddLibraryVariable.Environments), new[] { "DEV" })
                .AddParameter(nameof(AddLibraryVariable.Roles), new[] { "Web" })
                .AddParameter(nameof(AddLibraryVariable.Machines), new[] { "web-01" })
                .AddParameter(nameof(AddLibraryVariable.Sensitive), false);
            _ps.Invoke();

            Assert.Equal(1, _variableSet.Variables.Count);
            Assert.Equal("Test", _variableSet.Variables[0].Name);
            Assert.Equal("Test Value", _variableSet.Variables[0].Value);
        }

        [Fact]
        public void With_Object()
        {
            // Execute cmdlet
            _ps.AddCommand(CmdletName)
                .AddParameter(nameof(AddLibraryVariable.VariableSet), "Octopus")
                .AddParameter(nameof(AddLibraryVariable.InputObject), new VariableResource { Name = "Test" });
            _ps.Invoke();

            Assert.Equal(1, _variableSet.Variables.Count);
            Assert.Equal("Test", _variableSet.Variables[0].Name);
        }
    }
}
