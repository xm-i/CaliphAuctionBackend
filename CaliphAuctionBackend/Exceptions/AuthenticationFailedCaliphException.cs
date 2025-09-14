using System.Net;

namespace CaliphAuctionBackend.Exceptions;

public class AuthenticationFailedCaliphException(string? message = null, Exception? innerException = null) : CaliphException(message, innerException, HttpStatusCode.Unauthorized) {
}