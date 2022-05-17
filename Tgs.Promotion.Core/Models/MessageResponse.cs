namespace Tgs.Promotion.Core.Models;

public record MessageResponse
{
	public MessageResponse()
	{
		Valid = false;
		Message = string.Empty;
		Errors = Array.Empty<string>();
	}

	public bool Valid { get; set; }

	public string Message { get; set; }

	public string[] Errors { get; set; }
}
