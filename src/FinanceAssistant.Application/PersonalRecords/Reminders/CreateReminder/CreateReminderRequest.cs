namespace FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;

public sealed record CreateReminderRequest(string Content, DateOnly DueDate);
