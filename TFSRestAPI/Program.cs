// See https://aka.ms/new-console-template for more information
using TFSRestAPI;

QueryExecutor queryHandler = new QueryExecutor("ACVS", "");

var workItems = queryHandler.QueryOpenBugs("NewU");

Console.WriteLine("Query Results: {0} items found", workItems.Count);

Parallel.ForEach(workItems.OrderBy(wi => wi.Id).ToList(), workItem => {

    String description = String.Empty;
    if(workItem.Fields.ContainsKey("System.Description"))
        description = (String)workItem.Fields["System.Description"];

    String state = String.Empty;
    if(workItem.Fields.ContainsKey("System.State"))
        state = (String)workItem.Fields["System.State"];

    String title = String.Empty;
    if (workItem.Fields.ContainsKey("System.Title"))
        title = (String)workItem.Fields["System.Title"];

    String tags = String.Empty;
    if (workItem.Fields.ContainsKey("System.Tags"))
        tags = (String)workItem.Fields["System.Tags"];

    int? id = workItem.Id;

    if (description.ToLower().Contains("fargo") ||
        title.ToLower().Contains("fargo") ||
        tags.ToLower().Contains("fargo"))
    {
        Console.WriteLine(
        "{0}\t{1}\t{2}\t{3}",
        id, state, title, tags);
    }
});


