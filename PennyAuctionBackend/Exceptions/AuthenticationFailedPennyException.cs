using System.Net;

namespace PennyAuctionBackend.Exceptions;

public class AuthenticationFailedPennyException(string? message = null, Exception? innerException = null) : PennyException(message, innerException, HttpStatusCode.Unauthorized) {
}