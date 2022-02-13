using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSRestAPI
{
    // nuget:Microsoft.TeamFoundationServer.Client
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;

    public class QueryExecutor
    {
        private readonly Uri uri;
        private readonly string personalAccessToken;
        private const int BatchCountZeroBase = 199;
        private const int BatchCountBase = 200;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryExecutor" /> class.
        /// </summary>
        /// <param name="orgName">
        ///     An organization in Azure DevOps Services. If you don't have one, you can create one for free:
        ///     <see href="https://go.microsoft.com/fwlink/?LinkId=307137" />.
        /// </param>
        /// <param name="personalAccessToken">
        ///     A Personal Access Token, find out how to create one:
        ///     <see href="/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops" />.
        /// </param>
        public QueryExecutor(string orgName, string personalAccessToken)
        {
            this.uri = new Uri("https://dev.azure.com/" + orgName);
            this.personalAccessToken = personalAccessToken;
        }

        /// <summary>
        ///     Execute a WIQL (Work Item Query Language) query to return a list of open bugs.
        /// </summary>
        /// <param name="project">The name of your project within your organization.</param>
        /// <returns>A list of <see cref="WorkItem"/> objects representing all the open bugs.</returns>
        public List<WorkItem> QueryOpenBugs(string project)
        {

            List<WorkItem> workItemsList = null;

            var credentials = new VssBasicCredential(string.Empty, this.personalAccessToken);

            // create a wiql object and build our query
            var wiql = new Wiql()
            {
                
                Query = "Select [Id] " +
                        "From WorkItems " +
                        "Where [Work Item Type] = 'User Story' " +
                        "And [System.TeamProject] = '" + project + "' " +
                        //"And [System.State] <> 'Closed' " +
                        "Order By [State] Asc, [Changed Date] Desc",
            };

            // create instance of work item tracking http client
            using (var httpClient = new WorkItemTrackingHttpClient(this.uri, credentials))
            {
                // execute the query to get the list of work items in the results
                var result = httpClient.QueryByWiqlAsync(wiql);

                IList<WorkItemReference> workItems = result.Result.WorkItems.ToList();
                int workItemCount = workItems.Count;
                int batchCount = workItemCount / BatchCountBase;

                Console.WriteLine($"Returned {workItemCount} Work Items.");
                Console.WriteLine($"Work Item Batch Count = {batchCount}.");

                Task<List<WorkItem>>[] tasks = new Task<List<WorkItem>>[batchCount];
                Parallel.For(0, batchCount, batchIdx =>
                {
                    int min = batchIdx * BatchCountBase;
                    int max = min + BatchCountZeroBase;
                    Console.WriteLine($"Min={min}, Max={max}");
                    var ids = from workItem in workItems
                              let io = workItems.IndexOf(workItem)
                              where io > min && io < max
                              select workItem.Id;

                    if (ids.Any())
                    {
                        Task<List<WorkItem>> workItemsList = httpClient.GetWorkItemsAsync(ids);
                        if (workItemsList!=null && workItemsList.Result.Any())
                            tasks[batchIdx] = workItemsList;
                    }
                });

                Task.WaitAll(tasks);

                workItemsList = new List<WorkItem>(tasks.Count() * BatchCountBase);
                foreach(Task<List<WorkItem>> task in tasks)
                {
                    workItemsList.AddRange(task.Result);
                }
                Console.WriteLine($"Work Items Count after Processing {workItemsList.Count()}.");
            }
            return workItemsList;
        }

        

        /// <summary>
        /// This sample creates a new work item query for New Bugs, stores it under 'MyQueries', runs the query, and then sends the results to the console.
        /// </summary>
        public static void SampleREST(string collectionUri, string teamProjectName)
        {
            // Connection object could be created once per application and we use it to get httpclient objects. 
            // Httpclients have been reused between callers and threads.
            // Their lifetime has been managed by connection (we don't have to dispose them).
            // This is more robust then newing up httpclient objects directly.  

            // Be sure to send in the full collection uri, i.e. http://myserver:8080/tfs/defaultcollection
            // We are using default VssCredentials which uses NTLM against an Azure DevOps Server.  See additional provided
            // examples for creating credentials for other types of authentication.
            VssConnection connection = new VssConnection(new Uri(collectionUri), new VssCredentials());

            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
            if (myQueriesFolder != null)
            {
                string queryName = "REST Sample";

                // See if our 'REST Sample' query already exists under 'My Queries' folder.
                QueryHierarchyItem newBugsQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    newBugsQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                }
                if (newBugsQuery == null)
                {
                    // if the 'REST Sample' query does not exist, create it.
                    newBugsQuery = new QueryHierarchyItem()
                    {
                        Name = queryName,
                        Wiql = "SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State],[System.Tags] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] = 'Bug' AND [System.State] = 'New'",
                        IsFolder = false
                    };
                    newBugsQuery = witClient.CreateQueryAsync(newBugsQuery, teamProjectName, myQueriesFolder.Name).Result;
                }

                // run the 'REST Sample' query
                WorkItemQueryResult result = witClient.QueryByIdAsync(newBugsQuery.Id).Result;

                if (result.WorkItems.Any())
                {
                    int skip = 0;
                    const int batchSize = 100;
                    IEnumerable<WorkItemReference> workItemRefs;
                    do
                    {
                        workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);
                        if (workItemRefs.Any())
                        {
                            // get details for each work item in the batch
                            List<WorkItem> workItems = witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id)).Result;
                            foreach (WorkItem workItem in workItems)
                            {
                                // write work item to console
                                Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                            }
                        }
                        skip += batchSize;
                    }
                    while (workItemRefs.Count() == batchSize);
                }
                else
                {
                    Console.WriteLine("No work items were returned from query.");
                }
            }
        }
    }
}
