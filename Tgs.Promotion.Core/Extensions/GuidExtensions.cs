using System.Runtime.InteropServices;
using System.Security;

namespace Tgs.Promotion.Core.Extensions
{
	public static class GuidExtensions
	{
		private static long _counter = DateTime.UtcNow.Ticks;

		public static string ToStringWithoutHyphens(this Guid obj) => obj.ToString("N");

		public static string ToUpperString(this Guid obj) => obj.ToString().ToUpper();

		public static Guid NewSequentialGuid() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? NewSequentialGuidByWindows()
			: NewSequentialGuidByNonWindows();

		[DllImport("rpcrt4.dll", SetLastError = true)]
		[SuppressUnmanagedCodeSecurity]
		private static extern int UuidCreateSequential(out Guid guid);

		private static Guid NewSequentialGuidByWindows()
		{
			const int RPC_S_OK = 0;
			var hr = UuidCreateSequential(out var guid);
			if (hr != RPC_S_OK)
				throw new InvalidOperationException("UuidCreateSequential failed: " + hr);
			var s = guid.ToByteArray();

			// reverse 0~3
			var tmp = s[3];
			s[3] = s[0];
			s[0] = tmp;

			tmp = s[2];
			s[2] = s[1];
			s[1] = tmp;

			// reverse 4~5
			tmp = s[5];
			s[5] = s[4];
			s[4] = tmp;

			// reverse 6~7
			tmp = s[7];
			s[7] = s[6];
			s[6] = tmp;

			return new Guid(s);
		}

		//https://github.com/aspnet/EntityFrameworkCore/blob/master/src/EFCore/ValueGeneration/SequentialGuidValueGenerator.cs
		private static Guid NewSequentialGuidByNonWindows()
		{
			var guidBytes = Guid.NewGuid().ToByteArray();
			var counterBytes = BitConverter.GetBytes(Interlocked.Increment(ref _counter));

			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(counterBytes);
			}

			guidBytes[08] = counterBytes[1];
			guidBytes[09] = counterBytes[0];
			guidBytes[10] = counterBytes[7];
			guidBytes[11] = counterBytes[6];
			guidBytes[12] = counterBytes[5];
			guidBytes[13] = counterBytes[4];
			guidBytes[14] = counterBytes[3];
			guidBytes[15] = counterBytes[2];

			return new Guid(guidBytes);
		}
	}
}
