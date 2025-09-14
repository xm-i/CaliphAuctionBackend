using System.Net;

namespace CaliphAuctionBackend.Exceptions;

public class ConfigurationCaliphException(string? message = null, Exception? innerException = null) : CaliphException(message, innerException, HttpStatusCode.InternalServerError) {
}