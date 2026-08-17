namespace Shared.Dto;

public class DocumentDto
{
    public int Id { get; set; }
    public string FilePath { get; set; } = default!;
    public string UserFileName { get; set; } = default!;
    public long FileSize { get; set; }
}
