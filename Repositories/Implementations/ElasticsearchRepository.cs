using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class ElasticsearchRepository : IElasticsearchInterface
    {
        private readonly IElasticClient _client;
        private readonly string _defaultIndex;

        public ElasticsearchRepository(IElasticClient client)
        {
            _client = client;
            _defaultIndex = "talentbridgepro_jobs";
        }

        public async Task<int> CreateIndexAsync()
        {
            var indexExistsResponse = await _client.Indices.ExistsAsync(_defaultIndex);
            if (!indexExistsResponse.Exists)
            {
                var createIndexResponse = await _client.Indices.CreateAsync(_defaultIndex, c => c
                    .Settings(s => s
                        .Analysis(a => a
                            .Normalizers(n => n
                                .Custom("lowercase_normalizer", cn => cn
                                    .Filters("lowercase")
                                )
                            )
                        )
                    )
                    .Map<t_job>(m => m
                        .Properties(p => p
                            .Number(n => n
                                .Name(nn => nn.c_job_id)
                                .Type(NumberType.Integer)
                            )
                            .Number(n => n
                                .Name(nn => nn.c_userid)
                                .Type(NumberType.Integer)
                            )
                            .Number(n => n
                                .Name(nn => nn.c_company_id)
                                .Type(NumberType.Integer)
                            )
                            .Keyword(k => k
                                .Name(n => n.c_company_name)
                            )
                            .Keyword(k => k
                                .Name(n => n.c_job_type)
                                .Normalizer("lowercase_normalizer")
                            )
                            .Text(t => t
                                .Name(n => n.c_skills)
                                .Fields(f => f
                                    .Keyword(k => k
                                        .Name("keyword")
                                        .Normalizer("lowercase_normalizer")
                                    )
                                )
                            )
                            .Text(t => t
                                .Name(n => n.c_role)
                                .Fields(f => f
                                    .Keyword(k => k
                                        .Name("keyword")
                                        .Normalizer("lowercase_normalizer")
                                    )
                                )
                            )
                            .Text(t => t
                                .Name(n => n.c_location)
                                .Fields(f => f
                                    .Keyword(k => k
                                        .Name("keyword")
                                        .Normalizer("lowercase_normalizer")
                                    )
                                )
                            )
                            .Keyword(k => k
                                .Name(n => n.c_experience)
                            )
                            .Text(t => t
                                .Name(n => n.c_description)
                                .Fields(f => f
                                    .Keyword(k => k
                                        .Name("keyword")
                                        .Normalizer("lowercase_normalizer")
                                    )
                                )
                            )
                            .Keyword(k => k
                                .Name(n => n.c_salary)
                            )
                            .Keyword(k => k
                                .Name(n => n.c_jd_file)
                            )
                        )
                    )
                );

                if (!createIndexResponse.IsValid)
                {
                    throw new Exception($"Failed to create index: {createIndexResponse.DebugInformation}");
                }
                Console.WriteLine("Jobs index created successfully.");
                return 1;
            }
            else
            {
                Console.WriteLine("Jobs index already exists.");
                return 0;
            }
        }

        public async Task<IEnumerable<t_job>> GetAllDocumentsAsync()
        {
            var searchResponse = await _client.SearchAsync<t_job>(s => s
                .Index(_defaultIndex)
                .MatchAll()
                .Size(10000)
            );
            return searchResponse.IsValid ? searchResponse.Documents : new List<t_job>();
        }

        public async Task<(List<t_job> Jobs, long Total)> SearchJobsAsync(
     t_jobsearchrequest request,
     List<int>? excludeJobIds = null)
        {
            Console.WriteLine($"Search request: Keyword='{request.Keyword}', " +
                             $"MinSalary={request.MinSalary}, SortBy='{request.SortBy}'");

            try
            {
                var response = await _client.SearchAsync<t_job>(s => s
                    .Index(_defaultIndex)
                    .TrackTotalHits(true)
                    .Query(q => BuildQuery(request, excludeJobIds))
                    .Size(100)
                );

                if (!response.IsValid)
                {
                    Console.WriteLine($"Elasticsearch error: {response.DebugInformation}");
                    throw new Exception(response.DebugInformation);
                }

                Console.WriteLine($"Found {response.Total} jobs");
                return (response.Documents.ToList(), response.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                throw;
            }
        }

        private QueryContainer BuildQuery(t_jobsearchrequest request, List<int>? excludeJobIds = null)
        {
            // Build main query
            var boolQuery = new BoolQuery();
            var mustQueries = new List<QueryContainer>();
            var filterQueries = new List<QueryContainer>();
            var shouldQueries = new List<QueryContainer>();

            // 1. KEYWORD SEARCH (with case-insensitivity, fuzziness, and multiple fields)
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                // Multi-match search with boosting for different fields
                var multiMatchQuery = new MultiMatchQuery
                {
                    Fields = new Field[]
                    {
                        "c_role^3.0",
                        "c_skills^2.0",
                        "c_description",
                        "c_location"
                    },
                    Query = keyword,
                    Type = TextQueryType.BestFields,
                    Fuzziness = Fuzziness.Auto,
                    PrefixLength = 2,
                    FuzzyTranspositions = true,
                    MinimumShouldMatch = "75%",
                    Operator = Operator.Or
                };
                shouldQueries.Add(multiMatchQuery);

                // Match phrase prefix
                var matchPhrasePrefixQuery = new MatchPhrasePrefixQuery
                {
                    Field = "c_role",
                    Query = keyword,
                    Boost = 2.0
                };
                shouldQueries.Add(matchPhrasePrefixQuery);

                // Wildcard query
                var wildcardQuery = new WildcardQuery
                {
                    Field = "c_role",
                    Value = $"*{keyword}*",
                    CaseInsensitive = true
                };
                shouldQueries.Add(wildcardQuery);

                // Fuzzy query
                var fuzzyQuery = new FuzzyQuery
                {
                    Field = "c_role",
                    Value = keyword,
                    Fuzziness = Fuzziness.EditDistance(2),
                    PrefixLength = 2,
                    MaxExpansions = 50
                };
                shouldQueries.Add(fuzzyQuery);

                // Add keyword to must queries
                if (shouldQueries.Any())
                {
                    var keywordBoolQuery = new BoolQuery
                    {
                        Should = shouldQueries.ToArray(),
                        MinimumShouldMatch = 1
                    };
                    mustQueries.Add(keywordBoolQuery);
                }
            }

            // 2. JOB TYPE FILTER (case-insensitive - uses normalizer)
            if (request.JobTypes?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
            {
                var jobTypesLower = request.JobTypes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList();

                var termsQuery = new TermsQuery
                {
                    Field = "c_job_type",
                    Terms = jobTypesLower.Select(x => (object)x)
                };
                filterQueries.Add(termsQuery);
            }

            // 3. EXPERIENCE FILTER
            // EXPERIENCE RANGE FILTER (SINGLE VALUE ONLY)
            if (request.ExperienceLevels?.Count == 2 &&
                int.TryParse(request.ExperienceLevels[0], out int minExp) &&
                int.TryParse(request.ExperienceLevels[1], out int maxExp))
            {
                var experienceScriptQuery = new ScriptQuery
                {
                    Script = new InlineScript(@"
            if (doc['c_experience'].size() == 0) return false;

            try {
                String expStr = doc['c_experience'].value.toString();
                if (expStr == null || expStr.length() == 0) return false;

                int exp = Integer.parseInt(expStr.trim());
                return exp >= params.min && exp <= params.max;

            } catch (Exception e) {
                return false;
            }
        ")
                    {
                        Params = new Dictionary<string, object>
            {
                { "min", minExp },
                { "max", maxExp }
            }
                    }
                };

                filterQueries.Add(experienceScriptQuery);
            }


            // SALARY RANGE FILTER (SAFE SCRIPT - NO SHARD FAILURE)
            if (request.MinSalary.HasValue && request.MaxSalary.HasValue)
            {
                long minSalary = request.MinSalary.Value;
                long maxSalary = request.MaxSalary.Value;

                var salaryScriptQuery = new ScriptQuery
                {
                    Script = new InlineScript(@"
            if (doc['c_salary'].size() == 0) return false;

            try {
                String salStr = doc['c_salary'].value.toString();
                if (salStr == null || salStr.length() == 0) return false;

                long salary = Long.parseLong(salStr);
                return salary >= params.min && salary <= params.max;
            } catch (Exception e) {
                return false;
            }
        ")
                    {
                        Params = new Dictionary<string, object>
            {
                { "min", minSalary },
                { "max", maxSalary }
            }
                    }
                };

                filterQueries.Add(salaryScriptQuery);
            }


            // 5. EXCLUDE SPECIFIC JOB IDs
            if (excludeJobIds?.Any() == true)
            {
                var excludeQuery = new TermsQuery
                {
                    Field = "c_job_id",
                    Terms = excludeJobIds.Select(id => (object)id)
                };

                var excludeBoolQuery = new BoolQuery
                {
                    MustNot = new QueryContainer[] { excludeQuery }
                };
                filterQueries.Add(excludeBoolQuery);
            }

            // Build final bool query
            if (mustQueries.Any())
            {
                boolQuery.Must = mustQueries.ToArray();
            }

            if (filterQueries.Any())
            {
                boolQuery.Filter = filterQueries.ToArray();
            }

            // If no keyword search but we have filters
            if (string.IsNullOrWhiteSpace(request.Keyword) && filterQueries.Any())
            {
                boolQuery.Must = new QueryContainer[] { new MatchAllQuery() };
            }

            // If no queries at all, return match all
            if (!mustQueries.Any() && !filterQueries.Any())
            {
                return new MatchAllQuery();
            }

            return boolQuery;
        }

        public async Task<bool> CreateDocumentAsync(t_job job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (job.c_job_id <= 0)
                throw new ArgumentException("Job ID must be greater than 0");

            try
            {
                var response = await _client.IndexAsync(job, i => i
                    .Index(_defaultIndex)
                    .Id(job.c_job_id) // Use c_job_id as the document ID
                    .Refresh(Refresh.WaitFor) // Wait for refresh to ensure immediate visibility
                );

                if (!response.IsValid)
                {
                    Console.WriteLine($"Failed to create job {job.c_job_id}: {response.DebugInformation}");
                    throw new Exception($"Failed to create document: {response.DebugInformation}");
                    return false;
                }
                Console.WriteLine($"Job {job.c_job_id} created successfully in Elasticsearch");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating job {job.c_job_id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateDocumentAsync(t_job job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (job.c_job_id <= 0)
                throw new ArgumentException("Job ID must be greater than 0");

            try
            {
                var existsResponse = await _client.DocumentExistsAsync<t_job>(job.c_job_id, d => d.Index(_defaultIndex));

                if (!existsResponse.Exists)
                {
                    Console.WriteLine($"Job {job.c_job_id} not found. Creating new document...");
                    await CreateDocumentAsync(job);
                }

                var updateResponse = await _client.UpdateAsync<t_job, t_job>(
                    job.c_job_id,
                    u => u
                        .Index(_defaultIndex)
                        .Doc(job)
                        .DocAsUpsert(false) // Don't create if doesn't exist (we already checked)
                        .Refresh(Refresh.WaitFor)
                );

                if (!updateResponse.IsValid)
                {
                    Console.WriteLine($"Failed to update job {job.c_job_id}: {updateResponse.DebugInformation}");
                    throw new Exception($"Failed to update document: {updateResponse.DebugInformation}");
                    return false;
                }

                Console.WriteLine($"Job {job.c_job_id} updated successfully in Elasticsearch");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating job {job.c_job_id}: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> DeleteDocumentAsync(int jobId)
        {
            if (jobId <= 0)
                throw new ArgumentException("Job ID must be greater than 0");

            try
            {
                var response = await _client.DeleteAsync<t_job>(
                    jobId, 
                    d => d
                        .Index(_defaultIndex)
                        .Refresh(Refresh.WaitFor)
                );

                if (!response.IsValid)
                {
                    // If document doesn't exist, that's okay - return true
                    if (response.Result == Result.NotFound)
                    {
                        Console.WriteLine($"Job {jobId} not found in Elasticsearch (already deleted or never existed)");
                        
                    }

                    Console.WriteLine($"Failed to delete job {jobId}: {response.DebugInformation}");
                    throw new Exception($"Failed to delete document: {response.DebugInformation}");
                    return false;
                   
                }

                Console.WriteLine($"Job {jobId} deleted successfully from Elasticsearch");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting job {jobId}: {ex.Message}");
                return false;
            }
        }

    }
}