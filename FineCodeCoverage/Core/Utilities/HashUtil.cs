using System.Security.Cryptography;
using System.Text;

namespace FineCodeCoverage.Core.Utilities
{
	internal class HashUtil
	{
		public static string Hash(string input)
		{
			using (var md5 = MD5.Create())
			{
				var inputBytes = Encoding.ASCII.GetBytes(input);
				var outputBytes = md5.ComputeHash(inputBytes);
				var outputSb = new StringBuilder();

				for (int i = 0; i < outputBytes.Length; i++)
				{
					outputSb.Append(outputBytes[i].ToString("X2"));
				}

				return outputSb.ToString();
			}
		}

	}
}
