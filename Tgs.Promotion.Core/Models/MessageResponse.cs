namespace Tgs.Promotion.Models;

public record MessageResponse
{
	public MessageResponse()
	{
		Valid = false;
		Message = string.Empty;
		Errors = [];
	}

	public bool Valid { get; set; }

	public string Message { get; set; }

	public string[] Errors { get; set; }
}
