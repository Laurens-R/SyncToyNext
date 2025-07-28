using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLib = LibGit2Sharp;

namespace Stn.Core.SyncPoints.Git
{
    internal class GitClient
    {
        private GitLib.Repository _gitRepository;
        private Stn.Core.SyncPoints.Repository _repository;


        public GitClient(string localPath, Repository repository) {
            var options = new GitLib.RepositoryOptions();
            _gitRepository = new GitLib.Repository(localPath);
            _repository = repository;
        }
        
        public void Commit(string message, string syncpointID)
        {
            var user = new GitLib.Identity(Environment.UserName, Environment.UserName + "@repo.stn");
            var signature = new GitLib.Signature(user, DateTime.Now);
            var options = new GitLib.CommitOptions();
            options.PrettifyMessage = false;

            var gitMessage = $"STN{syncpointID}\n\n{message}";

            _gitRepository.Commit(message, signature, signature, options);
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
            
            _gitRepository.Network.Push(branch, options);
        }

        public void Pull()
        {
            var branch = _gitRepository.Branches.SingleOrDefault(b => b.CanonicalName == _gitRepository.Head.CanonicalName);
            if (branch == null) return;

            
        }
    
    }
}
