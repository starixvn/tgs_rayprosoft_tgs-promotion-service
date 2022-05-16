namespace Tgs.Promotion.Core.Models
{
	public record MessageResponse
	{
		public MessageResponse()
		{
			Valid = false;
			Message = string.Empty;
			Errors = new string[] { };
		}

		public bool Valid { get; set; }

		public string Message { get; set; }

		public string[] Errors { get; set; }
	}
}
