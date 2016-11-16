﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
	// Ad-hoc tests for TD.NET to try out behavior.
	public partial class VsixInstallerTests : IDisposable
	{
		static string vsixPathV1 = "TestVsix\\TestVsix.1.0.0.vsix";
        static string vsixPathV2 = "TestVsix\\TestVsix.2.0.0.vsix";
        static string vsixId = "MSBuilder.TestVsix";

        ITestOutputHelper output;
        BuildManager manager;

        public VsixInstallerTests(ITestOutputHelper output)
        {
            this.output = output;
            manager = new BuildManager();
        }

        public void Dispose()
        {
            manager.Dispose();
        }

        public static IEnumerable<object[]> GetInstalledVisualStudio()
        {
            using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var key = root.OpenSubKey(@"Software\Microsoft\VisualStudio\SxS\VS7"))
            {
                return key.GetValueNames().Select(s => new object[] { s }).ToArray();
            }
        }

        [MemberData(nameof(GetInstalledVisualStudio))]
        [Theory]
        public void Install(string visualStudioVersion)
		{
            try
            {
                var task = new InstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixPath = vsixPathV1,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                };

                Assert.True(task.Execute(), $"Failed to install extension {vsixId} from {vsixPathV1}.");
            }
            finally
            {
                var task = new UninstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                };

                Assert.True(task.Execute(), $"Failed to uninstall extension {vsixId}.");
            }
        }

        [MemberData(nameof(GetInstalledVisualStudio))]
        [Theory]
        public void Disable(string visualStudioVersion)
		{
            try
            {
                Assert.True(new InstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixPath = vsixPathV1,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to install extension {vsixId} from {vsixPathV1}.");

                Assert.True(new DisableVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    FailIfNotInstalled = true,
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to disable extension {vsixId} from {vsixPathV1}.");
            }
            finally
            {
                Assert.True(new UninstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to uninstall extension {vsixId}.");
            }
        }

        [MemberData(nameof(GetInstalledVisualStudio))]
        [Theory]
        public void Enable(string visualStudioVersion)
        {
            try
            {
                Assert.True(new InstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixPath = vsixPathV1,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to install extension {vsixId} from {vsixPathV1}.");

                Assert.True(new DisableVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    FailIfNotInstalled = true,
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to disable extension {vsixId} from {vsixPathV1}.");

                Assert.True(new EnableVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    FailIfNotInstalled = true,
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to enable extension {vsixId} from {vsixPathV1}.");
            }
            finally
            {
                Assert.True(new UninstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to uninstall extension {vsixId}.");
            }
        }

        [MemberData(nameof(GetInstalledVisualStudio))]
        [Theory]
        public void ListInstalled(string visualStudioVersion)
        {
            try
            {
                Assert.True(new InstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixPath = vsixPathV1,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to install extension {vsixId} from {vsixPathV1}.");

                var task = new ListVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixIdFilter = vsixId,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                };

                Assert.True(task.Execute(), $"Failed to list extensions matching '{vsixId}'.");

                Assert.Equal(1, task.InstalledVsix.Length);

                foreach (var extension in task.InstalledVsix)
                {
                    output.WriteLine("Extension {0} v{1} ({2} metadata items, InstalledPerMachine={3}).",
                        extension.ItemSpec, extension.GetMetadata("Version"), extension.MetadataCount, extension.GetMetadata("InstalledPerMachine"));
                }
            }
            finally
            {
                Assert.True(new UninstallVsix
                {
                    VisualStudioVersion = visualStudioVersion,
                    VsixId = vsixId,
                    RootSuffix = "Exp",
                    BuildEngine = new MockBuildEngine(output, true)
                }.Execute(), $"Failed to uninstall extension {vsixId}.");
            }
        }
    }
}