using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IElasticsearchInterface
    {
        Task<int> CreateIndexAsync();
        Task<IEnumerable<t_job>> GetAllDocumentsAsync();
        Task<(List<t_job> Jobs, long Total)> SearchJobsAsync(t_jobsearchrequest request, List<int>? excludeJobIds = null);
        Task<bool> CreateDocumentAsync(t_job job);
        Task<bool> UpdateDocumentAsync(t_job job);
        Task<bool> DeleteDocumentAsync(int jobId);
    }
}