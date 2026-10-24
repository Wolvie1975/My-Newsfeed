namespace myNewspaper.Web.Data;

public static class HomeBoxOfficeSchedule
{
    public static bool ShowResults(DateOnly today) =>
        today.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Monday or DayOfWeek.Tuesday;

    public static DateOnly ResultsWeekend(DateOnly today) =>
        today.AddDays(-(((int)today.DayOfWeek + 2) % 7));
}
