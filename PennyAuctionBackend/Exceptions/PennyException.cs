using System.Net;

namespace PennyAuctionBackend.Exceptions;

public class PennyException(string? message, Exception? innerException, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : Exception(message, innerException) {
	public HttpStatusCode StatusCode {
		get;
	} = statusCode;
}