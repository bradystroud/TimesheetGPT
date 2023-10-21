using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using TimesheetGPT.Core.Interfaces;
using TimesheetGPT.Core.Models;

namespace TimesheetGPT.Core.Services;

public class GraphService : IGraphService
{
    private readonly GraphServiceClient _client;

    public GraphService(GraphServiceClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client ?? throw new ArgumentNullException(nameof(client));
    }


    public async Task<List<Email>> GetSentEmails(DateTime date)
    {
        var nextDay = date.AddDays(1);
        var dateUtc = date.ToUniversalTime();
        var nextDayUtc = nextDay.ToUniversalTime();

        var messages = await _client.Me.MailFolders["sentitems"].Messages
            .GetAsync(rc =>
            {
                rc.QueryParameters.Top = 999;
                rc.QueryParameters.Select =
                    new[] { "subject", "bodyPreview", "toRecipients", "id" };
                rc.QueryParameters.Filter =
                    $"sentDateTime ge {dateUtc:yyyy-MM-ddTHH:mm:ssZ} and sentDateTime lt {nextDayUtc:yyyy-MM-ddTHH:mm:ssZ}";
                rc.QueryParameters.Orderby = new[] { "sentDateTime asc" };

            });

        if (messages is { Value.Count: > 1 })
        {
            return new List<Email>(messages.Value.Select(m => new Email
            {
                Subject = m.Subject,
                Body = m.BodyPreview,
                To = string.Join(", ", m.ToRecipients.Select(r => r.EmailAddress.Name).ToList()),
                Id = m.Id
            }));
        }

        return new List<Email>(); //slack
    }


    public async Task<List<Meeting>> GetMeetings(DateTime date)
    {
        var nextDay = date.AddDays(1);
        var dateUtc = date.ToUniversalTime();
        var nextDayUtc = nextDay.ToUniversalTime();

        var meetings = await _client.Me.CalendarView.GetAsync(rc =>
        {
            rc.QueryParameters.Top = 999;
            rc.QueryParameters.StartDateTime = dateUtc.ToString("o");
            rc.QueryParameters.EndDateTime = nextDayUtc.ToString("o");
            rc.QueryParameters.Orderby = new[] { "start/dateTime" };
            rc.QueryParameters.Select = new[] { "subject", "start", "end", "occurrenceId" };
        });

        if (meetings is { Value.Count: > 1 })
        {
            return meetings.Value.Select(m => new Meeting
            {
                Name = m.Subject,
                Length = DateTime.Parse(m.End.DateTime) - DateTime.Parse(m.Start.DateTime),
                Repeating = m.Type == EventType.Occurrence,
                // Sender = m.EmailAddress.Address TODO: Why is Organizer and attendees null? permissions?
            }).ToList();
        }

        return new List<Meeting>(); //slack
    }
    public async Task<List<TeamsCall>> GetTeamsCalls(DateTime date)
    {
        var nextDay = date.AddDays(1);
        var dateUtc = date.ToUniversalTime();
        var nextDayUtc = nextDay.ToUniversalTime();

        try
        {
            var calls = await _client.Communications.CallRecords.GetAsync(rc =>
            {
                rc.QueryParameters.Top = 999;
                rc.QueryParameters.Orderby = new[] { "startDateTime" };
                rc.QueryParameters.Select = new[] { "startDateTime", "endDateTime", "participants" };
                rc.QueryParameters.Filter = $"startDateTime ge {dateUtc:o} and endDateTime lt {nextDayUtc:o}";
            });

            if (calls is { Value.Count: > 1 })
            {
                return calls.Value.Select(m => new TeamsCall
                {
                    Attendees = m.Participants.Select(p => p.User.DisplayName).ToList(),
                    Length = m.EndDateTime - m.StartDateTime ?? TimeSpan.Zero,
                }).ToList();
            }
        }
        catch (ODataError e)
        {
            throw new Exception("Need CallRecords.Read.All scopes", e);
        }

        return new List<TeamsCall>();
    }

    public async Task<Email> GetEmailBody(string id)
    {
        var message = await _client.Me.Messages[id]
            .GetAsync(rc =>
            {
                rc.QueryParameters.Select =
                    new[] { "bodyPreview", "toRecipients" };
            });

        if (message != null)
        {
            return new Email
            {
                Body = message.BodyPreview,
                To = string.Join(", ", message.ToRecipients.Select(r => r.EmailAddress.Name).ToList())
            };
        }

        return new Email(); //slack
    }
}

public class TeamsCall
{
    public List<string> Attendees { get; set; }
    public TimeSpan Length { get; set; }
}
