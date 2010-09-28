using System;
using System.Collections.Generic;
using System.Linq;
using OpenFileSystem.IO;
using OpenFileSystem.IO.FileSystem.Local;
using OpenWrap.Configuration;
using OpenWrap.Dependencies;
using OpenWrap.Repositories;
using OpenWrap.Repositories.Http;
using OpenWrap.Services;

namespace OpenWrap
{
    public class CurrentDirectoryEnvironment : IEnvironment
    {
        public CurrentDirectoryEnvironment()
        {
            CurrentDirectory = LocalFileSystem.Instance.GetDirectory(Environment.CurrentDirectory);
        }

        public CurrentDirectoryEnvironment(string currentDirectory)
        {
            CurrentDirectory = LocalFileSystem.Instance.GetDirectory(currentDirectory);
        }

        public IDirectory ConfigurationDirectory { get; private set; }
        public IDirectory CurrentDirectory { get; set; }

        public IPackageRepository CurrentDirectoryRepository { get; set; }

        public WrapDescriptor Descriptor { get; set; }
        public ExecutionEnvironment ExecutionEnvironment { get; private set; }
        public IFileSystem FileSystem { get; set; }
        public IPackageRepository ProjectRepository { get; set; }
        public IEnumerable<IPackageRepository> RemoteRepositories { get; set; }
        public IPackageRepository SystemRepository { get; set; }

        public void Initialize()
        {
            FileSystem = LocalFileSystem.Instance;
            Descriptor = CurrentDirectory
                    .AncestorsAndSelf()
                    .SelectMany(x => x.Files("*.wrapdesc"))
                    .Select(x => new WrapDescriptorParser().ParseFile(x))
                    .FirstOrDefault();

            var projectRepositoryDirectory = Descriptor.File.Parent.FindProjectRepositoryDirectory();


            if (projectRepositoryDirectory != null)
                ProjectRepository = new FolderRepository(projectRepositoryDirectory, true)
                {
                    Name = "Project repository"
                };

            CurrentDirectoryRepository = new CurrentDirectoryRepository();

            SystemRepository = new FolderRepository(FileSystem.GetDirectory(InstallationPaths.UserRepositoryPath), false)
            {
                Name = "System repository"
            };

            ConfigurationDirectory = FileSystem.GetDirectory(InstallationPaths.ConfigurationDirectory);

            RemoteRepositories = WrapServices.GetService<IConfigurationManager>().LoadRemoteRepositories()
                    .Select(x => CreateRemoteRepository(x.Key, x.Value.Href))
                    .Where(x => x != null)
                    .Cast<IPackageRepository>()
                    .ToList();

            ExecutionEnvironment = new ExecutionEnvironment
            {
                Platform = IntPtr.Size == 4 ? "x86" : "x64",
                Profile = "net35"
            };
        }

        HttpRepository CreateRemoteRepository(string repositoryName, Uri repositoryHref)
        {
            if (repositoryHref.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                return new HttpRepository(FileSystem, repositoryName, new HttpRepositoryNavigator(repositoryHref));
            if (repositoryHref.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                return new IndexedFolderRepository(repositoryName, FileSystem.GetDirectory(repositoryHref.LocalPath));
            return null;
        }
    }
}