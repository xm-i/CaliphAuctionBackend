using System.Net;

namespace PennyAuctionBackend.Exceptions;

public class AccountLockedPennyException(string? message = null, Exception? innerException = null) : PennyException(message, innerException, HttpStatusCode.Locked) {
}