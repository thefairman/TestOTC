using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Services.ExternalServices.HttpServices.Directus
{
	public class DirectusExceptionExtensions
	{
		public string Code { get; set; }
		public string Collection { get; set; }
		public string Field { get; set; }
		public string Invalid { get; set; }
	}
	public class DirectusException : Exception
	{
		public DirectusExceptionExtensions Extensions { get; set; }
		public int? SuccessfullOffset { get; set; }
		public DirectusException(string message, DirectusExceptionExtensions extensions, int? successfullOffset)
			: base(message)
		{
			Extensions = extensions;
			SuccessfullOffset = successfullOffset;
		}

		public DirectusException(string? message, Exception? innerException, DirectusExceptionExtensions extensions, int? successfullOffset) : base(message, innerException)
		{
			Extensions = extensions;
			SuccessfullOffset = successfullOffset;
		}

		public override string ToString()
		{
			return $"Message: {Message}{Environment.NewLine}Code: {Extensions?.Code}{Environment.NewLine}" +
				$"Collection: {Extensions?.Collection}{Environment.NewLine}" +
				$"Field: {Extensions?.Field}{Environment.NewLine}" +
				$"Invalid: {Extensions?.Invalid}";
		}
	}
}
