namespace Funcy.Console.Ui.Pagination.Models;

public record MatchResult
{
    public bool IsMatch => Source != MatchSource.None;
    public MatchSource Source { get; set; }
    public string? MatchedValue { get; set; }
}