using System.Net;

namespace PennyAuctionBackend.Exceptions;

public class IpBlockedPennyException(string? message = null, Exception? innerException = null) : PennyException(message, innerException, HttpStatusCode.Locked) {
}