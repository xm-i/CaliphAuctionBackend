using System.Net;

namespace PennyAuctionBackend.Exceptions;

public class ConfigurationPennyException(string? message = null, Exception? innerException = null) : PennyException(message, innerException, HttpStatusCode.InternalServerError) {
}