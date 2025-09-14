using System.Net;

namespace CaliphAuctionBackend.Exceptions;

public class AccountLockedCaliphException(string? message = null, Exception? innerException = null) : CaliphException(message, innerException, HttpStatusCode.Locked) {
}