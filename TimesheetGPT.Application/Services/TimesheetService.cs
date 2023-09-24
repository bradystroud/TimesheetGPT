using Microsoft.Graph;
using TimesheetGPT.Application.Classes;
using TimesheetGPT.Application.Interfaces;

namespace TimesheetGPT.Application.Services;

public class TimesheetService
{
    private readonly IAIService _aiService;

    public TimesheetService(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<SummaryWithRaw> GenerateSummary(DateTime date, GraphServiceClient client)
    {
        IGraphService graphService = new GraphService(client);
        
        var emailSubjects = await graphService.GetEmailSubjects(date);
        var meetings = await graphService.GetMeetings(date);
        
        var aiService = new SemKerAIService();
        var summary = await aiService.GetSummary(StringifyData(emailSubjects, meetings));
        
        return new SummaryWithRaw
        {
            Emails = emailSubjects,
            Meetings = meetings,
            Summary = summary,
            ModelUsed = "GPT-4" //TODO: get this from somewhere
        };
    }
    
    private string StringifyData(IList<string> emails, IList<Meeting> meetings)
    {
        var result = "";
        foreach (var email in emails)
        {
            result += email + "\n";
        }
        
        foreach (var meeting in meetings)
        {
            result += $"{meeting.Name} - {meeting.Length} \n";
        }
        return result;
    }
}
