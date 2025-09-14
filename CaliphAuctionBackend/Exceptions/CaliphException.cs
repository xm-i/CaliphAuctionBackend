using System.Net;

namespace CaliphAuctionBackend.Exceptions;

public class CaliphException(string? message, Exception? innerException, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : Exception(message, innerException) {
	public HttpStatusCode StatusCode {
		get;
	} = statusCode;
}