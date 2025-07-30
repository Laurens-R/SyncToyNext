using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLib = LibGit2Sharp;

namespace Stn.Core.SyncPoints.Git
{
    internal class GitClient : IDisposable
    {
        private GitLib.Repository _gitRepository;
        private Stn.Core.SyncPoints.Repository _repository;

        public GitLib.Signature Signature
        {
            get
            {
                return new GitLib.Signature(Environment.UserName, Environment.UserName, DateTimeOffset.Now);
            }
        }


        public GitClient(string localPath, Repository repository) {
            var options = new GitLib.RepositoryOptions();
            _gitRepository = new GitLib.Repository(localPath);
            _repository = repository;
        }
      
        public IEnumerable<GitChange> Changes
        {
            get
            {
                var status = _gitRepository.RetrieveStatus(new GitLib.StatusOptions { });
                var filteredItems = status.Where(s => s.State != GitLib.FileStatus.Ignored);

                List<GitChange> changes = new List<GitChange>();

                foreach (var item in filteredItems)
                {
                    var change = new GitChange
                    {
                        Path = item.FilePath
                    };
                }

                return changes;
            }
        }

        public void Commit(string message, string syncpointID)
        {
            var status = _gitRepository.RetrieveStatus(new GitLib.StatusOptions { });

            var filteredItems = status.Where(s => s.State != GitLib.FileStatus.Ignored);

            foreach (var item in filteredItems)
            {
                _gitRepository.Index.Add(item.FilePath);
            }

            var signature = Signature;
            var gitMessage = $"STN{syncpointID}\n\n{message}";

            _gitRepository.Commit(message, signature, signature, new GitLib.CommitOptions());
        }

        public void Push()
        {
            var branch = _gitRepository.Branches.SingleOrDefault(b => b.CanonicalName == _gitRepository.Head.CanonicalName);

            if (branch == null) return;

            var options = new GitLib.PushOptions();
            options.OnPushTransferProgress += (int current, int total, long bytes) =>
            {
                if (Repository.UpdateProgressHandler != null) Repository.UpdateProgressHandler(current, total, $"Pusing git objects {current}/{total}");
                return true;
            };
            
            _gitRepository.Network.Push(_gitRepository.Head, options);
        }

        public string Pull()
        {
            var branch = _gitRepository.Branches.SingleOrDefault(b => b.CanonicalName == _gitRepository.Head.CanonicalName);
            if (branch == null) return string.Empty;

            // Create a signature for the merge commit
            var signature = new Signature("Your Name", "your.email@example.com", DateTimeOffset.Now);

            // Configure pull options
            var pullOptions = new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = "your-username",
                            Password = "your-password-or-token"
                        }
                },
                MergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default,
                    FailOnConflict = true
                }
            };

            var result = Commands.Pull(_gitRepository, signature, pullOptions);

            switch (result.Status)
            {
                case MergeStatus.Conflicts:
                    return "Merge conflicts detected.";
                case MergeStatus.UpToDate:
                    return "Already up to date.";
                case MergeStatus.FastForward:
                case MergeStatus.NonFastForward:
                    return "Pull successful.";
                default:
                    return $"Pull result: {result.Status}";
            }
        }

        public void Dispose()
        {
            _gitRepository.Dispose();
        }
    }
}
