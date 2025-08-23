namespace PennyAuctionBackend.Exceptions;

public class ValidationPennyException(string? message = null, Exception? innerException = null) : PennyException(message, innerException) {
}