namespace Tgs.Promotion.Client.Models;

public record MessageResponse
{
	public bool Valid { get; init; }

	public string Message { get; init; } = default!;

	public string[] Errors { get; init; }

	public MessageResponse()
	{
		Valid = false;
		Message = string.Empty;
		Errors = Array.Empty<string>();
	}
}
