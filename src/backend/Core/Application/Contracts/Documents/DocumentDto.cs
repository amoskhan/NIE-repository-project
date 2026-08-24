namespace Application.Contracts;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = default!;
    public string UserFileName { get; set; } = default!;
    public long FileSize { get; set; }
}
