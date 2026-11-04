
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class Ticket
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset ReportedTime { get; init; }
    public string Description { get; init; } = string.Empty;
}

public class CorruptedTicketException : Exception
{
    public CorruptedTicketException(string message) : base(message)
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        DateTimeOffset batchTime = DateTimeOffset.UtcNow;

        // sample records
        string[] rawTickets =
        {
            $" HD-101 | {batchTime.AddHours(-5):O} | VPN keeps disconnecting ",
            $" HD-102 | {batchTime.AddHours(-2):O} |  Outlook is not syncing ",
            $" HD-103 | {batchTime.AddHours(-4.5):O} | Payroll portal unavailable ",
            $" hd-101 | {batchTime.AddHours(-1):O} | Duplicate VPN ticket ",
            $" | {batchTime.AddHours(-1):O} | Missing ticket ID ",
            "HD-104 | invalid-date | Printer is not responding",
            "This row is completely unreadable"
        };

        HashSet<string> processedIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, Ticket> validTickets =
            new Dictionary<string, Ticket>(StringComparer.OrdinalIgnoreCase);

        StringBuilder report = new StringBuilder();

        int duplicateCount = 0;
        int rejectedCount = 0;
        int corruptedCount = 0;

        Console.WriteLine("Starting HelpDesk SLA Ingestor...\n");

        foreach (string rawData in rawTickets)
        {
            try
            {
                if (!TryProcessTicket(rawData, out Ticket ticket))
                {
                    rejectedCount++;
                    report.AppendLine($"REJECTED | {rawData.Trim()}");
                    continue;
                }

                if (!processedIds.Add(ticket.Id))
                {
                    duplicateCount++;
                    report.AppendLine($"DUPLICATE | {ticket.Id}");
                    continue;
                }

                validTickets.Add(ticket.Id, ticket);
                report.AppendLine($"ACCEPTED | {ticket.Id}");
            }
            catch (CorruptedTicketException ex)
            {
                corruptedCount++;
                report.AppendLine($"CORRUPTED | {ex.Message}");
            }
        }

        DateTimeOffset reportTime = DateTimeOffset.UtcNow;
        TimeSpan slaLimit = TimeSpan.FromHours(4);

        int breachedCount = 0;

        report.AppendLine();
        report.AppendLine("========== TICKET SLA STATUS =========");

        foreach (Ticket ticket in validTickets.Values)
        {
            TimeSpan pendingTime = reportTime - ticket.ReportedTime;

            bool slaBreached = pendingTime > slaLimit;

            if (slaBreached)
            {
                breachedCount++;
            }

            int totalHours = (int)pendingTime.TotalHours;
            int minutes = pendingTime.Minutes;

            string status = slaBreached ? "BREACHED" : "WITHIN SLA";

            report.AppendLine(
                $"{ticket.Id} | {ticket.Description} | " +
                $"Open: {totalHours}h {minutes}m | SLA: {status}"
            );
        }

        report.AppendLine();
        report.AppendLine("========== INGESTION SUMMARY ============");
        report.AppendLine($"Valid tickets     : {validTickets.Count}");
        report.AppendLine($"Duplicates skipped: {duplicateCount}");
        report.AppendLine($"Rejected tickets  : {rejectedCount}");
        report.AppendLine($"Corrupted records : {corruptedCount}");
        report.AppendLine($"SLA breaches      : {breachedCount}");
        report.AppendLine("=====================================");

        Console.WriteLine(report.ToString());
    }

    static bool TryProcessTicket(string rawData, out Ticket cleanTicket)
    {
        cleanTicket = null!;

        if (string.IsNullOrWhiteSpace(rawData))
        {
            throw new CorruptedTicketException("Empty ticket record received.");
        }

        string[] fields = rawData.Split('|');

        if (fields.Length != 3)
        {
            throw new CorruptedTicketException(
                "Ticket record has an unreadable format."
            );
        }

        string id = fields[0].Trim();
        string reportedTimeText = fields[1].Trim();
        string description = fields[2].Trim();

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new CorruptedTicketException(
                "Ticket received without an ID."
            );
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
            reportedTimeText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset reportedTime))
        {
            return false;
        }

        if (reportedTime > DateTimeOffset.UtcNow)
        {
            return false;
        }

        cleanTicket = new Ticket
        {
            Id = id,
            ReportedTime = reportedTime,
            Description = description
        };

        return true;
    }
}
