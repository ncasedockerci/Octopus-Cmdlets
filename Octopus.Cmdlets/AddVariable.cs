﻿using System;
using System.Linq;
using System.Management.Automation;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Platform.Model;

namespace Octopus.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "OctoVariable")]
    public class AddVariable : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            Position = 0,
            HelpMessage = "The project to get the variables for."
            )]
        public string Project { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1
            )]
        public string Name { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 2
            )]
        public string Value { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 3
            )]
        public string[] Environments { get; set; }

        //[Parameter(
        //    Mandatory = false,
        //    Position = 4
        //    )]
        //public string[] Roles { get; set; }

        //[Parameter(
        //    Mandatory = false,
        //    Position = 5
        //    )]
        //public string[] Machines { get; set; }

        //[Parameter(
        //    Mandatory = false,
        //    Position = 6
        //    )]
        //public string[] Steps { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 4
            )]
        public bool Sensitive { get; set; }

        public AddVariable()
        {
            Sensitive = false;
        }

        protected override void ProcessRecord()
        {
            var octopus = (OctopusRepository) SessionState.PSVariable.GetValue("OctopusRepository");
            if (octopus == null)
            {
                throw new Exception(
                    "Connection not established. Please connect to you Octopus Deploy instance with Connect-OctoServer");
            }

            WriteDebug("Got connection");

            // Find the project that owns the variables we want to edit
            var project = octopus.Projects.FindByName(Project);

            if (project == null)
            {
                const string msg = "Project '{0}' was found.";
                throw new Exception(string.Format(msg, Project));
            }

            WriteDebug("Found project" + project.Id);

            // Get the variables for editing
            var variableSet = octopus.VariableSets.Get(project.Link("Variables"));

            WriteDebug("Found variable set" + variableSet.Id);

            var variable = new VariableResource {Name = Name, Value = Value};
            var environments = octopus.Environments.FindByNames(Environments);
            var ids = environments.Select(environment => environment.Id).ToList();

            variable.Scope.Add(ScopeField.Environment, new ScopeValue(ids));
            variableSet.Variables.Add(variable);

            // Save the variables
            octopus.VariableSets.Modify(variableSet);

            WriteDebug("Modified the variable set");
        }
    }
}